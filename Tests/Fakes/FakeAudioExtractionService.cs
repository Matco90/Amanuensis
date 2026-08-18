using Amanuensis.Common.Enum;
using Amanuensis.Services.Contracts;
using System;
using System.Collections.Generic;
using System.Text;

namespace Amanuensis.Tests.Fakes
{
    public class FakeAudioExtractionService : IAudioExtractionService
    {
        public string? ReceivedFilePath { get; private set; }
        public AudioOutputFormat? ReceivedOutputFormat { get; private set; }
        public int CalledCount { get; private set; }

        public string ExtractedAudioFilePath { get; private set; }

        public FakeAudioExtractionService(string extractedAudioFilePath = "")
        {
            this.ExtractedAudioFilePath = extractedAudioFilePath;
        }

        public string ExtractAudio(string filePath, AudioOutputFormat outputFormat = AudioOutputFormat.Mp3)
        {
            CalledCount++;
            ReceivedFilePath = filePath;
            ReceivedOutputFormat = outputFormat;

            return ExtractedAudioFilePath;
        }
    }
}
