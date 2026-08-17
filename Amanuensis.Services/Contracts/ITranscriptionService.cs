using System;
using System.Collections.Generic;
using System.Text;

namespace Amanuensis.Services.Contracts
{
    public interface ITranscriptionService
    {
        Task<string> ConvertSpeechToText(string filePath);
    }
}
