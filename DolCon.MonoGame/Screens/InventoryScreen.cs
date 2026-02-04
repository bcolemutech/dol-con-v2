using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using DolCon.Core.Enums;
using DolCon.Core.Models;
using DolCon.Core.Services;
using DolCon.MonoGame.Input;

namespace DolCon.MonoGame.Screens;

/// <summary>
/// Inventory screen for viewing and managing items.
/// </summary>
public class InventoryScreen : ScreenBase
{
    private int _selectedIndex;
    private int _scrollOffset;
    private const int ItemsPerPage = 15;

    public override void Initialize()
    {
        _selectedIndex = 0;
        _scrollOffset = 0;
    }

    public override void Update(GameTime gameTime, InputManager input)
    {
        var player = GetCurrentPlayer();
        if (player == null) return;

        var itemCount = player.Inventory.Count;

        // Navigation
        if (input.IsKeyPressed(Keys.Up) || input.IsKeyPressed(Keys.W))
        {
            _selectedIndex = Math.Max(0, _selectedIndex - 1);
            EnsureVisible();
        }
        else if (input.IsKeyPressed(Keys.Down) || input.IsKeyPressed(Keys.S))
        {
            _selectedIndex = Math.Min(itemCount - 1, _selectedIndex + 1);
            EnsureVisible();
        }
        else if (input.IsKeyPressed(Keys.PageUp))
        {
            _selectedIndex = Math.Max(0, _selectedIndex - ItemsPerPage);
            EnsureVisible();
        }
        else if (input.IsKeyPressed(Keys.PageDown))
        {
            _selectedIndex = Math.Min(itemCount - 1, _selectedIndex + ItemsPerPage);
            EnsureVisible();
        }

        // Equip/unequip
        if (input.IsKeyPressed(Keys.E) && itemCount > 0)
        {
            var item = player.Inventory[_selectedIndex];
            if (item.Equipment != Equipment.None)
            {
                item.Equipped = !item.Equipped;
            }
        }

        // Drop item
        if (input.IsKeyPressed(Keys.X) && itemCount > 0)
        {
            player.Inventory.RemoveAt(_selectedIndex);
            if (_selectedIndex >= player.Inventory.Count)
            {
                _selectedIndex = Math.Max(0, player.Inventory.Count - 1);
            }
            EnsureVisible();
        }

        // Screen navigation
        if (input.IsKeyPressed(Keys.H))
        {
            ScreenManager.SwitchTo(ScreenType.Home);
        }
        else if (input.IsKeyPressed(Keys.N))
        {
            ScreenManager.SwitchTo(ScreenType.Navigation);
        }
        else if (input.IsKeyPressed(Keys.Escape))
        {
            ScreenManager.SwitchTo(ScreenType.Home);
        }
    }

    private void EnsureVisible()
    {
        if (_selectedIndex < _scrollOffset)
        {
            _scrollOffset = _selectedIndex;
        }
        else if (_selectedIndex >= _scrollOffset + ItemsPerPage)
        {
            _scrollOffset = _selectedIndex - ItemsPerPage + 1;
        }
    }

    private Player? GetCurrentPlayer()
    {
        return SaveGameService.Party.Players.FirstOrDefault(p => p.Id == SaveGameService.CurrentPlayerId);
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        var padding = 20;
        var viewport = GraphicsDevice.Viewport;

        // Title bar
        DrawRect(spriteBatch, new Rectangle(0, 0, viewport.Width, 50), new Color(30, 30, 50));
        DrawText(spriteBatch, "DOMINION OF LIGHT - Inventory", new Vector2(padding, 15), Color.Gold);

        var player = GetCurrentPlayer();
        if (player == null)
        {
            DrawText(spriteBatch, "No player found!", new Vector2(padding, 100), Color.Red);
            return;
        }

        // Player info panel (top-right)
        var infoPanel = new Rectangle(viewport.Width - 300 - padding, 60, 300, 150);
        DrawBorder(spriteBatch, infoPanel, Color.DarkGray, 2);
        DrawText(spriteBatch, player.Name, new Vector2(infoPanel.X + 10, infoPanel.Y + 10), Color.White);
        DrawText(spriteBatch, $"Gold: {player.gold}g {player.silver}s {player.copper}c",
            new Vector2(infoPanel.X + 10, infoPanel.Y + 40), Color.Gold);
        DrawText(spriteBatch, $"Items: {player.Inventory.Count}/50",
            new Vector2(infoPanel.X + 10, infoPanel.Y + 70), Color.LightGray);

        // Inventory list (left side)
        var listPanel = new Rectangle(padding, 60, viewport.Width - 350 - padding, viewport.Height - 180);
        DrawBorder(spriteBatch, listPanel, Color.DarkGray, 2);
        DrawText(spriteBatch, "Inventory", new Vector2(listPanel.X + 10, listPanel.Y + 10), Color.White);

        if (player.Inventory.Count == 0)
        {
            DrawText(spriteBatch, "No items", new Vector2(listPanel.X + 10, listPanel.Y + 50), Color.Gray);
        }
        else
        {
            int y = listPanel.Y + 40;
            var visibleItems = player.Inventory.Skip(_scrollOffset).Take(ItemsPerPage).ToList();

            for (int i = 0; i < visibleItems.Count; i++)
            {
                var item = visibleItems[i];
                var actualIndex = _scrollOffset + i;
                var isSelected = actualIndex == _selectedIndex;

                var bgColor = isSelected ? new Color(60, 60, 80) : Color.Transparent;
                if (isSelected)
                {
                    DrawRect(spriteBatch, new Rectangle(listPanel.X + 5, y - 2, listPanel.Width - 10, 28), bgColor);
                }

                var color = GetRarityColor(item.Rarity);
                var equippedMarker = item.Equipped ? "[E] " : "    ";
                var slotInfo = item.Equipment != Equipment.None ? $" ({item.Equipment})" : "";

                DrawText(spriteBatch, $"{equippedMarker}{item.Name}{slotInfo}",
                    new Vector2(listPanel.X + 10, y), color);

                y += 28;
            }

            // Scroll indicator
            if (player.Inventory.Count > ItemsPerPage)
            {
                var scrollText = $"({_scrollOffset + 1}-{Math.Min(_scrollOffset + ItemsPerPage, player.Inventory.Count)} of {player.Inventory.Count})";
                DrawText(spriteBatch, scrollText,
                    new Vector2(listPanel.Right - 150, listPanel.Bottom - 25), Color.Gray);
            }
        }

        // Item details (bottom-right if item selected)
        if (player.Inventory.Count > 0 && _selectedIndex < player.Inventory.Count)
        {
            var item = player.Inventory[_selectedIndex];
            var detailPanel = new Rectangle(viewport.Width - 300 - padding, 220, 300, 200);
            DrawBorder(spriteBatch, detailPanel, Color.DarkGray, 2);

            int y = detailPanel.Y + 10;
            DrawText(spriteBatch, item.Name, new Vector2(detailPanel.X + 10, y), GetRarityColor(item.Rarity));
            y += 30;
            DrawText(spriteBatch, $"Rarity: {item.Rarity}", new Vector2(detailPanel.X + 10, y), Color.LightGray);
            y += 25;
            if (item.Equipment != Equipment.None)
            {
                DrawText(spriteBatch, $"Slot: {item.Equipment}", new Vector2(detailPanel.X + 10, y), Color.LightGray);
                y += 25;
            }
            DrawText(spriteBatch, $"Value: {item.Price} coins", new Vector2(detailPanel.X + 10, y), Color.Gold);
        }

        // Controls panel (bottom)
        var controlsY = viewport.Height - 80;
        DrawRect(spriteBatch, new Rectangle(0, controlsY, viewport.Width, 80), new Color(30, 30, 50));
        DrawText(spriteBatch, "[E] Equip/Unequip  [X] Drop  [H] Home  [N] Navigation  [ESC] Back",
            new Vector2(padding, controlsY + 25), Color.Gray);
    }

    private Color GetRarityColor(Rarity rarity)
    {
        return rarity switch
        {
            Rarity.Common => Color.White,
            Rarity.Uncommon => Color.LightGreen,
            Rarity.Rare => Color.LightBlue,
            Rarity.Epic => Color.MediumPurple,
            Rarity.Legendary => Color.Orange,
            _ => Color.White
        };
    }
}
