using GrammarNazi.Core.Services.GrammarServices;
using GrammarNazi.Domain.Clients;
using GrammarNazi.Domain.Enums;
using NSubstitute;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace GrammarNazi.Tests.Services
{
    public class GroqApiServiceTests
    {
        [Fact]
        public void GrammarAlgorithm_Should_ReturnGroqApi()
        {
            // Arrange
            var groqApiClientMock = Substitute.For<IGroqApiClient>();
            var service = new GroqApiService(groqApiClientMock);

            // Act
            var result = service.GrammarAlgorithm;

            // Assert
            Assert.Equal(GrammarAlgorithms.GroqApi, result);
        }

        [Fact]
        public async Task GetCorrections_ValidResponse_Should_ReturnCorrections()
        {
            // Arrange
            var groqApiClientMock = Substitute.For<IGroqApiClient>();
            var service = new GroqApiService(groqApiClientMock);

            var apiResponse = @"[
                {
                    ""wrongWord"": ""I is"",
                    ""message"": ""Subject-verb agreement error"",
                    ""possibleReplacements"": [""I am""]
                }
            ]";

            groqApiClientMock.GetChatCompletion(Arg.Any<string>(), Arg.Any<string>())
                .Returns(Task.FromResult(apiResponse));

            // Act
            var result = await service.GetCorrections("I is an engineer");

            // Assert
            Assert.True(result.HasCorrections);
            Assert.Single(result.Corrections);
            Assert.Equal("I is", result.Corrections.First().WrongWord);
            Assert.Equal("Subject-verb agreement error", result.Corrections.First().Message);
            Assert.Contains("I am", result.Corrections.First().PossibleReplacements);
        }

        [Fact]
        public async Task GetCorrections_ResponseWithJsonCodeBlock_Should_ParseCorrectly()
        {
            // Arrange
            var groqApiClientMock = Substitute.For<IGroqApiClient>();
            var service = new GroqApiService(groqApiClientMock);

            var apiResponse = @"```json
            [
                {
                    ""wrongWord"": ""teh"",
                    ""message"": ""Spelling error"",
                    ""possibleReplacements"": [""the""]
                }
            ]
            ```";

            groqApiClientMock.GetChatCompletion(Arg.Any<string>(), Arg.Any<string>())
                .Returns(Task.FromResult(apiResponse));

            // Act
            var result = await service.GetCorrections("teh cat");

            // Assert
            Assert.True(result.HasCorrections);
            Assert.Single(result.Corrections);
            Assert.Equal("teh", result.Corrections.First().WrongWord);
        }

        [Fact]
        public async Task GetCorrections_EmptyResponse_Should_ReturnNoCorrections()
        {
            // Arrange
            var groqApiClientMock = Substitute.For<IGroqApiClient>();
            var service = new GroqApiService(groqApiClientMock);

            groqApiClientMock.GetChatCompletion(Arg.Any<string>(), Arg.Any<string>())
                .Returns(Task.FromResult(string.Empty));

            // Act
            var result = await service.GetCorrections("This is correct.");

            // Assert
            Assert.False(result.HasCorrections);
        }

        [Fact]
        public async Task GetCorrections_EmptyArrayResponse_Should_ReturnNoCorrections()
        {
            // Arrange
            var groqApiClientMock = Substitute.For<IGroqApiClient>();
            var service = new GroqApiService(groqApiClientMock);

            groqApiClientMock.GetChatCompletion(Arg.Any<string>(), Arg.Any<string>())
                .Returns(Task.FromResult("[]"));

            // Act
            var result = await service.GetCorrections("This is correct.");

            // Assert
            Assert.False(result.HasCorrections);
        }

        [Fact]
        public async Task GetCorrections_InvalidJsonResponse_Should_ReturnNoCorrections()
        {
            // Arrange
            var groqApiClientMock = Substitute.For<IGroqApiClient>();
            var service = new GroqApiService(groqApiClientMock);

            groqApiClientMock.GetChatCompletion(Arg.Any<string>(), Arg.Any<string>())
                .Returns(Task.FromResult("This is not valid JSON"));

            // Act
            var result = await service.GetCorrections("Some text");

            // Assert
            Assert.False(result.HasCorrections);
        }

        [Fact]
        public async Task GetCorrections_WhiteListWord_Should_BeFiltered()
        {
            // Arrange
            var groqApiClientMock = Substitute.For<IGroqApiClient>();
            var service = new GroqApiService(groqApiClientMock);
            service.SetWhiteListWords(new[] { "teh" });

            var apiResponse = @"[
                {
                    ""wrongWord"": ""teh"",
                    ""message"": ""Spelling error"",
                    ""possibleReplacements"": [""the""]
                }
            ]";

            groqApiClientMock.GetChatCompletion(Arg.Any<string>(), Arg.Any<string>())
                .Returns(Task.FromResult(apiResponse));

            // Act
            var result = await service.GetCorrections("teh cat");

            // Assert
            Assert.False(result.HasCorrections);
        }

        [Fact]
        public async Task GetCorrections_CorrectionWithoutReplacements_Should_BeFiltered()
        {
            // Arrange
            var groqApiClientMock = Substitute.For<IGroqApiClient>();
            var service = new GroqApiService(groqApiClientMock);

            var apiResponse = @"[
                {
                    ""wrongWord"": ""something"",
                    ""message"": ""Some error"",
                    ""possibleReplacements"": []
                }
            ]";

            groqApiClientMock.GetChatCompletion(Arg.Any<string>(), Arg.Any<string>())
                .Returns(Task.FromResult(apiResponse));

            // Act
            var result = await service.GetCorrections("something");

            // Assert
            Assert.False(result.HasCorrections);
        }

        [Fact]
        public async Task GetCorrections_MultipleCorrections_Should_ReturnAll()
        {
            // Arrange
            var groqApiClientMock = Substitute.For<IGroqApiClient>();
            var service = new GroqApiService(groqApiClientMock);

            var apiResponse = @"[
                {
                    ""wrongWord"": ""I is"",
                    ""message"": ""Subject-verb agreement"",
                    ""possibleReplacements"": [""I am""]
                },
                {
                    ""wrongWord"": ""I works"",
                    ""message"": ""Subject-verb agreement"",
                    ""possibleReplacements"": [""I work""]
                }
            ]";

            groqApiClientMock.GetChatCompletion(Arg.Any<string>(), Arg.Any<string>())
                .Returns(Task.FromResult(apiResponse));

            // Act
            var result = await service.GetCorrections("I is an engineer and I works here");

            // Assert
            Assert.True(result.HasCorrections);
            Assert.Equal(2, result.Corrections.Count());
        }

        [Fact]
        public async Task GetCorrections_Should_CallApiWithCorrectPrompt()
        {
            // Arrange
            var groqApiClientMock = Substitute.For<IGroqApiClient>();
            var service = new GroqApiService(groqApiClientMock);

            groqApiClientMock.GetChatCompletion(Arg.Any<string>(), Arg.Any<string>())
                .Returns(Task.FromResult("[]"));

            // Act
            await service.GetCorrections("Test text");

            // Assert
            await groqApiClientMock.Received(1).GetChatCompletion(
                Arg.Is<string>(s => s.Contains("grammar checker")),
                Arg.Is<string>(s => s.Contains("Test text"))
            );
        }

        [Fact]
        public async Task GetCorrections_WithSelectedLanguage_Should_IncludeLanguageInPrompt()
        {
            // Arrange
            var groqApiClientMock = Substitute.For<IGroqApiClient>();
            var service = new GroqApiService(groqApiClientMock);
            service.SetSelectedLanguage(SupportedLanguages.Spanish);

            groqApiClientMock.GetChatCompletion(Arg.Any<string>(), Arg.Any<string>())
                .Returns(Task.FromResult("[]"));

            // Act
            await service.GetCorrections("Hola mundo");

            // Assert
            await groqApiClientMock.Received(1).GetChatCompletion(
                Arg.Is<string>(s => s.Contains("Spanish")),
                Arg.Any<string>()
            );
        }
    }
}