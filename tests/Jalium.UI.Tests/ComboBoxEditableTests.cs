using System.Reflection;
using Jalium.UI;
using Jalium.UI.Controls;
using Jalium.UI.Controls.Themes;

namespace Jalium.UI.Tests;

[Collection("Application")]
public class ComboBoxEditableTests
{
    [Fact]
    public void ComboBox_IsEditable_DependencyProperty_ShouldExist()
    {
        var field = typeof(ComboBox).GetField("IsEditableProperty", BindingFlags.Public | BindingFlags.Static);

        Assert.NotNull(field);
        Assert.Equal(typeof(DependencyProperty), field!.FieldType);

        var dp = field.GetValue(null) as DependencyProperty;
        Assert.NotNull(dp);
        Assert.Equal("IsEditable", dp!.Name);
        Assert.Equal(typeof(bool), dp.PropertyType);
    }

    [Fact]
    public void ComboBox_IsEditable_ShouldBeSettable()
    {
        var comboBox = new ComboBox();

        comboBox.IsEditable = true;

        Assert.True(comboBox.IsEditable);
    }

    [Fact]
    public void ComboBox_IsEditable_True_ShouldShowEditableTextBoxWithBoundText()
    {
        ResetApplicationState();
        var app = new Application();

        try
        {
            var comboBox = new ComboBox
            {
                Width = 220,
                IsEditable = true,
                Text = "Custom Value"
            };

            var host = new StackPanel
            {
                Width = 400,
                Height = 300
            };

            host.Children.Add(comboBox);
            host.Measure(new Size(400, 300));
            host.Arrange(new Rect(0, 0, 400, 300));

            var editableTextBox = FindDescendant<TextBox>(comboBox);

            Assert.NotNull(editableTextBox);
            Assert.Equal(Visibility.Visible, editableTextBox!.Visibility);
            Assert.Equal("Custom Value", editableTextBox.Text);
        }
        finally
        {
            ResetApplicationState();
        }
    }

    [Fact]
    public void ComboBox_IsEditable_False_ShouldKeepEditableTextBoxCollapsed()
    {
        ResetApplicationState();
        var app = new Application();

        try
        {
            var comboBox = new ComboBox
            {
                Width = 220,
                IsEditable = false
            };

            var host = new StackPanel
            {
                Width = 400,
                Height = 300
            };

            host.Children.Add(comboBox);
            host.Measure(new Size(400, 300));
            host.Arrange(new Rect(0, 0, 400, 300));

            var editableTextBox = FindDescendant<TextBox>(comboBox);

            Assert.NotNull(editableTextBox);
            Assert.Equal(Visibility.Collapsed, editableTextBox!.Visibility);
        }
        finally
        {
            ResetApplicationState();
        }
    }

    private static void ResetApplicationState()
    {
        var currentField = typeof(Application).GetField("_current",
            BindingFlags.NonPublic | BindingFlags.Static);
        currentField?.SetValue(null, null);

        var resetMethod = typeof(ThemeManager).GetMethod("Reset",
            BindingFlags.NonPublic | BindingFlags.Static);
        resetMethod?.Invoke(null, null);
    }

    private static T? FindDescendant<T>(Visual root) where T : Visual
    {
        if (root is T match)
        {
            return match;
        }

        for (int i = 0; i < root.VisualChildrenCount; i++)
        {
            var child = root.GetVisualChild(i);
            if (child == null)
            {
                continue;
            }

            var found = FindDescendant<T>(child);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }
}
