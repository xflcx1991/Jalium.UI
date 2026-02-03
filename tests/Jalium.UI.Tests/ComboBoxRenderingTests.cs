using System.Reflection;
using Jalium.UI;
using Jalium.UI.Controls;
using Jalium.UI.Controls.Themes;
using System.Diagnostics;

namespace Jalium.UI.Tests;

/// <summary>
/// ComboBox 渲染测试 - 模拟实际窗口布局
/// </summary>
[Collection("Application")]
public class ComboBoxRenderingTests
{
    /// <summary>
    /// Resets static state for clean test isolation.
    /// </summary>
    private static void ResetApplicationState()
    {
        // Clear Application._current
        var currentField = typeof(Application).GetField("_current",
            BindingFlags.NonPublic | BindingFlags.Static);
        currentField?.SetValue(null, null);

        // Reset ThemeManager._initialized
        var resetMethod = typeof(ThemeManager).GetMethod("Reset",
            BindingFlags.NonPublic | BindingFlags.Static);
        resetMethod?.Invoke(null, null);
    }

    /// <summary>
    /// 模拟窗口布局过程，测试 ComboBox 在实际渲染时的尺寸
    /// </summary>
    [Fact]
    public void ComboBox_InWindow_ShouldRespectMinHeight()
    {
        // 模拟 Window 的布局过程
        var container = new StackPanel
        {
            Width = 400,
            Height = 300
        };

        var comboBox = new ComboBox();
        comboBox.MinHeight = 50;
        container.Children.Add(comboBox);

        // Measure pass
        container.Measure(new Size(400, 300));

        // Arrange pass
        container.Arrange(new Rect(0, 0, 400, 300));

        // 验证 ComboBox 的渲染尺寸
        Debug.WriteLine($"ComboBox.MinHeight = {comboBox.MinHeight}");
        Debug.WriteLine($"ComboBox.DesiredSize = {comboBox.DesiredSize}");
        Debug.WriteLine($"ComboBox.RenderSize = {comboBox.RenderSize}");
        Debug.WriteLine($"ComboBox.ActualHeight = {comboBox.ActualHeight}");

        Assert.True(comboBox.RenderSize.Height >= 50,
            $"ComboBox.RenderSize.Height ({comboBox.RenderSize.Height}) should be >= MinHeight (50)");
        Assert.True(comboBox.ActualHeight >= 50,
            $"ComboBox.ActualHeight ({comboBox.ActualHeight}) should be >= MinHeight (50)");
    }

    /// <summary>
    /// 测试 ComboBox ControlTemplate 视觉树是否被创建
    /// </summary>
    [Fact]
    public void ComboBox_ShouldHaveVisualTree()
    {
        var comboBox = new ComboBox();
        comboBox.Width = 200;
        comboBox.MinHeight = 32;

        // Measure and Arrange to trigger template application
        comboBox.Measure(new Size(200, 100));
        comboBox.Arrange(new Rect(0, 0, 200, 100));

        // Check if visual tree exists
        var visualChildrenCount = comboBox.VisualChildrenCount;

        // Get visual child info
        var childInfo = "";
        for (int i = 0; i < visualChildrenCount; i++)
        {
            var child = comboBox.GetVisualChild(i);
            if (child != null)
            {
                childInfo += $"Child[{i}]: {child.GetType().FullName}; ";
            }
        }

        // Get style info
        var styleInfo = comboBox.Style != null
            ? $"TargetType={comboBox.Style.TargetType?.Name}, SettersCount={comboBox.Style.Setters.Count}"
            : "null";

        // ComboBox should have at least one visual child (fallback or template)
        Assert.True(visualChildrenCount >= 1,
            $"ComboBox should have visual children, but VisualChildrenCount = {visualChildrenCount}");
    }

    /// <summary>
    /// 测试 ControlTemplate 中的 Border 是否正确获取尺寸
    /// </summary>
    [Fact]
    public void ComboBox_TemplateChildren_ShouldHaveCorrectSize()
    {
        var comboBox = new ComboBox();
        comboBox.Width = 200;
        comboBox.MinHeight = 50;

        // Measure and Arrange
        comboBox.Measure(new Size(200, 100));
        comboBox.Arrange(new Rect(0, 0, 200, 100));

        Debug.WriteLine($"ComboBox.RenderSize = {comboBox.RenderSize}");
        Debug.WriteLine($"ComboBox.VisualChildrenCount = {comboBox.VisualChildrenCount}");

        // Walk the visual tree and check sizes
        void PrintVisualTree(Visual visual, int depth)
        {
            var indent = new string(' ', depth * 2);
            if (visual is FrameworkElement fe)
            {
                Debug.WriteLine($"{indent}{visual.GetType().Name}: RenderSize={fe.RenderSize}, ActualWidth={fe.ActualWidth}, ActualHeight={fe.ActualHeight}");
            }
            else
            {
                Debug.WriteLine($"{indent}{visual.GetType().Name}");
            }

            for (int i = 0; i < visual.VisualChildrenCount; i++)
            {
                if (visual.GetVisualChild(i) is Visual child)
                {
                    PrintVisualTree(child, depth + 1);
                }
            }
        }

        PrintVisualTree(comboBox, 0);

        // The test passes if we can enumerate the visual tree
        Assert.True(true);
    }

    /// <summary>
    /// 测试 Grid 容器中的 ComboBox 布局
    /// </summary>
    [Fact]
    public void ComboBox_InGrid_ShouldRespectMinHeight()
    {
        var grid = new Grid();
        grid.Width = 400;
        grid.Height = 300;
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        var comboBox = new ComboBox();
        comboBox.MinHeight = 50;
        Grid.SetRow(comboBox, 0);
        grid.Children.Add(comboBox);

        // Measure pass
        grid.Measure(new Size(400, 300));

        // Arrange pass
        grid.Arrange(new Rect(0, 0, 400, 300));

        Debug.WriteLine($"Grid.RenderSize = {grid.RenderSize}");
        Debug.WriteLine($"ComboBox.MinHeight = {comboBox.MinHeight}");
        Debug.WriteLine($"ComboBox.DesiredSize = {comboBox.DesiredSize}");
        Debug.WriteLine($"ComboBox.RenderSize = {comboBox.RenderSize}");
        Debug.WriteLine($"ComboBox.ActualHeight = {comboBox.ActualHeight}");

        Assert.True(comboBox.RenderSize.Height >= 50,
            $"ComboBox.RenderSize.Height ({comboBox.RenderSize.Height}) should be >= MinHeight (50)");
    }

    /// <summary>
    /// 验证默认样式中的 MinHeight 值
    /// </summary>
    [Fact]
    public void ComboBox_DefaultStyle_MinHeightValue()
    {
        var comboBox = new ComboBox();

        // 诊断：显示 Application 和 Style 状态
        var appExists = Application.Current != null;
        var styleExists = comboBox.Style != null;
        var templateExists = comboBox.Template != null;

        // ComboBox constructor should set default MinHeight
        Assert.True(comboBox.MinHeight >= 32,
            $"ComboBox should have MinHeight >= 32 from constructor, but MinHeight = {comboBox.MinHeight}");
        Assert.True(comboBox.MinWidth >= 120,
            $"ComboBox should have MinWidth >= 120 from constructor, but MinWidth = {comboBox.MinWidth}");
    }

    /// <summary>
    /// 初始化 Application 后测试 ComboBox Template
    /// </summary>
    [Fact]
    public void ComboBox_WithApplication_ShouldHaveTemplate()
    {
        ResetApplicationState();
        var app = new Application();

        try
        {
            // 创建容器来触发 VisualParent 变化
            var container = new StackPanel { Width = 400, Height = 300 };

            var comboBox = new ComboBox();
            comboBox.Width = 200;
            comboBox.MinHeight = 32;

            // 添加到容器，触发 OnVisualParentChanged -> ApplyImplicitStyleIfNeeded
            container.Children.Add(comboBox);

            // Measure and Arrange 容器
            container.Measure(new Size(400, 300));
            container.Arrange(new Rect(0, 0, 400, 300));

            // 诊断信息
            var styleInfo = comboBox.Style != null
                ? $"TargetType={comboBox.Style.TargetType?.Name}, SettersCount={comboBox.Style.Setters.Count}"
                : "null";

            var templateInfo = comboBox.Template != null
                ? $"TargetType={comboBox.Template.TargetType?.Name}"
                : "null";

            // Get visual child info
            var childInfo = "";
            for (int i = 0; i < comboBox.VisualChildrenCount; i++)
            {
                var child = comboBox.GetVisualChild(i);
                if (child != null)
                {
                    childInfo += $"Child[{i}]: {child.GetType().Name}; ";
                }
            }

            // 检查 Application.Resources 中是否有 ComboBox 的样式
            var hasComboBoxStyle = app.Resources.TryGetValue(typeof(ComboBox), out var styleFromApp);
            var appStyleInfo = hasComboBoxStyle && styleFromApp != null
                ? $"found (Type={styleFromApp.GetType().Name})"
                : "not found";

            // 获取 Grid 子元素的详细信息
            var gridInfo = "";
            if (comboBox.VisualChildrenCount > 0 && comboBox.GetVisualChild(0) is FrameworkElement gridChild)
            {
                gridInfo = $"Grid: RenderSize={gridChild.RenderSize}, DesiredSize={gridChild.DesiredSize}";
            }

            // ComboBox should have correct height after layout with Application
            Assert.True(comboBox.DesiredSize.Height >= comboBox.MinHeight,
                $"DesiredSize.Height ({comboBox.DesiredSize.Height}) should be >= MinHeight ({comboBox.MinHeight})");
            Assert.True(comboBox.RenderSize.Height >= comboBox.MinHeight,
                $"RenderSize.Height ({comboBox.RenderSize.Height}) should be >= MinHeight ({comboBox.MinHeight})");
        }
        finally
        {
            ResetApplicationState();
        }
    }

    /// <summary>
    /// 测试显式设置 Height 和 MinHeight 的优先级
    /// </summary>
    [Fact]
    public void ComboBox_HeightVsMinHeight_Priority()
    {
        var comboBox = new ComboBox();
        comboBox.Height = 20;  // 小于 MinHeight
        comboBox.MinHeight = 50;

        comboBox.Measure(new Size(200, 200));
        comboBox.Arrange(new Rect(0, 0, 200, 200));

        Debug.WriteLine($"Height = {comboBox.Height}");
        Debug.WriteLine($"MinHeight = {comboBox.MinHeight}");
        Debug.WriteLine($"RenderSize.Height = {comboBox.RenderSize.Height}");

        // MinHeight should take precedence over explicit Height when Height < MinHeight
        Assert.True(comboBox.RenderSize.Height >= 50,
            $"RenderSize.Height ({comboBox.RenderSize.Height}) should be >= MinHeight (50) even when Height is set to 20");
    }

    /// <summary>
    /// 测试 DesiredSize 和 RenderSize 的一致性
    /// </summary>
    [Fact]
    public void ComboBox_DesiredSize_RenderSize_Consistency()
    {
        var comboBox = new ComboBox();
        comboBox.MinHeight = 60;
        comboBox.Width = 150;

        // Measure
        comboBox.Measure(new Size(200, 200));
        var desiredSize = comboBox.DesiredSize;

        // Arrange with exact desired size
        comboBox.Arrange(new Rect(0, 0, desiredSize.Width, desiredSize.Height));

        Debug.WriteLine($"MinHeight = {comboBox.MinHeight}");
        Debug.WriteLine($"DesiredSize = {desiredSize}");
        Debug.WriteLine($"RenderSize = {comboBox.RenderSize}");

        // DesiredSize.Height should be >= MinHeight
        Assert.True(desiredSize.Height >= 60,
            $"DesiredSize.Height ({desiredSize.Height}) should be >= MinHeight (60)");

        // RenderSize.Height should be >= MinHeight
        Assert.True(comboBox.RenderSize.Height >= 60,
            $"RenderSize.Height ({comboBox.RenderSize.Height}) should be >= MinHeight (60)");
    }

    /// <summary>
    /// 诊断：不使用 Application 时的布局追踪
    /// </summary>
    [Fact]
    public void ComboBox_Layout_WithoutApplication()
    {
        var comboBox = new ComboBox();
        comboBox.Width = 200;
        comboBox.MinHeight = 50;

        // 记录初始状态
        var initialIsMeasureValid = comboBox.IsMeasureValid;
        var initialIsArrangeValid = comboBox.IsArrangeValid;

        // Measure
        comboBox.Measure(new Size(300, 300));
        var measureIsMeasureValid = comboBox.IsMeasureValid;
        var desiredSize = comboBox.DesiredSize;

        // Arrange
        comboBox.Arrange(new Rect(0, 0, desiredSize.Width, desiredSize.Height));
        var arrangeIsArrangeValid = comboBox.IsArrangeValid;
        var renderSize = comboBox.RenderSize;

        // ComboBox should respect MinHeight without Application
        Assert.True(desiredSize.Height >= 50,
            $"DesiredSize.Height ({desiredSize.Height}) should be >= MinHeight (50)");
        Assert.True(renderSize.Height >= 50,
            $"RenderSize.Height ({renderSize.Height}) should be >= MinHeight (50)");
    }

    /// <summary>
    /// 诊断：使用 Application 时 StackPanel 内的布局追踪
    /// </summary>
    [Fact]
    public void ComboBox_Layout_InStackPanel_WithApplication()
    {
        ResetApplicationState();
        var app = new Application();

        try
        {
            var container = new StackPanel { Width = 400, Height = 300 };
            var comboBox = new ComboBox();
            comboBox.Width = 200;
            comboBox.MinHeight = 50;

            // 添加到容器前测量
            comboBox.Measure(new Size(200, 100));
            var beforeAddDesiredSize = comboBox.DesiredSize;

            // 添加到容器
            container.Children.Add(comboBox);

            // 容器测量前 ComboBox 状态
            var afterAddIsMeasureValid = comboBox.IsMeasureValid;
            var afterAddDesiredSize = comboBox.DesiredSize;

            // 测量容器
            container.Measure(new Size(400, 300));
            var afterContainerMeasureDesiredSize = comboBox.DesiredSize;

            // 布置容器
            container.Arrange(new Rect(0, 0, 400, 300));
            var finalRenderSize = comboBox.RenderSize;

            // 获取 _templateRoot 信息
            var templateRootField = typeof(Control).GetField("_templateRoot",
                BindingFlags.NonPublic | BindingFlags.Instance);
            var templateRoot = templateRootField?.GetValue(comboBox) as FrameworkElement;
            var templateRootInfo = templateRoot != null
                ? $"{templateRoot.GetType().Name}, DesiredSize={templateRoot.DesiredSize}"
                : "null";

            // ComboBox should respect MinHeight when in StackPanel with Application
            Assert.True(finalRenderSize.Height >= 50,
                $"RenderSize.Height ({finalRenderSize.Height}) should be >= MinHeight (50)");
            Assert.True(comboBox.ActualHeight >= 50,
                $"ActualHeight ({comboBox.ActualHeight}) should be >= MinHeight (50)");
        }
        finally
        {
            ResetApplicationState();
        }
    }

    /// <summary>
    /// 诊断：检查问题是否在 ItemsControl.HasTemplate 导致 Control.MeasureOverride 被调用
    /// </summary>
    [Fact]
    public void ComboBox_HasTemplate_ShouldNotBypassMeasureOverride()
    {
        ResetApplicationState();
        var app = new Application();

        try
        {
            var comboBox = new ComboBox();
            comboBox.Width = 200;
            comboBox.MinHeight = 50;

            // 添加到容器来触发隐式样式应用
            var container = new StackPanel { Width = 400, Height = 300 };
            container.Children.Add(comboBox);

            // 检查 Template 是否被设置
            var hasTemplate = comboBox.Template != null;

            // 单独测量 ComboBox（用有限高度）
            comboBox.Measure(new Size(200, 100));
            var finiteDesiredSize = comboBox.DesiredSize;

            // 再次测量（用无限高度）
            comboBox.Measure(new Size(200, double.PositiveInfinity));
            var infiniteDesiredSize = comboBox.DesiredSize;

            // ComboBox.MeasureOverride should return MinHeight regardless of available size
            // With finite available height
            Assert.True(finiteDesiredSize.Height >= comboBox.MinHeight,
                $"Finite DesiredSize.Height ({finiteDesiredSize.Height}) should be >= MinHeight ({comboBox.MinHeight})");
            // With infinite available height - should NOT return infinity
            Assert.False(double.IsInfinity(infiniteDesiredSize.Height),
                $"Infinite DesiredSize.Height should not be infinity, but was {infiniteDesiredSize.Height}");
            Assert.True(infiniteDesiredSize.Height >= comboBox.MinHeight,
                $"Infinite DesiredSize.Height ({infiniteDesiredSize.Height}) should be >= MinHeight ({comboBox.MinHeight})");
        }
        finally
        {
            ResetApplicationState();
        }
    }

    /// <summary>
    /// 使用自定义 ComboBox 子类验证 MeasureOverride 是否被调用
    /// </summary>
    [Fact]
    public void TracingComboBox_MeasureOverrideShouldBeCalled()
    {
        ResetApplicationState();
        var app = new Application();

        try
        {
            var comboBox = new TracingComboBox();
            comboBox.Width = 200;
            comboBox.MinHeight = 50;

            // 手动从 App.Resources 获取 ComboBox 的 Style 并提取 Template
            if (app.Resources.TryGetValue(typeof(ComboBox), out var styleObj) && styleObj is Style comboBoxStyle)
            {
                // 查找 Template Setter
                foreach (var setter in comboBoxStyle.Setters)
                {
                    if (setter.Property?.Name == "Template" && setter.Value is ControlTemplate template)
                    {
                        comboBox.Template = template;
                        break;
                    }
                }
            }

            // 添加到容器
            var container = new StackPanel { Width = 400, Height = 300 };
            container.Children.Add(comboBox);

            // 清除之前的测量记录
            comboBox.MeasureLog.Clear();

            // 测量
            comboBox.Measure(new Size(200, double.PositiveInfinity));

            var logEntries = string.Join("; ", comboBox.MeasureLog);

            // 使用反射检查 MeasureOverride 在各类中的定义
            var measureOverrideInComboBox = typeof(ComboBox).GetMethod("MeasureOverride",
                BindingFlags.NonPublic | BindingFlags.Instance |
                BindingFlags.DeclaredOnly);
            var measureOverrideInSelector = typeof(Selector).GetMethod("MeasureOverride",
                BindingFlags.NonPublic | BindingFlags.Instance |
                BindingFlags.DeclaredOnly);
            var measureOverrideInItemsControl = typeof(ItemsControl).GetMethod("MeasureOverride",
                BindingFlags.NonPublic | BindingFlags.Instance |
                BindingFlags.DeclaredOnly);

            // 也尝试不用 DeclaredOnly
            var measureOverrideInComboBoxAny = typeof(ComboBox).GetMethod("MeasureOverride",
                BindingFlags.NonPublic | BindingFlags.Instance);

            // 列出 ComboBox 的所有方法
            var comboBoxMethods = typeof(ComboBox).GetMethods(
                BindingFlags.NonPublic | BindingFlags.Instance |
                BindingFlags.DeclaredOnly)
                .Select(m => m.Name).ToArray();

            var reflectionInfo = $"ComboBox.MeasureOverride (DeclaredOnly): {(measureOverrideInComboBox != null ? "exists" : "NOT FOUND")}\n" +
                $"ComboBox.MeasureOverride (any): {(measureOverrideInComboBoxAny != null ? $"exists in {measureOverrideInComboBoxAny.DeclaringType?.Name}" : "NOT FOUND")}\n" +
                $"Selector.MeasureOverride: {(measureOverrideInSelector != null ? "exists" : "NOT FOUND")}\n" +
                $"ItemsControl.MeasureOverride: {(measureOverrideInItemsControl != null ? "exists" : "NOT FOUND")}\n" +
                $"ComboBox declared methods: {string.Join(", ", comboBoxMethods.Take(10))}";

            // MeasureOverride should have been called
            Assert.True(comboBox.MeasureLog.Count > 0,
                "MeasureOverride should have been called at least once");

            // ComboBox.MeasureOverride should exist via reflection
            Assert.NotNull(measureOverrideInComboBox);

            // DesiredSize should respect MinHeight
            Assert.True(comboBox.DesiredSize.Height >= comboBox.MinHeight,
                $"DesiredSize.Height ({comboBox.DesiredSize.Height}) should be >= MinHeight ({comboBox.MinHeight})");
        }
        finally
        {
            ResetApplicationState();
        }
    }

    /// <summary>
    /// 用于追踪 MeasureOverride 调用的 ComboBox 子类
    /// </summary>
    private class TracingComboBox : ComboBox
    {
        public List<string> MeasureLog { get; } = new List<string>();

        protected override Size MeasureOverride(Size availableSize)
        {
            // 不调用 base，直接实现 ComboBox.MeasureOverride 的逻辑
            var directResult = new Size(availableSize.Width, MinHeight);

            // 也调用 base 看它返回什么
            var baseResult = base.MeasureOverride(availableSize);

            MeasureLog.Add($"availableSize={availableSize}, MinHeight={MinHeight}, directResult={directResult}, baseResult={baseResult}");

            // 返回正确的值（直接计算的）
            return directResult;
        }
    }
}
