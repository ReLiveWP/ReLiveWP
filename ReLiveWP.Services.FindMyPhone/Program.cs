using System.Text;
using System.Xml;
using Microsoft.AspNetCore.Mvc.Formatters;
using ReLiveWP.Identity;
using ReLiveWP.Services.Grpc.FindMyPhone;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceEndpoints();

builder.Services.AddLiveIDAuthentication(o =>
{
    o.IdentityGrpcConfiguration = (c) => c.Address = new Uri(builder.Configuration["Endpoints:Identity"]!);
    o.LiveIDConfiguration = (c) =>
    {
        c.HeaderNameOverride = "X-WM-Services-Token";
        c.ValidServiceTargets = ["http://Passport.NET/tb", "relivewp.net", "services.relivewp.net"];
    };
});
builder.Services.AddControllers(o =>
{
    o.OutputFormatters.Insert(0, new Utf8XmlOutputFormatter());
    o.InputFormatters.Insert(0, new XmlSerializerInputFormatter(o));
});

builder.Services.AddGrpcClient<FindMyPhone.FindMyPhoneClient>(
    o => o.Address = new Uri(builder.Configuration["Endpoints:Skybox"]!));

var app = builder.Build();

app.Use(async (ctx, next) =>
{
    ctx.Request.EnableBuffering(); // this is dumb
    await next();
});

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

public class Utf8XmlOutputFormatter : XmlSerializerOutputFormatter
{
    public override XmlWriter CreateXmlWriter(OutputFormatterWriteContext context, TextWriter writer, XmlWriterSettings settings)
    {
        settings.Encoding = Encoding.UTF8;
        settings.OmitXmlDeclaration = true; // or true if you don't care
        return base.CreateXmlWriter(writer, settings);
    }
}
