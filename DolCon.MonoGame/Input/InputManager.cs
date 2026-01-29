using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace DolCon.MonoGame.Input;

/// <summary>
/// Manages keyboard input state and provides utilities for detecting key presses.
/// </summary>
public class InputManager
{
    private KeyboardState _currentKeyState;
    private KeyboardState _previousKeyState;

    /// <summary>
    /// Update the input state. Should be called once per frame at the start of Update.
    /// </summary>
    public void Update(GameTime gameTime)
    {
        _previousKeyState = _currentKeyState;
        _currentKeyState = Keyboard.GetState();
    }

    /// <summary>
    /// Returns true if the key was just pressed this frame (was up, now down).
    /// </summary>
    public bool IsKeyPressed(Keys key)
    {
        return _currentKeyState.IsKeyDown(key) && _previousKeyState.IsKeyUp(key);
    }

    /// <summary>
    /// Returns true if the key is currently being held down.
    /// </summary>
    public bool IsKeyDown(Keys key)
    {
        return _currentKeyState.IsKeyDown(key);
    }

    /// <summary>
    /// Returns true if the key was just released this frame (was down, now up).
    /// </summary>
    public bool IsKeyReleased(Keys key)
    {
        return _currentKeyState.IsKeyUp(key) && _previousKeyState.IsKeyDown(key);
    }

    /// <summary>
    /// Returns true if Alt key is held.
    /// </summary>
    public bool IsAltHeld => IsKeyDown(Keys.LeftAlt) || IsKeyDown(Keys.RightAlt);

    /// <summary>
    /// Returns true if Shift key is held.
    /// </summary>
    public bool IsShiftHeld => IsKeyDown(Keys.LeftShift) || IsKeyDown(Keys.RightShift);

    /// <summary>
    /// Returns true if Control key is held.
    /// </summary>
    public bool IsControlHeld => IsKeyDown(Keys.LeftControl) || IsKeyDown(Keys.RightControl);

    /// <summary>
    /// Gets the numeric key that was pressed (0-9), or null if none.
    /// </summary>
    public int? GetPressedNumericKey()
    {
        for (int i = 0; i <= 9; i++)
        {
            if (IsKeyPressed((Keys)(Keys.D0 + i)))
                return i;
        }
        return null;
    }

    /// <summary>
    /// Gets the direction key that was pressed, if any.
    /// Maps both arrow keys and WASD.
    /// </summary>
    public Direction? GetPressedDirection()
    {
        if (IsKeyPressed(Keys.Up) || IsKeyPressed(Keys.W)) return Direction.North;
        if (IsKeyPressed(Keys.Down) || IsKeyPressed(Keys.S)) return Direction.South;
        if (IsKeyPressed(Keys.Left) || IsKeyPressed(Keys.A)) return Direction.West;
        if (IsKeyPressed(Keys.Right) || IsKeyPressed(Keys.D)) return Direction.East;
        return null;
    }

    /// <summary>
    /// Returns true if any key was pressed this frame.
    /// </summary>
    public bool AnyKeyPressed()
    {
        return _currentKeyState.GetPressedKeyCount() > 0 && _previousKeyState.GetPressedKeyCount() == 0;
    }
}

/// <summary>
/// Simple direction enum for input handling.
/// </summary>
public enum Direction
{
    North,
    NorthEast,
    East,
    SouthEast,
    South,
    SouthWest,
    West,
    NorthWest
}
