using Amanuensis.Common.Enum;
using Amanuensis.Common.Exceptions;
using Amanuensis.Services;
using Amanuensis.Tests.Fakes;

namespace Tests
{
    public class DataServicesTest
    {
        [Fact]
        public async Task TranscriptAudioFromFile_WithAudioFile_TranscribesAndOptimizesText()
        {
            // Arrange
            const string rawTranscription = "testo grezzo";
            const string optimizedTranscription = "testo revisionato";

            FakeSettingsService settingsService;
            FakeTranscriptionService transcriptionService;
            FakeAudioExtractionService audioExtractionService;
            FakeLLMService llmService;
            DataServices dataServices;

            string filePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.mp3");

            await File.WriteAllBytesAsync(filePath, Array.Empty<byte>());

            try
            {
                settingsService = new FakeSettingsService();

                transcriptionService = new FakeTranscriptionService(rawTranscription);

                audioExtractionService = new FakeAudioExtractionService();

                llmService = new FakeLLMService(optimizedTranscription);

                dataServices = new DataServices(settingsService, transcriptionService, audioExtractionService, llmService);

                // Act
                string result = await dataServices.TranscriptAudioFromFile(filePath);

                // Assert
                Assert.Equal(optimizedTranscription, result);
                Assert.Equal(filePath, transcriptionService.ReceivedFilePath);
                Assert.Equal(rawTranscription, llmService.ReceivedUserPrompt);
                Assert.Equal(0, audioExtractionService.CalledCount);
            }
            finally
            {
                File.Delete(filePath);
            }
        }

        [Fact]
        public async Task ExceptionPropagation_Transcription()
        {
            // Arrange
            const string rawTranscription = "testo grezzo";
            const string optimizedTranscription = "testo revisionato";

            FakeSettingsService settingsService;
            FakeTranscriptionService transcriptionService;
            FakeAudioExtractionService audioExtractionService;
            FakeLLMService llmService;
            DataServices dataServices;

            string filePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.mp3");

            await File.WriteAllBytesAsync(filePath, Array.Empty<byte>());

            try
            {
                settingsService = new FakeSettingsService();

                transcriptionService = new FakeTranscriptionService(rawTranscription, true);

                audioExtractionService = new FakeAudioExtractionService();

                llmService = new FakeLLMService(optimizedTranscription);

                dataServices = new DataServices(settingsService, transcriptionService, audioExtractionService, llmService);

                // Act
                AmanuensisException exception = await Assert.ThrowsAsync<AmanuensisException>(() => dataServices.TranscriptAudioFromFile(filePath));

                // Assert

                Assert.Equal(AmanuensisErrorCode_Type.ProviderUnavailable, exception.ErrorCode);
                Assert.Null(llmService.ReceivedUserPrompt);
                Assert.Equal(0, audioExtractionService.CalledCount);
            }
            finally
            {
                File.Delete(filePath);
            }
        }

        [Fact]
        public async Task ExceptionPropagation_Optimization()
        {
            // Arrange
            const string rawTranscription = "testo grezzo";
            const string optimizedTranscription = "testo revisionato";

            FakeSettingsService settingsService;
            FakeTranscriptionService transcriptionService;
            FakeAudioExtractionService audioExtractionService;
            FakeLLMService llmService;
            DataServices dataServices;

            string filePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.mp3");

            await File.WriteAllBytesAsync(filePath, Array.Empty<byte>());

            try
            {
                settingsService = new FakeSettingsService();

                transcriptionService = new FakeTranscriptionService(rawTranscription);

                audioExtractionService = new FakeAudioExtractionService();

                llmService = new FakeLLMService(optimizedTranscription, true);

                dataServices = new DataServices(settingsService, transcriptionService, audioExtractionService, llmService);

                // Act
                AmanuensisException exception = await Assert.ThrowsAsync<AmanuensisException>(() => dataServices.TranscriptAudioFromFile(filePath));

                // Assert
                Assert.Equal(AmanuensisErrorCode_Type.ProviderUnavailable, exception.ErrorCode);
                Assert.Equal(filePath, transcriptionService.ReceivedFilePath);
                Assert.Equal(0, audioExtractionService.CalledCount);
            }
            finally
            {
                File.Delete(filePath);
            }
        }
    }
}
