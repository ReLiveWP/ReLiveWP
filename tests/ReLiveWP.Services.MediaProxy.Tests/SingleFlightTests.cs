using ReLiveWP.Services.MediaProxy.Utilities;

namespace ReLiveWP.Services.MediaProxy.Tests;

public class SingleFlightTests
{
    [Fact]
    public async Task ConcurrentCallsForOneKeyRunTheWorkOnce()
    {
        var flight = new SingleFlight<string, int>();
        var release = new TaskCompletionSource();
        var runs = 0;

        async Task<int> SlowWork()
        {
            Interlocked.Increment(ref runs);
            await release.Task;
            return 42;
        }

        var first = flight.RunOnceAsync("a", SlowWork, CancellationToken.None);
        var second = flight.RunOnceAsync("a", SlowWork, CancellationToken.None);
        release.SetResult();

        var results = await Task.WhenAll(first, second);

        Assert.Equal([42, 42], results);
        Assert.Equal(1, runs);
    }

    [Fact]
    public async Task DifferentKeysRunSeparately()
    {
        var flight = new SingleFlight<string, string>();

        var a = await flight.RunOnceAsync("a", () => Task.FromResult("a"), CancellationToken.None);
        var b = await flight.RunOnceAsync("b", () => Task.FromResult("b"), CancellationToken.None);

        Assert.Equal("a", a);
        Assert.Equal("b", b);
    }

    [Fact]
    public async Task ForgetsAKeyOnceItsWorkFinishes()
    {
        var flight = new SingleFlight<string, int>();
        var runs = 0;

        await flight.RunOnceAsync("a", () => Task.FromResult(Interlocked.Increment(ref runs)), CancellationToken.None);
        await flight.RunOnceAsync("a", () => Task.FromResult(Interlocked.Increment(ref runs)), CancellationToken.None);

        Assert.Equal(2, runs);
        Assert.Equal(0, flight.InFlightCount);
    }

    [Fact]
    public async Task ForgetsAKeyWhoseWorkFailed()
    {
        var flight = new SingleFlight<string, int>();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => flight.RunOnceAsync("a", () => throw new InvalidOperationException("boom"), CancellationToken.None));

        Assert.Equal(0, flight.InFlightCount);
        Assert.Equal(7, await flight.RunOnceAsync("a", () => Task.FromResult(7), CancellationToken.None));
    }
}
