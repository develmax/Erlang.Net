using System.Collections.Frozen;

namespace Erlang;

public sealed partial class ModuleRegistry
{
    private FrozenDictionary<(string Function, int Arity), ErlangFunction>? canonicalMatchSpecGuards419;

    // Called only at the end of actual CoreModules registration, before host
    // overrides. Native DBIF semantics cannot follow mutable guest registries.
    internal void SealCanonicalMatchSpecGuards419()
    {
        lock (gate)
        {
            if (canonicalMatchSpecGuards419 is not null) return;
            var selected = new Dictionary<(string Function, int Arity), ErlangFunction>();
            foreach (var signature in MatchSpecOperations.Pure)
            {
                if (signature is (ErlangModuleNames.Length, 1) or (ErlangModuleNames.IsInteger, 3)) continue;
                if (functions.TryGetValue((ErlangModuleNames.Module, signature.Name, signature.Arity), out var body))
                    selected.Add((signature.Name, signature.Arity), body);
            }
            canonicalMatchSpecGuards419 = selected.ToFrozenDictionary();
        }
    }

    internal Term InvokeCanonicalMatchSpecGuard419(ProcessContext context, string function, Term[] arguments)
    {
        if (function == ErlangModuleNames.Length && arguments.Length == 1)
            return ErlangListLength.ReadMatchSpec(arguments[0]);
        if (function == ErlangModuleNames.IsInteger && arguments.Length == 3)
        {
            if (arguments[1] is not Integer minimum || arguments[2] is not Integer maximum)
                throw new ErlangException(ErlangErrorReasons.BadArgument);

            return CoreModules.Boolean(arguments[0] is Integer number && number.Value >= minimum.Value && number.Value <= maximum.Value);
        }
        var selected = canonicalMatchSpecGuards419;
        if (selected is null || !selected.TryGetValue((function, arguments.Length), out var body))
            throw new NotSupportedException(EtsCompiledNames419.MissingCanonical);
        ValueTask<Term> result = body(context, arguments);
        if (!result.IsCompleted) throw new NotSupportedException(EtsCompiledNames419.AsyncCanonical);

        return result.GetAwaiter().GetResult();
    }
}
