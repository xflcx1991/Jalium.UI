namespace Jalium.UI.Controls.Navigation;

/// <summary>
/// Contains methods, properties, and events to support navigation within an application.
/// </summary>
public sealed class NavigationService
{
    private readonly Stack<JournalEntry> _backStack = new();
    private readonly Stack<JournalEntry> _forwardStack = new();
    private object? _content;
    private Uri? _currentSource;
    private Frame? _frame;

    /// <summary>
    /// Initializes a new instance of the <see cref="NavigationService"/> class.
    /// </summary>
    public NavigationService()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NavigationService"/> class for the specified frame.
    /// </summary>
    /// <param name="frame">The frame to provide navigation for.</param>
    internal NavigationService(Frame frame)
    {
        _frame = frame;
    }

    #region Properties

    /// <summary>
    /// Gets the content that is currently displayed.
    /// </summary>
    public object? Content
    {
        get => _content;
        set
        {
            if (_content != value)
            {
                var oldContent = _content;
                _content = value;
                OnContentChanged(oldContent, value);
            }
        }
    }

    /// <summary>
    /// Gets or sets the URI of the current content.
    /// </summary>
    public Uri? Source
    {
        get => _currentSource;
        set
        {
            if (value != null)
            {
                Navigate(value);
            }
        }
    }

    /// <summary>
    /// Gets or sets the URI of the current content, or the URI of the content that is being navigated to.
    /// </summary>
    public Uri? CurrentSource => _currentSource;

    /// <summary>
    /// Gets or sets the per-instance content loader used to resolve URI navigation.
    /// </summary>
    public Func<Uri, object?>? ContentLoader { get; set; }

    /// <summary>
    /// Gets or sets the global fallback content loader used when <see cref="ContentLoader"/> is not provided.
    /// </summary>
    public static Func<Uri, object?>? DefaultContentLoader { get; set; }

    /// <summary>
    /// Gets a value indicating whether there is at least one entry in back navigation history.
    /// </summary>
    public bool CanGoBack => _backStack.Count > 0;

    /// <summary>
    /// Gets a value indicating whether there is at least one entry in forward navigation history.
    /// </summary>
    public bool CanGoForward => _forwardStack.Count > 0;

    /// <summary>
    /// Gets an IEnumerable that can be used to enumerate the entries in back navigation history.
    /// </summary>
    public IEnumerable<JournalEntry> BackStack => _backStack;

    /// <summary>
    /// Gets an IEnumerable that can be used to enumerate the entries in forward navigation history.
    /// </summary>
    public IEnumerable<JournalEntry> ForwardStack => _forwardStack;

    #endregion

    #region Navigation Methods

    /// <summary>
    /// Navigates to the content specified by the URI.
    /// </summary>
    /// <param name="source">The URI of the content to navigate to.</param>
    /// <returns>True if navigation is not canceled; otherwise, false.</returns>
    public bool Navigate(Uri source)
    {
        return Navigate(source, null);
    }

    /// <summary>
    /// Navigates to the content specified by the URI with extra data.
    /// </summary>
    /// <param name="source">The URI of the content to navigate to.</param>
    /// <param name="extraData">Additional data for use by the target page.</param>
    /// <returns>True if navigation is not canceled; otherwise, false.</returns>
    public bool Navigate(Uri source, object? extraData)
    {
        return Navigate(source, extraData, false);
    }

    /// <summary>
    /// Navigates to the content specified by the URI with extra data and sandboxing option.
    /// </summary>
    /// <param name="source">The URI of the content to navigate to.</param>
    /// <param name="extraData">Additional data for use by the target page.</param>
    /// <param name="sandboxExternalContent">True to sandbox external content; otherwise, false.</param>
    /// <returns>True if navigation is not canceled; otherwise, false.</returns>
    public bool Navigate(Uri source, object? extraData, bool sandboxExternalContent)
    {
        // Create navigation event args
        var navigatingArgs = new NavigatingCancelEventArgs(
            extraData,
            NavigationMode.New,
            null);

        // Raise Navigating event
        Navigating?.Invoke(this, navigatingArgs);

        if (navigatingArgs.Cancel)
        {
            return false;
        }

        try
        {
            // Save current to back stack if we have content
            if (_content != null)
            {
                var journalEntry = CreateJournalEntry(_currentSource, _content);
                _backStack.Push(journalEntry);
            }

            // Clear forward stack
            _forwardStack.Clear();

            // Load new content
            var content = LoadContent(source);
            if (content == null)
            {
                RaiseNavigationFailed(source, new InvalidOperationException("Failed to load content"));
                return false;
            }

            _currentSource = source;
            _content = content;

            // Update frame if attached
            if (_frame != null)
            {
                _frame.Content = content;
            }

            // Raise Navigated event
            var navigatedArgs = new NavigationEventArgs(
                content,
                extraData,
                NavigationMode.New,
                content?.GetType());

            Navigated?.Invoke(this, navigatedArgs);

            // Raise LoadCompleted event
            LoadCompleted?.Invoke(this, navigatedArgs);

            return true;
        }
        catch (Exception ex)
        {
            RaiseNavigationFailed(source, ex);
            return false;
        }
    }

    /// <summary>
    /// Navigates to the specified content object.
    /// </summary>
    /// <param name="root">The object to navigate to.</param>
    /// <returns>True if navigation is not canceled; otherwise, false.</returns>
    public bool Navigate(object root)
    {
        return Navigate(root, null);
    }

    /// <summary>
    /// Navigates to the specified content object with extra data.
    /// </summary>
    /// <param name="root">The object to navigate to.</param>
    /// <param name="extraData">Additional data for use by the target.</param>
    /// <returns>True if navigation is not canceled; otherwise, false.</returns>
    public bool Navigate(object root, object? extraData)
    {
        // Create navigation event args
        var navigatingArgs = new NavigatingCancelEventArgs(
            extraData,
            NavigationMode.New,
            root?.GetType());

        // Raise Navigating event
        Navigating?.Invoke(this, navigatingArgs);

        if (navigatingArgs.Cancel)
        {
            return false;
        }

        try
        {
            // Save current to back stack if we have content
            if (_content != null)
            {
                var journalEntry = CreateJournalEntry(_currentSource, _content);
                _backStack.Push(journalEntry);
            }

            // Clear forward stack
            _forwardStack.Clear();

            _currentSource = null;
            _content = root;

            // Update frame if attached
            if (_frame != null)
            {
                _frame.Content = root;
            }

            // Raise Navigated event
            var navigatedArgs = new NavigationEventArgs(
                root,
                extraData,
                NavigationMode.New,
                root?.GetType());

            Navigated?.Invoke(this, navigatedArgs);

            // Raise LoadCompleted event
            LoadCompleted?.Invoke(this, navigatedArgs);

            return true;
        }
        catch (Exception ex)
        {
            RaiseNavigationFailed(null, ex);
            return false;
        }
    }

    /// <summary>
    /// Navigates to the most recent entry in back navigation history.
    /// </summary>
    public void GoBack()
    {
        if (!CanGoBack)
        {
            throw new InvalidOperationException("Cannot go back when there is no back history.");
        }

        var entry = _backStack.Pop();

        // Create navigation event args
        var navigatingArgs = new NavigatingCancelEventArgs(
            null,
            NavigationMode.Back,
            null);

        // Raise Navigating event
        Navigating?.Invoke(this, navigatingArgs);

        if (navigatingArgs.Cancel)
        {
            _backStack.Push(entry);
            return;
        }

        var previousSource = _currentSource;
        var previousContent = _content;
        JournalEntry? forwardEntry = null;

        try
        {
            // Save current to forward stack
            if (_content != null)
            {
                forwardEntry = CreateJournalEntry(_currentSource, _content);
                _forwardStack.Push(forwardEntry);
            }

            var restoredContent = RestoreJournalContent(entry);
            if (restoredContent == null)
            {
                throw new InvalidOperationException("Failed to load content for back navigation.");
            }

            _currentSource = entry.Source;
            _content = restoredContent;

            // Restore custom state
            entry.CustomContentState?.Replay(restoredContent, NavigationMode.Back);

            // Update frame if attached
            if (_frame != null)
            {
                _frame.Content = _content;
            }

            // Raise Navigated event
            var navigatedArgs = new NavigationEventArgs(
                _content,
                null,
                NavigationMode.Back,
                _content?.GetType());

            Navigated?.Invoke(this, navigatedArgs);
            LoadCompleted?.Invoke(this, navigatedArgs);
        }
        catch (Exception ex)
        {
            // Restore previous state and history if replay fails.
            if (forwardEntry != null && _forwardStack.Count > 0 && ReferenceEquals(_forwardStack.Peek(), forwardEntry))
            {
                _forwardStack.Pop();
            }

            _backStack.Push(entry);
            _currentSource = previousSource;
            _content = previousContent;
            RaiseNavigationFailed(entry.Source, ex);
        }
    }

    /// <summary>
    /// Navigates to the most recent entry in forward navigation history.
    /// </summary>
    public void GoForward()
    {
        if (!CanGoForward)
        {
            throw new InvalidOperationException("Cannot go forward when there is no forward history.");
        }

        var entry = _forwardStack.Pop();

        // Create navigation event args
        var navigatingArgs = new NavigatingCancelEventArgs(
            null,
            NavigationMode.Forward,
            null);

        // Raise Navigating event
        Navigating?.Invoke(this, navigatingArgs);

        if (navigatingArgs.Cancel)
        {
            _forwardStack.Push(entry);
            return;
        }

        var previousSource = _currentSource;
        var previousContent = _content;
        JournalEntry? backEntry = null;

        try
        {
            // Save current to back stack
            if (_content != null)
            {
                backEntry = CreateJournalEntry(_currentSource, _content);
                _backStack.Push(backEntry);
            }

            var restoredContent = RestoreJournalContent(entry);
            if (restoredContent == null)
            {
                throw new InvalidOperationException("Failed to load content for forward navigation.");
            }

            _currentSource = entry.Source;
            _content = restoredContent;

            // Restore custom state
            entry.CustomContentState?.Replay(restoredContent, NavigationMode.Forward);

            // Update frame if attached
            if (_frame != null)
            {
                _frame.Content = _content;
            }

            // Raise Navigated event
            var navigatedArgs = new NavigationEventArgs(
                _content,
                null,
                NavigationMode.Forward,
                _content?.GetType());

            Navigated?.Invoke(this, navigatedArgs);
            LoadCompleted?.Invoke(this, navigatedArgs);
        }
        catch (Exception ex)
        {
            // Restore previous state and history if replay fails.
            if (backEntry != null && _backStack.Count > 0 && ReferenceEquals(_backStack.Peek(), backEntry))
            {
                _backStack.Pop();
            }

            _forwardStack.Push(entry);
            _currentSource = previousSource;
            _content = previousContent;
            RaiseNavigationFailed(entry.Source, ex);
        }
    }

    /// <summary>
    /// Reloads the current page.
    /// </summary>
    public void Refresh()
    {
        if (_currentSource == null && _content == null)
        {
            return;
        }

        // Create navigation event args
        var navigatingArgs = new NavigatingCancelEventArgs(
            null,
            NavigationMode.Refresh,
            _content?.GetType());

        // Raise Navigating event
        Navigating?.Invoke(this, navigatingArgs);

        if (navigatingArgs.Cancel)
        {
            return;
        }

        // Reload content
        if (_currentSource != null)
        {
            var refreshedContent = LoadContent(_currentSource);
            if (refreshedContent == null)
            {
                RaiseNavigationFailed(_currentSource, new InvalidOperationException("Failed to refresh content."));
                return;
            }

            _content = refreshedContent;
        }

        // Update frame if attached
        if (_frame != null)
        {
            _frame.Content = _content;
        }

        // Raise Navigated event
        var navigatedArgs = new NavigationEventArgs(
            _content,
            null,
            NavigationMode.Refresh,
            _content?.GetType());

        Navigated?.Invoke(this, navigatedArgs);
        LoadCompleted?.Invoke(this, navigatedArgs);
    }

    /// <summary>
    /// Stops any pending navigation.
    /// </summary>
    public void StopLoading()
    {
        // Raise NavigationStopped event
        NavigationStopped?.Invoke(this, new NavigationEventArgs(
            _content,
            null,
            NavigationMode.New,
            _content?.GetType()));
    }

    /// <summary>
    /// Removes the most recent journal entry from the back stack.
    /// </summary>
    /// <returns>The removed journal entry, or null if the back stack was empty.</returns>
    public JournalEntry? RemoveBackEntry()
    {
        if (_backStack.Count == 0)
        {
            return null;
        }

        return _backStack.Pop();
    }

    /// <summary>
    /// Adds a new journal entry to the navigation history.
    /// </summary>
    /// <param name="state">The custom content state to add.</param>
    public void AddBackEntry(CustomContentState state)
    {
        var entry = CreateJournalEntry(_currentSource, _content);
        entry.CustomContentState = state;
        _backStack.Push(entry);
    }

    #endregion

    #region Events

    /// <summary>
    /// Occurs when a new navigation is requested.
    /// </summary>
    public event EventHandler<NavigatingCancelEventArgs>? Navigating;

    /// <summary>
    /// Occurs when the content that is being navigated to has been found.
    /// </summary>
    public event EventHandler<NavigationEventArgs>? Navigated;

    /// <summary>
    /// Occurs when an error is raised while navigating to the requested content.
    /// </summary>
    public event EventHandler<NavigationFailedEventArgs>? NavigationFailed;

    /// <summary>
    /// Occurs when the content that was navigated to has been loaded.
    /// </summary>
    public event EventHandler<NavigationEventArgs>? LoadCompleted;

    /// <summary>
    /// Occurs when the StopLoading method is called.
    /// </summary>
    public event EventHandler<NavigationEventArgs>? NavigationStopped;

    /// <summary>
    /// Occurs periodically during download of the content.
    /// </summary>
    public event EventHandler<NavigationProgressEventArgs>? NavigationProgress;

    /// <summary>
    /// Occurs when a page fragment navigation is requested.
    /// </summary>
    public event EventHandler<FragmentNavigationEventArgs>? FragmentNavigation;

    #endregion

    #region Private Methods

    private JournalEntry CreateJournalEntry(Uri? source, object? content)
    {
        var entry = new JournalEntry(source, content?.ToString())
        {
            Content = content,
            CustomContentState = GetCustomContentState()
        };

        return entry;
    }

    private object? RestoreJournalContent(JournalEntry entry)
    {
        if (entry.Content != null)
        {
            return entry.Content;
        }

        return LoadContent(entry.Source);
    }

    private object? LoadContent(Uri? source)
    {
        if (source == null)
        {
            return null;
        }

        if (ContentLoader != null)
        {
            return ContentLoader(source);
        }

        if (DefaultContentLoader != null)
        {
            return DefaultContentLoader(source);
        }

        var startupLoader = Application.StartupObjectLoader;
        var app = Application.Current;
        if (startupLoader != null && app != null)
        {
            var uriValue = source.IsAbsoluteUri ? source.AbsoluteUri : source.OriginalString;
            return startupLoader(app, uriValue);
        }

        return null;
    }

    private CustomContentState? GetCustomContentState()
    {
        if (_content is IProvideCustomContentState provider)
        {
            return provider.GetContentState();
        }
        return null;
    }

    private void RaiseNavigationFailed(Uri? source, Exception exception)
    {
        var args = new NavigationFailedEventArgs(source, exception);
        NavigationFailed?.Invoke(this, args);
    }

    private void OnContentChanged(object? oldContent, object? newContent)
    {
        // Notify content of navigation if it's a Page
        if (oldContent is Page oldPage)
        {
            oldPage.OnNavigatedFrom(new NavigationEventArgs(oldContent, null, NavigationMode.New, newContent?.GetType()));
        }

        if (newContent is Page newPage)
        {
            newPage.OnNavigatedTo(new NavigationEventArgs(newContent, null, NavigationMode.New, newContent?.GetType()));
        }
    }

    #endregion
}

/// <summary>
/// Provides data for the NavigationProgress event.
/// </summary>
public sealed class NavigationProgressEventArgs : EventArgs
{
    /// <summary>
    /// Gets the URI of the target content.
    /// </summary>
    public Uri? Uri { get; }

    /// <summary>
    /// Gets the number of bytes downloaded.
    /// </summary>
    public long BytesRead { get; }

    /// <summary>
    /// Gets the maximum number of bytes to download.
    /// </summary>
    public long MaxBytes { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="NavigationProgressEventArgs"/> class.
    /// </summary>
    public NavigationProgressEventArgs(Uri? uri, long bytesRead, long maxBytes)
    {
        Uri = uri;
        BytesRead = bytesRead;
        MaxBytes = maxBytes;
    }
}

/// <summary>
/// Provides data for the FragmentNavigation event.
/// </summary>
public sealed class FragmentNavigationEventArgs : EventArgs
{
    /// <summary>
    /// Gets the fragment identifier.
    /// </summary>
    public string Fragment { get; }

    /// <summary>
    /// Gets or sets a value indicating whether the fragment navigation was handled.
    /// </summary>
    public bool Handled { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="FragmentNavigationEventArgs"/> class.
    /// </summary>
    public FragmentNavigationEventArgs(string fragment)
    {
        Fragment = fragment;
    }
}

/// <summary>
/// Provides data for the NavigationFailed event.
/// </summary>
public sealed class NavigationFailedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the URI for the content that could not be navigated to.
    /// </summary>
    public Uri? Uri { get; }

    /// <summary>
    /// Gets the exception that was raised.
    /// </summary>
    public Exception Exception { get; }

    /// <summary>
    /// Gets or sets a value indicating whether the exception was handled.
    /// </summary>
    public bool Handled { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="NavigationFailedEventArgs"/> class.
    /// </summary>
    public NavigationFailedEventArgs(Uri? uri, Exception exception)
    {
        Uri = uri;
        Exception = exception;
    }
}
