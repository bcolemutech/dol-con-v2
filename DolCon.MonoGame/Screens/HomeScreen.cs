using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using DolCon.Core.Services;
using DolCon.MonoGame.Input;

namespace DolCon.MonoGame.Screens;

/// <summary>
/// Home screen showing party status, location, and navigation options.
/// </summary>
public class HomeScreen : ScreenBase
{
    public override void Update(GameTime gameTime, InputManager input)
    {
        // Screen navigation
        if (input.IsKeyPressed(Keys.N))
        {
            ScreenManager.SwitchTo(ScreenType.Navigation);
        }
        else if (input.IsKeyPressed(Keys.I))
        {
            ScreenManager.SwitchTo(ScreenType.Inventory);
        }
        else if (input.IsKeyPressed(Keys.L))
        {
            ScreenManager.SwitchTo(ScreenType.Location);
        }
        else if (input.IsKeyPressed(Keys.M))
        {
            ScreenManager.SwitchTo(ScreenType.WorldMap);
        }
        else if (input.IsKeyPressed(Keys.Escape))
        {
            ScreenManager.SwitchTo(ScreenType.MainMenu);
        }
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        var padding = 20;
        var viewport = GraphicsDevice.Viewport;

        // Title bar
        DrawRect(spriteBatch, new Rectangle(0, 0, viewport.Width, 50), new Color(30, 30, 50));
        DrawText(spriteBatch, "DOMINION OF LIGHT - Home", new Vector2(padding, 15), Color.Gold);

        // Location panel (top-right)
        var locationPanel = new Rectangle(viewport.Width - 300 - padding, 60, 300, 200);
        DrawBorder(spriteBatch, locationPanel, Color.DarkGray, 2);
        DrawText(spriteBatch, "Current Location", new Vector2(locationPanel.X + 10, locationPanel.Y + 10), Color.White);

        var cell = SaveGameService.CurrentCell;
        var burg = SaveGameService.CurrentBurg;
        var location = SaveGameService.CurrentLocation;
        var biome = SaveGameService.CurrentBiome;

        int y = locationPanel.Y + 40;
        DrawText(spriteBatch, $"Cell: {cell.i}", new Vector2(locationPanel.X + 10, y), Color.LightGray);
        y += 25;
        DrawText(spriteBatch, $"Biome: {biome}", new Vector2(locationPanel.X + 10, y), Color.LightGray);
        y += 25;

        // Show burg info - either the one we're in, or one nearby in the cell
        if (burg != null)
        {
            DrawText(spriteBatch, $"In Burg: {burg.name} ({burg.size})", new Vector2(locationPanel.X + 10, y), Color.LightBlue);
            y += 25;
        }
        else
        {
            // Check if there's a burg in this cell we could enter
            var cellBurg = SaveGameService.GetBurg(cell.burg);
            if (cellBurg != null)
            {
                DrawText(spriteBatch, $"Nearby: {cellBurg.name} ({cellBurg.size})", new Vector2(locationPanel.X + 10, y), Color.Cyan);
                y += 25;
            }
        }

        if (location != null)
        {
            DrawText(spriteBatch, $"Location: {location.Name}", new Vector2(locationPanel.X + 10, y), Color.LightGreen);
        }

        // Party status panel (left side)
        var partyPanel = new Rectangle(padding, 60, 300, 300);
        DrawBorder(spriteBatch, partyPanel, Color.DarkGray, 2);
        DrawText(spriteBatch, "Party Status", new Vector2(partyPanel.X + 10, partyPanel.Y + 10), Color.White);

        var party = SaveGameService.Party;
        y = partyPanel.Y + 40;

        // Stamina bar
        DrawText(spriteBatch, $"Stamina: {party.Stamina:P0}", new Vector2(partyPanel.X + 10, y), Color.LightGray);
        y += 25;
        var staminaBarBg = new Rectangle(partyPanel.X + 10, y, 200, 20);
        var staminaBar = new Rectangle(partyPanel.X + 10, y, (int)(200 * party.Stamina), 20);
        DrawRect(spriteBatch, staminaBarBg, Color.DarkGray);
        DrawRect(spriteBatch, staminaBar, Color.Green);
        y += 35;

        // Player info
        var player = party.Players.FirstOrDefault(p => p.Id == SaveGameService.CurrentPlayerId);
        if (player != null)
        {
            DrawText(spriteBatch, $"Player: {player.Name}", new Vector2(partyPanel.X + 10, y), Color.LightGray);
            y += 25;
            DrawText(spriteBatch, $"Gold: {player.gold}g {player.silver}s {player.copper}c",
                new Vector2(partyPanel.X + 10, y), Color.Gold);
            y += 25;
            DrawText(spriteBatch, $"Inventory: {player.Inventory.Count}/50 items",
                new Vector2(partyPanel.X + 10, y), Color.LightGray);
        }

        // Controls panel (bottom)
        var controlsY = viewport.Height - 80;
        DrawRect(spriteBatch, new Rectangle(0, controlsY, viewport.Width, 80), new Color(30, 30, 50));
        DrawText(spriteBatch, "[N] Navigation  [L] Locations  [I] Inventory  [M] World Map  [ESC] Main Menu",
            new Vector2(padding, controlsY + 25), Color.Gray);
    }
}
