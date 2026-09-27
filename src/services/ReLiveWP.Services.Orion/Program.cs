var builder = WebApplication.CreateBuilder(args);
builder.AddServiceEndpoints();

builder.Services.AddControllers();

builder.Services.AddHttpClient();

var app = builder.Build();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.MapDefaultEndpoints();

app.Run();
