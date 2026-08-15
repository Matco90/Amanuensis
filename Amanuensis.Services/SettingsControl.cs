using Amanuensis.Common.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace Amanuensis.Services
{
    public class SettingsControl
    {
        public Settings ReadSettings()
        {
            Settings settings = new Settings();
            string jsonPath;
            string jsonContent;
            JsonDocument document;
            JsonElement settingsElement;

            try
            {

                //check if settings file exists
                jsonPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
                if (!File.Exists(jsonPath)) return settings;

                //read json content
                jsonContent = File.ReadAllText(jsonPath);
                document = JsonDocument.Parse(jsonContent);
                settingsElement = document.RootElement.GetProperty("Settings");

                //deserialize content in settings
                settings = settingsElement.Deserialize<Settings>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new Settings();

            }
            catch (Exception)
            {
                throw;
            }

            return settings;
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
            catch (Exception)
            {
                throw;
            }

        }
    }
}
