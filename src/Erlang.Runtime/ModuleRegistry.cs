using System.Numerics;

namespace Erlang;

public sealed class ModuleRegistry
{
    private readonly object gate = new();
    private readonly Dictionary<(string Module, string Function, int Arity), ErlangFunction> functions = new();
    private readonly Dictionary<string, Atom> loadedModules = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Atom> preloadedModules = new(StringComparer.Ordinal);
    private readonly Dictionary<string, ModuleCodeDescriptor> codeDescriptors = new(StringComparer.Ordinal);
    private readonly Dictionary<string, ModuleInformationSnapshot> informationSnapshots = new(StringComparer.Ordinal);
    private readonly HashSet<string> knownModules = new(StringComparer.Ordinal);
    private readonly Dictionary<string, ModuleCodeSnapshot> oldCode = new(StringComparer.Ordinal);
    private readonly Dictionary<(string Module, string Function, int Arity), object> sourceOwners = new();

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
            informationSnapshots.Remove(module);
        }
    }

    public void RegisterSource(
        string module,
        string function,
        int arity,
        ErlangFunction body,
        object sourceOwner
    )
    {
        ArgumentNullException.ThrowIfNull(sourceOwner);
        lock (gate)
        {
            Register(
                module,
                function,
                arity,
                body
            );
            sourceOwners.Add((module, function, arity), sourceOwner);
        }
    }

    public object? SourceOwner(string module, string function, int arity)
    {
        lock (gate)
            return sourceOwners.GetValueOrDefault((module, function, arity));
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
            knownModules.Add(module);
            if (!loadedModules.ContainsKey(module))
                loadedModules.Add(module, Term.A(module));
        }
    }

    public bool IsModuleLoaded(string module)
    {
        lock (gate)
            return loadedModules.ContainsKey(module);
    }

    public bool HasOldCode(string module)
    {
        lock (gate)
            return oldCode.ContainsKey(module);
    }

    public Term DeleteModule(string module)
    {
        lock (gate)
        {
            if (!knownModules.Contains(module))
                return Term.A(ModuleCodeLifecycleNames.UnknownModule);
            if (oldCode.ContainsKey(module))
                throw new ErlangException(ErlangErrorReasons.BadArgument);
            var entries = functions.Where(entry => entry.Key.Module == module).Select(entry => (entry.Key, entry.Value)).ToArray();
            codeDescriptors.TryGetValue(module, out var descriptor);
            if (entries.Length != 0 || descriptor is not null)
                oldCode.Add(module, new ModuleCodeSnapshot(Array.AsReadOnly(entries), descriptor));
            foreach (var entry in entries)
            {
                functions.Remove(entry.Key);
                sourceOwners.Remove(entry.Key);
            }
            loadedModules.Remove(module);
            codeDescriptors.Remove(module);
            informationSnapshots.Remove(module);

            return Term.A(ErlangBooleanAtoms.True);
        }
    }

    public void PublishModuleCode(string module, ModuleCodeDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        lock (gate)
        {
            PublishModule(module);
            codeDescriptors[module] = descriptor;
            informationSnapshots.Remove(module);
        }
    }

    public Term ModuleInformation(string module, string? key = null)
    {
        lock (gate)
        {
            if (!informationSnapshots.TryGetValue(module, out var snapshot))
            {
                if (!loadedModules.ContainsKey(module))
                    throw new ErlangException(ErlangErrorReasons.BadArgument);
                var entries = functions.Where(entry => entry.Key.Module == module).Select(entry => (Signature: (entry.Key.Function, entry.Key.Arity), Body: entry.Value)).ToArray();
                codeDescriptors.TryGetValue(module, out var descriptor);
                if (descriptor is null && entries.Length == 0)
                    throw new ErlangException(ErlangErrorReasons.BadArgument);
                var exports = entries.Select(entry => entry.Signature).ToArray();
                var signatures = descriptor is null ? exports : descriptor.Functions.Concat(exports);
                var additional = descriptor is null ? entries : entries.Where(entry => !descriptor.Exports.Contains(entry.Signature)).ToArray();
                BitString fingerprint = descriptor is not null && additional.Length == 0 ? descriptor.Fingerprint : ModuleCodeFingerprints.Registered(module, additional, descriptor?.Fingerprint);
                Term compilation = descriptor?.Compilation ?? ModuleCodeFingerprints.Compilation(typeof(ModuleRegistry));
                snapshot = new ModuleInformationSnapshot(
                    module,
                    exports,
                    signatures,
                    fingerprint,
                    compilation,
                    descriptor?.Attributes
                );
                informationSnapshots.Add(module, snapshot);
            }

            return key is null ? snapshot.All : snapshot.Field(key);
        }
    }

    public IReadOnlyCollection<string> LoadedModules
    {
        get
        {
            lock (gate)
                return Array.AsReadOnly(loadedModules.Keys.ToArray());
        }
    }

    public void InstallPreloaded(Action<ModuleRegistry> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        Apply(target =>
        {
            var previous = loadedModules.Keys.ToHashSet(StringComparer.Ordinal);
            configure(target);
            foreach (var module in loadedModules)
            {
                if (!previous.Contains(module.Key))
                    preloadedModules.TryAdd(module.Key, module.Value);
            }
        });
    }

    public IReadOnlyCollection<string> PreloadedModules
    {
        get
        {
            lock (gate)
                return Array.AsReadOnly(preloadedModules.Keys.ToArray());
        }
    }

    internal Term PreloadedModuleList()
    {
        lock (gate)
        {
            Term result = Nil.Value;
            foreach (Atom module in preloadedModules.Values)
                result = new Cons(module, result);

            return result;
        }
    }

    internal Term LoadedModuleList()
    {
        lock (gate)
        {
            Term result = Nil.Value;
            foreach (string module in loadedModules.Keys.Concat(oldCode.Keys).Distinct(StringComparer.Ordinal))
                result = new Cons(Term.A(module), result);

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
            var previousPreloaded = new Dictionary<string, Atom>(preloadedModules, StringComparer.Ordinal);
            var previousDescriptors = new Dictionary<string, ModuleCodeDescriptor>(codeDescriptors, StringComparer.Ordinal);
            var previousInformation = new Dictionary<string, ModuleInformationSnapshot>(informationSnapshots, StringComparer.Ordinal);
            var previousKnown = knownModules.ToArray();
            var previousOld = new Dictionary<string, ModuleCodeSnapshot>(oldCode, StringComparer.Ordinal);
            var previousOwners = new Dictionary<(string Module, string Function, int Arity), object>(sourceOwners);
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
                preloadedModules.Clear();
                foreach (var module in previousPreloaded)
                    preloadedModules.Add(module.Key, module.Value);
                codeDescriptors.Clear();
                foreach (var entry in previousDescriptors)
                    codeDescriptors.Add(entry.Key, entry.Value);
                informationSnapshots.Clear();
                foreach (var entry in previousInformation)
                    informationSnapshots.Add(entry.Key, entry.Value);
                knownModules.Clear();
                knownModules.UnionWith(previousKnown);
                oldCode.Clear();
                foreach (var entry in previousOld)
                    oldCode.Add(entry.Key, entry.Value);
                sourceOwners.Clear();
                foreach (var entry in previousOwners)
                    sourceOwners.Add(entry.Key, entry.Value);
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
