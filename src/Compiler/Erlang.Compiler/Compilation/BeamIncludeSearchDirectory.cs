namespace Erlang.Compiler;

public sealed record BeamIncludeSearchDirectory(string PhysicalDirectory, string RecordedDirectory)
{
    /// <summary>Physical directories for renamed children of the recorded search directory.</summary>
    public IReadOnlyDictionary<string, string>? PhysicalChildDirectories { get; init; }
}
