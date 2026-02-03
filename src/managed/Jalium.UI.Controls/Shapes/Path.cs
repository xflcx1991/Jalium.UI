using Jalium.UI.Media;

namespace Jalium.UI.Controls.Shapes;

/// <summary>
/// Draws a series of connected lines and curves.
/// </summary>
public class Path : Shape
{
    #region Dependency Properties

    /// <summary>
    /// Identifies the Data dependency property.
    /// </summary>
    public static readonly DependencyProperty DataProperty =
        DependencyProperty.Register(nameof(Data), typeof(string), typeof(Path),
            new PropertyMetadata(null, OnDataChanged));

    #endregion

    #region CLR Properties

    /// <summary>
    /// Gets or sets the geometry data in mini-language format.
    /// Supported commands: M (moveto), L (lineto), Z (close path)
    /// Example: "M 0,5 L 3,8 L 8,2" draws a checkmark
    /// </summary>
    public string? Data
    {
        get => (string?)GetValue(DataProperty);
        set => SetValue(DataProperty, value);
    }

    #endregion

    #region Private Fields

    private PathData? _parsedData;

    #endregion

    #region Layout

    /// <inheritdoc />
    protected override Size MeasureOverride(Size availableSize)
    {
        // Get the natural bounds of the path geometry
        var naturalSize = _parsedData?.Bounds.Size ?? new Size(0, 0);

        // Account for stroke thickness in the natural size
        if (StrokeThickness > 0 && _parsedData != null)
        {
            naturalSize = new Size(
                naturalSize.Width + StrokeThickness,
                naturalSize.Height + StrokeThickness);
        }

        if (Stretch == Stretch.None)
        {
            // Use natural geometry size
            return naturalSize;
        }

        // For other stretch modes, prefer explicit Width/Height if set
        var width = double.IsNaN(Width) ? availableSize.Width : Width;
        var height = double.IsNaN(Height) ? availableSize.Height : Height;

        // If available size is infinite, fall back to natural geometry size
        if (double.IsPositiveInfinity(width))
        {
            width = naturalSize.Width;
        }
        if (double.IsPositiveInfinity(height))
        {
            height = naturalSize.Height;
        }

        return new Size(width, height);
    }

    #endregion

    #region Rendering

    /// <inheritdoc />
    protected override void OnRender(object drawingContext)
    {
        if (drawingContext is not DrawingContext dc || _parsedData == null)
            return;

        var width = RenderSize.Width;
        var height = RenderSize.Height;

        if (width <= 0 || height <= 0)
            return;

        Pen? pen = null;
        if (Stroke != null && StrokeThickness > 0)
        {
            pen = new Pen(Stroke, StrokeThickness)
            {
                StartLineCap = StrokeStartLineCap,
                EndLineCap = StrokeEndLineCap,
                LineJoin = StrokeLineJoin
            };
        }

        // Calculate scaling based on stretch mode
        var scaleX = 1.0;
        var scaleY = 1.0;
        var offsetX = 0.0;
        var offsetY = 0.0;

        var bounds = _parsedData.Bounds;
        if (bounds.Width > 0 && bounds.Height > 0)
        {
            switch (Stretch)
            {
                case Stretch.Fill:
                    scaleX = width / bounds.Width;
                    scaleY = height / bounds.Height;
                    break;
                case Stretch.Uniform:
                    var scale = Math.Min(width / bounds.Width, height / bounds.Height);
                    scaleX = scaleY = scale;
                    offsetX = (width - bounds.Width * scale) / 2;
                    offsetY = (height - bounds.Height * scale) / 2;
                    break;
                case Stretch.UniformToFill:
                    var scaleFill = Math.Max(width / bounds.Width, height / bounds.Height);
                    scaleX = scaleY = scaleFill;
                    offsetX = (width - bounds.Width * scaleFill) / 2;
                    offsetY = (height - bounds.Height * scaleFill) / 2;
                    break;
            }
        }

        // If we have Fill, use PathGeometry for proper filling
        if (Fill != null && _parsedData.Segments.Count > 0)
        {
            var pathGeometry = new PathGeometry();
            pathGeometry.FillRule = _parsedData.FillRule;
            var figure = new PathFigure();

            // Get the first point as start
            var firstSegment = _parsedData.Segments[0];
            var startPoint = TransformPoint(firstSegment.Start, scaleX, scaleY, offsetX, offsetY, bounds);
            figure.StartPoint = startPoint;
            figure.IsFilled = true;
            figure.IsClosed = _parsedData.IsClosed;

            // Add all segments
            foreach (var segment in _parsedData.Segments)
            {
                var endPoint = TransformPoint(segment.End, scaleX, scaleY, offsetX, offsetY, bounds);
                figure.Segments.Add(new LineSegment { Point = endPoint });
            }

            pathGeometry.Figures.Add(figure);
            dc.DrawGeometry(Fill, pen, pathGeometry);
        }
        else
        {
            // Draw the path segments as lines (stroke only)
            foreach (var segment in _parsedData.Segments)
            {
                if (segment.Type == InternalPathSegmentType.Line)
                {
                    var start = TransformPoint(segment.Start, scaleX, scaleY, offsetX, offsetY, bounds);
                    var end = TransformPoint(segment.End, scaleX, scaleY, offsetX, offsetY, bounds);
                    dc.DrawLine(pen, start, end);
                }
            }
        }
    }

    private static Point TransformPoint(Point p, double scaleX, double scaleY, double offsetX, double offsetY, Rect bounds)
    {
        return new Point(
            p.X * scaleX + offsetX - bounds.X * scaleX,
            p.Y * scaleY + offsetY - bounds.Y * scaleY);
    }

    #endregion

    #region Property Changed

    private static void OnDataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is Path path)
        {
            path._parsedData = PathData.Parse((string?)e.NewValue);
            path.InvalidateMeasure();
            path.InvalidateVisual();
        }
    }

    #endregion

    #region Path Parsing

    private class PathData
    {
        public List<InternalPathSegment> Segments { get; } = new();
        public Rect Bounds { get; private set; }
        public bool IsClosed { get; private set; }
        public FillRule FillRule { get; private set; } = FillRule.EvenOdd;

        public static PathData? Parse(string? data)
        {
            if (string.IsNullOrWhiteSpace(data))
                return null;

            var result = new PathData();
            var currentPoint = new Point(0, 0);
            var startPoint = new Point(0, 0);
            var minX = double.MaxValue;
            var minY = double.MaxValue;
            var maxX = double.MinValue;
            var maxY = double.MinValue;

            void UpdateBounds(Point p)
            {
                minX = Math.Min(minX, p.X);
                minY = Math.Min(minY, p.Y);
                maxX = Math.Max(maxX, p.X);
                maxY = Math.Max(maxY, p.Y);
            }

            // Tokenize the path data - split commands from numbers
            var tokens = Tokenize(data);
            var i = 0;
            char lastCommand = 'M';

            while (i < tokens.Count)
            {
                var token = tokens[i];
                char cmd;

                // Check if token is a command letter
                if (token.Length == 1 && char.IsLetter(token[0]))
                {
                    cmd = char.ToUpperInvariant(token[0]);
                    lastCommand = cmd;
                    i++;
                }
                else
                {
                    // Implicit command - use last command (L after M)
                    cmd = lastCommand == 'M' ? 'L' : lastCommand;
                }

                switch (cmd)
                {
                    case 'F': // FillRule: F0 = EvenOdd, F1 = Nonzero
                        if (i < tokens.Count && int.TryParse(tokens[i], out var fillRule))
                        {
                            result.FillRule = fillRule == 1 ? FillRule.Nonzero : FillRule.EvenOdd;
                            i++;
                        }
                        break;

                    case 'M': // MoveTo
                        if (i + 1 < tokens.Count &&
                            double.TryParse(tokens[i], out var mx) &&
                            double.TryParse(tokens[i + 1], out var my))
                        {
                            currentPoint = new Point(mx, my);
                            startPoint = currentPoint;
                            UpdateBounds(currentPoint);
                            i += 2;
                            lastCommand = 'L'; // Subsequent coordinates are LineTo
                        }
                        break;

                    case 'L': // LineTo
                        if (i + 1 < tokens.Count &&
                            double.TryParse(tokens[i], out var lx) &&
                            double.TryParse(tokens[i + 1], out var ly))
                        {
                            var endPoint = new Point(lx, ly);
                            result.Segments.Add(new InternalPathSegment(InternalPathSegmentType.Line, currentPoint, endPoint));
                            UpdateBounds(endPoint);
                            currentPoint = endPoint;
                            i += 2;
                        }
                        break;

                    case 'H': // Horizontal LineTo
                        if (i < tokens.Count && double.TryParse(tokens[i], out var hx))
                        {
                            var endPoint = new Point(hx, currentPoint.Y);
                            result.Segments.Add(new InternalPathSegment(InternalPathSegmentType.Line, currentPoint, endPoint));
                            UpdateBounds(endPoint);
                            currentPoint = endPoint;
                            i++;
                        }
                        break;

                    case 'V': // Vertical LineTo
                        if (i < tokens.Count && double.TryParse(tokens[i], out var vy))
                        {
                            var endPoint = new Point(currentPoint.X, vy);
                            result.Segments.Add(new InternalPathSegment(InternalPathSegmentType.Line, currentPoint, endPoint));
                            UpdateBounds(endPoint);
                            currentPoint = endPoint;
                            i++;
                        }
                        break;

                    case 'Z': // ClosePath
                        if (currentPoint.X != startPoint.X || currentPoint.Y != startPoint.Y)
                        {
                            result.Segments.Add(new InternalPathSegment(InternalPathSegmentType.Line, currentPoint, startPoint));
                        }
                        currentPoint = startPoint;
                        result.IsClosed = true;
                        break;

                    default:
                        i++; // Skip unknown command
                        break;
                }
            }

            if (minX < double.MaxValue && minY < double.MaxValue)
            {
                result.Bounds = new Rect(minX, minY, maxX - minX, maxY - minY);
            }

            return result;
        }

        /// <summary>
        /// Tokenizes SVG path data into commands and numbers.
        /// Handles formats like "M0,0 L6,6" where commands are adjacent to numbers.
        /// </summary>
        private static List<string> Tokenize(string data)
        {
            var tokens = new List<string>();
            var current = new System.Text.StringBuilder();

            for (int i = 0; i < data.Length; i++)
            {
                char c = data[i];

                if (char.IsLetter(c))
                {
                    // Flush current number if any
                    if (current.Length > 0)
                    {
                        tokens.Add(current.ToString());
                        current.Clear();
                    }
                    // Add command letter as separate token
                    tokens.Add(c.ToString());
                }
                else if (char.IsDigit(c) || c == '.')
                {
                    current.Append(c);
                }
                else if (c == '-')
                {
                    // Minus can be start of negative number or separator
                    if (current.Length > 0)
                    {
                        tokens.Add(current.ToString());
                        current.Clear();
                    }
                    current.Append(c);
                }
                else if (c == ' ' || c == ',' || c == '\t' || c == '\n' || c == '\r')
                {
                    // Whitespace/comma separator
                    if (current.Length > 0)
                    {
                        tokens.Add(current.ToString());
                        current.Clear();
                    }
                }
            }

            // Flush remaining
            if (current.Length > 0)
            {
                tokens.Add(current.ToString());
            }

            return tokens;
        }
    }

    private enum InternalPathSegmentType
    {
        Line
    }

    private record InternalPathSegment(InternalPathSegmentType Type, Point Start, Point End);

    #endregion
}
