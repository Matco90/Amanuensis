using Amanuensis.Common.Exceptions;
using Amanuensis.Services.Contracts;
using System;
using System.Collections.Generic;
using System.Text;

namespace Amanuensis.Tests.Fakes
{
    public class FakeTranscriptionService : ITranscriptionService
    {
        readonly string transcription;
        readonly bool returnException;

        public string? ReceivedFilePath { get; private set; }

        public FakeTranscriptionService(string transcription, bool returnException = false)
        {
            this.transcription = transcription;
            this.returnException = returnException;
        }

        public Task<string> ConvertSpeechToText(string filePath)
        {
            if (returnException) throw new AmanuensisException(Common.Enum.AmanuensisErrorCode_Type.ProviderUnavailable, "Servizio di trascrizione non raggiungibile");

            ReceivedFilePath = filePath;

            return Task.FromResult(transcription);
        }
    }
}
