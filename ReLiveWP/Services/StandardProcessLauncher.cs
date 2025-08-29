using System.Diagnostics;
using System.Net;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ReLiveWP.Interop;

namespace ReLiveWP.Services;

/// <summary>
/// Uses the .NET CLI to launch projects and redirect log output without spawning windows
/// </summary>
internal partial class StandardProcessLauncher : IHostedService, IDisposable
{
    private IntPtr hJob;

    private readonly List<Process> processes = [];
    private readonly IProjectMetadata[] projects;
    private readonly IHostEnvironment environment;

    public StandardProcessLauncher(IServiceProvider services, IHostEnvironment environment)
    {
        this.environment = environment;
        projects = [.. services.GetServices<IProjectMetadata>()];

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            hJob = Win32Job.CreateJobObject();
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        foreach (var project in projects)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            var directory = environment.IsDevelopment() ? (project.ProjectRelativePath ?? project.Name) : project.Name;

            var projectDirectory = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", directory));
            var projectPath = Path.Combine(projectDirectory, $"{project.Name}.csproj");
            var pid = project.Name[(project.Name.IndexOf('.') + 1)..];

            string processName;
            string[] arguments;
            if (environment.IsProduction())
            {
                processName = Path.Combine(projectDirectory, Path.GetFileNameWithoutExtension(projectPath));
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    processName += ".exe";
                }

                arguments = [];
            }
            else
            {
                processName = "dotnet";
                arguments = ["run", "--no-build", "--project", projectPath, "--"];
            }

            var psi = new ProcessStartInfo(processName)
            {
                WorkingDirectory = projectDirectory,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                WindowStyle = ProcessWindowStyle.Hidden,
            };

            foreach (var item in (string[])[.. arguments, .. project.Arguments])
                psi.ArgumentList.Add(item);

            foreach (var item in project.EnvironmentVariables)
                psi.Environment[item.Key] = item.Value;

            var process = Process.Start(psi)!;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                Win32Job.AssignProcessToJobObject(hJob, process.Handle);

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

    public void Dispose()
    {
        if (hJob != IntPtr.Zero)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                Win32Job.CloseHandle(hJob);

            hJob = 0;
        }

        GC.SuppressFinalize(this);
    }

    ~StandardProcessLauncher()
    {
        Dispose();
    }
}
