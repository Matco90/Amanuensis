using Amanuensis.Common.Entities;
using Amanuensis.Common.Enum;
using Amanuensis.Common.Exceptions;
using Amanuensis.Services.Contracts;
using FFmpeg.AutoGen;
using System.Runtime.InteropServices;


namespace Amanuensis.Services
{
    public class AudioExtractionControl : ServiceBase, IAudioExtractionService
    {

        public AudioExtractionControl(Settings settings)
        {
            string nativeLibrariesPath;

            this.settings = settings;

            nativeLibrariesPath = GetNativeLibrariesPath();

            if (!Directory.Exists(nativeLibrariesPath))
            {
                throw new AmanuensisException(AmanuensisErrorCode_Type.DirectoryNotFound, $"Librerie native FFmpeg non trovate: {nativeLibrariesPath}");
            }

            ffmpeg.RootPath = nativeLibrariesPath;

            ValidateFFmpegLibraries();
        }

        #region PUBLIC METHODS

        public unsafe string ExtractAudio(string filePath, AudioOutputFormat outputFormat = AudioOutputFormat.Mp3)
        {
            AVFormatContext* inputFormatContext = null;
            AVCodecContext* audioDecoderContext = null;
            AVPacket* packet = null;
            AVFrame* decodedFrame = null;
            AVCodecContext* audioEncoderContext = null;
            SwrContext* resamplerContext = null;
            AVFrame* convertedFrame = null;
            AVAudioFifo* audioFifo = null;
            AVFrame* encoderFrame = null;
            AVFormatContext* outputFormatContext = null;
            AVStream* outputAudioStream = null;
            AVPacket* encodedPacket = null;
            string audioFilePath = "";
            int result;
            long nextAudioPts = 0;
            int audioStreamIndex;
            bool extractionCompleted = false;

            try
            {
                //recupero il contesto dove vengono inseriti i parametri del file video
                inputFormatContext = OpenInputFormatContext(filePath);

                audioStreamIndex = GetAudioStreamIndex(inputFormatContext);

                //carico il contesto del decoder audio
                audioDecoderContext = CreateAudioDecoderContext(inputFormatContext, audioStreamIndex);

                string fileExtension = outputFormat == AudioOutputFormat.Mp3 ? "mp3" : "wav";

                // Percorso temporaneo compatibile con Windows e Linux.
                audioFilePath = Path.Combine(Path.GetTempPath(), $"speech-to-text-{Guid.NewGuid():N}.{fileExtension}");

                // Prepara il contenitore richiesto.
                result = ffmpeg.avformat_alloc_output_context2(&outputFormatContext, null, fileExtension, audioFilePath);

                if (result < 0 || outputFormatContext == null)
                {
                    throw new AmanuensisException(AmanuensisErrorCode_Type.AudioExtractionFailed, $"Impossibile creare il contenitore {outputFormat}. Codice errore: {result}");
                }

                audioEncoderContext = CreateAudioEncoderContext(outputFormat);

                outputAudioStream = CreateOutputAudioStream(outputFormatContext, audioEncoderContext);

                OpenOutputFileAndWriteHeader(outputFormatContext, audioFilePath);

                resamplerContext = CreateResamplerContext(audioEncoderContext, audioDecoderContext);

                convertedFrame = CreateConvertedFrame(audioEncoderContext);

                audioFifo = CreateAudioFifo(audioEncoderContext);

                encoderFrame = CreateEncoderFrame(audioEncoderContext);

                //recupero il pacchetto audio
                packet = ffmpeg.av_packet_alloc();

                if (packet == null)
                {
                    throw new AmanuensisException(AmanuensisErrorCode_Type.AudioExtractionFailed, "Impossibile allocare il pacchetto audio.");
                }

                // Pacchetto che riceverà i dati prodotti dall'encoder.
                encodedPacket = ffmpeg.av_packet_alloc();

                if (encodedPacket == null)
                {
                    throw new AmanuensisException(AmanuensisErrorCode_Type.AudioExtractionFailed, "Impossibile allocare il pacchetto audio codificato.");
                }

                decodedFrame = ConvertAllAudioPacketsFromFrame(inputFormatContext, packet, audioDecoderContext, audioStreamIndex, audioEncoderContext, resamplerContext, convertedFrame, audioFifo, encoderFrame, encodedPacket, outputFormatContext, outputAudioStream, ref nextAudioPts);

                // Comunica all'encoder che non arriveranno altri frame e recupera gli ultimi pacchetti trattenuti.
                EncodeFrameAndWritePackets(audioEncoderContext, null, encodedPacket, outputFormatContext, outputAudioStream);

                // Finalizza il contenitore audio.
                result = ffmpeg.av_write_trailer(outputFormatContext);

                if (result < 0)
                {
                    throw new AmanuensisException(AmanuensisErrorCode_Type.AudioExtractionFailed, $"Impossibile finalizzare il file {outputFormat}. Codice errore: {result}");
                }

                extractionCompleted = true;

            }
            finally
            {
                if (encodedPacket != null)
                {
                    ffmpeg.av_packet_free(&encodedPacket);
                }

                if (outputFormatContext != null)
                {
                    if (outputFormatContext->pb != null)
                    {
                        ffmpeg.avio_closep(&outputFormatContext->pb);
                    }

                    ffmpeg.avformat_free_context(outputFormatContext);
                    outputFormatContext = null;
                }

                if (encoderFrame != null)
                {
                    ffmpeg.av_frame_free(&encoderFrame);
                }

                //dealloco la coda fifo
                if (audioFifo != null)
                {
                    ffmpeg.av_audio_fifo_free(audioFifo);
                    audioFifo = null;
                }

                //dealloco il frame convertito
                if (convertedFrame != null)
                {
                    ffmpeg.av_frame_free(&convertedFrame);
                }

                //dealloco il contesto del resampler
                if (resamplerContext != null)
                {
                    ffmpeg.swr_free(&resamplerContext);
                }

                //dealloco il contesto dell'encoder
                if (audioEncoderContext != null)
                {
                    ffmpeg.avcodec_free_context(&audioEncoderContext);
                }

                //dealloco i frame audio
                if (decodedFrame != null)
                {
                    ffmpeg.av_frame_free(&decodedFrame);
                }

                //dealloco il pacchetto audio
                if (packet != null)
                {
                    ffmpeg.av_packet_free(&packet);
                }

                //dealloco il contesto del codec
                if (audioDecoderContext != null)
                {
                    ffmpeg.avcodec_free_context(&audioDecoderContext);
                }

                //dealloco il contesto del file multimediale
                if (inputFormatContext != null)
                {
                    ffmpeg.avformat_close_input(&inputFormatContext);
                }


                // Poi elimina esclusivamente il file incompleto.
                if (!extractionCompleted && !string.IsNullOrWhiteSpace(audioFilePath) && File.Exists(audioFilePath))
                {
                    try
                    {
                        File.Delete(audioFilePath);
                    }
                    catch (Exception)
                    {

                    }

                }
            }

            return audioFilePath;
        }

        #endregion

        #region PRIVATE METHODS

        private string GetNativeLibrariesPath()
        {
            string nativeLibrariesPath = "";
            string osDirectory = "";

            //check if the system architecture is x64
            if (RuntimeInformation.ProcessArchitecture != Architecture.X64)
            {
                throw new AmanuensisException(AmanuensisErrorCode_Type.PlatformNotSupported, $"Architettura non supportata: {RuntimeInformation.ProcessArchitecture}");
            }

            //find OS type
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                osDirectory = "win-x64";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                osDirectory = "linux-x64";
            }
            else
            {
                throw new AmanuensisException(AmanuensisErrorCode_Type.PlatformNotSupported, "Sistema operativo non supportato.");
            }

            nativeLibrariesPath = Path.Combine(AppContext.BaseDirectory, "runtimes", osDirectory, "native");

            return nativeLibrariesPath;
        }

        private void ValidateFFmpegLibraries()
        {
            try
            {
                ffmpeg.avutil_version();
                ffmpeg.avcodec_version();
                ffmpeg.avformat_version();
                ffmpeg.swresample_version();
            }
            catch (Exception ex) when (
                ex is DllNotFoundException ||
                ex is EntryPointNotFoundException ||
                ex is BadImageFormatException)
            {
                throw new AmanuensisException(AmanuensisErrorCode_Type.AudioExtractionFailed, "Impossibile caricare le librerie native FFmpeg.", ex);
            }
        }

        private unsafe AVFrame* ConvertAllAudioPacketsFromFrame(AVFormatContext* inputFormatContext, AVPacket* packet, AVCodecContext* audioDecoderContext, int audioStreamIndex, AVCodecContext* audioEncoderContext, SwrContext* resamplerContext, AVFrame* convertedFrame, AVAudioFifo* audioFifo, AVFrame* encoderFrame, AVPacket* encodedPacket, AVFormatContext* outputFormatContext, AVStream* outputAudioStream, ref long nextAudioPts)
        {
            int decodedFrameCount = 0;
            int tryAgainError = ffmpeg.AVERROR(ffmpeg.EAGAIN);
            int result;
            AVFrame* decodedFrame = default(AVFrame*);

            try
            {

                //alloco i frame audio
                decodedFrame = ffmpeg.av_frame_alloc();

                if (decodedFrame == null)
                {
                    throw new AmanuensisException(AmanuensisErrorCode_Type.AudioExtractionFailed, "Impossibile allocare il frame audio.");
                }

                //ciclo tutti i packet dei frame e tengo solo quelli audio
                while ((result = ffmpeg.av_read_frame(inputFormatContext, packet)) >= 0)
                {
                    try
                    {
                        // Ignora video, sottotitoli e altri stream.
                        if (packet->stream_index != audioStreamIndex) continue;

                        // Invia il pacchetto audio compresso al decoder.
                        int sendResult = ffmpeg.avcodec_send_packet(audioDecoderContext, packet);

                        if (sendResult < 0)
                        {
                            throw new AmanuensisException(AmanuensisErrorCode_Type.AudioExtractionFailed, $"Errore nell'invio del pacchetto al decoder. Codice errore: {sendResult}");
                        }

                        // Un pacchetto può produrre zero, uno o più frame.
                        while (true)
                        {
                            //inserice il frame decodificato in decodedFrame
                            int receiveResult = ffmpeg.avcodec_receive_frame(audioDecoderContext, decodedFrame);

                            if (receiveResult == tryAgainError || receiveResult == ffmpeg.AVERROR_EOF)
                            {
                                break;
                            }

                            if (receiveResult < 0)
                            {
                                throw new AmanuensisException(AmanuensisErrorCode_Type.AudioExtractionFailed, $"Errore durante la decodifica audio. Codice errore: {receiveResult}");
                            }

                            try
                            {
                                // Qui abbiamo un frame audio decodificato.
                                ConvertDecodedFrameAndWriteToFifo(decodedFrame, convertedFrame, audioEncoderContext, resamplerContext, audioFifo);
                                EncodeAvailableFramesFromFifo(audioFifo, encoderFrame, audioEncoderContext, encodedPacket, outputFormatContext, outputAudioStream, ref nextAudioPts);
                                decodedFrameCount++;
                            }
                            finally
                            {
                                ffmpeg.av_frame_unref(decodedFrame);
                            }
                        }
                    }
                    finally
                    {
                        ffmpeg.av_packet_unref(packet);
                    }
                }

                if (result != ffmpeg.AVERROR_EOF)
                {
                    throw new AmanuensisException(AmanuensisErrorCode_Type.AudioExtractionFailed, $"Errore durante la lettura del file. Codice errore: {result}");
                }

                // Segnala al decoder che non arriveranno altri pacchetti.
                result = ffmpeg.avcodec_send_packet(audioDecoderContext, null);

                if (result < 0 && result != ffmpeg.AVERROR_EOF)
                {
                    throw new AmanuensisException(AmanuensisErrorCode_Type.AudioExtractionFailed, $"Errore durante il flush del decoder. Codice errore: {result}");
                }

                // Recupera gli eventuali frame rimasti nel buffer del decoder.
                while (true)
                {
                    int receiveResult = ffmpeg.avcodec_receive_frame(audioDecoderContext, decodedFrame);

                    if (receiveResult == ffmpeg.AVERROR_EOF) break;

                    if (receiveResult == tryAgainError)
                    {
                        throw new AmanuensisException(AmanuensisErrorCode_Type.AudioExtractionFailed, "Il decoder richiede altri pacchetti dopo il flush.");
                    }

                    if (receiveResult < 0)
                    {
                        throw new AmanuensisException(AmanuensisErrorCode_Type.AudioExtractionFailed, $"Errore durante lo svuotamento del decoder. Codice errore: {receiveResult}");
                    }

                    try
                    {
                        // Anche questo è un frame audio valido.
                        ConvertDecodedFrameAndWriteToFifo(decodedFrame, convertedFrame, audioEncoderContext, resamplerContext, audioFifo);
                        EncodeAvailableFramesFromFifo(audioFifo, encoderFrame, audioEncoderContext, encodedPacket, outputFormatContext, outputAudioStream, ref nextAudioPts);
                        decodedFrameCount++;
                    }
                    finally
                    {
                        ffmpeg.av_frame_unref(decodedFrame);
                    }
                }

                if (decodedFrameCount == 0)
                {
                    throw new AmanuensisException(AmanuensisErrorCode_Type.AudioExtractionFailed, "Il decoder non ha prodotto alcun frame audio.");
                }

                // Recupera gli ultimi campioni trattenuti dal resampler.
                FlushResamplerToFifo(convertedFrame, audioEncoderContext, resamplerContext, audioFifo);
                EncodeAvailableFramesFromFifo(audioFifo, encoderFrame, audioEncoderContext, encodedPacket, outputFormatContext, outputAudioStream, ref nextAudioPts);

                // Completa con silenzio e codifica gli ultimi campioni rimasti.
                EncodeRemainingSamplesFromFifo(audioFifo, encoderFrame, audioEncoderContext, encodedPacket, outputFormatContext, outputAudioStream, ref nextAudioPts);
            }
            catch (Exception)
            {
                if (decodedFrame != null)
                {
                    ffmpeg.av_frame_free(&decodedFrame);
                }

                throw;
            }

            return decodedFrame;

        }

        private unsafe AVCodecContext* CreateAudioEncoderContext(AudioOutputFormat outputFormat)
        {
            AVCodec* audioEncoder = null;
            AVCodecContext* audioEncoderContext = null;
            int result;

            try
            {
                string encoderName = outputFormat == AudioOutputFormat.Mp3 ? "libmp3lame" : "pcm_s16le";
                audioEncoder = ffmpeg.avcodec_find_encoder_by_name(encoderName);

                if (audioEncoder == null)
                {
                    throw new AmanuensisException(AmanuensisErrorCode_Type.AudioExtractionFailed, $"Encoder {encoderName} non disponibile.");
                }

                audioEncoderContext = ffmpeg.avcodec_alloc_context3(audioEncoder);

                if (audioEncoderContext == null)
                {
                    throw new AmanuensisException(AmanuensisErrorCode_Type.AudioExtractionFailed, $"Impossibile allocare il contesto dell'encoder {outputFormat}.");
                }

                audioEncoderContext->sample_rate = 16000;
                audioEncoderContext->sample_fmt = outputFormat == AudioOutputFormat.Mp3 ? AVSampleFormat.AV_SAMPLE_FMT_FLTP : AVSampleFormat.AV_SAMPLE_FMT_S16;

                if (outputFormat == AudioOutputFormat.Mp3)
                {
                    audioEncoderContext->bit_rate = 24000;
                }

                audioEncoderContext->time_base = new AVRational
                {
                    num = 1,
                    den = audioEncoderContext->sample_rate
                };

                // Un solo canale: uscita mono.
                ffmpeg.av_channel_layout_default(&audioEncoderContext->ch_layout, 1);

                result = ffmpeg.avcodec_open2(audioEncoderContext, audioEncoder, null);

                if (result < 0)
                {
                    throw new AmanuensisException(AmanuensisErrorCode_Type.AudioExtractionFailed, $"Impossibile aprire l'encoder {outputFormat}. Codice errore: {result}");
                }

            }
            catch (Exception)
            {
                if (audioEncoderContext != null)
                {
                    ffmpeg.avcodec_free_context(&audioEncoderContext);
                }

                throw;
            }

            return audioEncoderContext;
        }

        private unsafe SwrContext* CreateResamplerContext(AVCodecContext* audioEncoderContext, AVCodecContext* audioDecoderContext)
        {
            SwrContext* resamplerContext = null;
            int result;

            try
            {
                result = ffmpeg.swr_alloc_set_opts2(&resamplerContext,
                                                    &audioEncoderContext->ch_layout, audioEncoderContext->sample_fmt, audioEncoderContext->sample_rate,
                                                    &audioDecoderContext->ch_layout, audioDecoderContext->sample_fmt, audioDecoderContext->sample_rate,
                                                    0, null);

                if (result < 0 || resamplerContext == null)
                {
                    throw new AmanuensisException(AmanuensisErrorCode_Type.AudioExtractionFailed,
                        $"Impossibile configurare il ricampionatore audio. Codice errore: {result}");
                }

                //inizializza contesto del resampler
                result = ffmpeg.swr_init(resamplerContext);

                if (result < 0)
                {
                    throw new AmanuensisException(AmanuensisErrorCode_Type.AudioExtractionFailed, $"Impossibile inizializzare il ricampionatore audio. Codice errore: {result}");
                }
            }
            catch (Exception)
            {
                if (resamplerContext != null)
                {
                    ffmpeg.swr_free(&resamplerContext);
                }

                throw;
            }

            return resamplerContext;
        }

        private unsafe AVFrame* CreateConvertedFrame(AVCodecContext* audioEncoderContext)
        {
            AVFrame* convertedFrame = null;
            int result;

            try
            {
                convertedFrame = ffmpeg.av_frame_alloc();

                if (convertedFrame == null)
                {
                    throw new AmanuensisException(AmanuensisErrorCode_Type.AudioExtractionFailed, "Impossibile allocare il frame audio convertito.");
                }

                convertedFrame->format = (int)audioEncoderContext->sample_fmt;

                convertedFrame->sample_rate = audioEncoderContext->sample_rate;

                result = ffmpeg.av_channel_layout_copy(&convertedFrame->ch_layout, &audioEncoderContext->ch_layout);

                if (result < 0)
                {
                    throw new AmanuensisException(AmanuensisErrorCode_Type.AudioExtractionFailed, $"Impossibile configurare i canali del frame convertito. Codice errore: {result}");
                }
            }
            catch (Exception)
            {
                if (convertedFrame != null)
                {
                    ffmpeg.av_frame_free(&convertedFrame);
                }

                throw;
            }

            return convertedFrame;
        }

        private unsafe AVAudioFifo* CreateAudioFifo(AVCodecContext* audioEncoderContext)
        {
            AVAudioFifo* audioFifo;
            int initialCapacity;

            initialCapacity = audioEncoderContext->frame_size;

            if (initialCapacity <= 0)
            {
                initialCapacity = 1024;
            }

            audioFifo = ffmpeg.av_audio_fifo_alloc(audioEncoderContext->sample_fmt, audioEncoderContext->ch_layout.nb_channels, initialCapacity);

            if (audioFifo == null)
            {
                throw new AmanuensisException(AmanuensisErrorCode_Type.AudioExtractionFailed, "Impossibile allocare la coda FIFO audio.");
            }

            return audioFifo;
        }

        private unsafe void ConvertDecodedFrameAndWriteToFifo(AVFrame* decodedFrame, AVFrame* convertedFrame, AVCodecContext* audioEncoderContext, SwrContext* resamplerContext, AVAudioFifo* audioFifo)
        {
            int result;
            int outputSampleCapacity;
            int convertedSampleCount;
            int requiredFifoSize;
            int writtenSampleCount;

            // Rimuove i dati della conversione precedente.
            ffmpeg.av_frame_unref(convertedFrame);

            // Riconfigura il frame nel formato richiesto dall'encoder.
            convertedFrame->format = (int)audioEncoderContext->sample_fmt;
            convertedFrame->sample_rate = audioEncoderContext->sample_rate;

            result = ffmpeg.av_channel_layout_copy(&convertedFrame->ch_layout, &audioEncoderContext->ch_layout);

            if (result < 0)
            {
                throw new AmanuensisException(AmanuensisErrorCode_Type.AudioExtractionFailed, $"Impossibile configurare i canali del frame convertito. Codice errore: {result}");
            }

            // Calcola lo spazio massimo necessario per il risultato.
            outputSampleCapacity = ffmpeg.swr_get_out_samples(resamplerContext, decodedFrame->nb_samples);

            if (outputSampleCapacity < 0)
            {
                throw new AmanuensisException(AmanuensisErrorCode_Type.AudioExtractionFailed, $"Impossibile calcolare i campioni convertiti. Codice errore: {outputSampleCapacity}");
            }

            if (outputSampleCapacity == 0) return;

            convertedFrame->nb_samples = outputSampleCapacity;

            // Alloca il buffer che conterrà i campioni convertiti.
            result = ffmpeg.av_frame_get_buffer(convertedFrame, 0);

            if (result < 0)
            {
                throw new AmanuensisException(AmanuensisErrorCode_Type.AudioExtractionFailed, $"Impossibile allocare il buffer del frame convertito. Codice errore: {result}");
            }

            // Converte formato, frequenza e numero di canali.
            convertedSampleCount = ffmpeg.swr_convert(resamplerContext, convertedFrame->extended_data, outputSampleCapacity, decodedFrame->extended_data, decodedFrame->nb_samples);

            if (convertedSampleCount < 0)
            {
                throw new AmanuensisException(AmanuensisErrorCode_Type.AudioExtractionFailed, $"Errore durante la conversione audio. Codice errore: {convertedSampleCount}");
            }

            if (convertedSampleCount == 0) return;

            convertedFrame->nb_samples = convertedSampleCount;

            // Ingrandisce la FIFO per contenere anche i nuovi campioni.
            requiredFifoSize = checked(ffmpeg.av_audio_fifo_size(audioFifo) + convertedSampleCount);

            result = ffmpeg.av_audio_fifo_realloc(audioFifo, requiredFifoSize);

            if (result < 0)
            {
                throw new AmanuensisException(AmanuensisErrorCode_Type.AudioExtractionFailed, $"Impossibile espandere la FIFO audio. Codice errore: {result}");
            }

            // Copia i campioni convertiti nella FIFO.
            writtenSampleCount = ffmpeg.av_audio_fifo_write(audioFifo, (void**)convertedFrame->extended_data, convertedSampleCount);

            if (writtenSampleCount != convertedSampleCount)
            {
                throw new AmanuensisException(AmanuensisErrorCode_Type.AudioExtractionFailed, $"Scrittura incompleta nella FIFO: {writtenSampleCount} campioni scritti su {convertedSampleCount}.");
            }
        }

        private unsafe void FlushResamplerToFifo(AVFrame* convertedFrame, AVCodecContext* audioEncoderContext, SwrContext* resamplerContext, AVAudioFifo* audioFifo)
        {
            int outputSampleCapacity;
            int result;
            int convertedSampleCount;
            int requiredFifoSize;
            int writtenSampleCount;

            while (true)
            {
                outputSampleCapacity = ffmpeg.swr_get_out_samples(resamplerContext, 0);

                if (outputSampleCapacity < 0)
                {
                    throw new AmanuensisException(AmanuensisErrorCode_Type.AudioExtractionFailed, $"Impossibile calcolare i campioni rimasti nel resampler. Codice errore: {outputSampleCapacity}");
                }

                if (outputSampleCapacity == 0) break;

                ffmpeg.av_frame_unref(convertedFrame);

                convertedFrame->format = (int)audioEncoderContext->sample_fmt;
                convertedFrame->sample_rate = audioEncoderContext->sample_rate;

                result = ffmpeg.av_channel_layout_copy(&convertedFrame->ch_layout, &audioEncoderContext->ch_layout);

                if (result < 0)
                {
                    throw new AmanuensisException(AmanuensisErrorCode_Type.AudioExtractionFailed, $"Impossibile configurare i canali del frame di flush. Codice errore: {result}");
                }

                convertedFrame->nb_samples = outputSampleCapacity;

                result = ffmpeg.av_frame_get_buffer(convertedFrame, 0);

                if (result < 0)
                {
                    throw new AmanuensisException(AmanuensisErrorCode_Type.AudioExtractionFailed, $"Impossibile allocare il buffer per il flush del resampler. Codice errore: {result}");
                }

                // Input nullo: chiede al resampler di restituire i campioni trattenuti.
                convertedSampleCount = ffmpeg.swr_convert(resamplerContext, convertedFrame->extended_data, outputSampleCapacity, null, 0);

                if (convertedSampleCount < 0)
                {
                    throw new AmanuensisException(AmanuensisErrorCode_Type.AudioExtractionFailed, $"Errore durante il flush del resampler. Codice errore: {convertedSampleCount}");
                }

                if (convertedSampleCount == 0) break;

                convertedFrame->nb_samples = convertedSampleCount;

                requiredFifoSize = checked(ffmpeg.av_audio_fifo_size(audioFifo) + convertedSampleCount);

                result = ffmpeg.av_audio_fifo_realloc(audioFifo, requiredFifoSize);

                if (result < 0)
                {
                    throw new AmanuensisException(AmanuensisErrorCode_Type.AudioExtractionFailed, $"Impossibile espandere la FIFO durante il flush. Codice errore: {result}");
                }

                writtenSampleCount = ffmpeg.av_audio_fifo_write(audioFifo, (void**)convertedFrame->extended_data, convertedSampleCount);

                if (writtenSampleCount != convertedSampleCount)
                {
                    throw new AmanuensisException(AmanuensisErrorCode_Type.AudioExtractionFailed, $"Scrittura incompleta nella FIFO durante il flush: {writtenSampleCount} campioni scritti su {convertedSampleCount}.");
                }
            }
        }

        private unsafe AVFrame* CreateEncoderFrame(AVCodecContext* audioEncoderContext)
        {
            AVFrame* encoderFrame = null;
            int result;

            try
            {
                encoderFrame = ffmpeg.av_frame_alloc();

                if (encoderFrame == null)
                {
                    throw new AmanuensisException(AmanuensisErrorCode_Type.AudioExtractionFailed, "Impossibile allocare il frame dell'encoder.");
                }

                encoderFrame->format = (int)audioEncoderContext->sample_fmt;
                encoderFrame->sample_rate = audioEncoderContext->sample_rate;
                encoderFrame->nb_samples = audioEncoderContext->frame_size > 0
                    ? audioEncoderContext->frame_size
                    : 1024;

                result = ffmpeg.av_channel_layout_copy(&encoderFrame->ch_layout, &audioEncoderContext->ch_layout);

                if (result < 0)
                {
                    throw new AmanuensisException(AmanuensisErrorCode_Type.AudioExtractionFailed, $"Impossibile configurare i canali del frame dell'encoder. Codice errore: {result}");
                }

                result = ffmpeg.av_frame_get_buffer(encoderFrame, 0);

                if (result < 0)
                {
                    throw new AmanuensisException(AmanuensisErrorCode_Type.AudioExtractionFailed, $"Impossibile allocare il buffer del frame dell'encoder. Codice errore: {result}");
                }
            }
            catch
            {
                if (encoderFrame != null)
                {
                    ffmpeg.av_frame_free(&encoderFrame);
                }

                throw;
            }

            return encoderFrame;
        }

        private unsafe bool TryReadEncoderFrameFromFifo(AVAudioFifo* audioFifo, AVFrame* encoderFrame, AVCodecContext* audioEncoderContext)
        {
            int requiredSampleCount = encoderFrame->nb_samples;
            int availableSampleCount = ffmpeg.av_audio_fifo_size(audioFifo);

            // Non abbiamo ancora abbastanza campioni per un frame MP3 completo.
            if (availableSampleCount < requiredSampleCount)
            {
                return false;
            }

            // Il buffer potrebbe essere ancora usato dall'encoder:
            // FFmpeg lo rende modificabile o ne crea una copia.
            int result = ffmpeg.av_frame_make_writable(encoderFrame);

            if (result < 0)
            {
                throw new AmanuensisException(AmanuensisErrorCode_Type.AudioExtractionFailed, $"Impossibile rendere scrivibile il frame dell'encoder. Codice errore: {result}");
            }

            int readSampleCount = ffmpeg.av_audio_fifo_read(audioFifo, (void**)encoderFrame->extended_data, requiredSampleCount);

            if (readSampleCount != requiredSampleCount)
            {
                throw new AmanuensisException(AmanuensisErrorCode_Type.AudioExtractionFailed, $"Lettura incompleta dalla FIFO: {readSampleCount} campioni letti su {requiredSampleCount}.");
            }

            encoderFrame->nb_samples = requiredSampleCount;

            return true;
        }

        private unsafe AVStream* CreateOutputAudioStream(AVFormatContext* outputFormatContext, AVCodecContext* audioEncoderContext)
        {
            int result;
            AVStream* outputAudioStream = null;

            // Aggiunge uno stream audio al contenitore MP3.
            outputAudioStream = ffmpeg.avformat_new_stream(outputFormatContext, null);

            if (outputAudioStream == null)
            {
                throw new AmanuensisException(AmanuensisErrorCode_Type.AudioExtractionFailed, "Impossibile creare lo stream audio MP3.");
            }

            // Lo stream usa la stessa unità temporale dell'encoder.
            outputAudioStream->time_base = audioEncoderContext->time_base;

            // Copia nello stream frequenza, canali, bitrate e codec dell'encoder.
            result = ffmpeg.avcodec_parameters_from_context(outputAudioStream->codecpar, audioEncoderContext);

            if (result < 0)
            {
                throw new AmanuensisException(AmanuensisErrorCode_Type.AudioExtractionFailed, $"Impossibile copiare i parametri dell'encoder nello stream MP3. Codice errore: {result}");
            }

            return outputAudioStream;
        }

        private unsafe void OpenOutputFileAndWriteHeader(AVFormatContext* outputFormatContext, string audioFilePath)
        {
            int result;

            // Il muxer MP3 richiede un file gestito da AVIO
            if ((outputFormatContext->oformat->flags & ffmpeg.AVFMT_NOFILE) == 0)
            {
                result = ffmpeg.avio_open(&outputFormatContext->pb, audioFilePath, ffmpeg.AVIO_FLAG_WRITE);

                if (result < 0)
                {
                    throw new AmanuensisException(AmanuensisErrorCode_Type.AudioExtractionFailed, $"Impossibile aprire il file MP3 in scrittura. Codice errore: {result}");
                }
            }

            result = ffmpeg.avformat_write_header(outputFormatContext, null);

            if (result < 0)
            {
                throw new AmanuensisException(AmanuensisErrorCode_Type.AudioExtractionFailed, $"Impossibile scrivere l'intestazione MP3. Codice errore: {result}");
            }
        }

        private unsafe void EncodeFrameAndWritePackets(AVCodecContext* audioEncoderContext, AVFrame* encoderFrame, AVPacket* encodedPacket, AVFormatContext* outputFormatContext, AVStream* outputAudioStream)
        {
            int result;

            // Invia all'encoder i campioni PCM contenuti nel frame.
            result = ffmpeg.avcodec_send_frame(audioEncoderContext, encoderFrame);

            if (result < 0)
            {
                throw new AmanuensisException(AmanuensisErrorCode_Type.AudioExtractionFailed, $"Impossibile inviare il frame all'encoder MP3. Codice errore: {result}");
            }

            // Un frame può produrre zero, uno o più pacchetti MP3.
            while (true)
            {
                result = ffmpeg.avcodec_receive_packet(audioEncoderContext, encodedPacket);

                if (result == ffmpeg.AVERROR(ffmpeg.EAGAIN) || result == ffmpeg.AVERROR_EOF) break;

                if (result < 0)
                {
                    throw new AmanuensisException(AmanuensisErrorCode_Type.AudioExtractionFailed, $"Errore durante la codifica MP3. Codice errore: {result}");
                }

                try
                {
                    // Converte i timestamp dall'unità dell'encoder a quella scelta dal contenitore.

                    ffmpeg.av_packet_rescale_ts(encodedPacket, audioEncoderContext->time_base, outputAudioStream->time_base);

                    encodedPacket->stream_index = outputAudioStream->index;

                    result = ffmpeg.av_interleaved_write_frame(outputFormatContext, encodedPacket);

                    if (result < 0)
                    {
                        throw new AmanuensisException(AmanuensisErrorCode_Type.AudioExtractionFailed, $"Errore durante la scrittura del pacchetto MP3. Codice errore: {result}");
                    }
                }
                finally
                {
                    ffmpeg.av_packet_unref(encodedPacket);
                }
            }
        }

        private unsafe void EncodeAvailableFramesFromFifo(AVAudioFifo* audioFifo, AVFrame* encoderFrame, AVCodecContext* audioEncoderContext, AVPacket* encodedPacket, AVFormatContext* outputFormatContext, AVStream* outputAudioStream, ref long nextAudioPts)
        {
            while (TryReadEncoderFrameFromFifo(audioFifo, encoderFrame, audioEncoderContext))
            {
                encoderFrame->pts = nextAudioPts;

                // time_base è 1 / sample_rate: ogni campione corrisponde quindi a un'unità PTS.
                nextAudioPts += encoderFrame->nb_samples;

                EncodeFrameAndWritePackets(audioEncoderContext, encoderFrame, encodedPacket, outputFormatContext, outputAudioStream);
            }
        }

        private unsafe void EncodeRemainingSamplesFromFifo(AVAudioFifo* audioFifo, AVFrame* encoderFrame, AVCodecContext* audioEncoderContext, AVPacket* encodedPacket, AVFormatContext* outputFormatContext, AVStream* outputAudioStream, ref long nextAudioPts)
        {
            int remainingSampleCount = ffmpeg.av_audio_fifo_size(audioFifo);
            int requiredSampleCount;
            int result;
            int readSampleCount;
            int silenceSampleCount;

            if (remainingSampleCount == 0)
            {
                return;
            }

            requiredSampleCount = encoderFrame->nb_samples;

            if (remainingSampleCount >= requiredSampleCount)
            {
                throw new AmanuensisException(AmanuensisErrorCode_Type.AudioExtractionFailed, "La FIFO contiene ancora uno o più frame audio completi.");
            }

            result = ffmpeg.av_frame_make_writable(encoderFrame);

            if (result < 0)
            {
                throw new AmanuensisException(AmanuensisErrorCode_Type.AudioExtractionFailed, $"Impossibile rendere scrivibile l'ultimo frame audio. Codice errore: {result}");
            }

            readSampleCount = ffmpeg.av_audio_fifo_read(audioFifo, (void**)encoderFrame->extended_data, remainingSampleCount);

            if (readSampleCount != remainingSampleCount)
            {
                throw new AmanuensisException(AmanuensisErrorCode_Type.AudioExtractionFailed, $"Lettura incompleta dell'ultimo frame: {readSampleCount} campioni letti su {remainingSampleCount}.");
            }

            silenceSampleCount = requiredSampleCount - remainingSampleCount;

            // Riempie di silenzio la parte del frame non coperta dalla FIFO.
            result = ffmpeg.av_samples_set_silence(encoderFrame->extended_data, remainingSampleCount, silenceSampleCount, audioEncoderContext->ch_layout.nb_channels, audioEncoderContext->sample_fmt);

            if (result < 0)
            {
                throw new AmanuensisException(AmanuensisErrorCode_Type.AudioExtractionFailed, $"Impossibile completare con silenzio l'ultimo frame MP3. Codice errore: {result}");
            }

            encoderFrame->nb_samples = requiredSampleCount;
            encoderFrame->pts = nextAudioPts;

            nextAudioPts += requiredSampleCount;

            EncodeFrameAndWritePackets(audioEncoderContext, encoderFrame, encodedPacket, outputFormatContext, outputAudioStream);
        }

        private unsafe int GetAudioStreamIndex(AVFormatContext* inputFormatContext)
        {
            int audioStreamIndex;

            // -> serve ad accedere al contenuto della struttura tramite un puntatore
            if (inputFormatContext->nb_streams == 0)
            {
                throw new AmanuensisException(AmanuensisErrorCode_Type.AudioTrackNotFound, "Il file non contiene flussi multimediali.");
            }

            //recuperiamo l'indice dello stream audio stream audio
            audioStreamIndex = ffmpeg.av_find_best_stream(inputFormatContext, AVMediaType.AVMEDIA_TYPE_AUDIO, -1, -1, null, 0);

            if (audioStreamIndex < 0)
            {
                throw new AmanuensisException(AmanuensisErrorCode_Type.AudioTrackNotFound, $"Il file non contiene una traccia audio utilizzabile. Codice errore: {audioStreamIndex}");
            }

            return audioStreamIndex;
        }

        private unsafe AVFormatContext* OpenInputFormatContext(string filePath)
        {
            AVFormatContext* inputFormatContext = null;
            int result;

            result = ffmpeg.avformat_open_input(&inputFormatContext, filePath, null, null);

            if (result < 0)
            {
                throw new AmanuensisException(AmanuensisErrorCode_Type.AudioExtractionFailed, $"FFmpeg non riesce ad aprire il file. Codice errore: {result}");
            }

            //verifico se il file è leggibile
            result = ffmpeg.avformat_find_stream_info(inputFormatContext, null);

            if (result < 0)
            {
                ffmpeg.avformat_close_input(&inputFormatContext);

                throw new AmanuensisException(AmanuensisErrorCode_Type.AudioExtractionFailed, $"FFmpeg non riesce a leggere i flussi del file. Codice errore: {result}");
            }

            return inputFormatContext;
        }

        private unsafe AVCodecContext* CreateAudioDecoderContext(AVFormatContext* inputFormatContext, int audioStreamIndex)
        {
            AVCodec* audioDecoder = null;
            AVCodecContext* audioDecoderContext = null;
            AVCodecParameters* audioCodecParameters = null;
            AVStream* audioStream = null;
            int result;

            try
            {
                //recupero il puntatore alla traccia audio
                audioStream = inputFormatContext->streams[audioStreamIndex];

                if (audioStream == null)
                {
                    throw new AmanuensisException(AmanuensisErrorCode_Type.AudioExtractionFailed, "Impossibile recuperare lo stream audio.");
                }

                //recupero il puntatore ai paramteri del codec
                audioCodecParameters = audioStream->codecpar;

                if (audioCodecParameters == null)
                {
                    throw new AmanuensisException(AmanuensisErrorCode_Type.AudioExtractionFailed, "Impossibile recuperare i parametri della traccia audio.");
                }

                if (audioCodecParameters->codec_type != AVMediaType.AVMEDIA_TYPE_AUDIO)
                {
                    throw new AmanuensisException(AmanuensisErrorCode_Type.AudioExtractionFailed, "Lo stream selezionato non è di tipo audio.");
                }

                //cerco se esiste un decoder compatibile con la traccia, se lo trova restituisce il puntatore al codec
                audioDecoder = ffmpeg.avcodec_find_decoder(audioCodecParameters->codec_id);

                if (audioDecoder == null)
                {
                    throw new AmanuensisException(AmanuensisErrorCode_Type.UnsupportedFileFormat, $"Nessun decoder disponibile per il codec audio {audioCodecParameters->codec_id}.");
                }

                audioDecoderContext = ffmpeg.avcodec_alloc_context3(audioDecoder);

                if (audioDecoderContext == null)
                {
                    throw new AmanuensisException(AmanuensisErrorCode_Type.AudioExtractionFailed, "Impossibile allocare il contesto del decoder audio.");
                }

                //configuro il codec con i parametri letti
                result = ffmpeg.avcodec_parameters_to_context(audioDecoderContext, audioCodecParameters);

                if (result < 0)
                {
                    throw new AmanuensisException(AmanuensisErrorCode_Type.AudioExtractionFailed, $"Impossibile configurare il decoder audio. Codice errore: {result}");
                }

                //recupera il parametro time base
                audioDecoderContext->pkt_timebase = audioStream->time_base;

                //apre il codec
                result = ffmpeg.avcodec_open2(audioDecoderContext, audioDecoder, null);

                if (result < 0)
                {
                    throw new AmanuensisException(AmanuensisErrorCode_Type.AudioExtractionFailed, $"Impossibile aprire il decoder audio. Codice errore: {result}");
                }
            }
            catch (Exception)
            {
                ffmpeg.avcodec_free_context(&audioDecoderContext);
                throw;
            }

            return audioDecoderContext;
        }

        #endregion
    }
}
