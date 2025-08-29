using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ReLiveWP.Services;

/// <summary>
/// Uses .NET Aspire APIs to launch projects under the Visual Studio debugger and redirect log output without spawning windows
/// </summary>
internal class AspireProcessLauncher : IHostedService
{
    private readonly string hostVar;
    private readonly HttpClient httpClient;
    private readonly IProjectMetadata[] projects;
    private readonly CancellationTokenSource cts = new();
    private readonly Dictionary<string, string> projectNames = [];

    public AspireProcessLauncher(IServiceProvider services)
    {
        projects = [.. services.GetServices<IProjectMetadata>()];
        hostVar = Environment.GetEnvironmentVariable("DEBUG_SESSION_PORT")!;
        var tokenVar = Environment.GetEnvironmentVariable("DEBUG_SESSION_TOKEN");
        var certVar = Environment.GetEnvironmentVariable("DEBUG_SESSION_SERVER_CERTIFICATE");
        var idPrefix = Environment.GetEnvironmentVariable("DCP_INSTANCE_ID_PREFIX");

        var idBytes = new byte[6];
        Random.Shared.NextBytes(idBytes);
        var id = Convert.ToHexStringLower(idBytes);

        var handler = new HttpClientHandler();
        handler.ServerCertificateCustomValidationCallback = (request, cert, chain, errors) =>
        {
            return true; // TODO: verify this based on certVar
        };

        httpClient = new HttpClient(handler);
        httpClient.BaseAddress = new Uri((certVar != null ? "https://" : "http://") + hostVar);
        httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + tokenVar);
        httpClient.DefaultRequestHeaders.Add("Microsoft-Developer-DCP-Instance-ID", idPrefix + id);
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await ConnectAsync(cts.Token);

        foreach (var project in projects)
        {
            await LaunchProjectAsync(project.Name, project.ProjectRelativePath, project.Arguments, project.EnvironmentVariables);
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await cts.CancelAsync();
    }

    private async Task LaunchProjectAsync(string projectName, string? projectRelativePath, IReadOnlyList<string> args, IReadOnlyDictionary<string, string> environment)
    {
        var directory = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", projectRelativePath ?? projectName));
        var project = Path.Combine(directory, $"{projectName}.csproj");

        var payload = new
        {
            launch_configurations = (object[])[
                new
                {
                    type = "project",
                    project_path = project,
                    launch_profile = "http",
                }
            ],
            env = environment.Select(e => new { name = e.Key, value = e.Value }).ToArray(),
            args
        };

        var data = JsonSerializer.Serialize(payload);
        var request = new HttpRequestMessage(HttpMethod.Put, "run_session?api-version=2024-03-03");
        request.Content = new StringContent(data, Encoding.UTF8, "application/json");

        var response = await httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            Console.WriteLine(body);
        }

        if (response.StatusCode == HttpStatusCode.Created)
        {
            var location = response.Headers.Location!.Segments[^1];
            projectNames[location] = projectName[(projectName.IndexOf('.') + 1)..];
        }
    }

    private async Task ConnectAsync(CancellationToken token)
    {
        var ws = new ClientWebSocket();
        await ws.ConnectAsync(new Uri("wss://" + hostVar + "/run_session/notify"), httpClient, token);

        _ = Task.Run(() => WebSocketListenLoop(ws, token));
    }

    private async Task WebSocketListenLoop(WebSocket ws, CancellationToken ct)
    {
        try
        {
            var buffer = new byte[8192];

            while (ws.State == WebSocketState.Open)
            {
                var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), ct);

                if (result.MessageType == WebSocketMessageType.Close || ct.IsCancellationRequested)
                {
                    await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", ct);
                    break;
                }

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);

                    // Handle continuation frames
                    while (!result.EndOfMessage)
                    {
                        result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), ct);
                        message += Encoding.UTF8.GetString(buffer, 0, result.Count);
                    }

                    var jsonDoc = JsonDocument.Parse(message);
                    switch (jsonDoc.RootElement.GetProperty("notification_type").GetString())
                    {
                        case "serviceLogs":
                            {
                                var logLine = jsonDoc.RootElement.GetProperty("log_message").GetString()!;
                                var sessionId = jsonDoc.RootElement.GetProperty("session_id").GetString()!;
                                foreach (var line in logLine.Split('\n'))
                                {
                                    Console.WriteLine("[{0}] {1}", projectNames[sessionId], line);
                                }

                                break;
                            }
                        default:
                            break;
                    }
                }
            }
        }
        catch (TaskCanceledException)
        {
        }
    }
}
