using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReLiveWP.Services;

internal interface IProjectMetadata
{
    public string Name { get; }
    public string? ProjectRelativePath { get; }
    public IReadOnlyList<string> Arguments { get; }
    public IReadOnlyDictionary<string, string> EnvironmentVariables { get; }
}
