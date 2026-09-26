using ReLiveWP.Services.MediaProxy.Utilities;

namespace ReLiveWP.Services.MediaProxy.Tests;

public class PipelineStageTimingsTests
{
    [Fact]
    public void RecordsStagesInTheOrderTheyFinish()
    {
        var timings = new PipelineStageTimings();

        using (timings.MeasureStage("receive")) { }
        using (timings.MeasureStage("decode")) { }

        Assert.Equal(["receive", "decode"], timings.Stages.Select(s => s.Stage));
        Assert.All(timings.Stages, s => Assert.True(s.Elapsed >= TimeSpan.Zero));
    }

    [Fact]
    public void FormatsEachStageInWholeMilliseconds()
    {
        var timings = new PipelineStageTimings();

        using (timings.MeasureStage("receive")) { }
        using (timings.MeasureStage("encode")) { }

        Assert.Matches(@"^receive=\d+ms encode=\d+ms$", timings.FormatStages());
    }

    [Fact]
    public void FormatsNothingWhenNoStageRan()
    {
        Assert.Equal("", new PipelineStageTimings().FormatStages());
    }
}
