using Amanuensis.Common;
using Amanuensis.Common.Container;
using Amanuensis.Common.Enum;
using OpenAI.Audio;
using System.Reflection.Metadata;

namespace Amanuensis.Services
{
    public class DataServices : ServiceBase
    {
        SettingsControl settingsControl;
        GroqControl groqControl;
        DeepgramControl deepgramControl;
        AudioExtractionControl audioExtractionControl;
        OllamaControl ollamaControl;

        public DataServices()
        {
            settingsControl = new SettingsControl();
            settings = settingsControl.ReadSettings();

            groqControl = new GroqControl(settings);
            deepgramControl = new DeepgramControl(settings);
            audioExtractionControl = new AudioExtractionControl(settings);
            ollamaControl = new OllamaControl(settings);
        }

        #region PUBLIC METHODS

        public async Task<string> ConvertSpeechToText(string filePath)
        {
            string speechText = "";
            bool deleteAudioFile = false;

            try
            {
                //se il file è di tipo video, prima estrapolo l'audio
                if (Constants.VideoExtensionsList.Exists(e=> e.ToLower() == Path.GetExtension(filePath).ToLower()))
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

                speechText = $"Errore durante la conversione dell'audio - {ex.Message}";
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

        #endregion
    }
}
