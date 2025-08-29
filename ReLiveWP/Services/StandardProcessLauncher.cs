using System.Diagnostics;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ReLiveWP.Services;

/// <summary>
/// Uses the .NET CLI to launch projects and redirect log output without spawning windows
/// </summary>
internal class StandardProcessLauncher(IServiceProvider services) : IHostedService
{
    private readonly List<Process> processes = [];
    private readonly IProjectMetadata[] projects = [.. services.GetServices<IProjectMetadata>()];

    public Task StartAsync(CancellationToken cancellationToken)
    {
        foreach (var project in projects)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            var projectDirectory = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", project.ProjectRelativePath ?? project.Name));
            var projectPath = Path.Combine(projectDirectory, $"{project.Name}.csproj");
            var pid = project.Name[(project.Name.IndexOf('.') + 1)..];

            var psi = new ProcessStartInfo("dotnet")
            {
                WorkingDirectory = projectDirectory,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                ArgumentList =
                {
                    "run", "--no-build", "--project", projectPath, "--"
                },
                UseShellExecute = false,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            foreach (var item in project.Arguments)
                psi.ArgumentList.Add(item);

            foreach (var item in project.EnvironmentVariables)
                psi.Environment[item.Key] = item.Value;

            var process = Process.Start(psi)!;
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            process.OutputDataReceived += (o, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    foreach (var line in e.Data.Split('\n'))
                    {
                        Console.WriteLine("[{0}] {1}", pid, line);
                    }
                }
            }; 

            process.ErrorDataReceived += (o, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    foreach (var line in e.Data.Split('\n'))
                    {
                        Console.Error.WriteLine("[{0}] {1}", pid, line);
                    }
                }
            };

            processes.Add(process);
        }

        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        foreach (var process in processes)
        {
            process.Kill();
        }

        await Task.WhenAll(processes.Select(s => s.WaitForExitAsync(cancellationToken)));
    }
}
