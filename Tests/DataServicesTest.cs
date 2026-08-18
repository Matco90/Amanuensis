using Amanuensis.Common.Entities;
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
                TranscriptionResult result = await dataServices.TranscriptAudioFromFile(filePath);

                // Assert
                Assert.Equal(optimizedTranscription, result.Transcription);
                Assert.Equal(OperationStatus_Type.Done, result.Status);
                Assert.Null(result.ErrorCode);
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
        public async Task TranscriptAudioFromFile_WhenTranscriptionFails_ReturnsErrorResult()
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
                TranscriptionResult transcriptionResult = await dataServices.TranscriptAudioFromFile(filePath);

                // Assert
                Assert.Equal(string.Empty, transcriptionResult.Transcription);
                Assert.Equal(OperationStatus_Type.Error, transcriptionResult.Status);
                Assert.Equal(AmanuensisErrorCode_Type.ProviderUnavailable, transcriptionResult.ErrorCode);
                Assert.Null(llmService.ReceivedUserPrompt);
                Assert.Equal(0, audioExtractionService.CalledCount);
            }
            finally
            {
                File.Delete(filePath);
            }
        }

        [Fact]
        public async Task TranscriptAudioFromFile_WhenOptimizationFails_ReturnsPartialResult()
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
                TranscriptionResult transcriptionResult = await dataServices.TranscriptAudioFromFile(filePath);

                // Assert
                Assert.Equal(rawTranscription, transcriptionResult.Transcription);
                Assert.Equal(OperationStatus_Type.PartiallyDone, transcriptionResult.Status);
                Assert.Equal(AmanuensisErrorCode_Type.ProviderUnavailable, transcriptionResult.ErrorCode);
                Assert.Equal(filePath, transcriptionService.ReceivedFilePath);
                Assert.Equal(0, audioExtractionService.CalledCount);
            }
            finally
            {
                File.Delete(filePath);
            }
        }

        [Fact]
        public async Task TranscriptAudioFromFile_WithVideoFile_TranscribesAndOptimizesText()
        {
            // Arrange
            const string rawTranscription = "testo grezzo";
            const string optimizedTranscription = "testo revisionato";

            FakeSettingsService settingsService;
            FakeTranscriptionService transcriptionService;
            FakeAudioExtractionService audioExtractionService;
            FakeLLMService llmService;
            DataServices dataServices;

            string filePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.mp4");
            await File.WriteAllBytesAsync(filePath, Array.Empty<byte>());

            string extractedFilePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.mp3");
            await File.WriteAllBytesAsync(extractedFilePath, Array.Empty<byte>());

            try
            {
                settingsService = new FakeSettingsService();

                transcriptionService = new FakeTranscriptionService(rawTranscription);

                audioExtractionService = new FakeAudioExtractionService(extractedFilePath);

                llmService = new FakeLLMService(optimizedTranscription);

                dataServices = new DataServices(settingsService, transcriptionService, audioExtractionService, llmService);

                // Act
                TranscriptionResult result = await dataServices.TranscriptAudioFromFile(filePath);

                // Assert
                Assert.Equal(optimizedTranscription, result.Transcription);
                Assert.Equal(OperationStatus_Type.Done, result.Status);
                Assert.Null(result.ErrorCode);
                Assert.Equal(filePath, audioExtractionService.ReceivedFilePath);
                Assert.Equal(AudioOutputFormat.Mp3, audioExtractionService.ReceivedOutputFormat);
                Assert.Equal(1, audioExtractionService.CalledCount);
                Assert.Equal(extractedFilePath, audioExtractionService.ExtractedAudioFilePath);
                Assert.Equal(extractedFilePath, transcriptionService.ReceivedFilePath);
                Assert.Equal(rawTranscription, llmService.ReceivedUserPrompt);
                Assert.False(File.Exists(extractedFilePath));

            }
            finally
            {
                File.Delete(filePath);
                File.Delete(extractedFilePath);
            }
        }

        [Fact]
        public async Task TranscriptAudioFromFile_WithUnsupportedFileFormat_ReturnsErrorResult()
        {
            // Arrange
            const string rawTranscription = "testo grezzo";
            const string optimizedTranscription = "testo revisionato";

            FakeSettingsService settingsService = new FakeSettingsService();
            FakeTranscriptionService transcriptionService = new FakeTranscriptionService(rawTranscription);
            FakeAudioExtractionService audioExtractionService = new FakeAudioExtractionService();
            FakeLLMService llmService = new FakeLLMService(optimizedTranscription);
            DataServices dataServices = new DataServices(settingsService, transcriptionService, audioExtractionService, llmService);

            string filePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.txt");
            await File.WriteAllBytesAsync(filePath, Array.Empty<byte>());

            try
            {
                // Act
                TranscriptionResult result = await dataServices.TranscriptAudioFromFile(filePath);

                // Assert
                Assert.Equal(string.Empty, result.Transcription);
                Assert.Equal(OperationStatus_Type.Error, result.Status);
                Assert.Equal(AmanuensisErrorCode_Type.UnsupportedFileFormat, result.ErrorCode);
                Assert.Null(transcriptionService.ReceivedFilePath);
                Assert.Null(llmService.ReceivedUserPrompt);
                Assert.Equal(0, audioExtractionService.CalledCount);
            }
            finally
            {
                File.Delete(filePath);
            }
        }
    }
}
