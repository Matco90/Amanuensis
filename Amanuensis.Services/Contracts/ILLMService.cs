using System;
using System.Collections.Generic;
using System.Text;

namespace Amanuensis.Services.Contracts
{
    public interface ILLMService
    {
        Task<string> ChatAsync(string systemPrompt, string userPrompt, string modelName, CancellationToken cancellationToken = default);
    }
}
