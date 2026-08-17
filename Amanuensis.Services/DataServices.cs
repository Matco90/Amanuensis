using Amanuensis.Common;
using Amanuensis.Common.Container;
using Amanuensis.Common.Enum;
using Amanuensis.Common.Exceptions;
using OpenAI.Audio;
using SharpCompress.Common;
using System.Reflection.Metadata;
using Constants = Amanuensis.Common.Constants;

namespace Amanuensis.Services
{
    public class DataServices : ServiceBase
    {
        SettingsControl settingsControl;
        //GroqControl groqControl;
        DeepgramControl deepgramControl;
        AudioExtractionControl audioExtractionControl;
        OllamaControl ollamaControl;

        public DataServices()
        {
            settingsControl = new SettingsControl();
            settings = settingsControl.ReadSettings();

            //groqControl = new GroqControl(settings);
            deepgramControl = new DeepgramControl(settings);
            audioExtractionControl = new AudioExtractionControl(settings);
            ollamaControl = new OllamaControl(settings);
        }

        #region PUBLIC METHODS

        public async Task<string> TranscriptAudioFromFile(string filePath)
        {
            string transcription = "";

            try
            {
                CheckFile(filePath);

                //trascrizione audio
                transcription = await ConvertSpeechToText(filePath);

                //ottimizzazione testo con AI
                transcription = await OptimizeAudioTranscription(transcription);

            }
            catch (Exception)
            {

                throw;
            }

            return transcription;
        }

        public async Task<string> ConvertSpeechToText(string filePath)
        {
            string speechText = "";
            bool deleteAudioFile = false;

            try
            {

                //se il file è di tipo video, prima estrapolo l'audio
                if (Constants.VideoExtensionsList.Exists(e => e.ToLower() == Path.GetExtension(filePath).ToLower()))
                {
                    filePath = audioExtractionControl.ExtractAudio(filePath, AudioOutputFormat.Mp3);
                    deleteAudioFile = true;
                }

                speechText = await deepgramControl.ConvertSpeechToText(filePath);

                //cancello il file audio estrapolato dal video
                if (deleteAudioFile) File.Delete(filePath);
            }
            catch (Exception ex)
            {
                if (deleteAudioFile && File.Exists(filePath))
                {
                    try
                    {
                        File.Delete(filePath);
                    }
                    catch (Exception)
                    {
                       //ignorato per non sovrascrivere l'eccezione originale
                    }

                }

                throw;
            }

            return speechText;
        }

        public async Task<string> OptimizeAudioTranscription(string audioTranscription)
        {
            string optimizedTranscription = "";

            try
            {
                optimizedTranscription = await ollamaControl.ChatAsync(PromptContainer.OptimizeTranscriptionPrompt(), audioTranscription, Constants.ProcessingTextModel);
            }
            catch (Exception)
            {
                throw;
            }

            return optimizedTranscription;
        }

        public async Task<string> SummarizeText(string text)
        {
            string summarizedText = "";

            try
            {
                summarizedText = await ollamaControl.ChatAsync(PromptContainer.SummarizeTextPrompt(), text, Constants.ProcessingTextModel);
            }
            catch (Exception)
            {
                throw;
            }

            return summarizedText;
        }

        public async Task<string> ConvertIntoEmail(string text)
        {
            string mailText = "";

            try
            {
                mailText = await ollamaControl.ChatAsync(PromptContainer.ConvertInEmailPrompt(), text, Constants.ProcessingTextModel);
            }
            catch (Exception)
            {
                throw;
            }

            return mailText;
        }

        #endregion

        #region PRIVATE METHODS

        private string GetAudioFromVideoFile(string filePath, AudioOutputFormat audioOutputFormat = AudioOutputFormat.Mp3)
        {
            string audiFilePath = "";

            audiFilePath = audioExtractionControl.ExtractAudio(filePath, audioOutputFormat);

            return audiFilePath;
        }

        private void CheckFile(string filePath)
        {
            //verifico che il file esista
            if (!File.Exists(filePath)) throw new AmanuensisException(AmanuensisErrorCode_Type.FileNotFound, $"File non trovato nel percoso: {filePath}");

            //verifico che il file sia in un formato compatibile
            if (!Constants.VideoExtensionsList.Exists(e => e.ToLower() == Path.GetExtension(filePath).ToLower()) && !Constants.AudioExtensionsList.Exists(e => e.ToLower() == Path.GetExtension(filePath).ToLower()))
            {
                throw new AmanuensisException(AmanuensisErrorCode_Type.UnsupportedFileFormat, $"Foramto file non compatibile {Path.GetExtension(filePath)}");
            }
        }

        #endregion
    }
}
