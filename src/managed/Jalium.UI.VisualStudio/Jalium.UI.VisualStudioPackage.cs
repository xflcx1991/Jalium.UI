using System;
using System.Runtime.InteropServices;
using System.Threading;
using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

namespace Jalium.UI.VisualStudio
{
    /// <summary>
    /// Jalium.UI Visual Studio 扩展包
    /// 提供 JALXAML 文件编译和文件嵌套支持
    /// </summary>
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [Guid(VisualStudioPackage.PackageGuidString)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.SolutionOpening_string, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideFileNesting(".jalxaml.cs", ".jalxaml")]
    [ProvideFileNesting(".g.cs", ".jalxaml")]
    [ProvideFileNesting(".juib", ".jalxaml")]
    public sealed class VisualStudioPackage : ToolkitPackage
    {
        /// <summary>
        /// VisualStudioPackage GUID string.
        /// </summary>
        public const string PackageGuidString = "7cfd4ac5-74f0-4f1f-91bf-5f091dd4d22e";

        /// <summary>
        /// 包初始化
        /// </summary>
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await base.InitializeAsync(cancellationToken, progress);

            // 切换到主线程
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            // 初始化命令
            await this.RegisterCommandsAsync();

            // 输出初始化信息
            await VS.StatusBar.ShowMessageAsync("Jalium.UI Tools initialized");
        }
    }

    /// <summary>
    /// 文件嵌套提供者属性 - 定义文件嵌套关系
    /// 例如：.jalxaml.cs 嵌套到 .jalxaml 下
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class ProvideFileNestingAttribute : RegistrationAttribute
    {
        private readonly string _nestedExtension;
        private readonly string _parentExtension;

        public ProvideFileNestingAttribute(string nestedExtension, string parentExtension)
        {
            _nestedExtension = nestedExtension;
            _parentExtension = parentExtension;
        }

        public override void Register(RegistrationContext context)
        {
            using (var key = context.CreateKey($@"FileExtensionMapping\{_nestedExtension}"))
            {
                key.SetValue("", _parentExtension);
            }
        }

        public override void Unregister(RegistrationContext context)
        {
            context.RemoveKey($@"FileExtensionMapping\{_nestedExtension}");
        }
    }
}
