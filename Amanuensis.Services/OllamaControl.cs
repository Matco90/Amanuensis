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
using Amanuensis.Common.Exceptions;
using Amanuensis.Common.Enum;

namespace Amanuensis.Services
{
    public class OllamaControl : ServiceBase
    {

        HttpClient httpClientOllamaLLM;
        HttpClient httpClientOllamaEmbed;

        public OllamaControl(Settings settings)
        {
            string? apiKey = "";
            this.settings = settings;

            apiKey = string.IsNullOrWhiteSpace(settings.OllamaAPIKey) ? Environment.GetEnvironmentVariable("OLLAMA_API_KEY") : settings.OllamaAPIKey;

            if (string.IsNullOrWhiteSpace(apiKey)) throw new AmanuensisException(AmanuensisErrorCode_Type.MissingApiKey, "La chiave API di Ollama non è configurata.");

            httpClientOllamaLLM = new HttpClient { BaseAddress = new Uri(Constants.OLLAMA_CLOUD_URL), Timeout = TimeSpan.FromSeconds(Constants.AIAssistantOllamaTimeoutSeconds) };
            httpClientOllamaLLM.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        }

        public async Task<string> ChatAsync(string systemPrompt, string userPrompt, string modelName, CancellationToken cancellationToken = default)
        {
            List<OllamaChatMessage> ollamaChatMessages = new List<OllamaChatMessage>();

            try
            {

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
                HttpResponseMessage response = await httpClientOllamaLLM.PostAsync("chat", requestContent, cancellationToken);
                string responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

                if ((int)response.StatusCode >= 500)
                {
                    throw new AmanuensisException(AmanuensisErrorCode_Type.ProviderUnavailable, $"Ollama non disponibile: {(int)response.StatusCode}");
                }

                if (!response.IsSuccessStatusCode)
                {
                    throw new AmanuensisException(AmanuensisErrorCode_Type.ProviderRequestRejected, $"Ollama ha rifiutato la richiesta: {(int)response.StatusCode}");
                }

                OllamaChatResponse ollamaResponse = JsonConvert.DeserializeObject<OllamaChatResponse>(responseContent);


                return ollamaResponse?.message?.content ?? responseContent;

            }
            catch (OperationCanceledException ex)
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    throw new AmanuensisException(AmanuensisErrorCode_Type.ProcessingTimeout, "Ollama non ha risposto entro il tempo previsto.", ex);
                }
                else
                {
                    throw;
                }
            }
            catch (HttpRequestException ex)
            {
                throw new AmanuensisException(AmanuensisErrorCode_Type.ProviderUnavailable, "Impossibile contattare Ollama.", ex);
            }


        }

    }
}
