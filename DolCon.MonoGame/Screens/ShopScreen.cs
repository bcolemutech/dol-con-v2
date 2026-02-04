using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using DolCon.Core.Enums;
using DolCon.Core.Models;
using DolCon.Core.Services;
using DolCon.MonoGame.Input;

namespace DolCon.MonoGame.Screens;

/// <summary>
/// Shop screen for buying, selling, and services.
/// </summary>
public class ShopScreen : ScreenBase
{
    private readonly IShopService _shopService;
    private Scene _scene = new();
    private int _selectedIndex;
    private int _scrollOffset;
    private const int ItemsPerPage = 10;
    private string _message = "";

    public ShopScreen(IShopService shopService)
    {
        _shopService = shopService;
    }

    /// <summary>
    /// Initialize with a scene containing shop data.
    /// </summary>
    public void InitializeWithScene(Scene scene)
    {
        _scene = scene;
        _selectedIndex = 0;
        _scrollOffset = 0;

        try
        {
            // Process the scene to get initial selections
            _scene = _shopService.ProcessShop(_scene);
            _message = _scene.Message ?? "Welcome!";
        }
        catch (Exception ex)
        {
            _message = $"Error loading shop: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"ShopScreen.InitializeWithScene error: {ex}");
        }
    }

    public override void Initialize()
    {
        _selectedIndex = 0;
        _scrollOffset = 0;
    }

    public override void Update(GameTime gameTime, InputManager input)
    {
        // Leave shop
        if (input.IsKeyPressed(Keys.Escape) || input.IsKeyPressed(Keys.L))
        {
            LeaveShop();
            return;
        }

        // Back to service menu (if already selected a service)
        if (input.IsKeyPressed(Keys.B) && _scene.SelectedService != null)
        {
            _scene.SelectedService = null;
            _scene.Selections.Clear();
            _scene = _shopService.ProcessShop(_scene);
            _selectedIndex = 0;
            _scrollOffset = 0;
            _message = "Select a service.";
            return;
        }

        // Navigation
        if (_scene.Selections.Count > 0)
        {
            if (input.IsKeyPressed(Keys.Up) || input.IsKeyPressed(Keys.W))
            {
                _selectedIndex = Math.Max(0, _selectedIndex - 1);
                EnsureVisible();
            }
            else if (input.IsKeyPressed(Keys.Down) || input.IsKeyPressed(Keys.S))
            {
                _selectedIndex = Math.Min(_scene.Selections.Count - 1, _selectedIndex + 1);
                EnsureVisible();
            }
            else if (input.IsKeyPressed(Keys.PageUp))
            {
                _selectedIndex = Math.Max(0, _selectedIndex - ItemsPerPage);
                EnsureVisible();
            }
            else if (input.IsKeyPressed(Keys.PageDown))
            {
                _selectedIndex = Math.Min(_scene.Selections.Count - 1, _selectedIndex + ItemsPerPage);
                EnsureVisible();
            }
            else if (input.IsKeyPressed(Keys.Enter) || input.IsKeyPressed(Keys.Space))
            {
                ProcessSelection();
            }
        }
    }

    private void LeaveShop()
    {
        _scene.IsCompleted = true;
        _scene.Reset();
        ScreenManager.SwitchTo(ScreenType.Navigation);
    }

    private void EnsureVisible()
    {
        if (_selectedIndex < _scrollOffset)
            _scrollOffset = _selectedIndex;
        else if (_selectedIndex >= _scrollOffset + ItemsPerPage)
            _scrollOffset = _selectedIndex - ItemsPerPage + 1;
    }

    private void ProcessSelection()
    {
        if (_scene.Selections.Count == 0) return;

        try
        {
            // Convert 0-based index to 1-based key for scene selection
            _scene.Selection = _selectedIndex + 1;

            // Verify the selection key exists
            if (!_scene.Selections.ContainsKey(_scene.Selection))
            {
                _message = "Invalid selection";
                return;
            }

            _scene = _shopService.ProcessShop(_scene);
            _message = _scene.Message ?? "";

            // Reset selection index if list changed
            if (_selectedIndex >= _scene.Selections.Count)
                _selectedIndex = Math.Max(0, _scene.Selections.Count - 1);

            EnsureVisible();

            // Auto-save after successful purchase
            if (!string.IsNullOrEmpty(_message) && !_message.Contains("don't have enough") && !_message.Contains("not available"))
            {
                SaveHelper.TriggerSave();
            }
        }
        catch (Exception ex)
        {
            _message = $"Error: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"ShopScreen.ProcessSelection error: {ex}");
        }
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        var padding = 20;
        var viewport = GraphicsDevice.Viewport;

        // Title bar
        DrawRect(spriteBatch, new Rectangle(0, 0, viewport.Width, 50), new Color(30, 40, 30));
        var title = _scene.Location?.Name ?? "Shop";
        DrawText(spriteBatch, $"DOMINION OF LIGHT - {title}", new Vector2(padding, 15), Color.Gold);

        // Message area
        if (!string.IsNullOrEmpty(_message))
        {
            // Strip Spectre.Console markup for display
            var cleanMessage = StripMarkup(_message);
            var msgColor = _message.Contains("don't have enough") ? Color.Red : Color.Yellow;
            DrawText(spriteBatch, cleanMessage, new Vector2(padding, 60), msgColor);
        }

        // Player money panel (top-right)
        var player = SaveGameService.Party.Players.FirstOrDefault();
        if (player != null)
        {
            var moneyPanel = new Rectangle(viewport.Width - 250 - padding, 60, 250, 80);
            DrawBorder(spriteBatch, moneyPanel, Color.Gold, 2);
            DrawText(spriteBatch, "Your Coin:", new Vector2(moneyPanel.X + 10, moneyPanel.Y + 10), Color.White);
            DrawText(spriteBatch, $"{player.gold}g {player.silver}s {player.copper}c",
                new Vector2(moneyPanel.X + 10, moneyPanel.Y + 40), Color.Gold);
        }

        // Current service header
        int contentY = 100;
        if (_scene.SelectedService != null)
        {
            DrawText(spriteBatch, $"Service: {_scene.SelectedService}", new Vector2(padding, contentY), Color.Cyan);
            contentY += 30;
        }
        else
        {
            DrawText(spriteBatch, "Select a service:", new Vector2(padding, contentY), Color.White);
            contentY += 30;
        }

        // Selection list panel
        var listPanel = new Rectangle(padding, contentY, viewport.Width - 300 - padding, viewport.Height - 200);
        DrawBorder(spriteBatch, listPanel, Color.DarkGray, 1);

        if (_scene.Selections.Count == 0)
        {
            DrawText(spriteBatch, "No options available", new Vector2(listPanel.X + 10, listPanel.Y + 20), Color.Gray);
        }
        else
        {
            DrawSelectionList(spriteBatch, listPanel);
        }

        // Controls panel (bottom)
        var controlsY = viewport.Height - 80;
        DrawRect(spriteBatch, new Rectangle(0, controlsY, viewport.Width, 80), new Color(30, 30, 50));

        var controls = _scene.SelectedService != null
            ? "[Up/Down] Navigate  [Enter] Select  [B] Back  [L/ESC] Leave"
            : "[Up/Down] Navigate  [Enter] Select  [L/ESC] Leave";
        DrawText(spriteBatch, controls, new Vector2(padding, controlsY + 25), Color.Gray);
    }

    private void DrawSelectionList(SpriteBatch spriteBatch, Rectangle panel)
    {
        int y = panel.Y + 10;
        var selections = _scene.Selections.ToList();
        var visibleItems = selections.Skip(_scrollOffset).Take(ItemsPerPage).ToList();

        // Header row for priced items (when a service is selected)
        if (_scene.SelectedService != null)
        {
            DrawText(spriteBatch, "Item", new Vector2(panel.X + 40, y), Color.Gray);
            DrawText(spriteBatch, "Price", new Vector2(panel.X + 350, y), Color.Gray);
            y += 25;
            DrawRect(spriteBatch, new Rectangle(panel.X + 5, y, panel.Width - 10, 1), Color.DarkGray);
            y += 5;
        }

        for (int i = 0; i < visibleItems.Count; i++)
        {
            var kvp = visibleItems[i];
            var actualIndex = _scrollOffset + i;
            var isSelected = actualIndex == _selectedIndex;
            var selection = kvp.Value;

            if (isSelected)
            {
                DrawRect(spriteBatch, new Rectangle(panel.X + 5, y - 2, panel.Width - 10, 26), new Color(60, 60, 80));
            }

            var prefix = isSelected ? "> " : "  ";
            var canAfford = selection.Afford;
            var color = !canAfford ? Color.Gray : (isSelected ? Color.Yellow : Color.White);

            DrawText(spriteBatch, $"{prefix}{selection.Name}", new Vector2(panel.X + 10, y), color);

            // Show price for service selections
            if (_scene.SelectedService != null)
            {
                var price = Math.Abs(selection.Price);
                var copper = price % 10;
                var silver = price / 10 % 10;
                var gold = price / 100;

                // Negative price means selling (player gains money)
                var isSelling = selection.Price < 0;
                var pricePrefix = isSelling ? "+" : "";
                var priceColor = isSelling ? Color.Green : (canAfford ? Color.Gold : Color.Red);

                DrawText(spriteBatch, $"{pricePrefix}{gold}g {silver}s {copper}c",
                    new Vector2(panel.X + 350, y), priceColor);
            }

            y += 26;
        }

        // Scroll indicator
        if (_scene.Selections.Count > ItemsPerPage)
        {
            var scrollText = $"({_scrollOffset + 1}-{Math.Min(_scrollOffset + ItemsPerPage, _scene.Selections.Count)} of {_scene.Selections.Count})";
            DrawText(spriteBatch, scrollText, new Vector2(panel.Right - 150, panel.Bottom - 25), Color.Gray);
        }
    }

    /// <summary>
    /// Strip Spectre.Console markup tags for plain text display.
    /// </summary>
    private static string StripMarkup(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;

        // Remove [tag] and [/tag] patterns
        var result = text;
        while (result.Contains('['))
        {
            var start = result.IndexOf('[');
            var end = result.IndexOf(']', start);
            if (end > start)
            {
                result = result.Remove(start, end - start + 1);
            }
            else
            {
                break;
            }
        }
        return result;
    }
}
