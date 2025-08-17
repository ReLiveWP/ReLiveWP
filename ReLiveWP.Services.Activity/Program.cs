using Atom.Formatters;
using Microsoft.AspNetCore.Mvc.Formatters;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers(c =>
{
    c.InputFormatters.Clear();
    c.InputFormatters.Add(new AtomInputFormatter(c));
    c.OutputFormatters.Clear();
    c.OutputFormatters.Add(new AtomOutputFormatter());
});

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseAuthorization();

app.MapControllers();

app.Run();
