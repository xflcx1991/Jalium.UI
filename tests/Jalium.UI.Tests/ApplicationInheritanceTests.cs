namespace Jalium.UI.Tests;

using RootApplication = Jalium.UI.Application;

public class ApplicationInheritanceTests
{
    [Fact]
    public void RootApplication_ShouldBeInheritable()
    {
        Assert.False(typeof(RootApplication).IsSealed);
        Assert.True(typeof(DerivedRootApplication).IsAssignableTo(typeof(RootApplication)));
    }

    private sealed class DerivedRootApplication : RootApplication
    {
    }
}
