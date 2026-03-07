using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Jalium.UI.Media.Animation;

internal static class AnimationModuleInitializer
{
    [ModuleInitializer]
    [SuppressMessage("Usage", "CA2255:The 'ModuleInitializer' attribute should not be used in libraries")]
    internal static void Initialize()
    {
        UIElement.AutomaticTransitionAnimationFactory = AnimationFactory.CreateTransitionAnimation;
    }
}
