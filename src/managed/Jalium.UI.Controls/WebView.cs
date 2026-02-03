namespace Jalium.UI.Controls;

/// <summary>
/// Represents a control that hosts web content using WebView2.
/// </summary>
public class WebView : FrameworkElement
{
    private bool _isInitialized;
    private bool _isNavigating;
    private string _documentTitle = string.Empty;

    #region Dependency Properties

    /// <summary>
    /// Identifies the Source dependency property.
    /// </summary>
    public static readonly DependencyProperty SourceProperty =
        DependencyProperty.Register(nameof(Source), typeof(Uri), typeof(WebView),
            new PropertyMetadata(null, OnSourceChanged));

    /// <summary>
    /// Identifies the CanGoBack dependency property.
    /// </summary>
    private static readonly DependencyPropertyKey CanGoBackPropertyKey =
        DependencyProperty.RegisterReadOnly(nameof(CanGoBack), typeof(bool), typeof(WebView),
            new PropertyMetadata(false));

    /// <summary>
    /// Identifies the CanGoBack dependency property.
    /// </summary>
    public static readonly DependencyProperty CanGoBackProperty = CanGoBackPropertyKey.DependencyProperty;

    /// <summary>
    /// Identifies the CanGoForward dependency property.
    /// </summary>
    private static readonly DependencyPropertyKey CanGoForwardPropertyKey =
        DependencyProperty.RegisterReadOnly(nameof(CanGoForward), typeof(bool), typeof(WebView),
            new PropertyMetadata(false));

    /// <summary>
    /// Identifies the CanGoForward dependency property.
    /// </summary>
    public static readonly DependencyProperty CanGoForwardProperty = CanGoForwardPropertyKey.DependencyProperty;

    /// <summary>
    /// Identifies the ZoomFactor dependency property.
    /// </summary>
    public static readonly DependencyProperty ZoomFactorProperty =
        DependencyProperty.Register(nameof(ZoomFactor), typeof(double), typeof(WebView),
            new PropertyMetadata(1.0, OnZoomFactorChanged, CoerceZoomFactor));

    /// <summary>
    /// Identifies the DefaultBackgroundColor dependency property.
    /// </summary>
    public static readonly DependencyProperty DefaultBackgroundColorProperty =
        DependencyProperty.Register(nameof(DefaultBackgroundColor), typeof(Media.Color), typeof(WebView),
            new PropertyMetadata(Media.Color.White, OnDefaultBackgroundColorChanged));

    #endregion

    #region Events

    /// <summary>
    /// Occurs when navigation to a new page begins.
    /// </summary>
    public event EventHandler<WebViewNavigationStartingEventArgs>? NavigationStarting;

    /// <summary>
    /// Occurs when navigation is complete.
    /// </summary>
    public event EventHandler<WebViewNavigationCompletedEventArgs>? NavigationCompleted;

    /// <summary>
    /// Occurs when a new window is requested.
    /// </summary>
    public event EventHandler<WebViewNewWindowRequestedEventArgs>? NewWindowRequested;

    /// <summary>
    /// Occurs when the document title changes.
    /// </summary>
    public event EventHandler<WebViewDocumentTitleChangedEventArgs>? DocumentTitleChanged;

    /// <summary>
    /// Occurs when the WebView receives a message from a web page.
    /// </summary>
    public event EventHandler<WebViewWebMessageReceivedEventArgs>? WebMessageReceived;

    /// <summary>
    /// Occurs when content is loading.
    /// </summary>
    public event EventHandler<WebViewContentLoadingEventArgs>? ContentLoading;

    /// <summary>
    /// Occurs when the source changes.
    /// </summary>
    public event EventHandler<WebViewSourceChangedEventArgs>? SourceChanged;

    #endregion

    #region CLR Properties

    /// <summary>
    /// Gets or sets the URI source of the web content.
    /// </summary>
    public Uri? Source
    {
        get => (Uri?)GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    /// <summary>
    /// Gets a value indicating whether navigation back is possible.
    /// </summary>
    public bool CanGoBack => (bool)(GetValue(CanGoBackProperty) ?? false);

    /// <summary>
    /// Gets a value indicating whether navigation forward is possible.
    /// </summary>
    public bool CanGoForward => (bool)(GetValue(CanGoForwardProperty) ?? false);

    /// <summary>
    /// Gets or sets the zoom factor of the web content.
    /// </summary>
    public double ZoomFactor
    {
        get => (double)(GetValue(ZoomFactorProperty) ?? 1.0);
        set => SetValue(ZoomFactorProperty, value);
    }

    /// <summary>
    /// Gets or sets the default background color.
    /// </summary>
    public Media.Color DefaultBackgroundColor
    {
        get => (Media.Color)(GetValue(DefaultBackgroundColorProperty) ?? Media.Color.White);
        set => SetValue(DefaultBackgroundColorProperty, value);
    }

    /// <summary>
    /// Gets the current document title.
    /// </summary>
    public string DocumentTitle => _documentTitle;

    /// <summary>
    /// Gets a value indicating whether the WebView is initialized.
    /// </summary>
    public bool IsInitialized => _isInitialized;

    /// <summary>
    /// Gets a value indicating whether navigation is in progress.
    /// </summary>
    public bool IsNavigating => _isNavigating;

    #endregion

    #region Public Methods

    /// <summary>
    /// Navigates to the specified URI.
    /// </summary>
    public void Navigate(Uri source)
    {
        Source = source;
    }

    /// <summary>
    /// Navigates to the specified URI string.
    /// </summary>
    public void Navigate(string source)
    {
        if (Uri.TryCreate(source, UriKind.Absolute, out var uri))
        {
            Navigate(uri);
        }
    }

    /// <summary>
    /// Navigates to the specified HTML content.
    /// </summary>
    public void NavigateToString(string htmlContent)
    {
        NavigateToStringInternal(htmlContent);
    }

    /// <summary>
    /// Navigates back in the history.
    /// </summary>
    public void GoBack()
    {
        if (CanGoBack)
        {
            GoBackInternal();
        }
    }

    /// <summary>
    /// Navigates forward in the history.
    /// </summary>
    public void GoForward()
    {
        if (CanGoForward)
        {
            GoForwardInternal();
        }
    }

    /// <summary>
    /// Reloads the current page.
    /// </summary>
    public void Refresh()
    {
        RefreshInternal();
    }

    /// <summary>
    /// Stops loading the current page.
    /// </summary>
    public void Stop()
    {
        StopInternal();
    }

    /// <summary>
    /// Executes JavaScript in the web page.
    /// </summary>
    public Task<string> ExecuteScriptAsync(string script)
    {
        return ExecuteScriptInternalAsync(script);
    }

    /// <summary>
    /// Posts a message to the web page.
    /// </summary>
    public void PostWebMessageAsString(string message)
    {
        PostWebMessageAsStringInternal(message);
    }

    /// <summary>
    /// Posts a JSON message to the web page.
    /// </summary>
    public void PostWebMessageAsJson(string json)
    {
        PostWebMessageAsJsonInternal(json);
    }

    /// <summary>
    /// Initializes the WebView asynchronously.
    /// </summary>
    public async Task EnsureCoreWebView2Async()
    {
        if (!_isInitialized)
        {
            await InitializeWebView2Async();
            _isInitialized = true;
        }
    }

    #endregion

    #region Layout

    /// <inheritdoc />
    protected override Size MeasureOverride(Size availableSize)
    {
        // WebView fills available space
        return availableSize;
    }

    /// <inheritdoc />
    protected override Size ArrangeOverride(Size finalSize)
    {
        UpdateWebViewSize(finalSize);
        return finalSize;
    }

    #endregion

    #region Property Changed Handlers

    private static void OnSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is WebView webView)
        {
            webView.OnSourceChanged((Uri?)e.NewValue);
        }
    }

    private void OnSourceChanged(Uri? newSource)
    {
        if (newSource != null && _isInitialized)
        {
            NavigateInternal(newSource);
        }
    }

    private static void OnZoomFactorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is WebView webView)
        {
            webView.SetZoomFactorInternal((double)e.NewValue!);
        }
    }

    private static object CoerceZoomFactor(DependencyObject d, object? value)
    {
        var zoom = (double)(value ?? 1.0);
        return Math.Max(0.25, Math.Min(4.0, zoom));
    }

    private static void OnDefaultBackgroundColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is WebView webView)
        {
            webView.SetDefaultBackgroundColorInternal((Media.Color)e.NewValue!);
        }
    }

    #endregion

    #region Internal Methods (Platform Implementation Hooks)

    /// <summary>
    /// Initializes WebView2 asynchronously.
    /// </summary>
    protected virtual Task InitializeWebView2Async()
    {
        // Platform-specific WebView2 initialization
        return Task.CompletedTask;
    }

    /// <summary>
    /// Navigates to the specified URI internally.
    /// </summary>
    protected virtual void NavigateInternal(Uri source)
    {
        _isNavigating = true;
        OnNavigationStarting(source, false);
    }

    /// <summary>
    /// Navigates to HTML content internally.
    /// </summary>
    protected virtual void NavigateToStringInternal(string htmlContent)
    {
        _isNavigating = true;
        // Platform-specific implementation
    }

    /// <summary>
    /// Navigates back internally.
    /// </summary>
    protected virtual void GoBackInternal()
    {
        // Platform-specific implementation
    }

    /// <summary>
    /// Navigates forward internally.
    /// </summary>
    protected virtual void GoForwardInternal()
    {
        // Platform-specific implementation
    }

    /// <summary>
    /// Refreshes the current page internally.
    /// </summary>
    protected virtual void RefreshInternal()
    {
        // Platform-specific implementation
    }

    /// <summary>
    /// Stops loading internally.
    /// </summary>
    protected virtual void StopInternal()
    {
        _isNavigating = false;
        // Platform-specific implementation
    }

    /// <summary>
    /// Executes JavaScript internally.
    /// </summary>
    protected virtual Task<string> ExecuteScriptInternalAsync(string script)
    {
        // Platform-specific implementation
        return Task.FromResult(string.Empty);
    }

    /// <summary>
    /// Posts a string message internally.
    /// </summary>
    protected virtual void PostWebMessageAsStringInternal(string message)
    {
        // Platform-specific implementation
    }

    /// <summary>
    /// Posts a JSON message internally.
    /// </summary>
    protected virtual void PostWebMessageAsJsonInternal(string json)
    {
        // Platform-specific implementation
    }

    /// <summary>
    /// Sets the zoom factor internally.
    /// </summary>
    protected virtual void SetZoomFactorInternal(double zoomFactor)
    {
        // Platform-specific implementation
    }

    /// <summary>
    /// Sets the default background color internally.
    /// </summary>
    protected virtual void SetDefaultBackgroundColorInternal(Media.Color color)
    {
        // Platform-specific implementation
    }

    /// <summary>
    /// Updates the WebView size.
    /// </summary>
    protected virtual void UpdateWebViewSize(Size size)
    {
        // Platform-specific implementation
    }

    /// <summary>
    /// Updates the navigation state.
    /// </summary>
    protected void UpdateNavigationState(bool canGoBack, bool canGoForward)
    {
        SetValue(CanGoBackPropertyKey.DependencyProperty, canGoBack);
        SetValue(CanGoForwardPropertyKey.DependencyProperty, canGoForward);
    }

    #endregion

    #region Event Raising Methods

    /// <summary>
    /// Raises the NavigationStarting event.
    /// </summary>
    protected virtual void OnNavigationStarting(Uri uri, bool isRedirected)
    {
        var args = new WebViewNavigationStartingEventArgs(uri, isRedirected);
        NavigationStarting?.Invoke(this, args);
    }

    /// <summary>
    /// Raises the NavigationCompleted event.
    /// </summary>
    protected virtual void OnNavigationCompleted(bool isSuccess, int httpStatusCode)
    {
        _isNavigating = false;
        var args = new WebViewNavigationCompletedEventArgs(isSuccess, httpStatusCode);
        NavigationCompleted?.Invoke(this, args);
    }

    /// <summary>
    /// Raises the DocumentTitleChanged event.
    /// </summary>
    protected virtual void OnDocumentTitleChanged(string title)
    {
        _documentTitle = title;
        DocumentTitleChanged?.Invoke(this, new WebViewDocumentTitleChangedEventArgs(title));
    }

    /// <summary>
    /// Raises the WebMessageReceived event.
    /// </summary>
    protected virtual void OnWebMessageReceived(string message)
    {
        WebMessageReceived?.Invoke(this, new WebViewWebMessageReceivedEventArgs(message));
    }

    /// <summary>
    /// Raises the NewWindowRequested event.
    /// </summary>
    protected virtual void OnNewWindowRequested(Uri uri, bool isUserInitiated)
    {
        var args = new WebViewNewWindowRequestedEventArgs(uri, isUserInitiated);
        NewWindowRequested?.Invoke(this, args);
    }

    /// <summary>
    /// Raises the ContentLoading event.
    /// </summary>
    protected virtual void OnContentLoading(bool isLoading)
    {
        ContentLoading?.Invoke(this, new WebViewContentLoadingEventArgs(isLoading));
    }

    /// <summary>
    /// Raises the SourceChanged event.
    /// </summary>
    protected virtual void OnSourceChanged(Uri? source, bool isNewDocument)
    {
        SourceChanged?.Invoke(this, new WebViewSourceChangedEventArgs(source, isNewDocument));
    }

    #endregion
}

#region Event Args

/// <summary>
/// Event arguments for navigation starting events.
/// </summary>
public class WebViewNavigationStartingEventArgs : EventArgs
{
    /// <summary>
    /// Gets the URI being navigated to.
    /// </summary>
    public Uri Uri { get; }

    /// <summary>
    /// Gets a value indicating whether this is a redirect.
    /// </summary>
    public bool IsRedirected { get; }

    /// <summary>
    /// Gets or sets a value indicating whether to cancel the navigation.
    /// </summary>
    public bool Cancel { get; set; }

    /// <summary>
    /// Initializes a new instance of the WebViewNavigationStartingEventArgs class.
    /// </summary>
    public WebViewNavigationStartingEventArgs(Uri uri, bool isRedirected)
    {
        Uri = uri;
        IsRedirected = isRedirected;
    }
}

/// <summary>
/// Event arguments for navigation completed events.
/// </summary>
public class WebViewNavigationCompletedEventArgs : EventArgs
{
    /// <summary>
    /// Gets a value indicating whether the navigation was successful.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets the HTTP status code.
    /// </summary>
    public int HttpStatusCode { get; }

    /// <summary>
    /// Initializes a new instance of the WebViewNavigationCompletedEventArgs class.
    /// </summary>
    public WebViewNavigationCompletedEventArgs(bool isSuccess, int httpStatusCode)
    {
        IsSuccess = isSuccess;
        HttpStatusCode = httpStatusCode;
    }
}

/// <summary>
/// Event arguments for new window requested events.
/// </summary>
public class WebViewNewWindowRequestedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the URI for the new window.
    /// </summary>
    public Uri Uri { get; }

    /// <summary>
    /// Gets a value indicating whether the request was user-initiated.
    /// </summary>
    public bool IsUserInitiated { get; }

    /// <summary>
    /// Gets or sets a value indicating whether the new window creation is handled.
    /// </summary>
    public bool Handled { get; set; }

    /// <summary>
    /// Initializes a new instance of the WebViewNewWindowRequestedEventArgs class.
    /// </summary>
    public WebViewNewWindowRequestedEventArgs(Uri uri, bool isUserInitiated)
    {
        Uri = uri;
        IsUserInitiated = isUserInitiated;
    }
}

/// <summary>
/// Event arguments for document title changed events.
/// </summary>
public class WebViewDocumentTitleChangedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the new document title.
    /// </summary>
    public string Title { get; }

    /// <summary>
    /// Initializes a new instance of the WebViewDocumentTitleChangedEventArgs class.
    /// </summary>
    public WebViewDocumentTitleChangedEventArgs(string title)
    {
        Title = title;
    }
}

/// <summary>
/// Event arguments for web message received events.
/// </summary>
public class WebViewWebMessageReceivedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the message received.
    /// </summary>
    public string WebMessageAsString { get; }

    /// <summary>
    /// Initializes a new instance of the WebViewWebMessageReceivedEventArgs class.
    /// </summary>
    public WebViewWebMessageReceivedEventArgs(string message)
    {
        WebMessageAsString = message;
    }
}

/// <summary>
/// Event arguments for content loading events.
/// </summary>
public class WebViewContentLoadingEventArgs : EventArgs
{
    /// <summary>
    /// Gets a value indicating whether content is loading.
    /// </summary>
    public bool IsLoading { get; }

    /// <summary>
    /// Initializes a new instance of the WebViewContentLoadingEventArgs class.
    /// </summary>
    public WebViewContentLoadingEventArgs(bool isLoading)
    {
        IsLoading = isLoading;
    }
}

/// <summary>
/// Event arguments for source changed events.
/// </summary>
public class WebViewSourceChangedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the new source URI.
    /// </summary>
    public Uri? Source { get; }

    /// <summary>
    /// Gets a value indicating whether this is a new document.
    /// </summary>
    public bool IsNewDocument { get; }

    /// <summary>
    /// Initializes a new instance of the WebViewSourceChangedEventArgs class.
    /// </summary>
    public WebViewSourceChangedEventArgs(Uri? source, bool isNewDocument)
    {
        Source = source;
        IsNewDocument = isNewDocument;
    }
}

#endregion
