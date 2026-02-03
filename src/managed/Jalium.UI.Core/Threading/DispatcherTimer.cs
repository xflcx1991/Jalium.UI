using Jalium.UI;

namespace Jalium.UI.Threading;

/// <summary>
/// A timer that is integrated into the <see cref="Dispatcher"/> queue which is
/// processed at a specified interval of time and at a specified priority.
/// </summary>
public class DispatcherTimer
{
    private readonly Dispatcher _dispatcher;
    private Timer? _timer;
    private TimeSpan _interval;
    private bool _isEnabled;
    private object? _tag;

    /// <summary>
    /// Occurs when the timer interval has elapsed.
    /// </summary>
    public event EventHandler? Tick;

    /// <summary>
    /// Initializes a new instance of the <see cref="DispatcherTimer"/> class.
    /// </summary>
    public DispatcherTimer()
        : this(DispatcherPriority.Background)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DispatcherTimer"/> class
    /// which processes timer events at the specified priority.
    /// </summary>
    /// <param name="priority">The priority at which to invoke the timer.</param>
    public DispatcherTimer(DispatcherPriority priority)
        : this(priority, Dispatcher.CurrentDispatcher ?? Dispatcher.GetForCurrentThread())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DispatcherTimer"/> class
    /// which processes timer events at the specified priority on the specified dispatcher.
    /// </summary>
    /// <param name="priority">The priority at which to invoke the timer.</param>
    /// <param name="dispatcher">The dispatcher to associate with the timer.</param>
    public DispatcherTimer(DispatcherPriority priority, Dispatcher dispatcher)
    {
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        Priority = priority;
        _interval = TimeSpan.Zero;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DispatcherTimer"/> class
    /// which uses the specified time interval, priority, event handler, and dispatcher.
    /// </summary>
    /// <param name="interval">The period of time between ticks.</param>
    /// <param name="priority">The priority at which to invoke the timer.</param>
    /// <param name="callback">The event handler to call when the Tick event occurs.</param>
    /// <param name="dispatcher">The dispatcher to associate with the timer.</param>
    public DispatcherTimer(TimeSpan interval, DispatcherPriority priority, EventHandler callback, Dispatcher dispatcher)
        : this(priority, dispatcher)
    {
        _interval = interval;

        if (callback != null)
        {
            Tick += callback;
        }
    }

    /// <summary>
    /// Gets the <see cref="Dispatcher"/> associated with this <see cref="DispatcherTimer"/>.
    /// </summary>
    public Dispatcher Dispatcher => _dispatcher;

    /// <summary>
    /// Gets or sets a value that indicates whether the timer is running.
    /// </summary>
    public bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            if (_isEnabled != value)
            {
                _isEnabled = value;

                if (_isEnabled)
                {
                    StartTimer();
                }
                else
                {
                    StopTimer();
                }
            }
        }
    }

    /// <summary>
    /// Gets or sets the period of time between timer ticks.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="value"/> is less than 0 or greater than <see cref="int.MaxValue"/> milliseconds.
    /// </exception>
    public TimeSpan Interval
    {
        get => _interval;
        set
        {
            if (value.TotalMilliseconds < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "Interval cannot be negative.");
            }

            if (value.TotalMilliseconds > int.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "Interval is too large.");
            }

            bool wasRunning = _isEnabled;

            if (wasRunning)
            {
                StopTimer();
            }

            _interval = value;

            if (wasRunning)
            {
                StartTimer();
            }
        }
    }

    /// <summary>
    /// Gets or sets the priority at which timer events are dispatched.
    /// </summary>
    public DispatcherPriority Priority { get; set; }

    /// <summary>
    /// Gets or sets a user-defined data object.
    /// </summary>
    public object? Tag
    {
        get => _tag;
        set => _tag = value;
    }

    /// <summary>
    /// Starts the <see cref="DispatcherTimer"/>.
    /// </summary>
    public void Start()
    {
        IsEnabled = true;
    }

    /// <summary>
    /// Stops the <see cref="DispatcherTimer"/>.
    /// </summary>
    public void Stop()
    {
        IsEnabled = false;
    }

    private void StartTimer()
    {
        if (_timer != null)
        {
            return;
        }

        // Calculate the interval in milliseconds
        // Use at least 1ms to avoid infinite spinning
        int intervalMs = Math.Max(1, (int)_interval.TotalMilliseconds);

        _timer = new Timer(
            OnTimerCallback,
            null,
            intervalMs,
            intervalMs);
    }

    private void StopTimer()
    {
        if (_timer == null)
        {
            return;
        }

        _timer.Dispose();
        _timer = null;
    }

    private void OnTimerCallback(object? state)
    {
        if (!_isEnabled)
        {
            return;
        }

        // Dispatch the tick event to the associated dispatcher's thread
        try
        {
            if (_dispatcher.CheckAccess())
            {
                // Already on the dispatcher thread
                RaiseTick();
            }
            else
            {
                // Marshal to the dispatcher thread
                _dispatcher.BeginInvoke(RaiseTick);
            }
        }
        catch
        {
            // Ignore exceptions if the dispatcher is shutting down
        }
    }

    private void RaiseTick()
    {
        if (!_isEnabled)
        {
            return;
        }

        try
        {
            Tick?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            // Log the exception but don't stop the timer
            System.Diagnostics.Debug.WriteLine($"DispatcherTimer.Tick exception: {ex}");
        }
    }
}
