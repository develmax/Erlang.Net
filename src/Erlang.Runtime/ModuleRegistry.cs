using System.Numerics;

namespace Erlang;

public sealed class ModuleRegistry
{
    private readonly object gate = new();
    private readonly Dictionary<(string Module, string Function, int Arity), ErlangFunction> functions = new();

    public void Register(
        string module,
        string function,
        int arity,
        ErlangFunction body
    )
    {
        AtomNameRegistry.Observe(module);
        AtomNameRegistry.Observe(function);
        lock (gate)
            functions.Add((module, function, arity), body);
    }

    public ValueTask<Term> Call(
        ProcessContext context,
        string module,
        string function,
        params Term[] arguments
    )
    {
        ErlangFunction? body;
        lock (gate)
            functions.TryGetValue((module, function, arguments.Length), out body);

        return body is not null ? body(context, arguments) : throw new ErlangException(ErlangErrorReasons.UndefinedFunction);
    }

    public bool HasExport(string module, string function, int arity)
    {
        lock (gate)
            return functions.ContainsKey((module, function, arity));
    }

    public void Apply(Action<ModuleRegistry> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        lock (gate)
        {
            var previous = new Dictionary<(string Module, string Function, int Arity), ErlangFunction>(functions);
            try
            {
                configure(this);
            }
            catch
            {
                functions.Clear();
                foreach (var entry in previous)
                    functions.Add(entry.Key, entry.Value);
                throw;
            }
        }
    }

    public IReadOnlyCollection<(string Module, string Function, int Arity)> Exports
    {
        get
        {
            lock (gate)
                return Array.AsReadOnly(functions.Keys.ToArray());
        }
    }
}
