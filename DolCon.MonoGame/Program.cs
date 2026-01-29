using DolCon.Core.Data;
using DolCon.MonoGame;

// Initialize enemy index at startup
EnemyIndex.Initialize();

using var game = new Game1();
game.Run();
