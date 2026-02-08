namespace DolCon.Core.Tests;

using DolCon.Core.Utilities;
using FluentAssertions;

public class CellClusterViewportTests
{
    [Fact]
    public void Constructor_ComputesBoundingBox()
    {
        var vertices = new List<(double X, double Y)>
        {
            (0, 0), (100, 0), (100, 100), (0, 100)
        };

        var viewport = new CellClusterViewport(vertices, 500, 500);

        viewport.Should().NotBeNull();
    }

    [Fact]
    public void WorldToScreen_CenterOfBBox_MapsToCenterOfTarget()
    {
        // 100x100 world box, 500x500 target
        var vertices = new List<(double X, double Y)>
        {
            (0, 0), (100, 0), (100, 100), (0, 100)
        };

        var viewport = new CellClusterViewport(vertices, 500, 500);

        var (sx, sy) = viewport.WorldToScreen(50, 50);

        // Center of world should map to center of target
        sx.Should().BeApproximately(250f, 1f);
        sy.Should().BeApproximately(250f, 1f);
    }

    [Fact]
    public void WorldToScreen_ScalesUniformly_FitsWithinTarget()
    {
        // 200x100 world (wider than tall), 400x400 target
        var vertices = new List<(double X, double Y)>
        {
            (0, 0), (200, 0), (200, 100), (0, 100)
        };

        var viewport = new CellClusterViewport(vertices, 400, 400);

        // All corners should be within the target area
        var topLeft = viewport.WorldToScreen(0, 0);
        var bottomRight = viewport.WorldToScreen(200, 100);

        topLeft.ScreenX.Should().BeGreaterThanOrEqualTo(0);
        topLeft.ScreenY.Should().BeGreaterThanOrEqualTo(0);
        bottomRight.ScreenX.Should().BeLessThanOrEqualTo(400);
        bottomRight.ScreenY.Should().BeLessThanOrEqualTo(400);
    }

    [Fact]
    public void WorldToScreen_WideWorld_ScalesWithPadding()
    {
        // 100x100 world, 500x500 target
        var vertices = new List<(double X, double Y)>
        {
            (0, 0), (100, 0), (100, 100), (0, 100)
        };

        var viewport = new CellClusterViewport(vertices, 500, 500);

        // Top-left of world should NOT be at (0,0) due to padding
        var (sx, sy) = viewport.WorldToScreen(0, 0);
        sx.Should().BeGreaterThan(0);
        sy.Should().BeGreaterThan(0);
    }

    [Fact]
    public void WorldToScreen_RectangularWorld_MaintainsAspectRatio()
    {
        // 200x100 world (2:1 ratio), 400x400 target
        var vertices = new List<(double X, double Y)>
        {
            (0, 0), (200, 0), (200, 100), (0, 100)
        };

        var viewport = new CellClusterViewport(vertices, 400, 400);

        // Width in screen space should be 2x height in screen space
        var topLeft = viewport.WorldToScreen(0, 0);
        var topRight = viewport.WorldToScreen(200, 0);
        var bottomLeft = viewport.WorldToScreen(0, 100);

        var screenWidth = topRight.ScreenX - topLeft.ScreenX;
        var screenHeight = bottomLeft.ScreenY - topLeft.ScreenY;

        (screenWidth / screenHeight).Should().BeApproximately(2f, 0.01f);
    }

    [Fact]
    public void WorldToScreen_SinglePoint_DoesNotCrash()
    {
        var vertices = new List<(double X, double Y)>
        {
            (50, 50)
        };

        var viewport = new CellClusterViewport(vertices, 500, 500);

        // Should map to center of target
        var (sx, sy) = viewport.WorldToScreen(50, 50);
        sx.Should().BeApproximately(250f, 1f);
        sy.Should().BeApproximately(250f, 1f);
    }

    [Fact]
    public void WorldToScreen_WithOffset_ShiftsCorrectly()
    {
        // World at (1000, 2000) to (1100, 2100)
        var vertices = new List<(double X, double Y)>
        {
            (1000, 2000), (1100, 2000), (1100, 2100), (1000, 2100)
        };

        var viewport = new CellClusterViewport(vertices, 500, 500);

        var (sx, sy) = viewport.WorldToScreen(1050, 2050);

        // Center should still map to center of target
        sx.Should().BeApproximately(250f, 1f);
        sy.Should().BeApproximately(250f, 1f);
    }

    [Fact]
    public void Scale_ReturnsComputedScale()
    {
        var vertices = new List<(double X, double Y)>
        {
            (0, 0), (100, 0), (100, 100), (0, 100)
        };

        var viewport = new CellClusterViewport(vertices, 500, 500);

        viewport.Scale.Should().BeGreaterThan(0);
    }
}
