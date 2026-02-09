using DolCon.Core.Services;
using FluentAssertions;

namespace DolCon.Core.Tests;

public class SaveGameServiceTests
{
    public class SanitizePlayerNameTests
    {
        [Theory]
        [InlineData("Gandalf", "Gandalf")]
        [InlineData("Sir Lancelot", "Sir Lancelot")]
        [InlineData("   Spaces   ", "Spaces")]
        [InlineData("", "Unknown")]
        [InlineData("   ", "Unknown")]
        public void SanitizePlayerName_ReturnsExpectedResult(string input, string expected)
        {
            SaveGameService.SanitizePlayerName(input).Should().Be(expected);
        }

        [Fact]
        public void SanitizePlayerName_StripsInvalidFileNameCharacters()
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            var input = $"Player{invalidChars[0]}Name";
            var result = SaveGameService.SanitizePlayerName(input);
            result.Should().Be("PlayerName");
        }

        [Fact]
        public void SanitizePlayerName_HandlesNull()
        {
            SaveGameService.SanitizePlayerName(null!).Should().Be("Unknown");
        }
    }

    public class GenerateSaveNameTests
    {
        [Fact]
        public void GenerateSaveName_ReturnsMapAndPlayerName()
        {
            var result = SaveGameService.GenerateSaveName("mythical-world", "Gandalf", Array.Empty<string>());
            result.Should().Be("mythical-world.Gandalf");
        }

        [Fact]
        public void GenerateSaveName_AppendsNumber_WhenNameExists()
        {
            var existing = new[] { "mythical-world.Gandalf.json" };
            var result = SaveGameService.GenerateSaveName("mythical-world", "Gandalf", existing);
            result.Should().Be("mythical-world.Gandalf-2");
        }

        [Fact]
        public void GenerateSaveName_FindsNextAvailableNumber()
        {
            var existing = new[] { "mythical-world.Gandalf.json", "mythical-world.Gandalf-2.json" };
            var result = SaveGameService.GenerateSaveName("mythical-world", "Gandalf", existing);
            result.Should().Be("mythical-world.Gandalf-3");
        }

        [Fact]
        public void GenerateSaveName_SanitizesPlayerName()
        {
            var result = SaveGameService.GenerateSaveName("mythical-world", "  Gandalf  ", Array.Empty<string>());
            result.Should().Be("mythical-world.Gandalf");
        }

        [Fact]
        public void GenerateSaveName_DoesNotCollideWithDifferentMap()
        {
            var existing = new[] { "other-world.Gandalf.json" };
            var result = SaveGameService.GenerateSaveName("mythical-world", "Gandalf", existing);
            result.Should().Be("mythical-world.Gandalf");
        }
    }
}
