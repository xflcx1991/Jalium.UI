namespace Jalium.UI;

/// <summary>
/// 模板（DataTemplate / ItemsPanelTemplate / ControlTemplate）"延迟解析 XAML"时
/// 用于跨程序集传递祖先 ResourceDictionary 链的 ThreadStatic 桥。
///
/// 背景：模板内部 XAML 在 <c>LoadContent()</c> 时才被解析；解析器创建一个全新的
/// 空 <c>XamlParserContext</c>，此时模板被声明时的祖先资源（<c>UserControl.Resources</c> 等）
/// 已不在解析栈中。<c>{StaticResource ...}</c> 因此找不到外层声明的资源。
///
/// 修复策略：
///   1. 模板首次被 XAML 解析器扫描到时，记下当时 ambient 栈中的 ResourceDictionary 链
///      到 <see cref="DataTemplate.AmbientResourceDictionaries"/>（或同名 ItemsPanelTemplate 字段）。
///   2. <c>LoadContent()</c> 调用 XamlParser 之前把这条链放到本类的 ThreadStatic 槽。
///   3. XamlParser 实现端读取本类静态属性，把这些 ResourceDictionary 注入到新 XamlParserContext
///      的 ambient 解析栈中，让 <c>{StaticResource}</c> 能解析到。
///
/// 用 ThreadStatic 而不是改 XamlParser 委托签名，是为了保持现有公共 API 不变。
/// </summary>
public static class TemplateAmbientResourceContext
{
    [ThreadStatic]
    private static IReadOnlyList<ResourceDictionary>? _current;

    /// <summary>
    /// 获取当前模板解析时由 LoadContent 临时设置的祖先 ResourceDictionary 链（可能为 null）。
    /// </summary>
    public static IReadOnlyList<ResourceDictionary>? Current => _current;

    /// <summary>
    /// 进入模板 XAML 解析作用域；返回一个 <see cref="IDisposable"/> 用 using 自动还原。
    /// 嵌套 push 时会保留上一层值并在 Dispose 时还原，避免内嵌模板互相覆盖。
    /// </summary>
    public static IDisposable Push(IReadOnlyList<ResourceDictionary>? dictionaries)
    {
        var previous = _current;
        _current = dictionaries;
        return new PopScope(previous);
    }

    private sealed class PopScope : IDisposable
    {
        private readonly IReadOnlyList<ResourceDictionary>? _previous;
        private bool _disposed;

        public PopScope(IReadOnlyList<ResourceDictionary>? previous)
        {
            _previous = previous;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _current = _previous;
        }
    }
}
