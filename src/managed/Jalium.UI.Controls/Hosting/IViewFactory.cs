using System.Diagnostics.CodeAnalysis;
using Jalium.UI.Controls;
using Microsoft.Extensions.DependencyInjection;

namespace Jalium.UI.Hosting;

/// <summary>
/// Service that creates view instances (typically <see cref="FrameworkElement"/>
/// subclasses such as <c>Page</c>/<c>UserControl</c>/<c>Window</c>) from the DI
/// container and — when a ViewModel has been paired via
/// <c>AddView&lt;TView, TViewModel&gt;()</c> — automatically resolves and
/// assigns the ViewModel as the view's <see cref="FrameworkElement.DataContext"/>.
/// </summary>
/// <remarks>
/// Navigation hosts (such as <c>Frame</c>) call into <see cref="CreateView(Type)"/>
/// first and fall back to <see cref="Activator.CreateInstance(Type)"/> only when
/// this service is unavailable or returns <see langword="null"/>.
/// </remarks>
public interface IViewFactory
{
    /// <summary>
    /// Creates a view of type <paramref name="viewType"/>. The instance is
    /// resolved from DI if registered, otherwise constructed through
    /// <see cref="ActivatorUtilities.CreateInstance(IServiceProvider, Type, object[])"/>
    /// so constructor-injected services still work.
    /// </summary>
    object? CreateView(Type viewType);

    /// <summary>
    /// Strongly-typed overload of <see cref="CreateView(Type)"/>.
    /// </summary>
    TView? CreateView<TView>() where TView : class;

    /// <summary>
    /// Attaches the registered ViewModel (if any) to an already-constructed view.
    /// Useful for XAML-created controls that want MVVM wiring without going
    /// through <see cref="CreateView(Type)"/>.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> if a ViewModel was found and assigned to
    /// <see cref="FrameworkElement.DataContext"/>; <see langword="false"/>
    /// if no registration exists for the view's runtime type.
    /// </returns>
    bool TryAttachViewModel(FrameworkElement view);
}

internal sealed class ViewFactory : IViewFactory
{
    private readonly IServiceProvider _services;
    private readonly ViewRegistry _registry;

    public ViewFactory(IServiceProvider services, ViewRegistry registry)
    {
        _services = services;
        _registry = registry;
    }

    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2067:Target method argument",
        Justification = "View types are registered by callers who already keep them rooted via AddView<TView>(); pairing types are public-constructor classes preserved by the DI container.")]
    [UnconditionalSuppressMessage(
        "AOT",
        "IL3050:Generic code may not be available at runtime",
        Justification = "ActivatorUtilities.CreateInstance uses a cached constructor invoker; the view type is always a concrete class reachable from user code.")]
    public object? CreateView(Type viewType)
    {
        ArgumentNullException.ThrowIfNull(viewType);

        // Prefer DI: user-registered views (constructor injection) take priority.
        var view = _services.GetService(viewType);

        // Fall back to ActivatorUtilities so constructors can still pull services
        // even when AddView<T>() wasn't called.
        view ??= ActivatorUtilities.CreateInstance(_services, viewType);

        if (view is FrameworkElement fe)
        {
            TryAttachViewModel(fe);
        }

        return view;
    }

    public TView? CreateView<TView>() where TView : class
        => CreateView(typeof(TView)) as TView;

    public bool TryAttachViewModel(FrameworkElement view)
    {
        ArgumentNullException.ThrowIfNull(view);

        // Don't clobber a DataContext the caller already set (XAML binding,
        // navigation parameter, etc.).
        if (view.DataContext != null)
        {
            return false;
        }

        if (!_registry.TryGetViewModelType(view.GetType(), out var vmType) || vmType == null)
        {
            return false;
        }

        var viewModel = _services.GetService(vmType) ?? ActivatorUtilities.CreateInstance(_services, vmType);
        view.DataContext = viewModel;
        return true;
    }
}
