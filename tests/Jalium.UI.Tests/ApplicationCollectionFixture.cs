using Jalium.UI.Markup;

namespace Jalium.UI.Tests;

[CollectionDefinition("Application")]
public sealed class ApplicationCollection : ICollectionFixture<ApplicationCollectionFixture>
{
}

public sealed class ApplicationCollectionFixture
{
    public ApplicationCollectionFixture()
    {
        ThemeLoader.Initialize();
    }
}
