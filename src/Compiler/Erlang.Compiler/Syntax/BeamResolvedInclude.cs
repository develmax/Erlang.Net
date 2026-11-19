namespace Erlang.Compiler;

internal sealed record BeamResolvedInclude(string PhysicalPath, string? RecordedPath = null);
