using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ReLiveWP.Services;

namespace ReLiveWP;

internal static class BuilderExtensions
{
    public static ProjectBuilder AddProject(this IHostApplicationBuilder appBuilder, string name, string? projectRelativePath = null)
    {
        var builder = new ProjectBuilder(name, projectRelativePath);
        appBuilder.Services.AddSingleton<IProjectMetadata>(builder);

        return builder;
    }
}
