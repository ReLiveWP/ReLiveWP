
namespace ReLiveWP.Services
{
    internal interface IProjectBuilder
    {
        IReadOnlyList<string> Arguments { get; }
        IReadOnlyDictionary<string, string> EnvironmentVariables { get; }
        string Name { get; }

        IProjectBuilder WithArgument(string argument);
        IProjectBuilder WithArguments(params string[] arguments);
        IProjectBuilder WithEnvironmentVariable(string key, string value);
        IProjectBuilder WithEnvironmentVariables(IReadOnlyDictionary<string, string> variables);

        IProjectBuilder DependsOn(IProjectBuilder project);
    }
}