using System.Text;
using Atom.Formatters;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using ReLiveWP.Services.Activity.Models.Atom;

namespace ReLiveWP.Services.Activity.Tests;

public class StatusEntryTests
{
    private const string DeviceStatusBody = """
        <?xml version="1.0" encoding="UTF-8"?>
        <entry xmlns:live="http://api.live.com/schemas" xmlns="http://www.w3.org/2005/Atom">
          <title>Sent from my Windows Phone</title>
          <live:networks>
            <live:sourceId>WL</live:sourceId>
          </live:networks>
        </entry>
        """;

    [Fact]
    public async Task DeviceStatusBodyReadsTitle()
    {
        var entry = await ReadDeviceStatusBodyAsync();

        Assert.Equal("Sent from my Windows Phone", entry?.Title?.Value);
    }

    private static async Task<LiveEntry?> ReadDeviceStatusBodyAsync()
    {
        var formatter = new AtomInputFormatter(new MvcOptions());
        var httpContext = new DefaultHttpContext();
        httpContext.Request.ContentType = "application/atom+xml";
        httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(DeviceStatusBody));

        var metadata = new EmptyModelMetadataProvider().GetMetadataForType(typeof(LiveEntry));
        var context = new InputFormatterContext(httpContext, "", new ModelStateDictionary(), metadata, (stream, encoding) => new StreamReader(stream, encoding));

        var result = await formatter.ReadAsync(context);

        Assert.False(result.HasError);
        return result.Model as LiveEntry;
    }
}
