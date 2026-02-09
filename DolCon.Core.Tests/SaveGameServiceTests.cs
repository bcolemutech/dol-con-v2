using DolCon.Core.Services;
using FluentAssertions;

namespace DolCon.Core.Tests;

public class SaveGameServiceTests
{
    public class SanitizeFileComponentTests
    {
        [Theory]
        [InlineData("Gandalf", "Gandalf")]
        [InlineData("Sir Lancelot", "Sir Lancelot")]
        [InlineData("   Spaces   ", "Spaces")]
        [InlineData("", "Unknown")]
        [InlineData("   ", "Unknown")]
        public void SanitizeFileComponent_ReturnsExpectedResult(string input, string expected)
        {
            SaveGameService.SanitizeFileComponent(input).Should().Be(expected);
        }

        [Fact]
        public void SanitizeFileComponent_StripsInvalidFileNameCharacters()
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            var input = $"Player{invalidChars[0]}Name";
            var result = SaveGameService.SanitizeFileComponent(input);
            result.Should().Be("PlayerName");
        }

        [Fact]
        public void SanitizeFileComponent_HandlesNull()
        {
            SaveGameService.SanitizeFileComponent(null!).Should().Be("Unknown");
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
        public void GenerateSaveName_SanitizesMapName()
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            var result = SaveGameService.GenerateSaveName($"my{invalidChars[0]}map", "Gandalf", Array.Empty<string>());
            result.Should().Be("mymap.Gandalf");
        }

        [Fact]
        public void GenerateSaveName_DoesNotCollideWithDifferentMap()
        {
            var existing = new[] { "other-world.Gandalf.json" };
            var result = SaveGameService.GenerateSaveName("mythical-world", "Gandalf", existing);
            result.Should().Be("mythical-world.Gandalf");
        }

        [Fact]
        public void GenerateSaveName_DetectsCaseInsensitiveCollision()
        {
            var existing = new[] { "mythical-world.gandalf.json" };
            var result = SaveGameService.GenerateSaveName("mythical-world", "Gandalf", existing);
            result.Should().Be("mythical-world.Gandalf-2");
        }

        [Fact]
        public void GenerateSaveName_DetectsCaseInsensitiveCollisionWithSuffix()
        {
            var existing = new[] { "Mythical-World.Gandalf.json", "mythical-world.gandalf-2.json" };
            var result = SaveGameService.GenerateSaveName("mythical-world", "Gandalf", existing);
            result.Should().Be("mythical-world.Gandalf-3");
        }
    }

    public class FormatSaveDisplayNameTests
    {
        [Fact]
        public void FormatSaveDisplayName_ShowsPlayerAndMap()
        {
            var result = SaveGameService.FormatSaveDisplayName("mythical-world.Gandalf.json");
            result.Should().Be("Gandalf (mythical-world)");
        }

        [Fact]
        public void FormatSaveDisplayName_HandlesNoDelimiter()
        {
            var result = SaveGameService.FormatSaveDisplayName("oldsave.json");
            result.Should().Be("oldsave");
        }

        [Fact]
        public void FormatSaveDisplayName_HandlesSuffixedName()
        {
            var result = SaveGameService.FormatSaveDisplayName("mythical-world.Gandalf-2.json");
            result.Should().Be("Gandalf-2 (mythical-world)");
        }
    }
}
