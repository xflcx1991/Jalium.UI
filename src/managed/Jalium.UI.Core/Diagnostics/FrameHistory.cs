namespace Jalium.UI.Diagnostics;

/// <summary>
/// Per-window ring buffer of frame timings. RenderDebugHud pushes one sample
/// per completed frame; DevTools reads the buffer to render trend curves.
/// </summary>
public sealed class FrameHistory
{
    public readonly struct Sample
    {
        public Sample(double layoutMs, double renderMs, double presentMs, double totalMs, int dirtyElements)
        {
            LayoutMs = layoutMs;
            RenderMs = renderMs;
            PresentMs = presentMs;
            TotalMs = totalMs;
            DirtyElements = dirtyElements;
        }

        public double LayoutMs { get; }
        public double RenderMs { get; }
        public double PresentMs { get; }
        public double TotalMs { get; }
        public int DirtyElements { get; }
    }

    public const int Capacity = 300;

    private readonly Sample[] _samples = new Sample[Capacity];
    private int _head;
    private int _count;
    private long _totalFrames;
    private readonly object _lock = new();

    public int Count => Volatile.Read(ref _count);
    public long TotalFrames => Interlocked.Read(ref _totalFrames);

    public void Push(Sample sample)
    {
        lock (_lock)
        {
            _samples[_head] = sample;
            _head = (_head + 1) % Capacity;
            if (_count < Capacity) _count++;
            Interlocked.Increment(ref _totalFrames);
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            _head = 0;
            _count = 0;
            Array.Clear(_samples);
            Interlocked.Exchange(ref _totalFrames, 0);
        }
    }

    /// <summary>
    /// Copies the samples in chronological order (oldest first) into the provided buffer.
    /// Returns the number of samples copied.
    /// </summary>
    public int CopyTo(Span<Sample> destination)
    {
        lock (_lock)
        {
            int n = Math.Min(_count, destination.Length);
            int start = (_head - _count + Capacity) % Capacity;
            for (int i = 0; i < n; i++)
            {
                destination[i] = _samples[(start + i) % Capacity];
            }
            return n;
        }
    }
}
