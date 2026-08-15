using Deepgram;
using Deepgram.Clients.Interfaces.v1;
using Deepgram.Models.Listen.v1.REST;
using DocumentFormat.OpenXml.Spreadsheet;
using SharpCompress.Common;
using Amanuensis.Common.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Amanuensis.Services
{
    public class DeepgramControl : ServiceBase
    {
        IListenRESTClient deepgramClient;

        public DeepgramControl(Settings settings)
        {
            this.settings = settings;

            Library.Initialize();

            if (!string.IsNullOrWhiteSpace(settings.DeepgramAPIKey))
            {
                deepgramClient = ClientFactory.CreateListenRESTClient(settings.DeepgramAPIKey);
            }
            else
            {
                //se non è presente l'api key nei settings se la va a leggere direttamente dalla variabile d'ambiente DEEPGRAM_API_KEY
                deepgramClient = ClientFactory.CreateListenRESTClient();
            }
            
        }

        public async Task<string> ConvertSpeechToText(string filePath)
        {
            string speechText = "";
            byte[] audioData;
            PreRecordedSchema preRecordedSchema;

            try
            {
                preRecordedSchema = new PreRecordedSchema() { Model = "nova-3", Language = "it", SmartFormat = true };

                //leggo il file audio
                audioData = await File.ReadAllBytesAsync(filePath);

                SyncResponse response = await deepgramClient.TranscribeFile(audioData, preRecordedSchema);

                speechText = response.Results.Channels[0].Alternatives[0].Transcript;
            }
            catch (Exception)
            {
                throw;
            }

            return speechText;

        }
    }
}
