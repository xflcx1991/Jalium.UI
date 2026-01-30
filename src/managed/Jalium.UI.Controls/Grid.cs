using Jalium.UI;

namespace Jalium.UI.Controls;

/// <summary>
/// Defines a flexible grid area that consists of columns and rows.
/// </summary>
public class Grid : Panel
{
    #region Attached Properties

    /// <summary>
    /// Identifies the Row attached property.
    /// </summary>
    public static readonly DependencyProperty RowProperty =
        DependencyProperty.RegisterAttached("Row", typeof(int), typeof(Grid),
            new PropertyMetadata(0));

    /// <summary>
    /// Identifies the Column attached property.
    /// </summary>
    public static readonly DependencyProperty ColumnProperty =
        DependencyProperty.RegisterAttached("Column", typeof(int), typeof(Grid),
            new PropertyMetadata(0));

    /// <summary>
    /// Identifies the RowSpan attached property.
    /// </summary>
    public static readonly DependencyProperty RowSpanProperty =
        DependencyProperty.RegisterAttached("RowSpan", typeof(int), typeof(Grid),
            new PropertyMetadata(1));

    /// <summary>
    /// Identifies the ColumnSpan attached property.
    /// </summary>
    public static readonly DependencyProperty ColumnSpanProperty =
        DependencyProperty.RegisterAttached("ColumnSpan", typeof(int), typeof(Grid),
            new PropertyMetadata(1));

    /// <summary>
    /// Gets the value of the Row attached property for a given element.
    /// </summary>
    public static int GetRow(UIElement element) =>
        (int)(element.GetValue(RowProperty) ?? 0);

    /// <summary>
    /// Sets the value of the Row attached property for a given element.
    /// </summary>
    public static void SetRow(UIElement element, int value) =>
        element.SetValue(RowProperty, value);

    /// <summary>
    /// Gets the value of the Column attached property for a given element.
    /// </summary>
    public static int GetColumn(UIElement element) =>
        (int)(element.GetValue(ColumnProperty) ?? 0);

    /// <summary>
    /// Sets the value of the Column attached property for a given element.
    /// </summary>
    public static void SetColumn(UIElement element, int value) =>
        element.SetValue(ColumnProperty, value);

    /// <summary>
    /// Gets the value of the RowSpan attached property for a given element.
    /// </summary>
    public static int GetRowSpan(UIElement element) =>
        (int)(element.GetValue(RowSpanProperty) ?? 1);

    /// <summary>
    /// Sets the value of the RowSpan attached property for a given element.
    /// </summary>
    public static void SetRowSpan(UIElement element, int value) =>
        element.SetValue(RowSpanProperty, Math.Max(1, value));

    /// <summary>
    /// Gets the value of the ColumnSpan attached property for a given element.
    /// </summary>
    public static int GetColumnSpan(UIElement element) =>
        (int)(element.GetValue(ColumnSpanProperty) ?? 1);

    /// <summary>
    /// Sets the value of the ColumnSpan attached property for a given element.
    /// </summary>
    public static void SetColumnSpan(UIElement element, int value) =>
        element.SetValue(ColumnSpanProperty, Math.Max(1, value));

    #endregion

    #region Properties

    /// <summary>
    /// Gets the collection of row definitions.
    /// </summary>
    public RowDefinitionCollection RowDefinitions { get; } = new();

    /// <summary>
    /// Gets the collection of column definitions.
    /// </summary>
    public ColumnDefinitionCollection ColumnDefinitions { get; } = new();

    #endregion

    #region Layout

    /// <inheritdoc />
    protected override Size MeasureOverride(Size availableSize)
    {
        // Ensure at least one row and column
        var rowCount = Math.Max(1, RowDefinitions.Count);
        var columnCount = Math.Max(1, ColumnDefinitions.Count);

        // Initialize row and column sizes
        var rowHeights = new double[rowCount];
        var columnWidths = new double[columnCount];
        var rowStarValues = new double[rowCount];
        var columnStarValues = new double[columnCount];

        // Get definitions (use default if not defined)
        var rowDefs = GetEffectiveRowDefinitions(rowCount);
        var columnDefs = GetEffectiveColumnDefinitions(columnCount);

        // First pass: Calculate auto and fixed sizes
        for (int i = 0; i < rowCount; i++)
        {
            var def = rowDefs[i];
            if (def.Height.IsAbsolute)
            {
                rowHeights[i] = Math.Clamp(def.Height.Value, def.MinHeight, def.MaxHeight);
            }
            else if (def.Height.IsStar)
            {
                rowStarValues[i] = def.Height.Value;
            }
        }

        for (int i = 0; i < columnCount; i++)
        {
            var def = columnDefs[i];
            if (def.Width.IsAbsolute)
            {
                columnWidths[i] = Math.Clamp(def.Width.Value, def.MinWidth, def.MaxWidth);
            }
            else if (def.Width.IsStar)
            {
                columnStarValues[i] = def.Width.Value;
            }
        }

        // Measure children in auto rows/columns
        foreach (var child in Children)
        {
            if (child is not FrameworkElement fe) continue;

            var row = Math.Min(GetRow(child), rowCount - 1);
            var column = Math.Min(GetColumn(child), columnCount - 1);
            var rowSpan = Math.Min(GetRowSpan(child), rowCount - row);
            var columnSpan = Math.Min(GetColumnSpan(child), columnCount - column);

            // Check if child is in any auto row/column
            bool inAutoRow = false;
            bool inAutoColumn = false;
            for (int i = row; i < row + rowSpan; i++)
            {
                if (rowDefs[i].Height.IsAuto) inAutoRow = true;
            }
            for (int i = column; i < column + columnSpan; i++)
            {
                if (columnDefs[i].Width.IsAuto) inAutoColumn = true;
            }

            if (inAutoRow || inAutoColumn)
            {
                // Measure with available space
                fe.Measure(new Size(
                    inAutoColumn ? double.PositiveInfinity : availableSize.Width,
                    inAutoRow ? double.PositiveInfinity : availableSize.Height));

                // Update auto sizes
                if (inAutoRow && rowSpan == 1)
                {
                    var def = rowDefs[row];
                    if (def.Height.IsAuto)
                    {
                        rowHeights[row] = Math.Max(rowHeights[row],
                            Math.Clamp(fe.DesiredSize.Height, def.MinHeight, def.MaxHeight));
                    }
                }

                if (inAutoColumn && columnSpan == 1)
                {
                    var def = columnDefs[column];
                    if (def.Width.IsAuto)
                    {
                        columnWidths[column] = Math.Max(columnWidths[column],
                            Math.Clamp(fe.DesiredSize.Width, def.MinWidth, def.MaxWidth));
                    }
                }
            }
        }

        // Calculate remaining space for star sizing
        double fixedRowHeight = rowHeights.Sum();
        double fixedColumnWidth = columnWidths.Sum();
        double totalRowStars = rowStarValues.Sum();
        double totalColumnStars = columnStarValues.Sum();

        double availableRowSpace = Math.Max(0, availableSize.Height - fixedRowHeight);
        double availableColumnSpace = Math.Max(0, availableSize.Width - fixedColumnWidth);

        // Distribute star space
        if (totalRowStars > 0)
        {
            double starUnitHeight = availableRowSpace / totalRowStars;
            for (int i = 0; i < rowCount; i++)
            {
                if (rowStarValues[i] > 0)
                {
                    var def = rowDefs[i];
                    rowHeights[i] = Math.Clamp(starUnitHeight * rowStarValues[i], def.MinHeight, def.MaxHeight);
                }
            }
        }

        if (totalColumnStars > 0)
        {
            double starUnitWidth = availableColumnSpace / totalColumnStars;
            for (int i = 0; i < columnCount; i++)
            {
                if (columnStarValues[i] > 0)
                {
                    var def = columnDefs[i];
                    columnWidths[i] = Math.Clamp(starUnitWidth * columnStarValues[i], def.MinWidth, def.MaxWidth);
                }
            }
        }

        // Measure all children with their final available sizes
        foreach (var child in Children)
        {
            if (child is not FrameworkElement fe) continue;

            var row = Math.Min(GetRow(child), rowCount - 1);
            var column = Math.Min(GetColumn(child), columnCount - 1);
            var rowSpan = Math.Min(GetRowSpan(child), rowCount - row);
            var columnSpan = Math.Min(GetColumnSpan(child), columnCount - column);

            double cellWidth = 0;
            double cellHeight = 0;

            for (int i = column; i < column + columnSpan; i++)
                cellWidth += columnWidths[i];
            for (int i = row; i < row + rowSpan; i++)
                cellHeight += rowHeights[i];

            fe.Measure(new Size(cellWidth, cellHeight));
        }

        // Return the total size
        return new Size(columnWidths.Sum(), rowHeights.Sum());
    }

    /// <inheritdoc />
    protected override Size ArrangeOverride(Size finalSize)
    {
        var rowCount = Math.Max(1, RowDefinitions.Count);
        var columnCount = Math.Max(1, ColumnDefinitions.Count);

        // Get definitions
        var rowDefs = GetEffectiveRowDefinitions(rowCount);
        var columnDefs = GetEffectiveColumnDefinitions(columnCount);

        // Calculate final row heights and column widths
        var rowHeights = new double[rowCount];
        var columnWidths = new double[columnCount];
        var rowStarValues = new double[rowCount];
        var columnStarValues = new double[columnCount];

        double fixedRowHeight = 0;
        double fixedColumnWidth = 0;
        double totalRowStars = 0;
        double totalColumnStars = 0;

        // First pass: fixed and auto sizes
        for (int i = 0; i < rowCount; i++)
        {
            var def = rowDefs[i];
            if (def.Height.IsAbsolute)
            {
                rowHeights[i] = Math.Clamp(def.Height.Value, def.MinHeight, def.MaxHeight);
                fixedRowHeight += rowHeights[i];
            }
            else if (def.Height.IsAuto)
            {
                // Use the measured auto size
                rowHeights[i] = Math.Clamp(def.ActualHeight, def.MinHeight, def.MaxHeight);
                fixedRowHeight += rowHeights[i];
            }
            else if (def.Height.IsStar)
            {
                rowStarValues[i] = def.Height.Value;
                totalRowStars += def.Height.Value;
            }
        }

        for (int i = 0; i < columnCount; i++)
        {
            var def = columnDefs[i];
            if (def.Width.IsAbsolute)
            {
                columnWidths[i] = Math.Clamp(def.Width.Value, def.MinWidth, def.MaxWidth);
                fixedColumnWidth += columnWidths[i];
            }
            else if (def.Width.IsAuto)
            {
                columnWidths[i] = Math.Clamp(def.ActualWidth, def.MinWidth, def.MaxWidth);
                fixedColumnWidth += columnWidths[i];
            }
            else if (def.Width.IsStar)
            {
                columnStarValues[i] = def.Width.Value;
                totalColumnStars += def.Width.Value;
            }
        }

        // Distribute star space
        double availableRowSpace = Math.Max(0, finalSize.Height - fixedRowHeight);
        double availableColumnSpace = Math.Max(0, finalSize.Width - fixedColumnWidth);

        if (totalRowStars > 0)
        {
            double starUnitHeight = availableRowSpace / totalRowStars;
            for (int i = 0; i < rowCount; i++)
            {
                if (rowStarValues[i] > 0)
                {
                    var def = rowDefs[i];
                    rowHeights[i] = Math.Clamp(starUnitHeight * rowStarValues[i], def.MinHeight, def.MaxHeight);
                }
            }
        }

        if (totalColumnStars > 0)
        {
            double starUnitWidth = availableColumnSpace / totalColumnStars;
            for (int i = 0; i < columnCount; i++)
            {
                if (columnStarValues[i] > 0)
                {
                    var def = columnDefs[i];
                    columnWidths[i] = Math.Clamp(starUnitWidth * columnStarValues[i], def.MinWidth, def.MaxWidth);
                }
            }
        }

        // Calculate offsets
        var rowOffsets = new double[rowCount + 1];
        var columnOffsets = new double[columnCount + 1];

        for (int i = 0; i < rowCount; i++)
        {
            rowOffsets[i + 1] = rowOffsets[i] + rowHeights[i];
            rowDefs[i].ActualHeight = rowHeights[i];
            rowDefs[i].Offset = rowOffsets[i];
        }

        for (int i = 0; i < columnCount; i++)
        {
            columnOffsets[i + 1] = columnOffsets[i] + columnWidths[i];
            columnDefs[i].ActualWidth = columnWidths[i];
            columnDefs[i].Offset = columnOffsets[i];
        }

        // Arrange children
        foreach (var child in Children)
        {
            if (child is not FrameworkElement fe) continue;

            var row = Math.Clamp(GetRow(child), 0, rowCount - 1);
            var column = Math.Clamp(GetColumn(child), 0, columnCount - 1);
            var rowSpan = Math.Clamp(GetRowSpan(child), 1, rowCount - row);
            var columnSpan = Math.Clamp(GetColumnSpan(child), 1, columnCount - column);

            double x = columnOffsets[column];
            double y = rowOffsets[row];
            double width = columnOffsets[column + columnSpan] - x;
            double height = rowOffsets[row + rowSpan] - y;

            var cellRect = new Rect(x, y, width, height);
            fe.Arrange(cellRect);
            // Note: Do NOT call SetVisualBounds here - ArrangeCore already handles margin
        }

        return finalSize;
    }

    private RowDefinition[] GetEffectiveRowDefinitions(int count)
    {
        var defs = new RowDefinition[count];
        for (int i = 0; i < count; i++)
        {
            defs[i] = i < RowDefinitions.Count
                ? RowDefinitions[i]
                : new RowDefinition { Height = GridLength.Star };
        }
        return defs;
    }

    private ColumnDefinition[] GetEffectiveColumnDefinitions(int count)
    {
        var defs = new ColumnDefinition[count];
        for (int i = 0; i < count; i++)
        {
            defs[i] = i < ColumnDefinitions.Count
                ? ColumnDefinitions[i]
                : new ColumnDefinition { Width = GridLength.Star };
        }
        return defs;
    }

    #endregion
}
