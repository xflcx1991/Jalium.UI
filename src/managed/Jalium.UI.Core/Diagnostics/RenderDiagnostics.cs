using System.Collections.Concurrent;

namespace Jalium.UI.Diagnostics;

/// <summary>
/// Per-frame render diagnostics: Overdraw map, dirty region history, GPU resource stats.
/// Populated by the rendering pipeline; consumed by DevTools overlays.
/// </summary>
public static class RenderDiagnostics
{
    public enum OverlayMode
    {
        None,
        Overdraw,
        DirtyRegions,
    }

    public sealed class OverdrawCell
    {
        public int X;
        public int Y;
        public int Width;
        public int Height;
        public int DrawCount;
    }

    public sealed class DirtyRegionSnapshot
    {
        public DateTime Timestamp { get; }
        public Rect Region { get; }
        public int FrameIndex { get; }
        internal DirtyRegionSnapshot(Rect region, int frameIndex)
        {
            Timestamp = DateTime.Now;
            Region = region;
            FrameIndex = frameIndex;
        }
    }

    public sealed class GpuResourceSnapshot
    {
        public DateTime Timestamp { get; }
        public int GlyphAtlasSlotsUsed { get; }
        public int GlyphAtlasSlotsTotal { get; }
        public long GlyphAtlasBytes { get; }
        public int PathCacheEntries { get; }
        public long PathCacheBytes { get; }
        public int TextureCount { get; }
        public long TextureBytes { get; }

        internal GpuResourceSnapshot(
            int glyphUsed, int glyphTotal, long glyphBytes,
            int pathEntries, long pathBytes,
            int textureCount, long textureBytes)
        {
            Timestamp = DateTime.Now;
            GlyphAtlasSlotsUsed = glyphUsed;
            GlyphAtlasSlotsTotal = glyphTotal;
            GlyphAtlasBytes = glyphBytes;
            PathCacheEntries = pathEntries;
            PathCacheBytes = pathBytes;
            TextureCount = textureCount;
            TextureBytes = textureBytes;
        }
    }

    public const int OverdrawGridCells = 32;
    private const int DirtyHistoryCapacity = 128;

    private static OverlayMode s_mode;
    private static GpuResourceSnapshot? s_latestGpuSnapshot;
    private static readonly ConcurrentQueue<DirtyRegionSnapshot> s_dirtyHistory = new();
    private static int s_frameCounter;
    private static readonly object s_overdrawLock = new();
    private static int[,]? s_overdrawBins;
    private static int s_overdrawBinWidth;
    private static int s_overdrawBinHeight;
    private static double s_overdrawCellW;
    private static double s_overdrawCellH;
    private static double s_overdrawWindowW;
    private static double s_overdrawWindowH;

    public static OverlayMode Mode
    {
        get => s_mode;
        set
        {
            if (s_mode != value)
            {
                s_mode = value;
                OverlayModeChanged?.Invoke(null, EventArgs.Empty);
            }
        }
    }

    public static event EventHandler? OverlayModeChanged;

    public static GpuResourceSnapshot? LatestGpuSnapshot => s_latestGpuSnapshot;

    public static void PublishGpuSnapshot(GpuResourceSnapshot snapshot)
    {
        s_latestGpuSnapshot = snapshot;
    }

    public static void PublishGpuSnapshot(
        int glyphUsed, int glyphTotal, long glyphBytes,
        int pathEntries = 0, long pathBytes = 0,
        int textureCount = 0, long textureBytes = 0)
    {
        s_latestGpuSnapshot = new GpuResourceSnapshot(
            glyphUsed, glyphTotal, glyphBytes,
            pathEntries, pathBytes, textureCount, textureBytes);
    }

    public static void RecordDirtyRegion(Rect region)
    {
        if (region.IsEmpty) return;
        int index = Interlocked.Increment(ref s_frameCounter);
        s_dirtyHistory.Enqueue(new DirtyRegionSnapshot(region, index));
        while (s_dirtyHistory.Count > DirtyHistoryCapacity && s_dirtyHistory.TryDequeue(out _)) { }
    }

    public static IReadOnlyList<DirtyRegionSnapshot> SnapshotDirtyHistory() => s_dirtyHistory.ToArray();

    public static void ResetOverdrawForFrame(double windowWidth, double windowHeight)
    {
        if (Mode != OverlayMode.Overdraw) return;
        if (windowWidth <= 0 || windowHeight <= 0) return;
        lock (s_overdrawLock)
        {
            if (s_overdrawBins == null ||
                s_overdrawBinWidth != OverdrawGridCells ||
                s_overdrawBinHeight != OverdrawGridCells)
            {
                s_overdrawBins = new int[OverdrawGridCells, OverdrawGridCells];
                s_overdrawBinWidth = OverdrawGridCells;
                s_overdrawBinHeight = OverdrawGridCells;
            }
            Array.Clear(s_overdrawBins);
            s_overdrawWindowW = windowWidth;
            s_overdrawWindowH = windowHeight;
            s_overdrawCellW = windowWidth / OverdrawGridCells;
            s_overdrawCellH = windowHeight / OverdrawGridCells;
        }
    }

    public static void RecordDraw(Rect bounds)
    {
        if (Mode != OverlayMode.Overdraw) return;
        lock (s_overdrawLock)
        {
            if (s_overdrawBins == null || s_overdrawCellW <= 0 || s_overdrawCellH <= 0) return;
            double maxX = Math.Min(bounds.X + bounds.Width, s_overdrawWindowW);
            double maxY = Math.Min(bounds.Y + bounds.Height, s_overdrawWindowH);
            if (maxX <= 0 || maxY <= 0) return;
            int x0 = Math.Max(0, (int)Math.Floor(Math.Max(0, bounds.X) / s_overdrawCellW));
            int y0 = Math.Max(0, (int)Math.Floor(Math.Max(0, bounds.Y) / s_overdrawCellH));
            int x1 = Math.Min(OverdrawGridCells - 1, (int)Math.Floor((maxX - 0.01) / s_overdrawCellW));
            int y1 = Math.Min(OverdrawGridCells - 1, (int)Math.Floor((maxY - 0.01) / s_overdrawCellH));
            for (int y = y0; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    s_overdrawBins[x, y]++;
                }
            }
        }
    }

    public static IReadOnlyList<OverdrawCell> SnapshotOverdraw()
    {
        lock (s_overdrawLock)
        {
            if (s_overdrawBins == null) return Array.Empty<OverdrawCell>();
            var list = new List<OverdrawCell>();
            for (int y = 0; y < OverdrawGridCells; y++)
            {
                for (int x = 0; x < OverdrawGridCells; x++)
                {
                    int count = s_overdrawBins[x, y];
                    if (count == 0) continue;
                    list.Add(new OverdrawCell
                    {
                        X = (int)(x * s_overdrawCellW),
                        Y = (int)(y * s_overdrawCellH),
                        Width = (int)Math.Ceiling(s_overdrawCellW),
                        Height = (int)Math.Ceiling(s_overdrawCellH),
                        DrawCount = count,
                    });
                }
            }
            return list;
        }
    }
}
