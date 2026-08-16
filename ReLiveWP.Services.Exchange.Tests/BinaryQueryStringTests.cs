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

    private static readonly byte[] DeviceGuid =
    [
        0xCF, 0x3A, 0x8C, 0x57, 0xA7, 0x51, 0x34, 0x3A,
        0xD2, 0xD1, 0x4A, 0x1F, 0x78, 0x9F, 0xEF, 0xAD,
    ];

    private const string DeviceGuidHex = "CF3A8C57A751343AD2D14A1F789FEFAD";

    private static string BinaryQuery(byte[] deviceId, string deviceType)
    {
        var bytes = new List<byte> { 140, (byte)EasCommand.Sync, 0x09, 0x04, (byte)deviceId.Length };
        bytes.AddRange(deviceId);
        bytes.Add(0);
        bytes.Add((byte)deviceType.Length);
        bytes.AddRange(System.Text.Encoding.ASCII.GetBytes(deviceType));

        return Convert.ToBase64String([.. bytes]).TrimEnd('=');
    }

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
    public async Task Spec_example_decodes_the_fields_around_the_device_id()
    {
        var ctx = await ParseAsync(SpecExample);

        Assert.Equal(EasCommand.Sync, ctx.Command);
        Assert.Equal("14.0", ctx.ProtocolVersion);
        Assert.Equal("SmartPhone", ctx.DeviceType);
    }

    // a device sends its id hex encoded in the query string form and raw in the binary form, so
    // the binary field is hex encoded on the way in to land on the one id the device goes by
    [Fact]
    public async Task A_device_guid_comes_back_as_its_hex_string()
    {
        var ctx = await ParseAsync(BinaryQuery(DeviceGuid, "SmartPhone"));

        Assert.Equal(DeviceGuidHex, ctx.DeviceId);
    }

    // the text form is the other half of the pair: both spellings of the same request must agree,
    // or sync state and device records key off two different ids for one device
    [Fact]
    public async Task Binary_and_text_forms_agree_on_DeviceId()
    {
        var binary = await ParseAsync(BinaryQuery(DeviceGuid, "SmartPhone"));
        var text = await ParseAsync(
            $"Cmd=Sync&User=u&DeviceId={DeviceGuidHex}&DeviceType=SmartPhone");

        Assert.Equal(text.DeviceId, binary.DeviceId);
        Assert.Equal(text.DeviceType, binary.DeviceType);
        Assert.Equal(text.Command, binary.Command);
    }

    // decoding the raw bytes as text is what produced a second, junk row for a device that had
    // already synced under its hex id
    [Fact]
    public async Task Raw_bytes_are_never_decoded_as_text()
    {
        var ctx = await ParseAsync(BinaryQuery(DeviceGuid, "SmartPhone"));

        Assert.Equal(System.Text.Encoding.UTF8.GetString(DeviceGuid).Length, 15);
        Assert.NotEqual(System.Text.Encoding.UTF8.GetString(DeviceGuid), ctx.DeviceId);
    }
}
