using System.Text.Json;
using ReLiveWP.Services.Exchange.Middleware;

namespace ReLiveWP.Services.Exchange.Services;

public class EasRequestLog
{
    private readonly string _path;
    private readonly ILogger<EasRequestLog> _logger;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public EasRequestLog(IConfiguration config, ILogger<EasRequestLog> logger)
    {
        _logger = logger;
        //_path = config["EasRequestLog:Path"] ?? "logs/eas-requests.jsonl";
        //Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(_path))!);
    }

    public async Task RecordAsync(ActiveSyncContext ctx)
    {
#if DEBUG
        //await _lock.WaitAsync();
        //try
        //{
        //    string[] lines = [
        //        "------------------------------------------------------------",
        //        "Command: " + ctx.Command.ToString(),
        //        "DeviceId: " + ctx.DeviceId,
        //        ctx.XmlDocument?.OuterXml ?? "No xml",
        //        ctx.OutputXml ?? "No xml",
        //        "------------------------------------------------------------"
        //    ];

        //    await File.AppendAllLinesAsync(_path, lines);
        //}
        //catch (Exception ex)
        //{
        //    _logger.LogWarning(ex, "Failed to write EAS request log entry");
        //}
        //finally
        //{
        //    _lock.Release();
        //}

        _logger.LogDebug("{Command}, {DeviceId}, {InputXml}, {OutputXml}", ctx.Command.ToString(), ctx.DeviceId, ctx.XmlDocument?.OuterXml ?? "No xml", ctx.OutputXml ?? "No xml");
#endif
    }
}
