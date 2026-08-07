using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using ReLiveWP.Services.Exchange.Middleware;
using ReLiveWP.Services.Exchange.Models;

namespace ReLiveWP.Services.Exchange.Tests;

public class BinaryQueryStringTests
{
    // the spec's own worked example: Sync, protocol 14.0, en-US, DeviceId "v140Device",
    // DeviceType "SmartPhone"
    private const string SpecExample = "jAAJBAp2MTQwRGV2aWNlAApTbWFydFBob25l";

    private static async Task<ActiveSyncContext> ParseAsync(string query)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Path = "/Microsoft-Server-ActiveSync";
        context.Request.QueryString = new QueryString("?" + query);
        context.Request.Body = new MemoryStream();

        var middleware = new ActiveSyncMiddleware(
            _ => Task.CompletedTask,
            NullLogger<ActiveSyncMiddleware>.Instance,
            new ConfigurationBuilder().Build());

        await middleware.InvokeAsync(context);

        return (ActiveSyncContext)context.Items[ActiveSyncMiddleware.ContextKey]!;
    }

    [Fact]
    public async Task Spec_example_decodes_field_for_field()
    {
        var ctx = await ParseAsync(SpecExample);

        Assert.Equal(EasCommand.Sync, ctx.Command);
        Assert.Equal("14.0", ctx.ProtocolVersion);
        Assert.Equal("v140Device", ctx.DeviceId);
        Assert.Equal("SmartPhone", ctx.DeviceType);
    }

    [Fact]
    public async Task DeviceId_is_not_hex_encoded()
    {
        var ctx = await ParseAsync(SpecExample);

        // what the old parser produced for the same bytes
        Assert.NotEqual("76313430446576696365", ctx.DeviceId);
    }

    // the text form is the other half of the pair: both spellings of the same request must agree,
    // or sync state and device records key off two different ids for one device
    [Fact]
    public async Task Binary_and_text_forms_agree_on_DeviceId()
    {
        var binary = await ParseAsync(SpecExample);
        var text = await ParseAsync("Cmd=Sync&User=u&DeviceId=v140Device&DeviceType=SmartPhone");

        Assert.Equal(text.DeviceId, binary.DeviceId);
        Assert.Equal(text.DeviceType, binary.DeviceType);
        Assert.Equal(text.Command, binary.Command);
    }
}
