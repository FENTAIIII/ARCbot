using ARCbot.Helpers;
using ARCbot.Models;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace ARCbot.Services;

/// <summary>
/// 管理单个 Node.js 进程的完整生命周期：
/// 启动、流重定向、UTF-8 编码、停止、事件通知。
/// 
/// 每个运行中的 BotInstance 对应一个 NodeProcessManager 实例。
/// </summary>
public class NodeProcessManager : IDisposable
{
    private Process? _process;
    private readonly AuthInterceptor _authInterceptor;

    public string InstanceName { get; }
    public bool IsRunning => _process is { HasExited: false };

    /// <summary>收到 stdout 一行输出时触发</summary>
    public event EventHandler<ConsoleEntry>? OutputReceived;

    /// <summary>进程退出时触发</summary>
    public event EventHandler<int>? ProcessExited;

    public NodeProcessManager(string instanceName, AuthInterceptor authInterceptor)
    {
        InstanceName = instanceName;
        _authInterceptor = authInterceptor;
    }

    /// <summary>
    /// 启动 Node.js 进程，执行实例的 index.js。
    /// </summary>
    public void Start()
    {
        if (IsRunning)
            throw new InvalidOperationException($"实例 '{InstanceName}' 已在运行中。");

        var srcDir = PathHelper.GetInstanceSrcDir(InstanceName);
        var indexJs = System.IO.Path.Combine(srcDir, "index.js");

        if (!System.IO.File.Exists(PathHelper.NodeExePath))
            throw new FileNotFoundException("Node.js 运行时未找到，请先在设置中下载。");

        if (!System.IO.File.Exists(indexJs))
            throw new FileNotFoundException($"入口文件不存在: {indexJs}");

        var psi = new ProcessStartInfo
        {
            FileName = PathHelper.NodeExePath,
            Arguments = "index.js",
            WorkingDirectory = srcDir,
            UseShellExecute = false,
            CreateNoWindow = true,

            // ─── 流重定向 ───
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,

            // ─── 强制 UTF-8 编码，彻底杜绝中文乱码 ───
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };

        // 设置环境变量确保 Node.js 输出 UTF-8
        psi.Environment["NODE_OPTIONS"] = "--max-old-space-size=512";
        psi.Environment["LANG"] = "zh_CN.UTF-8";
        psi.Environment["CHCP"] = "65001";
        psi.Environment["PYTHONIOENCODING"] = "utf-8";

        // 将 .env 所在目录加入环境（dotenv 会自动读取）
        // 确保工作目录正确，.env 文件在实例根目录
        var envFilePath = PathHelper.GetInstanceEnvPath(InstanceName);
        if (System.IO.File.Exists(envFilePath))
        {
            // 有些项目需要 .env 在工作目录，复制一份到 src
            var srcEnvPath = System.IO.Path.Combine(srcDir, ".env");
            System.IO.File.Copy(envFilePath, srcEnvPath, overwrite: true);
        }

        _process = new Process { StartInfo = psi, EnableRaisingEvents = true };

        _process.OutputDataReceived += (_, e) =>
        {
            if (e.Data == null) return;

            var entry = new ConsoleEntry
            {
                Level = ClassifyLogLevel(e.Data),
                Text = e.Data,
                InstanceName = InstanceName
            };

            OutputReceived?.Invoke(this, entry);

            // ─── 全局微软登录拦截 ───
            _authInterceptor.AnalyzeLine(InstanceName, e.Data);
        };

        _process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data == null) return;

            var entry = new ConsoleEntry
            {
                Level = LogLevel.Error,
                Text = e.Data,
                InstanceName = InstanceName
            };

            OutputReceived?.Invoke(this, entry);

            // stderr 也可能包含登录信息
            _authInterceptor.AnalyzeLine(InstanceName, e.Data);
        };

        _process.Exited += (_, _) =>
        {
            var exitCode = _process?.ExitCode ?? -1;
            ProcessExited?.Invoke(this, exitCode);
        };

        _process.Start();
        _process.BeginOutputReadLine();
        _process.BeginErrorReadLine();

        EmitInfo($"✅ 实例 '{InstanceName}' 已启动 (PID: {_process.Id})");
    }

    /// <summary>
    /// 向进程的 stdin 写入一行文本。
    /// </summary>
    public async Task WriteInputAsync(string text)
    {
        if (_process?.StandardInput == null || !IsRunning) return;

        await _process.StandardInput.WriteLineAsync(text);
        await _process.StandardInput.FlushAsync();

        OutputReceived?.Invoke(this, new ConsoleEntry
        {
            Level = LogLevel.Stdin,
            Text = $"> {text}",
            InstanceName = InstanceName
        });
    }

    /// <summary>
    /// 优雅停止进程：先尝试关闭 stdin，等待退出；超时则强制 Kill。
    /// </summary>
    public async Task StopAsync(int timeoutMs = 5000)
    {
        if (_process == null || !IsRunning)
        {
            EmitInfo($"⚠️ 实例 '{InstanceName}' 未在运行。");
            return;
        }

        try
        {
            EmitInfo($"⏹️ 正在停止实例 '{InstanceName}'...");

            // 尝试优雅关闭
            _process.StandardInput?.Close();

            var exited = await Task.Run(() => _process.WaitForExit(timeoutMs));
            if (!exited)
            {
                _process.Kill(entireProcessTree: true);
                EmitInfo($"🔴 实例 '{InstanceName}' 已被强制终止。");
            }
            else
            {
                EmitInfo($"⬛ 实例 '{InstanceName}' 已正常退出 (Code: {_process.ExitCode})。");
            }
        }
        catch (Exception ex)
        {
            EmitInfo($"❌ 停止实例时出错: {ex.Message}");
        }
    }

    /// <summary>根据日志内容简单分类日志级别</summary>
    private static LogLevel ClassifyLogLevel(string text)
    {
        if (text.Contains("warn", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("WARNING", StringComparison.OrdinalIgnoreCase))
            return LogLevel.Warn;

        if (text.Contains("error", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("ERR!", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("FATAL", StringComparison.OrdinalIgnoreCase))
            return LogLevel.Error;

        return LogLevel.Info;
    }

    private void EmitInfo(string text)
    {
        OutputReceived?.Invoke(this, new ConsoleEntry
        {
            Level = LogLevel.Info,
            Text = text,
            InstanceName = InstanceName
        });
    }

    public void Dispose()
    {
        if (_process != null)
        {
            if (!_process.HasExited)
            {
                try { _process.Kill(entireProcessTree: true); } catch { /* ignore */ }
            }
            _process.Dispose();
            _process = null;
        }
        GC.SuppressFinalize(this);
    }
}