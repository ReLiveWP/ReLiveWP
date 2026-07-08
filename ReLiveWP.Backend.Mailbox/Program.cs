using Microsoft.EntityFrameworkCore;
using ReLiveWP.Backend.Mailbox.Data;
using ReLiveWP.Backend.Mailbox.Services;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceEndpoints();

builder.Services.AddGrpc();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<MailboxDbContext>(options =>
    options.UseNpgsql(connectionString)
           .AddInterceptors(new ChangeLogInterceptor()));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
    scope.ServiceProvider.GetRequiredService<MailboxDbContext>().Database.Migrate();

app.MapGrpcService<MailboxStoreService>();

app.MapDefaultEndpoints();

app.Run();
