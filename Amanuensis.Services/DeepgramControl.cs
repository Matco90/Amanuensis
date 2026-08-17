using Amanuensis.Common.Entities;
using Amanuensis.Common.Enum;
using Amanuensis.Common.Exceptions;
using Amanuensis.Services.Contracts;
using Deepgram;
using Deepgram.Clients.Interfaces.v1;
using Deepgram.Models.Exceptions.v1;
using Deepgram.Models.Listen.v1.REST;
using DocumentFormat.OpenXml.Spreadsheet;
using SharpCompress.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Amanuensis.Services
{
    public class DeepgramControl : ServiceBase, ITranscriptionService
    {
        IListenRESTClient deepgramClient;

        public DeepgramControl(Settings settings)
        {
            string? apiKey;

            this.settings = settings;

            Library.Initialize();

            apiKey = !string.IsNullOrWhiteSpace(settings.DeepgramAPIKey) ? settings.DeepgramAPIKey : Environment.GetEnvironmentVariable(Common.Constants.EnvironmentVariableDeepgramApiKeyName);

            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                deepgramClient = ClientFactory.CreateListenRESTClient(apiKey);
            }
            else
            {
                throw new AmanuensisException(AmanuensisErrorCode_Type.MissingApiKey, "La chiave API di Deepgram non è configurata.");
            }

        }

        public async Task<string> ConvertSpeechToText(string filePath)
        {
            string speechText = "";
            byte[] audioData;
            PreRecordedSchema preRecordedSchema;
            SyncResponse response = default(SyncResponse);
            CancellationTokenSource timeoutToken = new(TimeSpan.FromSeconds(Common.Constants.DeepgramTimeoutSeconds));

            try
            {
                preRecordedSchema = new PreRecordedSchema() { Model = "nova-3", Language = "it", SmartFormat = true };

                //leggo il file audio
                audioData = await File.ReadAllBytesAsync(filePath);

                response = await deepgramClient.TranscribeFile(audioData, preRecordedSchema, timeoutToken);

                speechText = response.Results.Channels[0].Alternatives[0].Transcript;
            }
            catch (OperationCanceledException ex) when (timeoutToken.IsCancellationRequested)
            {
                throw new AmanuensisException(AmanuensisErrorCode_Type.ProcessingTimeout, "Deepgram non ha risposto entro il tempo previsto.", ex);
            }
            catch (DeepgramConectionException ex)
            {
                throw new AmanuensisException(AmanuensisErrorCode_Type.ProviderUnavailable, "Impossibile contattare Deepgram.", ex);
            }
            catch (DeepgramRESTException ex)
            {
                throw new AmanuensisException(AmanuensisErrorCode_Type.ProviderRequestRejected, "Deepgram ha rifiutato la richiesta.", ex);
            }
            catch (IOException ex)
            {
                throw new AmanuensisException(AmanuensisErrorCode_Type.InvalidFile, $"Deepgram ha riscontrato problemi con il file {filePath}.", ex);
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new AmanuensisException(AmanuensisErrorCode_Type.InvalidFile, $"Deepgram non ha le autorizzazione per operare sul file {filePath}.", ex);
            }
            finally { timeoutToken.Dispose(); }

            return speechText;

        }
    }
}
