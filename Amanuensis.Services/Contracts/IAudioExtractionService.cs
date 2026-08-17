using Amanuensis.Common.Enum;
using System;
using System.Collections.Generic;
using System.Text;

namespace Amanuensis.Services.Contracts
{
    public interface IAudioExtractionService
    {
        string ExtractAudio(string filePath, AudioOutputFormat outputFormat = AudioOutputFormat.Mp3);
    }
}
