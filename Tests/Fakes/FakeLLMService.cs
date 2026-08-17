using Amanuensis.Common.Exceptions;
using Amanuensis.Services.Contracts;
using Azure;
using System;
using System.Collections.Generic;
using System.Text;

namespace Amanuensis.Tests.Fakes
{
    public class FakeLLMService : ILLMService
    {
        public string? ReceivedSystemPrompt { get; private set; }
        public string? ReceivedUserPrompt { get; private set; }
        public string? ReceivedModelName { get; private set; }

        readonly string response;
        readonly bool returnException;

        public FakeLLMService(string response, bool returnException = false)
        {
            this.response = response;
            this.returnException = returnException;
        }

        public Task<string> ChatAsync(string systemPrompt, string userPrompt, string modelName, CancellationToken cancellationToken = default)
        {
            if (returnException) throw new AmanuensisException(Common.Enum.AmanuensisErrorCode_Type.ProviderUnavailable, "Servizio di ottimizzazione non raggiungibile");

            ReceivedSystemPrompt = systemPrompt;
            ReceivedModelName = modelName;
            ReceivedUserPrompt = userPrompt;

            return Task.FromResult(response);
        }
    }
}
