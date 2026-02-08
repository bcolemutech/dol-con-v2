namespace DolCon.Core.Utilities;

/// <summary>
/// Computes a viewport transform to fit a cluster of cell polygons into a target screen rectangle.
/// Calculates bounding box with padding, uniform scale, and centering offset.
/// </summary>
public class CellClusterViewport
{
    private const double PaddingFactor = 0.05;

    private readonly double _offsetX;
    private readonly double _offsetY;

    /// <summary>
    /// The uniform scale factor applied to world coordinates.
    /// </summary>
    public double Scale { get; }

    /// <summary>
    /// Creates a viewport transform for the given set of world-space vertices
    /// to fit within a target rectangle of the given dimensions.
    /// </summary>
    /// <param name="vertices">All vertex coordinates to include in the bounding box.</param>
    /// <param name="targetWidth">Width of the target screen area in pixels.</param>
    /// <param name="targetHeight">Height of the target screen area in pixels.</param>
    public CellClusterViewport(
        IReadOnlyList<(double X, double Y)> vertices,
        double targetWidth,
        double targetHeight)
    {
        if (vertices.Count == 0)
        {
            Scale = 1;
            _offsetX = targetWidth / 2;
            _offsetY = targetHeight / 2;
            return;
        }

        // Compute axis-aligned bounding box
        double minX = vertices[0].X, maxX = vertices[0].X;
        double minY = vertices[0].Y, maxY = vertices[0].Y;

        for (int i = 1; i < vertices.Count; i++)
        {
            if (vertices[i].X < minX) minX = vertices[i].X;
            if (vertices[i].X > maxX) maxX = vertices[i].X;
            if (vertices[i].Y < minY) minY = vertices[i].Y;
            if (vertices[i].Y > maxY) maxY = vertices[i].Y;
        }

        double bboxWidth = maxX - minX;
        double bboxHeight = maxY - minY;

        // Handle degenerate cases (single point or line)
        if (bboxWidth < 0.001) bboxWidth = 1;
        if (bboxHeight < 0.001) bboxHeight = 1;

        // Add padding
        double padX = bboxWidth * PaddingFactor;
        double padY = bboxHeight * PaddingFactor;
        minX -= padX;
        maxX += padX;
        minY -= padY;
        maxY += padY;
        bboxWidth = maxX - minX;
        bboxHeight = maxY - minY;

        // Uniform scale to fit
        Scale = Math.Min(targetWidth / bboxWidth, targetHeight / bboxHeight);

        // Center the result
        double scaledWidth = bboxWidth * Scale;
        double scaledHeight = bboxHeight * Scale;
        _offsetX = (targetWidth - scaledWidth) / 2 - minX * Scale;
        _offsetY = (targetHeight - scaledHeight) / 2 - minY * Scale;
    }

    /// <summary>
    /// Transforms a world coordinate to screen coordinate within the target rectangle.
    /// </summary>
    public (float ScreenX, float ScreenY) WorldToScreen(double worldX, double worldY)
    {
        return (
            (float)(worldX * Scale + _offsetX),
            (float)(worldY * Scale + _offsetY));
    }
}
