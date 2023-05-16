namespace DolCon.Tests;

using Enums;
using FluentAssertions;

public class GameServiceTests
{
    [Fact]
    public void EscapeKeyParsesToExitScreen()
    {
        var result = (Screen)ConsoleKey.E;

        result.Should().Be(Screen.Exit);
    }
    
    [Fact]
    public void HKeyParsesToHomeScreen()
    {
        var result = (Screen)ConsoleKey.H;

        result.Should().Be(Screen.Home);
    }
    
    [Fact]
    public void NKeyParsesToNavigationScreen()
    {
        var result = (Screen)ConsoleKey.N;

        result.Should().Be(Screen.Navigation);
    }
    
    [Fact]
    public void IKeyParsesToInventoryScreen()
    {
        var result = (Screen)ConsoleKey.I;

        result.Should().Be(Screen.Inventory);
    }
    
    [Fact]
    public void CKeyParsesToCharacterScreen()
    {
        var result = (Screen)ConsoleKey.C;

        result.Should().Be(Screen.Character);
    }
    
    [Fact]
    public void QKeyParsesToQuestsScreen()
    {
        var result = (Screen)ConsoleKey.Q;

        result.Should().Be(Screen.Quests);
    }
}
