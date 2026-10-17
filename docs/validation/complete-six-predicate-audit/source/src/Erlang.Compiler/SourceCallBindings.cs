namespace Erlang.Compiler;

public sealed class SourceCallBindings
{
    public static SourceCallBindings Empty { get; } = new(false, [], []);

    public bool SuppressAll { get; }
    public IReadOnlyList<(string Name, int Arity)> Suppressed { get; }
    public IReadOnlyList<(string Module, string Name, int Arity)> Imports { get; }
    public bool IsEmpty => !SuppressAll && Suppressed.Count == 0 && Imports.Count == 0;

    public SourceCallBindings(
        bool suppressAll,
        IEnumerable<(string Name, int Arity)> suppressed,
        IEnumerable<(string Module, string Name, int Arity)> imports
    )
    {
        SuppressAll = suppressAll;
        Suppressed = Array.AsReadOnly(suppressed.Distinct().ToArray());
        Imports = Array.AsReadOnly(imports.ToArray());
    }

    internal bool IsSuppressed(string name, int arity) => SuppressAll || Suppressed.Contains((name, arity));

    internal string? ImportedModule(string name, int arity)
    {
        foreach (var import in Imports)
            if (import.Name == name && import.Arity == arity)
                return import.Module;

        return null;
    }
}
