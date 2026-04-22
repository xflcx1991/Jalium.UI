using System.Linq;
using Jalium.UI.Controls;
using Microsoft.Extensions.Hosting;

namespace Jalium.UI;

/// <summary>
/// The running Jalium.UI application returned by <see cref="AppBuilder.Build"/>.
/// Wraps both the <see cref="IHost"/> (generic-host lifetime, hosted services,
/// service provider) and the Jalium.UI <see cref="Jalium.UI.Application"/>
/// (window tree and native message loop).
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Run()"/> is the normal entry point. It starts the host (invoking any
/// <see cref="IHostedService"/> instances), then enters the UI message loop, and
/// finally stops the host and disposes it. The returned integer is the process exit
/// code.
/// </para>
/// <para>
/// For unit tests or headless scenarios you can call <see cref="StartAsync"/> and
/// <see cref="StopAsync"/> directly without running the UI loop. <see cref="Dispose"/>
/// stops the host if it is still running and tears down all resources.
/// </para>
/// </remarks>
public sealed class JaliumApp : IHost
{
    private readonly IHost _host;
    private readonly Application _application;
    private readonly string[]? _args;
    private bool _disposed;

    internal JaliumApp(IHost host, Application application, string[]? args)
    {
        _host = host;
        _application = application;
        _args = args;
    }

    /// <summary>
    /// The configured <see cref="Jalium.UI.Application"/> instance. Use this to set
    /// <see cref="Jalium.UI.Application.MainWindow"/>, subscribe to lifecycle events,
    /// or read application-level resources after <see cref="AppBuilder.Build"/> has run.
    /// </summary>
    public Application Application => _application;

    /// <summary>
    /// Convenience accessor for <see cref="Jalium.UI.Application.MainWindow"/>.
    /// </summary>
    public Window? MainWindow
    {
        get => _application.MainWindow;
        set => _application.MainWindow = value;
    }

    /// <inheritdoc />
    public IServiceProvider Services => _host.Services;

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken = default)
        => _host.StartAsync(cancellationToken);

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken = default)
        => _host.StopAsync(cancellationToken);

    /// <summary>
    /// Starts the host, runs the Jalium.UI message loop until shutdown, then stops
    /// and disposes the host. Uses the command-line arguments originally supplied
    /// to <see cref="AppBuilder.CreateBuilder(string[])"/> (or
    /// <see cref="System.Environment.GetCommandLineArgs"/> as a fallback).
    /// </summary>
    /// <returns>The process exit code.</returns>
    public int Run()
    {
        var args = _args ?? System.Environment.GetCommandLineArgs().Skip(1).ToArray();
        return Run(args);
    }

    /// <summary>
    /// Starts the host, runs the Jalium.UI message loop with the supplied startup
    /// arguments, then stops and disposes the host.
    /// </summary>
    /// <param name="args">Startup arguments forwarded to <see cref="Application.Run(string[])"/>.</param>
    /// <returns>The process exit code.</returns>
    public int Run(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);
        ObjectDisposedException.ThrowIf(_disposed, this);

        _host.StartAsync(CancellationToken.None).GetAwaiter().GetResult();

        int exitCode;
        try
        {
            exitCode = _application.Run(args);
        }
        finally
        {
            try
            {
                _host.StopAsync(CancellationToken.None).GetAwaiter().GetResult();
            }
            catch (Exception)
            {
                // Swallow shutdown-time exceptions — the UI message loop has already
                // exited and the process is on the way out; letting an unrelated host
                // shutdown throw would mask the real exit code.
            }

            Dispose();
        }

        return exitCode;
    }

    /// <summary>
    /// Convenience overload that sets <see cref="MainWindow"/> before running.
    /// </summary>
    public int Run(Window mainWindow)
    {
        ArgumentNullException.ThrowIfNull(mainWindow);
        MainWindow = mainWindow;
        return Run();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        _disposed = true;
        _host.Dispose();
    }
}
