using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio.Shell;

namespace Jalium.UI.VisualStudio.Commands
{
    /// <summary>
    /// 编译 JALXAML 命令 - 手动编译当前文件
    /// </summary>
    [Command(PackageIds.guidJaliumCmdSetString, PackageIds.CompileJalxamlCommandId)]
    internal sealed class CompileJalxamlCommand : BaseCommand<CompileJalxamlCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            try
            {
                var activeDocument = await VS.Documents.GetActiveDocumentViewAsync();
                var filePath = activeDocument?.FilePath;

                if (string.IsNullOrEmpty(filePath))
                {
                    await VS.MessageBox.ShowWarningAsync("Jalium.UI", "没有打开的文件");
                    return;
                }

                if (!filePath.EndsWith(".jalxaml", StringComparison.OrdinalIgnoreCase))
                {
                    await VS.MessageBox.ShowWarningAsync("Jalium.UI", "当前文件不是 JALXAML 文件");
                    return;
                }

                await VS.StatusBar.ShowMessageAsync("正在编译 JALXAML...");

                // 调用编译器
                var (success, output) = await CompileFileAsync(filePath);

                if (success)
                {
                    await VS.StatusBar.ShowMessageAsync("JALXAML 编译成功");
                }
                else
                {
                    await VS.StatusBar.ShowMessageAsync("JALXAML 编译失败");
                    if (!string.IsNullOrEmpty(output))
                    {
                        await VS.MessageBox.ShowErrorAsync("Jalium.UI 编译错误", output);
                    }
                }
            }
            catch (Exception ex)
            {
                await VS.MessageBox.ShowErrorAsync("Jalium.UI 编译错误", ex.Message);
            }
        }

        /// <summary>
        /// Compiles a single JALXAML file using the jalxamlc compiler.
        /// </summary>
        internal static async Task<(bool Success, string Output)> CompileFileAsync(string filePath)
        {
            var outputPath = Path.ChangeExtension(filePath, ".juib");

            await VS.StatusBar.ShowProgressAsync($"编译: {Path.GetFileName(filePath)}", 1, 2);

            // Find the compiler executable
            var compilerPath = FindCompilerPath();
            if (string.IsNullOrEmpty(compilerPath))
            {
                return (false, "找不到 jalxamlc 编译器。请确保已安装 Jalium.UI.Compiler。");
            }

            // Build compiler arguments
            var arguments = $"-c -o \"{outputPath}\" \"{filePath}\"";

            // Run the compiler
            var result = await RunProcessAsync(compilerPath, arguments, Path.GetDirectoryName(filePath));

            await VS.StatusBar.ShowProgressAsync($"输出: {Path.GetFileName(outputPath)}", 2, 2);

            return result;
        }

        /// <summary>
        /// Finds the path to the jalxamlc compiler.
        /// </summary>
        private static string FindCompilerPath()
        {
            // Try multiple locations to find the compiler

            // 1. Check if it's a dotnet tool
            var dotnetToolPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".dotnet", "tools", "jalxamlc.exe");
            if (File.Exists(dotnetToolPath))
            {
                return dotnetToolPath;
            }

            // 2. Check solution directory for local build
            var solution = ThreadHelper.JoinableTaskFactory.Run(() => VS.Solutions.GetCurrentSolutionAsync());
            if (solution != null)
            {
                var solutionDir = Path.GetDirectoryName(solution.FullPath);
                if (!string.IsNullOrEmpty(solutionDir))
                {
                    // Check bin output
                    var localCompiler = Path.Combine(solutionDir,
                        "src", "managed", "Jalium.UI.Compiler", "bin", "Debug", "net10.0-windows", "Jalium.UI.Compiler.exe");
                    if (File.Exists(localCompiler))
                    {
                        return localCompiler;
                    }

                    // Check release output
                    localCompiler = Path.Combine(solutionDir,
                        "src", "managed", "Jalium.UI.Compiler", "bin", "Release", "net10.0-windows", "Jalium.UI.Compiler.exe");
                    if (File.Exists(localCompiler))
                    {
                        return localCompiler;
                    }
                }
            }

            // 3. Use dotnet run as fallback
            return "dotnet";
        }

        /// <summary>
        /// Runs an external process and captures its output.
        /// </summary>
        internal static async Task<(bool Success, string Output)> RunProcessAsync(string fileName, string arguments, string workingDirectory)
        {
            var output = new StringBuilder();
            var error = new StringBuilder();

            using (var process = new Process())
            {
                // If using dotnet as the compiler, adjust arguments
                if (fileName == "dotnet")
                {
                    var solution = await VS.Solutions.GetCurrentSolutionAsync();
                    if (solution != null)
                    {
                        var solutionDir = Path.GetDirectoryName(solution.FullPath);
                        var projectPath = Path.Combine(solutionDir, "src", "managed", "Jalium.UI.Compiler", "Jalium.UI.Compiler.csproj");
                        arguments = $"run --project \"{projectPath}\" -- {arguments}";
                    }
                }

                process.StartInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    WorkingDirectory = workingDirectory ?? Environment.CurrentDirectory,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8
                };

                process.OutputDataReceived += (s, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        output.AppendLine(e.Data);
                    }
                };

                process.ErrorDataReceived += (s, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        error.AppendLine(e.Data);
                    }
                };

                try
                {
                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    // Wait for the process with a timeout
                    var completed = await Task.Run(() => process.WaitForExit(60000));
                    if (!completed)
                    {
                        process.Kill();
                        return (false, "编译超时（60秒）");
                    }

                    var success = process.ExitCode == 0;
                    var combinedOutput = success ? output.ToString() : error.ToString();

                    return (success, combinedOutput.Trim());
                }
                catch (Exception ex)
                {
                    return (false, $"启动编译器失败: {ex.Message}");
                }
            }
        }
    }
}
