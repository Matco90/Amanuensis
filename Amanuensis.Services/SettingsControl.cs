using Amanuensis.Common.Entities;
using Amanuensis.Common.Exceptions;
using Amanuensis.Services.Contracts;
using Elastic.Clients.Elasticsearch.Cluster;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace Amanuensis.Services
{
    public class SettingsControl: ISettingsService
    {
        private Settings settings;

        public SettingsControl()
        {
            settings = new Settings();
            ReadSettings();
        }

        public void ReadSettings()
        {
            string jsonPath;
            string jsonContent;
            JsonDocument document;
            JsonElement settingsElement;
            Settings tempSettings;

            try
            {

                //check if settings file exists
                jsonPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
                if (!File.Exists(jsonPath)) return;

                //read json content
                jsonContent = File.ReadAllText(jsonPath);
                document = JsonDocument.Parse(jsonContent);
                settingsElement = document.RootElement.GetProperty("Settings");

                //deserialize content in settings
                tempSettings = settingsElement.Deserialize<Settings>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new Settings();

                //assign proprerty already read to settings
                settings.OllamaAPIKey = tempSettings.OllamaAPIKey;
                settings.DeepgramAPIKey = tempSettings.DeepgramAPIKey;
                settings.GroqApiKey = tempSettings.GroqApiKey;

            }
            catch (Exception ex)
            {
                throw new AmanuensisException(Common.Enum.AmanuensisErrorCode_Type.LoadSettingsError, "Errore durante il caricamento delle impostazioni", ex);
            }
        }

        public void SaveSettings(Settings settings)
        {
            string jsonContent;
            string jsonPath;

            try
            {
                jsonPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
                jsonContent = JsonSerializer.Serialize(new { Settings = settings }, new JsonSerializerOptions { WriteIndented = true });

                File.WriteAllText(jsonPath, jsonContent);
            }
            catch (Exception ex)
            {
                throw new AmanuensisException(Common.Enum.AmanuensisErrorCode_Type.SaveSettingsError, "Errore durante il salvataggio delle impostazioni", ex);
            }

        }

        public Settings GetSettings()
        {
            return settings;
        }
    }
}
