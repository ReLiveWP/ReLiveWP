using System.Diagnostics;
using System.Globalization;
using System.Text;
using ReLiveWP.ServiceDefaults;

namespace ReLiveWP.Services.MediaProxy.Utilities;

public sealed class PipelineStageTimings
{
    private readonly List<(string Stage, TimeSpan Elapsed)> stages = [];

    public IReadOnlyList<(string Stage, TimeSpan Elapsed)> Stages => stages;

    public StageScope MeasureStage(string stage)
    {
        var span = ServiceTelemetry.ActivitySource.StartActivity($"pipeline {stage}");
        return new StageScope(this, stage, span, Stopwatch.GetTimestamp());
    }

    public string FormatStages()
    {
        var builder = new StringBuilder();

        foreach (var (stage, elapsed) in stages)
        {
            if (builder.Length > 0)
                builder.Append(' ');

            builder.Append(CultureInfo.InvariantCulture, $"{stage}={elapsed.TotalMilliseconds:F0}ms");
        }

        return builder.ToString();
    }

    public readonly struct StageScope(PipelineStageTimings owner, string stage, Activity? span, long started) : IDisposable
    {
        public void Dispose()
        {
            owner.stages.Add((stage, Stopwatch.GetElapsedTime(started)));
            span?.Dispose();
        }
    }
}
