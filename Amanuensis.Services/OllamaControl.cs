using LLama.Native;
using MongoDB.Bson.IO;
using Amanuensis.Common;
using Amanuensis.Common.Entities;
using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using JsonConvert = Newtonsoft.Json.JsonConvert;

namespace Amanuensis.Services
{
    public class OllamaControl : ServiceBase
    {

        HttpClient httpClientOllamaLLM;
        HttpClient httpClientOllamaEmbed;

        public OllamaControl(Settings settings)
        {
            this.settings = settings;

            httpClientOllamaLLM = new HttpClient { BaseAddress = new Uri(Constants.OLLAMA_CLOUD_URL), Timeout = TimeSpan.FromMinutes(Constants.AIAssistantOllamaTimeoutSeconds) };
            httpClientOllamaLLM.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", string.IsNullOrWhiteSpace(settings.OllamaAPIKey) ? Environment.GetEnvironmentVariable("OLLAMA_API_KEY") : settings.OllamaAPIKey);

        }

        public async Task<string> ChatAsync(string systemPrompt, string userPrompt, string modelName)
        {
            List<OllamaChatMessage> ollamaChatMessages = new List<OllamaChatMessage>();


            //costruisco la lista dei messaggi
            ollamaChatMessages.Add(new OllamaChatMessage() { role = "system", content = systemPrompt });
            ollamaChatMessages.Add(new OllamaChatMessage() { role = "user", content = userPrompt });

            //costruisco il file json
            OllamaChatRequest ollamaRequest = new OllamaChatRequest()
            {
                model = modelName,
                stream = false,
                messages = ollamaChatMessages
            };

            StringContent requestContent = new StringContent(JsonConvert.SerializeObject(ollamaRequest), Encoding.UTF8, "application/json");
            HttpResponseMessage response = await httpClientOllamaLLM.PostAsync("chat", requestContent);
            string responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"Errore Ollama: {(int)response.StatusCode} - {responseContent}");
            }

            OllamaChatResponse ollamaResponse = JsonConvert.DeserializeObject<OllamaChatResponse>(responseContent);

            return ollamaResponse?.message?.content ?? responseContent;

        }

    }
}
