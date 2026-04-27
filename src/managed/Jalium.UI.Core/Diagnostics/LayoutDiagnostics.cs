using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Jalium.UI.Diagnostics;

/// <summary>
/// Aggregates Measure/Arrange/Invalidation statistics for each UIElement while
/// recording is enabled. Zero cost when <see cref="IsRecording"/> is false.
/// </summary>
public static class LayoutDiagnostics
{
    public enum InvalidationKind
    {
        Measure,
        Arrange,
        Visual,
    }

    public sealed class ElementStats
    {
        private readonly object _lock = new();
        public WeakReference<UIElement> ElementRef { get; }
        public string ElementName { get; }
        public string TypeName { get; }
        public int MeasureCount { get; private set; }
        public int ArrangeCount { get; private set; }
        public int InvalidateMeasureCount { get; private set; }
        public int InvalidateArrangeCount { get; private set; }
        public int InvalidateVisualCount { get; private set; }
        public double MeasureTotalMicros { get; private set; }
        public double MeasureWorstMicros { get; private set; }
        public double ArrangeTotalMicros { get; private set; }
        public double ArrangeWorstMicros { get; private set; }
        public long LastUpdateTimestamp { get; private set; }

        internal ElementStats(UIElement element)
        {
            ElementRef = new WeakReference<UIElement>(element);
            TypeName = element.GetType().Name;
            ElementName = element is FrameworkElement fe && !string.IsNullOrEmpty(fe.Name) ? fe.Name : "";
        }

        internal void RecordMeasure(double microseconds)
        {
            lock (_lock)
            {
                MeasureCount++;
                MeasureTotalMicros += microseconds;
                if (microseconds > MeasureWorstMicros) MeasureWorstMicros = microseconds;
                LastUpdateTimestamp = Stopwatch.GetTimestamp();
            }
        }

        internal void RecordArrange(double microseconds)
        {
            lock (_lock)
            {
                ArrangeCount++;
                ArrangeTotalMicros += microseconds;
                if (microseconds > ArrangeWorstMicros) ArrangeWorstMicros = microseconds;
                LastUpdateTimestamp = Stopwatch.GetTimestamp();
            }
        }

        internal void RecordInvalidation(InvalidationKind kind)
        {
            lock (_lock)
            {
                switch (kind)
                {
                    case InvalidationKind.Measure: InvalidateMeasureCount++; break;
                    case InvalidationKind.Arrange: InvalidateArrangeCount++; break;
                    case InvalidationKind.Visual: InvalidateVisualCount++; break;
                }
                LastUpdateTimestamp = Stopwatch.GetTimestamp();
            }
        }

        public double MeasureAverageMicros => MeasureCount == 0 ? 0 : MeasureTotalMicros / MeasureCount;
        public double ArrangeAverageMicros => ArrangeCount == 0 ? 0 : ArrangeTotalMicros / ArrangeCount;
    }

    public sealed class InvalidationEntry
    {
        public DateTime Timestamp { get; }
        public WeakReference<UIElement> ElementRef { get; }
        public string TypeName { get; }
        public string ElementName { get; }
        public InvalidationKind Kind { get; }
        public string? StackSummary { get; }

        internal InvalidationEntry(UIElement element, InvalidationKind kind)
        {
            Timestamp = DateTime.Now;
            ElementRef = new WeakReference<UIElement>(element);
            TypeName = element.GetType().Name;
            ElementName = element is FrameworkElement fe && !string.IsNullOrEmpty(fe.Name) ? fe.Name : "";
            Kind = kind;
            StackSummary = null;
        }

        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("Forwards to CaptureStackSummary which uses StackFrame.GetMethod (may be incomplete after trimming).")]
        internal InvalidationEntry(UIElement element, InvalidationKind kind, bool captureStack)
            : this(element, kind)
        {
            if (captureStack)
            {
                StackSummary = CaptureStackSummary();
            }
        }

        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("StackFrame.GetMethod metadata may be incomplete after trimming.")]
        private static string? CaptureStackSummary()
        {
            try
            {
                var st = new StackTrace(2, false);
                var sb = new System.Text.StringBuilder();
                int count = Math.Min(st.FrameCount, 6);
                for (int i = 0; i < count; i++)
                {
                    var frame = st.GetFrame(i);
                    var method = frame?.GetMethod();
                    if (method == null) continue;
                    if (sb.Length > 0) sb.Append(" > ");
                    sb.Append(method.DeclaringType?.Name).Append('.').Append(method.Name);
                }
                return sb.ToString();
            }
            catch
            {
                return null;
            }
        }
    }

    private static int s_recording;
    private static int s_captureStack;
    private const int MaxInvalidationEntries = 512;

    // Stats live in a ConditionalWeakTable so that GC reclaims entries for
    // elements the target window has detached — we never pin UIElements in
    // a strong-ref queue (the previous s_knownElements design leaked the
    // entire target-window visual history on long recording sessions).
    private static readonly ConditionalWeakTable<UIElement, ElementStats> s_statsByElement = new();
    private static readonly ConcurrentQueue<InvalidationEntry> s_invalidations = new();

    public static bool IsRecording => Volatile.Read(ref s_recording) != 0;
    public static bool CaptureStackTraces
    {
        get => Volatile.Read(ref s_captureStack) != 0;
        set => Volatile.Write(ref s_captureStack, value ? 1 : 0);
    }

    public static event EventHandler? StateChanged;

    public static void StartRecording()
    {
        if (Interlocked.Exchange(ref s_recording, 1) == 0)
            StateChanged?.Invoke(null, EventArgs.Empty);
    }

    public static void StopRecording()
    {
        if (Interlocked.Exchange(ref s_recording, 0) == 1)
            StateChanged?.Invoke(null, EventArgs.Empty);
    }

    public static void Reset()
    {
        s_statsByElement.Clear();
        while (s_invalidations.TryDequeue(out _)) { }
    }

    private static ElementStats GetOrCreateStats(UIElement element)
    {
        if (!s_statsByElement.TryGetValue(element, out var stats))
        {
            stats = new ElementStats(element);
            s_statsByElement.Add(element, stats);
        }
        return stats;
    }

    internal static void NotifyMeasure(UIElement element, double microseconds)
    {
        if (Volatile.Read(ref s_recording) == 0) return;
        if (DiagnosticsScope.ShouldIgnore(element)) return;
        GetOrCreateStats(element).RecordMeasure(microseconds);
    }

    internal static void NotifyArrange(UIElement element, double microseconds)
    {
        if (Volatile.Read(ref s_recording) == 0) return;
        if (DiagnosticsScope.ShouldIgnore(element)) return;
        GetOrCreateStats(element).RecordArrange(microseconds);
    }

    internal static void NotifyInvalidation(UIElement element, InvalidationKind kind)
    {
        if (Volatile.Read(ref s_recording) == 0) return;
        if (DiagnosticsScope.ShouldIgnore(element)) return;
        GetOrCreateStats(element).RecordInvalidation(kind);
        // Hot path: bind to the cheap entry. The RUC stack-capture overload is invoked
        // through the static delegate below, which is set by DevTools when stack tracing
        // is opted in. Keeping the RUC call out of UIElement's hot loop keeps the layout
        // engine AOT-safe by default.
        var enrich = s_stackCaptureEnricher;
        var entry = enrich != null && Volatile.Read(ref s_captureStack) != 0
            ? enrich(element, kind)
            : new InvalidationEntry(element, kind);
        s_invalidations.Enqueue(entry);
        while (s_invalidations.Count > MaxInvalidationEntries && s_invalidations.TryDequeue(out _)) { }
    }

    /// <summary>
    /// Optional enricher invoked when <see cref="CaptureStackTraces"/> is true. DevTools
    /// installs <see cref="CreateInvalidationEntryWithStack"/> here under its RUC scope so
    /// the layout hot path can stay non-RUC and AOT-safe.
    /// </summary>
    public static Func<UIElement, InvalidationKind, InvalidationEntry>? StackCaptureEnricher
    {
        get => s_stackCaptureEnricher;
        set => s_stackCaptureEnricher = value;
    }
    private static Func<UIElement, InvalidationKind, InvalidationEntry>? s_stackCaptureEnricher;

    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("Stack capture uses StackFrame.GetMethod, whose metadata may be incomplete after trimming.")]
    public static InvalidationEntry CreateInvalidationEntryWithStack(UIElement element, InvalidationKind kind)
        => new InvalidationEntry(element, kind, captureStack: true);

    /// <summary>
    /// Snapshot of per-element stats for display. We iterate the weak table
    /// directly — GC-reclaimed entries disappear automatically so the list
    /// size tracks live elements, not the entire recording history.
    /// </summary>
    public static IReadOnlyList<ElementStats> SnapshotStats()
    {
        var list = new List<ElementStats>();
        foreach (var kvp in s_statsByElement)
        {
            // Belt-and-suspenders: even if an element slipped into the table
            // before ExcludeRoot/MarkDiagnosticsIgnoredSubtree tagged its
            // ancestors, hide it from the report now that the flag is set.
            if (kvp.Key.IsDiagnosticsIgnored) continue;
            list.Add(kvp.Value);
        }
        return list;
    }

    public static IReadOnlyList<InvalidationEntry> SnapshotInvalidations()
    {
        var all = s_invalidations.ToArray();
        // Same belt-and-suspenders filter as SnapshotStats.
        var list = new List<InvalidationEntry>(all.Length);
        foreach (var e in all)
        {
            if (e.ElementRef.TryGetTarget(out var visual) && visual.IsDiagnosticsIgnored)
                continue;
            list.Add(e);
        }
        return list;
    }
}
