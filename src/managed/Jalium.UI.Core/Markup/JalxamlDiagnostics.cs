using System.Diagnostics;
using System.IO;

namespace Jalium.UI.Markup;

/// <summary>
/// 统一的 JALXAML 加载诊断通道,默认静默。打开后会把关键环节(XmlnsDefinition 扫描、
/// force-load、XAML 资源查找、ResourceDictionary 解析、.uic 加载与 fallback)以 <c>[Jalium.UI.Xaml]</c>
/// 前缀写入 <see cref="Debug.WriteLine(string)"/>,可在 Visual Studio 的 Output / Debug 窗口里过滤查看。
/// </summary>
/// <remarks>
/// <para>启用方式(任选其一,以先到者为准):</para>
/// <list type="bullet">
///   <item>环境变量 <c>JALIUM_XAML_TRACE=1</c></item>
///   <item>程序启动早期设置 <see cref="Enabled"/> <c>= true</c></item>
/// </list>
/// <para>
/// 设计原则:<see cref="Enabled"/> 关闭时所有 <c>Log</c> 调用零开销(先布尔短路,不做插值)。
/// 因此调用点都使用 <see cref="string.Format(string, object?[])"/> 重载或手动 <c>if (Enabled)</c> 守卫。
/// </para>
/// </remarks>
public static class JalxamlDiagnostics
{
    private static int _enabled = InitEnabledFromEnv();
    private static readonly string? _traceFilePath = InitTraceFileFromEnv();
    private static readonly object _fileLock = new();

    private static int InitEnabledFromEnv()
    {
        try
        {
            var value = Environment.GetEnvironmentVariable("JALIUM_XAML_TRACE");
            if (!string.IsNullOrEmpty(value)
                && (value.Equals("1", StringComparison.Ordinal)
                    || value.Equals("true", StringComparison.OrdinalIgnoreCase)
                    || value.Equals("on", StringComparison.OrdinalIgnoreCase)))
            {
                return 1;
            }
        }
        catch
        {
            // 环境变量访问被沙箱拒绝等情况,按 disabled 处理。
        }
        return 0;
    }

    private static string? InitTraceFileFromEnv()
    {
        try
        {
            var path = Environment.GetEnvironmentVariable("JALIUM_XAML_TRACE_FILE");
            if (!string.IsNullOrWhiteSpace(path))
            {
                // 首次启动截断旧文件,确保每次运行日志都是干净的。
                try { File.WriteAllText(path, string.Empty); } catch { /* ignore */ }
                return path;
            }
        }
        catch
        {
            // 沙箱/环境变量不可访问,跳过文件输出。
        }
        return null;
    }

    /// <summary>
    /// 获取当前的 trace 文件路径(从 <c>JALIUM_XAML_TRACE_FILE</c> 环境变量读取)。<c>null</c>
    /// 表示未配置文件输出。适用于 WinExe 场景下 <see cref="Debug.WriteLine"/> 看不到的时候。
    /// </summary>
    public static string? TraceFilePath => _traceFilePath;

    /// <summary>
    /// 启用/关闭诊断日志。线程安全。
    /// </summary>
    public static bool Enabled
    {
        get => Volatile.Read(ref _enabled) != 0;
        set => Volatile.Write(ref _enabled, value ? 1 : 0);
    }

    /// <summary>
    /// 写一条无格式化参数的日志。开关关闭时零开销。
    /// </summary>
    public static void Log(string message)
    {
        if (Enabled)
        {
            Emit(message);
        }
    }

    /// <summary>
    /// 写一条带格式化参数的日志。开关关闭时不做字符串插值,保持零开销。
    /// </summary>
    public static void Log(string format, object? arg0)
    {
        if (Enabled)
        {
            Emit(string.Format(format, arg0));
        }
    }

    /// <summary>
    /// 写一条带格式化参数的日志。开关关闭时不做字符串插值,保持零开销。
    /// </summary>
    public static void Log(string format, object? arg0, object? arg1)
    {
        if (Enabled)
        {
            Emit(string.Format(format, arg0, arg1));
        }
    }

    /// <summary>
    /// 写一条带格式化参数的日志。开关关闭时不做字符串插值,保持零开销。
    /// </summary>
    public static void Log(string format, object? arg0, object? arg1, object? arg2)
    {
        if (Enabled)
        {
            Emit(string.Format(format, arg0, arg1, arg2));
        }
    }

    /// <summary>
    /// 写一条带格式化参数的日志。开关关闭时不做字符串插值,保持零开销。
    /// </summary>
    public static void Log(string format, params object?[] args)
    {
        if (Enabled)
        {
            Emit(string.Format(format, args));
        }
    }

    private static void Emit(string formatted)
    {
        var line = $"[Jalium.UI.Xaml] {formatted}";
        Debug.WriteLine(line);

        // 文件 sink:WinExe 场景下 Debug.WriteLine 没 debugger attach 时不可见,
        // 设置 JALIUM_XAML_TRACE_FILE=<path> 即可把日志落到文件。
        if (_traceFilePath is not null)
        {
            try
            {
                lock (_fileLock)
                {
                    File.AppendAllText(_traceFilePath, line + Environment.NewLine);
                }
            }
            catch
            {
                // 写文件失败不阻塞 XAML 解析。
            }
        }
    }
}
