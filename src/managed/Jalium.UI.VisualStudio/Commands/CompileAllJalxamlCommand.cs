using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio.Shell;

namespace Jalium.UI.VisualStudio.Commands
{
    /// <summary>
    /// 编译项目中所有 JALXAML 命令
    /// </summary>
    [Command(PackageIds.guidJaliumCmdSetString, PackageIds.CompileAllJalxamlCommandId)]
    internal sealed class CompileAllJalxamlCommand : BaseCommand<CompileAllJalxamlCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            try
            {
                var solution = await VS.Solutions.GetCurrentSolutionAsync();
                if (solution == null)
                {
                    await VS.MessageBox.ShowWarningAsync("Jalium.UI", "没有打开的解决方案");
                    return;
                }

                await VS.StatusBar.ShowMessageAsync("正在编译所有 JALXAML 文件...");

                // 获取解决方案目录下的所有 JALXAML 文件
                var solutionDir = Path.GetDirectoryName(solution.FullPath);
                if (string.IsNullOrEmpty(solutionDir))
                {
                    await VS.MessageBox.ShowWarningAsync("Jalium.UI", "无法获取解决方案目录");
                    return;
                }

                var jalxamlFiles = Directory.GetFiles(solutionDir, "*.jalxaml", SearchOption.AllDirectories);
                var totalFiles = jalxamlFiles.Length;

                if (totalFiles == 0)
                {
                    await VS.MessageBox.ShowWarningAsync("Jalium.UI", "没有找到 JALXAML 文件");
                    return;
                }

                var compiledFiles = 0;
                var failedFiles = new List<(string File, string Error)>();

                foreach (var file in jalxamlFiles)
                {
                    compiledFiles++;
                    await VS.StatusBar.ShowProgressAsync($"编译 ({compiledFiles}/{totalFiles}): {Path.GetFileName(file)}", compiledFiles, totalFiles);

                    // Call the compiler for each file
                    var (success, output) = await CompileJalxamlCommand.CompileFileAsync(file);

                    if (!success)
                    {
                        failedFiles.Add((file, output));
                    }
                }

                // Show results
                if (failedFiles.Count == 0)
                {
                    await VS.StatusBar.ShowMessageAsync($"成功编译 {totalFiles} 个 JALXAML 文件");
                }
                else
                {
                    var errorSummary = new StringBuilder();
                    errorSummary.AppendLine($"编译完成: {totalFiles - failedFiles.Count} 成功, {failedFiles.Count} 失败");
                    errorSummary.AppendLine();

                    foreach (var (file, error) in failedFiles)
                    {
                        errorSummary.AppendLine($"❌ {Path.GetFileName(file)}");
                        if (!string.IsNullOrEmpty(error))
                        {
                            errorSummary.AppendLine($"   {error}");
                        }
                    }

                    await VS.StatusBar.ShowMessageAsync($"编译完成: {totalFiles - failedFiles.Count}/{totalFiles} 成功");
                    await VS.MessageBox.ShowWarningAsync("Jalium.UI 编译结果", errorSummary.ToString());
                }
            }
            catch (Exception ex)
            {
                await VS.MessageBox.ShowErrorAsync("Jalium.UI 编译错误", ex.Message);
            }
        }
    }
}
