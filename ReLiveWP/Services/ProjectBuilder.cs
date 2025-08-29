using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReLiveWP.Services;

internal class ProjectBuilder : IProjectMetadata, IProjectBuilder
{
    private readonly string projectName;
    private readonly string? projectRelativePath;
    private readonly List<string> projectArguments;
    private readonly Dictionary<string, string> projectVariables;

    public string Name
        => projectName;
    public string? ProjectRelativePath
        => projectRelativePath;
    public IReadOnlyList<string> Arguments
        => projectArguments;
    public IReadOnlyDictionary<string, string> EnvironmentVariables
        => projectVariables;

    public ProjectBuilder(string projectName, string? projectRelativePath = null)
    {
        this.projectName = projectName;
        this.projectRelativePath = projectRelativePath;
        this.projectArguments = [];
        this.projectVariables = [];
    }

    public IProjectBuilder WithArgument(string argument)
    {
        this.projectArguments.Add(argument);
        return this;
    }

    public IProjectBuilder WithArguments(params string[] arguments)
    {
        this.projectArguments.AddRange(arguments);
        return this;
    }

    public IProjectBuilder WithEnvironmentVariable(string key, string value)
    {
        projectVariables[key] = value;
        return this;
    }

    public IProjectBuilder WithEnvironmentVariables(IReadOnlyDictionary<string, string> variables)
    {
        foreach (var item in variables)
            projectVariables.Add(item.Key, item.Value);

        return this;
    }

    public IProjectBuilder DependsOn(IProjectBuilder project)
    {
        // for now this does nothing, in future, add a dependency
        return this;
    }
}
