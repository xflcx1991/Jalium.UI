using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Jalium.UI.Build;

/// <summary>
/// MSBuild 任务：编译 JALXAML 文件到 .juib 二进制包
/// 通过调用 jalxamlc 编译器工具实现
/// </summary>
public class CompileJalxamlTask : Microsoft.Build.Utilities.Task
{
    /// <summary>
    /// 输入的 JALXAML 源文件
    /// </summary>
    [Required]
    public ITaskItem[]? SourceFiles { get; set; }

    /// <summary>
    /// 输出目录
    /// </summary>
    [Required]
    public string? OutputDirectory { get; set; }

    /// <summary>
    /// 项目根目录
    /// </summary>
    public string? ProjectDirectory { get; set; }

    /// <summary>
    /// 是否启用优化
    /// </summary>
    public bool EnableOptimization { get; set; } = true;

    /// <summary>
    /// 是否生成调试信息
    /// </summary>
    public bool GenerateDebugInfo { get; set; } = false;

    /// <summary>
    /// 中间输出目录
    /// </summary>
    public string? IntermediateOutputPath { get; set; }

    /// <summary>
    /// jalxamlc 编译器路径（如果为空则使用 dotnet tool）
    /// </summary>
    public string? CompilerPath { get; set; }

    /// <summary>
    /// 输出的编译结果文件
    /// </summary>
    [Output]
    public ITaskItem[]? CompiledFiles { get; set; }

    public override bool Execute()
    {
        if (SourceFiles == null || SourceFiles.Length == 0)
        {
            Log.LogMessage(MessageImportance.Normal, "没有找到 JALXAML 文件需要编译");
            CompiledFiles = Array.Empty<ITaskItem>();
            return true;
        }

        if (string.IsNullOrEmpty(OutputDirectory))
        {
            Log.LogError("OutputDirectory 未指定");
            return false;
        }

        // 确保输出目录存在
        Directory.CreateDirectory(OutputDirectory);

        var compiledItems = new List<ITaskItem>();
        var hasErrors = false;

        foreach (var sourceFile in SourceFiles)
        {
            var sourcePath = sourceFile.GetMetadata("FullPath");
            if (string.IsNullOrEmpty(sourcePath))
            {
                sourcePath = sourceFile.ItemSpec;
            }

            if (!File.Exists(sourcePath))
            {
                Log.LogError("源文件不存在: {0}", sourcePath);
                hasErrors = true;
                continue;
            }

            try
            {
                var outputFile = CompileFile(sourcePath);
                if (outputFile != null)
                {
                    var item = new TaskItem(outputFile);
                    item.SetMetadata("SourceFile", sourcePath);
                    compiledItems.Add(item);
                    Log.LogMessage(MessageImportance.Normal, "已编译: {0} -> {1}", sourcePath, outputFile);
                }
            }
            catch (Exception ex)
            {
                Log.LogError("编译失败 [{0}]: {1}", sourcePath, ex.Message);
                hasErrors = true;
            }
        }

        CompiledFiles = compiledItems.ToArray();
        return !hasErrors;
    }

    private string? CompileFile(string sourcePath)
    {
        // 计算输出文件路径
        var fileName = Path.GetFileNameWithoutExtension(sourcePath);
        var outputPath = Path.Combine(OutputDirectory!, fileName + ".juib");

        Log.LogMessage(MessageImportance.Low, "开始编译: {0}", sourcePath);

        // 构建编译器参数
        var args = new StringBuilder();

        // 优化选项
        if (!EnableOptimization)
        {
            args.Append("-O0 ");
        }

        // 调试信息
        if (GenerateDebugInfo)
        {
            args.Append("-g ");
        }

        // 输出路径
        args.Append($"-o \"{outputPath}\" ");

        // 输入文件
        args.Append($"\"{sourcePath}\"");

        // 执行编译器
        var exitCode = RunCompiler(args.ToString(), out var output, out var error);

        if (exitCode != 0)
        {
            if (!string.IsNullOrEmpty(error))
            {
                Log.LogError(error);
            }
            if (!string.IsNullOrEmpty(output))
            {
                Log.LogMessage(MessageImportance.High, output);
            }
            return null;
        }

        if (!string.IsNullOrEmpty(output))
        {
            Log.LogMessage(MessageImportance.Normal, output);
        }

        return outputPath;
    }

    private int RunCompiler(string arguments, out string output, out string error)
    {
        string fileName;
        string args;

        if (!string.IsNullOrEmpty(CompilerPath) && File.Exists(CompilerPath))
        {
            // 使用指定的编译器路径
            fileName = CompilerPath;
            args = arguments;
        }
        else
        {
            // 使用 dotnet tool 执行编译器
            fileName = "dotnet";
            args = $"jalxamlc {arguments}";
        }

        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = args,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            WorkingDirectory = ProjectDirectory ?? Directory.GetCurrentDirectory()
        };

        Log.LogMessage(MessageImportance.Low, "执行: {0} {1}", fileName, args);

        using var process = Process.Start(psi);
        if (process == null)
        {
            output = "";
            error = "无法启动编译器进程";
            return -1;
        }

        output = process.StandardOutput.ReadToEnd();
        error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        return process.ExitCode;
    }
}

/// <summary>
/// MSBuild 任务：生成 JALXAML 代码隐藏文件
/// </summary>
public class GenerateJalxamlCodeBehindTask : Microsoft.Build.Utilities.Task
{
    [Required]
    public ITaskItem[]? SourceFiles { get; set; }

    [Required]
    public string? OutputDirectory { get; set; }

    public string? RootNamespace { get; set; }

    public string? CompilerPath { get; set; }

    public string? ProjectDirectory { get; set; }

    [Output]
    public ITaskItem[]? GeneratedFiles { get; set; }

    public override bool Execute()
    {
        if (SourceFiles == null || SourceFiles.Length == 0)
        {
            GeneratedFiles = Array.Empty<ITaskItem>();
            return true;
        }

        Directory.CreateDirectory(OutputDirectory!);

        var generatedItems = new List<ITaskItem>();
        var hasErrors = false;

        foreach (var sourceFile in SourceFiles)
        {
            var sourcePath = sourceFile.GetMetadata("FullPath");
            if (string.IsNullOrEmpty(sourcePath))
            {
                sourcePath = sourceFile.ItemSpec;
            }

            try
            {
                var outputFile = GenerateCodeBehind(sourcePath);
                if (outputFile != null)
                {
                    var item = new TaskItem(outputFile);
                    item.SetMetadata("SourceFile", sourcePath);
                    generatedItems.Add(item);
                }
            }
            catch (Exception ex)
            {
                Log.LogError("代码生成失败 [{0}]: {1}", sourcePath, ex.Message);
                hasErrors = true;
            }
        }

        GeneratedFiles = generatedItems.ToArray();
        return !hasErrors;
    }

    private string? GenerateCodeBehind(string sourcePath)
    {
        var fileName = Path.GetFileNameWithoutExtension(sourcePath);
        var outputPath = Path.Combine(OutputDirectory!, fileName + ".g.cs");

        // 构建编译器参数
        var args = new StringBuilder();
        args.Append("-c "); // 生成代码

        if (!string.IsNullOrEmpty(RootNamespace))
        {
            args.Append($"-n \"{RootNamespace}\" ");
        }

        // 移除尾部反斜杠以避免引号转义问题
        var outputDir = OutputDirectory!.TrimEnd('\\', '/');
        args.Append($"-d \"{outputDir}\" ");
        args.Append($"\"{sourcePath}\"");

        // 执行编译器
        var exitCode = RunCompiler(args.ToString(), out var output, out var error);

        if (exitCode != 0)
        {
            if (!string.IsNullOrEmpty(error))
            {
                Log.LogError(error);
            }
            return null;
        }

        if (!string.IsNullOrEmpty(output))
        {
            Log.LogMessage(MessageImportance.Normal, output);
        }

        return File.Exists(outputPath) ? outputPath : null;
    }

    private int RunCompiler(string arguments, out string output, out string error)
    {
        string fileName;
        string args;

        if (!string.IsNullOrEmpty(CompilerPath) && File.Exists(CompilerPath))
        {
            fileName = CompilerPath;
            args = arguments;
        }
        else
        {
            fileName = "dotnet";
            args = $"jalxamlc {arguments}";
        }

        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = args,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            WorkingDirectory = ProjectDirectory ?? Directory.GetCurrentDirectory()
        };

        Log.LogMessage(MessageImportance.Low, "执行: {0} {1}", fileName, args);

        using var process = Process.Start(psi);
        if (process == null)
        {
            output = "";
            error = "无法启动编译器进程";
            return -1;
        }

        output = process.StandardOutput.ReadToEnd();
        error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        return process.ExitCode;
    }
}
