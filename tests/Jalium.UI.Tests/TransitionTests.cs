using System.Reflection;
using Jalium.UI.Controls;
using Jalium.UI.Markup;
using Jalium.UI.Media.Animation;
using AnimationDuration = Jalium.UI.Media.Animation.Duration;

namespace Jalium.UI.Tests;

public class TransitionTests
{
    [Fact]
    public void DetachedElement_DoesNotStartAutomaticTransition()
    {
        var element = new TransitionElement();

        element.TestDouble = 12.0;

        Assert.Equal(12.0, element.TestDouble);
        Assert.False(element.HasAnimatedValue(TransitionElement.TestDoubleProperty));
        Assert.False(element.HasAutomaticTransition(TransitionElement.TestDoubleProperty));
    }

    [Fact]
    public void AttachedElement_LocalSet_StartsAutomaticTransition()
    {
        var host = new TransitionHostPanel();
        var element = new TransitionElement();
        host.AddChild(element);
        Dispatcher.GetForCurrentThread().ProcessQueue();

        element.TestDouble = 10.0;

        Assert.True(element.HasAnimatedValue(TransitionElement.TestDoubleProperty));
        Assert.True(element.HasAutomaticTransition(TransitionElement.TestDoubleProperty));
        Assert.Equal(0.0, element.TestDouble);
    }

    [Fact]
    public void SetLayerValue_StartsAutomaticTransition_WhenElementIsArmed()
    {
        var host = new TransitionHostPanel();
        var element = new TransitionElement();
        host.AddChild(element);
        Dispatcher.GetForCurrentThread().ProcessQueue();

        element.SetLayerValue(
            TransitionElement.TestDoubleProperty,
            7.0,
            DependencyObject.LayerValueSource.StyleSetter);

        Assert.True(element.HasAutomaticTransition(TransitionElement.TestDoubleProperty));
        Assert.Equal(0.0, element.TestDouble);
    }

    [Fact]
    public void ClearValue_TransitionsToUnderlyingLayerValue()
    {
        var host = new TransitionHostPanel();
        var element = new TransitionElement();
        element.SetLayerValue(
            TransitionElement.TestDoubleProperty,
            3.0,
            DependencyObject.LayerValueSource.StyleSetter);
        element.TestDouble = 8.0;

        host.AddChild(element);
        Dispatcher.GetForCurrentThread().ProcessQueue();

        element.ClearValue(TransitionElement.TestDoubleProperty);

        Assert.True(element.HasAutomaticTransition(TransitionElement.TestDoubleProperty));
        Assert.Equal(8.0, element.TestDouble);
    }

    [Fact]
    public void ClearLayerValue_TransitionsToDefaultValue()
    {
        var host = new TransitionHostPanel();
        var element = new TransitionElement();
        element.SetLayerValue(
            TransitionElement.TestDoubleProperty,
            5.0,
            DependencyObject.LayerValueSource.StyleSetter);

        host.AddChild(element);
        Dispatcher.GetForCurrentThread().ProcessQueue();

        element.ClearLayerValue(
            TransitionElement.TestDoubleProperty,
            DependencyObject.LayerValueSource.StyleSetter);

        Assert.True(element.HasAutomaticTransition(TransitionElement.TestDoubleProperty));
        Assert.Equal(5.0, element.TestDouble);
    }

    [Fact]
    public void MidFlightRetarget_StartsFromCurrentDisplayedValue()
    {
        var host = new TransitionHostPanel();
        var element = new TransitionElement();
        host.AddChild(element);
        Dispatcher.GetForCurrentThread().ProcessQueue();

        element.TestDouble = 10.0;
        Thread.Sleep(50);
        TickAnimations(element);
        var midValue = element.TestDouble;

        Assert.InRange(midValue, 0.01, 9.99);

        element.TestDouble = 20.0;

        Assert.InRange(element.TestDouble, midValue - 0.05, midValue + 0.05);
        Assert.True(element.HasAutomaticTransition(TransitionElement.TestDoubleProperty));
    }

    [Fact]
    public void ExplicitAnimation_PreventsAutomaticTransition()
    {
        var host = new TransitionHostPanel();
        var element = new TransitionElement();
        host.AddChild(element);
        Dispatcher.GetForCurrentThread().ProcessQueue();

        element.BeginAnimation(
            TransitionElement.TestDoubleProperty,
            new DoubleAnimation
            {
                From = 0.0,
                To = 1.0,
                Duration = TimeSpan.FromSeconds(1)
            });

        element.TestDouble = 5.0;

        Assert.True(element.HasExplicitAnimation(TransitionElement.TestDoubleProperty));
        Assert.False(element.HasAutomaticTransition(TransitionElement.TestDoubleProperty));
    }

    [Fact]
    public void UserDefinedControl_CustomDependencyProperty_TransitionsByDefault()
    {
        var host = new TransitionHostPanel();
        var element = new UserDefinedTransitionControl();
        host.AddChild(element);
        Dispatcher.GetForCurrentThread().ProcessQueue();

        element.UserScale = 24.0;

        Assert.True(element.HasAutomaticTransition(UserDefinedTransitionControl.UserScaleProperty));
        Assert.Equal(0.0, element.UserScale);
    }

    [Fact]
    public void UserDefinedAttachedProperty_TransitionsByDefault()
    {
        var host = new TransitionHostPanel();
        var element = new UserDefinedTransitionControl();
        host.AddChild(element);
        Dispatcher.GetForCurrentThread().ProcessQueue();

        element.SetValue(UserDefinedTransitionProperties.AttachedProgressProperty, 6.0);

        Assert.True(element.HasAutomaticTransition(UserDefinedTransitionProperties.AttachedProgressProperty));
        Assert.Equal(0.0, (double)element.GetValue(UserDefinedTransitionProperties.AttachedProgressProperty)!);
    }

    [Fact]
    public void UserDefinedCustomPropertyType_UsesMetadataTransitionFactory()
    {
        var host = new TransitionHostPanel();
        var element = new CustomTransitionValueElement();
        host.AddChild(element);
        Dispatcher.GetForCurrentThread().ProcessQueue();

        element.CustomValue = new CustomTransitionValue(10.0);

        Assert.True(element.HasAutomaticTransition(CustomTransitionValueElement.CustomValueProperty));
        Assert.Equal(0.0, element.CustomValue.Amount);

        Thread.Sleep(50);
        TickAnimations(element);

        Assert.InRange(element.CustomValue.Amount, 0.01, 9.99);
    }

    [Fact]
    public void XamlReader_ParsesTransitionConfiguration()
    {
        const string xaml = """
            <StackPanel xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation">
                <Border TransitionProperty="All" />
                <Border TransitionProperty="None" />
                <Border TransitionProperty="Opacity, Width, Background"
                        TransitionDuration="0:0:0.18"
                        TransitionTimingFunction="Recommended" />
            </StackPanel>
            """;

        var panel = Assert.IsType<StackPanel>(XamlReader.Parse(xaml));
        var allBorder = Assert.IsType<Border>(panel.Children[0]);
        var noneBorder = Assert.IsType<Border>(panel.Children[1]);
        var configuredBorder = Assert.IsType<Border>(panel.Children[2]);

        Assert.Equal("All", allBorder.TransitionProperty);
        Assert.Equal("None", noneBorder.TransitionProperty);
        Assert.Equal("Opacity, Width, Background", configuredBorder.TransitionProperty);
        Assert.Equal(new AnimationDuration(TimeSpan.FromMilliseconds(180)), configuredBorder.TransitionDuration);
        Assert.Equal(TransitionTimingFunction.Recommended, configuredBorder.TransitionTimingFunction);
    }

    private static void TickAnimations(UIElement element)
    {
        var tickMethod = typeof(UIElement).GetMethod(
            "OnRenderingTick",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(tickMethod);
        tickMethod!.Invoke(element, new object?[] { null, EventArgs.Empty });
    }

    private sealed class TransitionHostPanel : FrameworkElement
    {
        public void AddChild(UIElement child)
        {
            AddVisualChild(child);
        }
    }

    private sealed class TransitionElement : FrameworkElement
    {
        public static readonly DependencyProperty TestDoubleProperty =
            DependencyProperty.Register(
                nameof(TestDouble),
                typeof(double),
                typeof(TransitionElement),
                new PropertyMetadata(0.0));

        public double TestDouble
        {
            get => (double)GetValue(TestDoubleProperty)!;
            set => SetValue(TestDoubleProperty, value);
        }
    }

    private sealed class UserDefinedTransitionControl : FrameworkElement
    {
        public static readonly DependencyProperty UserScaleProperty =
            DependencyProperty.Register(
                nameof(UserScale),
                typeof(double),
                typeof(UserDefinedTransitionControl),
                new PropertyMetadata(0.0));

        public double UserScale
        {
            get => (double)GetValue(UserScaleProperty)!;
            set => SetValue(UserScaleProperty, value);
        }
    }

    private static class UserDefinedTransitionProperties
    {
        public static readonly DependencyProperty AttachedProgressProperty =
            DependencyProperty.RegisterAttached(
                "AttachedProgress",
                typeof(double),
                typeof(UserDefinedTransitionProperties),
                new PropertyMetadata(0.0));
    }

    private readonly struct CustomTransitionValue : IEquatable<CustomTransitionValue>
    {
        public CustomTransitionValue(double amount)
        {
            Amount = amount;
        }

        public double Amount { get; }

        public bool Equals(CustomTransitionValue other) => Amount.Equals(other.Amount);

        public override bool Equals(object? obj) => obj is CustomTransitionValue other && Equals(other);

        public override int GetHashCode() => Amount.GetHashCode();
    }

    private sealed class CustomTransitionValueElement : FrameworkElement
    {
        public static readonly DependencyProperty CustomValueProperty =
            DependencyProperty.Register(
                nameof(CustomValue),
                typeof(CustomTransitionValue),
                typeof(CustomTransitionValueElement),
                new PropertyMetadata(new CustomTransitionValue(0.0))
                {
                    AutomaticTransitionFactory = static (_, fromValue, toValue, duration, _) =>
                    {
                        return new CustomTransitionValueAnimation
                        {
                            From = fromValue is CustomTransitionValue from ? from : null,
                            To = toValue is CustomTransitionValue to ? to : null,
                            Duration = new AnimationDuration(duration)
                        };
                    }
                });

        public CustomTransitionValue CustomValue
        {
            get => (CustomTransitionValue)GetValue(CustomValueProperty)!;
            set => SetValue(CustomValueProperty, value);
        }
    }

    private sealed class CustomTransitionValueAnimation : AnimationTimeline<CustomTransitionValue>
    {
        public CustomTransitionValue? From { get; init; }

        public CustomTransitionValue? To { get; init; }

        protected override CustomTransitionValue GetCurrentValueCore(
            CustomTransitionValue defaultOriginValue,
            CustomTransitionValue defaultDestinationValue,
            AnimationClock animationClock)
        {
            var from = From ?? defaultOriginValue;
            var to = To ?? defaultDestinationValue;
            var progress = animationClock.CurrentProgress;

            return new CustomTransitionValue(from.Amount + (to.Amount - from.Amount) * progress);
        }
    }
}
