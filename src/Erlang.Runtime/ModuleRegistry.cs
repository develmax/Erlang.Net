using System.Numerics;

namespace Erlang;

public sealed class ModuleRegistry
{
    private readonly object gate = new();
    private readonly Dictionary<(string Module, string Function, int Arity), ErlangFunction> functions = new();
    private readonly Dictionary<string, Atom> loadedModules = new(StringComparer.Ordinal);

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
        {
            functions.Add((module, function, arity), body);
            PublishModule(module);
        }
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

    public void PublishModule(string module)
    {
        AtomNameRegistry.Observe(module);
        lock (gate)
        {
            if (!loadedModules.ContainsKey(module))
                loadedModules.Add(module, Term.A(module));
        }
    }

    public bool IsModuleLoaded(string module)
    {
        lock (gate)
            return loadedModules.ContainsKey(module);
    }

    public IReadOnlyCollection<string> LoadedModules
    {
        get
        {
            lock (gate)
                return Array.AsReadOnly(loadedModules.Keys.ToArray());
        }
    }

    internal Term LoadedModuleList()
    {
        lock (gate)
        {
            Term result = Nil.Value;
            foreach (Atom module in loadedModules.Values)
                result = new Cons(module, result);

            return result;
        }
    }

    public void Apply(Action<ModuleRegistry> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        lock (gate)
        {
            var previous = new Dictionary<(string Module, string Function, int Arity), ErlangFunction>(functions);
            var previousModules = new Dictionary<string, Atom>(loadedModules, StringComparer.Ordinal);
            try
            {
                configure(this);
            }
            catch
            {
                functions.Clear();
                foreach (var entry in previous)
                    functions.Add(entry.Key, entry.Value);
                loadedModules.Clear();
                foreach (var module in previousModules)
                    loadedModules.Add(module.Key, module.Value);
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
