using System.IO;
using System.IO.Compression;
using System.Net.Http;
using ARCbot.Helpers;

namespace ARCbot.Services;

/// <summary>
/// �������� Node.js ��Я����ʱ�ͻ����˻�������
/// ֧�ֽ��Ȼص���ȡ�����ơ�
/// </summary>
public class DownloadService
{
    private readonly HttpClient _httpClient;

    // Node.js ��Я�����ص�ַ��Windows x64��
    private const string NodeJsUrl =
        "https://nodejs.org/dist/v20.11.1/node-v20.11.1-win-x64.zip";

    // �����˻��������ص�ַ
    private const string BaseAgentUrl =
        "https://zip1.webgetstore.com/2026/04/13/6b22070a3b7dc42cc840faf020ff0ff4.zip?sg=4ae7284f4af496ed2f49bfca2b8d17d1&e=69dc6ea3&fileName=minecraft-ai-agent.zip&fi=282406185";

    public DownloadService()
    {
        _httpClient = new HttpClient(new HttpClientHandler
        {
            AllowAutoRedirect = true   // �����ض���
        })
        {
            Timeout = TimeSpan.FromMinutes(10)
        };
    }

    /// <summary>��� Node.js ����ʱ�Ƿ��Ѵ���</summary>
    public bool IsNodeInstalled() => File.Exists(PathHelper.NodeExePath);

    /// <summary>���������Ƿ�������</summary>
    public bool IsBaseAgentDownloaded() => File.Exists(PathHelper.BaseAgentZipPath);

    /// <summary>
    /// �����ļ���ָ��·����֧�ֽ��Ȼص���
    /// </summary>
    private async Task DownloadFileAsync(
        string url,
        string destPath,
        IProgress<(long downloaded, long total)>? progress = null,
        CancellationToken ct = default)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);

        using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength ?? -1L;

        await using var contentStream = await response.Content.ReadAsStreamAsync(ct);
        await using var fileStream = new FileStream(destPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

        var buffer = new byte[81920];
        long totalRead = 0;
        int bytesRead;

        while ((bytesRead = await contentStream.ReadAsync(buffer, ct)) > 0)
        {
            await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), ct);
            totalRead += bytesRead;
            progress?.Report((totalRead, totalBytes));
        }
    }

    /// <summary>
    /// ���ز���ѹ Node.js ��Я������ʱ��
    /// ��ѹ�� node.exe �ŵ� runtime/ Ŀ¼��
    /// </summary>
    public async Task DownloadNodeRuntimeAsync(
        IProgress<(long downloaded, long total)>? progress = null,
        CancellationToken ct = default)
    {
        if (IsNodeInstalled()) return;

        var zipPath = Path.Combine(PathHelper.DownloadsDir, "node_runtime.zip");

        // 1. ����
        await DownloadFileAsync(NodeJsUrl, zipPath, progress, ct);

        // 2. ��ѹ ���� Node.js �ٷ� zip ����һ��Ŀ¼����Ҫ�ҵ� node.exe
        var tempExtract = Path.Combine(PathHelper.DownloadsDir, "node_temp");
        if (Directory.Exists(tempExtract))
            Directory.Delete(tempExtract, true);

        ZipFile.ExtractToDirectory(zipPath, tempExtract);

        // ���� node.exe ���ƶ��� runtime/
        Directory.CreateDirectory(PathHelper.RuntimeDir);
        var nodeExe = Directory.GetFiles(tempExtract, "node.exe", SearchOption.AllDirectories).FirstOrDefault();
        if (nodeExe == null)
            throw new FileNotFoundException("�����ص� Node.js ����δ�ҵ� node.exe");

        File.Copy(nodeExe, PathHelper.NodeExePath, overwrite: true);

        // ͬʱ���� npm ����ļ��������Ҫ npm install��
        var sourceDir = Path.GetDirectoryName(nodeExe)!;
        CopyDirectory(sourceDir, PathHelper.RuntimeDir);

        // 3. ������ʱ�ļ�
        Directory.Delete(tempExtract, true);
        File.Delete(zipPath);
    }

    /// <summary>
    /// ���ػ����˻�������
    /// </summary>
    public async Task DownloadBaseAgentAsync(
        IProgress<(long downloaded, long total)>? progress = null,
        CancellationToken ct = default)
    {
        // ʼ�����������Ի�ȡ���°汾
        await DownloadFileAsync(BaseAgentUrl, PathHelper.BaseAgentZipPath, progress, ct);
    }

    /// <summary>
    /// ����������ѹ��ָ��ʵ��Ŀ¼��
    /// <summary>
    /// 将基础包解压到指定实例目录。
    /// </summary>
    public void ExtractBaseAgentToInstance(string instanceName, bool useCustom = false)
    {
        var instanceDir = PathHelper.GetInstanceDir(instanceName);
        Directory.CreateDirectory(instanceDir);

        string zipPath = useCustom ? PathHelper.CustomBaseAgentZipPath : PathHelper.BaseAgentZipPath;

        if (!File.Exists(zipPath))
        {
            if (useCustom)
                throw new FileNotFoundException("自定义基础包未导入，请在设置中导入。");
            else
                throw new FileNotFoundException("云端基础包未下载，请在设置中下载。");
        }

        ZipFile.ExtractToDirectory(zipPath, instanceDir, overwriteFiles: true);

        // 确保 plugins 目录存在
        Directory.CreateDirectory(PathHelper.GetInstancePluginsDir(instanceName));
    }

    /// <summary>�ݹ鸴��Ŀ¼</summary>
    private static void CopyDirectory(string sourceDir, string destDir)
    {
        Directory.CreateDirectory(destDir);

        foreach (var file in Directory.GetFiles(sourceDir))
        {
            var destFile = Path.Combine(destDir, Path.GetFileName(file));
            File.Copy(file, destFile, overwrite: true);
        }

        foreach (var dir in Directory.GetDirectories(sourceDir))
        {
            var destSubDir = Path.Combine(destDir, Path.GetFileName(dir));
            CopyDirectory(dir, destSubDir);
        }
    }
}