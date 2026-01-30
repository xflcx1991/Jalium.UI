using Jalium.UI.Input;
using Jalium.UI.Media;

namespace Jalium.UI.Controls;

/// <summary>
/// Represents a control that provides a draggable divider between two areas.
/// Used with Grid to create resizable panels.
/// </summary>
public class GridSplitter : Control
{
    #region Dependency Properties

    /// <summary>
    /// Identifies the ResizeDirection dependency property.
    /// </summary>
    public static readonly DependencyProperty ResizeDirectionProperty =
        DependencyProperty.Register(nameof(ResizeDirection), typeof(GridResizeDirection), typeof(GridSplitter),
            new PropertyMetadata(GridResizeDirection.Auto, OnResizeDirectionChanged));

    /// <summary>
    /// Identifies the ResizeBehavior dependency property.
    /// </summary>
    public static readonly DependencyProperty ResizeBehaviorProperty =
        DependencyProperty.Register(nameof(ResizeBehavior), typeof(GridResizeBehavior), typeof(GridSplitter),
            new PropertyMetadata(GridResizeBehavior.BasedOnAlignment));

    /// <summary>
    /// Identifies the ShowsPreview dependency property.
    /// </summary>
    public static readonly DependencyProperty ShowsPreviewProperty =
        DependencyProperty.Register(nameof(ShowsPreview), typeof(bool), typeof(GridSplitter),
            new PropertyMetadata(false));

    /// <summary>
    /// Identifies the PreviewStyle dependency property.
    /// </summary>
    public static readonly DependencyProperty PreviewStyleProperty =
        DependencyProperty.Register(nameof(PreviewStyle), typeof(Style), typeof(GridSplitter),
            new PropertyMetadata(null));

    /// <summary>
    /// Identifies the DragIncrement dependency property.
    /// </summary>
    public static readonly DependencyProperty DragIncrementProperty =
        DependencyProperty.Register(nameof(DragIncrement), typeof(double), typeof(GridSplitter),
            new PropertyMetadata(1.0));

    /// <summary>
    /// Identifies the KeyboardIncrement dependency property.
    /// </summary>
    public static readonly DependencyProperty KeyboardIncrementProperty =
        DependencyProperty.Register(nameof(KeyboardIncrement), typeof(double), typeof(GridSplitter),
            new PropertyMetadata(10.0));

    #endregion

    #region CLR Properties

    /// <summary>
    /// Gets or sets the direction in which the splitter resizes.
    /// </summary>
    public GridResizeDirection ResizeDirection
    {
        get => (GridResizeDirection)(GetValue(ResizeDirectionProperty) ?? GridResizeDirection.Auto);
        set => SetValue(ResizeDirectionProperty, value);
    }

    /// <summary>
    /// Gets or sets how the splitter resizes adjacent rows/columns.
    /// </summary>
    public GridResizeBehavior ResizeBehavior
    {
        get => (GridResizeBehavior)(GetValue(ResizeBehaviorProperty) ?? GridResizeBehavior.BasedOnAlignment);
        set => SetValue(ResizeBehaviorProperty, value);
    }

    /// <summary>
    /// Gets or sets whether a preview is shown during drag.
    /// </summary>
    public bool ShowsPreview
    {
        get => (bool)(GetValue(ShowsPreviewProperty) ?? false);
        set => SetValue(ShowsPreviewProperty, value);
    }

    /// <summary>
    /// Gets or sets the style for the preview.
    /// </summary>
    public Style? PreviewStyle
    {
        get => (Style?)GetValue(PreviewStyleProperty);
        set => SetValue(PreviewStyleProperty, value);
    }

    /// <summary>
    /// Gets or sets the minimum drag increment.
    /// </summary>
    public double DragIncrement
    {
        get => (double)(GetValue(DragIncrementProperty) ?? 1.0);
        set => SetValue(DragIncrementProperty, value);
    }

    /// <summary>
    /// Gets or sets the keyboard movement increment.
    /// </summary>
    public double KeyboardIncrement
    {
        get => (double)(GetValue(KeyboardIncrementProperty) ?? 10.0);
        set => SetValue(KeyboardIncrementProperty, value);
    }

    #endregion

    #region Private Fields

    private bool _isDragging;
    private Point _dragStartPoint;
    private double _originalDimension1;
    private double _originalDimension2;
    private GridResizeDirection _effectiveResizeDirection;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="GridSplitter"/> class.
    /// </summary>
    public GridSplitter()
    {
        Focusable = true;
        Background = new SolidColorBrush(Color.FromRgb(60, 60, 60));
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;

        AddHandler(MouseDownEvent, new RoutedEventHandler(OnMouseDownHandler));
        AddHandler(MouseUpEvent, new RoutedEventHandler(OnMouseUpHandler));
        AddHandler(MouseMoveEvent, new RoutedEventHandler(OnMouseMoveHandler));
        AddHandler(KeyDownEvent, new RoutedEventHandler(OnKeyDownHandler));
    }

    #endregion

    #region Input Handling

    private void OnMouseDownHandler(object sender, RoutedEventArgs e)
    {
        if (!IsEnabled) return;

        if (e is MouseButtonEventArgs mouseArgs && mouseArgs.ChangedButton == MouseButton.Left)
        {
            Focus();
            StartDrag(mouseArgs.GetPosition(this));
            e.Handled = true;
        }
    }

    private void OnMouseUpHandler(object sender, RoutedEventArgs e)
    {
        if (e is MouseButtonEventArgs mouseArgs && mouseArgs.ChangedButton == MouseButton.Left)
        {
            if (_isDragging)
            {
                FinishDrag();
            }
            e.Handled = true;
        }
    }

    private void OnMouseMoveHandler(object sender, RoutedEventArgs e)
    {
        if (_isDragging && e is MouseEventArgs mouseArgs)
        {
            UpdateDrag(mouseArgs.GetPosition(this));
            e.Handled = true;
        }
    }

    private void OnKeyDownHandler(object sender, RoutedEventArgs e)
    {
        if (!IsEnabled) return;

        if (e is KeyEventArgs keyArgs)
        {
            var delta = 0.0;
            var isHorizontal = GetEffectiveResizeDirection() == GridResizeDirection.Columns;

            switch (keyArgs.Key)
            {
                case Key.Left when isHorizontal:
                    delta = -KeyboardIncrement;
                    break;
                case Key.Right when isHorizontal:
                    delta = KeyboardIncrement;
                    break;
                case Key.Up when !isHorizontal:
                    delta = -KeyboardIncrement;
                    break;
                case Key.Down when !isHorizontal:
                    delta = KeyboardIncrement;
                    break;
            }

            if (delta != 0)
            {
                ApplyResize(delta);
                e.Handled = true;
            }
        }
    }

    /// <inheritdoc />
    protected override void OnLostMouseCapture()
    {
        base.OnLostMouseCapture();
        if (_isDragging)
        {
            FinishDrag();
        }
    }

    #endregion

    #region Drag Operations

    private void StartDrag(Point position)
    {
        CaptureMouse();
        _isDragging = true;
        _dragStartPoint = position;
        _effectiveResizeDirection = GetEffectiveResizeDirection();

        // Store original dimensions
        var grid = GetParentGrid();
        if (grid != null)
        {
            var (index1, index2) = GetResizeIndices();
            if (_effectiveResizeDirection == GridResizeDirection.Columns)
            {
                if (index1 >= 0 && index1 < grid.ColumnDefinitions.Count)
                    _originalDimension1 = grid.ColumnDefinitions[index1].ActualWidth;
                if (index2 >= 0 && index2 < grid.ColumnDefinitions.Count)
                    _originalDimension2 = grid.ColumnDefinitions[index2].ActualWidth;
            }
            else
            {
                if (index1 >= 0 && index1 < grid.RowDefinitions.Count)
                    _originalDimension1 = grid.RowDefinitions[index1].ActualHeight;
                if (index2 >= 0 && index2 < grid.RowDefinitions.Count)
                    _originalDimension2 = grid.RowDefinitions[index2].ActualHeight;
            }
        }

        InvalidateVisual();
    }

    private void UpdateDrag(Point position)
    {
        var delta = _effectiveResizeDirection == GridResizeDirection.Columns
            ? position.X - _dragStartPoint.X
            : position.Y - _dragStartPoint.Y;

        // Apply drag increment
        if (DragIncrement > 1)
        {
            delta = Math.Round(delta / DragIncrement) * DragIncrement;
        }

        ApplyResize(delta);
    }

    private void FinishDrag()
    {
        ReleaseMouseCapture();
        _isDragging = false;
        InvalidateVisual();
    }

    private void ApplyResize(double delta)
    {
        var grid = GetParentGrid();
        if (grid == null) return;

        var (index1, index2) = GetResizeIndices();

        if (_effectiveResizeDirection == GridResizeDirection.Columns)
        {
            if (index1 >= 0 && index1 < grid.ColumnDefinitions.Count &&
                index2 >= 0 && index2 < grid.ColumnDefinitions.Count)
            {
                var def1 = grid.ColumnDefinitions[index1];
                var def2 = grid.ColumnDefinitions[index2];

                var newWidth1 = Math.Max(def1.MinWidth, _originalDimension1 + delta);
                var newWidth2 = Math.Max(def2.MinWidth, _originalDimension2 - delta);

                if (newWidth1 >= def1.MinWidth && newWidth2 >= def2.MinWidth)
                {
                    def1.Width = new GridLength(newWidth1, GridUnitType.Pixel);
                    def2.Width = new GridLength(newWidth2, GridUnitType.Pixel);
                }
            }
        }
        else
        {
            if (index1 >= 0 && index1 < grid.RowDefinitions.Count &&
                index2 >= 0 && index2 < grid.RowDefinitions.Count)
            {
                var def1 = grid.RowDefinitions[index1];
                var def2 = grid.RowDefinitions[index2];

                var newHeight1 = Math.Max(def1.MinHeight, _originalDimension1 + delta);
                var newHeight2 = Math.Max(def2.MinHeight, _originalDimension2 - delta);

                if (newHeight1 >= def1.MinHeight && newHeight2 >= def2.MinHeight)
                {
                    def1.Height = new GridLength(newHeight1, GridUnitType.Pixel);
                    def2.Height = new GridLength(newHeight2, GridUnitType.Pixel);
                }
            }
        }

        grid.InvalidateMeasure();
    }

    #endregion

    #region Helper Methods

    private Grid? GetParentGrid()
    {
        var parent = VisualParent;
        while (parent != null)
        {
            if (parent is Grid grid)
                return grid;
            parent = (parent as Visual)?.VisualParent;
        }
        return null;
    }

    private GridResizeDirection GetEffectiveResizeDirection()
    {
        if (ResizeDirection != GridResizeDirection.Auto)
            return ResizeDirection;

        // Auto: determine from dimensions
        if (ActualWidth <= ActualHeight)
            return GridResizeDirection.Columns;
        else
            return GridResizeDirection.Rows;
    }

    private (int Index1, int Index2) GetResizeIndices()
    {
        var grid = GetParentGrid();
        if (grid == null) return (-1, -1);

        var col = Grid.GetColumn(this);
        var row = Grid.GetRow(this);

        switch (ResizeBehavior)
        {
            case GridResizeBehavior.PreviousAndNext:
                return _effectiveResizeDirection == GridResizeDirection.Columns
                    ? (col - 1, col + 1)
                    : (row - 1, row + 1);

            case GridResizeBehavior.PreviousAndCurrent:
                return _effectiveResizeDirection == GridResizeDirection.Columns
                    ? (col - 1, col)
                    : (row - 1, row);

            case GridResizeBehavior.CurrentAndNext:
                return _effectiveResizeDirection == GridResizeDirection.Columns
                    ? (col, col + 1)
                    : (row, row + 1);

            case GridResizeBehavior.BasedOnAlignment:
            default:
                // Based on alignment
                if (_effectiveResizeDirection == GridResizeDirection.Columns)
                {
                    return HorizontalAlignment == HorizontalAlignment.Left
                        ? (col - 1, col)
                        : (col, col + 1);
                }
                else
                {
                    return VerticalAlignment == VerticalAlignment.Top
                        ? (row - 1, row)
                        : (row, row + 1);
                }
        }
    }

    #endregion

    #region Layout

    /// <inheritdoc />
    protected override Size MeasureOverride(Size availableSize)
    {
        var direction = GetEffectiveResizeDirection();
        if (direction == GridResizeDirection.Columns)
        {
            var width = double.IsNaN(Width) || Width <= 0 ? 5 : Width;
            return new Size(width, double.IsPositiveInfinity(availableSize.Height) ? 1 : availableSize.Height);
        }
        else
        {
            var height = double.IsNaN(Height) || Height <= 0 ? 5 : Height;
            return new Size(double.IsPositiveInfinity(availableSize.Width) ? 1 : availableSize.Width, height);
        }
    }

    #endregion

    #region Rendering

    /// <inheritdoc />
    protected override void OnRender(object drawingContext)
    {
        if (drawingContext is not DrawingContext dc)
            return;

        var rect = new Rect(RenderSize);

        // Draw background
        var bgBrush = _isDragging
            ? new SolidColorBrush(Color.FromRgb(0, 120, 212))
            : (Background ?? new SolidColorBrush(Color.FromRgb(60, 60, 60)));

        dc.DrawRectangle(bgBrush, null, rect);

        // Draw grip dots
        var direction = GetEffectiveResizeDirection();
        DrawGripDots(dc, rect, direction == GridResizeDirection.Columns);
    }

    private void DrawGripDots(DrawingContext dc, Rect rect, bool isVertical)
    {
        var dotBrush = new SolidColorBrush(Color.FromRgb(120, 120, 120));
        var dotSize = 2;
        var spacing = 4;
        var dotCount = 3;

        var centerX = rect.Width / 2;
        var centerY = rect.Height / 2;

        if (isVertical)
        {
            // Vertical splitter (horizontal resize) - draw dots vertically
            var totalHeight = (dotCount - 1) * spacing;
            var startY = centerY - totalHeight / 2;

            for (int i = 0; i < dotCount; i++)
            {
                var y = startY + i * spacing;
                dc.DrawEllipse(dotBrush, null, new Point(centerX, y), dotSize / 2, dotSize / 2);
            }
        }
        else
        {
            // Horizontal splitter (vertical resize) - draw dots horizontally
            var totalWidth = (dotCount - 1) * spacing;
            var startX = centerX - totalWidth / 2;

            for (int i = 0; i < dotCount; i++)
            {
                var x = startX + i * spacing;
                dc.DrawEllipse(dotBrush, null, new Point(x, centerY), dotSize / 2, dotSize / 2);
            }
        }
    }

    #endregion

    #region Property Changed Callbacks

    private static void OnResizeDirectionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is GridSplitter splitter)
        {
            splitter.InvalidateMeasure();
            splitter.InvalidateVisual();
        }
    }

    #endregion
}

/// <summary>
/// Specifies which rows or columns are resized by a GridSplitter.
/// </summary>
public enum GridResizeDirection
{
    /// <summary>
    /// Automatically determine resize direction based on splitter size.
    /// </summary>
    Auto,

    /// <summary>
    /// Resize columns.
    /// </summary>
    Columns,

    /// <summary>
    /// Resize rows.
    /// </summary>
    Rows
}

/// <summary>
/// Specifies which adjacent rows/columns a GridSplitter resizes.
/// </summary>
public enum GridResizeBehavior
{
    /// <summary>
    /// Determine behavior based on splitter alignment.
    /// </summary>
    BasedOnAlignment,

    /// <summary>
    /// Resize the previous and current row/column.
    /// </summary>
    PreviousAndCurrent,

    /// <summary>
    /// Resize the current and next row/column.
    /// </summary>
    CurrentAndNext,

    /// <summary>
    /// Resize the previous and next row/column.
    /// </summary>
    PreviousAndNext
}
