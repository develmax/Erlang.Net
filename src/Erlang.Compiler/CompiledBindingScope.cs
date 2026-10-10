// Modified: CLR subset of v3_core ulinearize_exprs/uexprs known-variable
// constraints. Sibling exports constrain explicit matches without entering
// the child's lexical environment or anonymous-function capture.
// This is not the complete Core known-variable/FUV transformation.

namespace Erlang.Compiler;

internal sealed class CompiledBindingScope : Dictionary<string, Term>
{
    public IReadOnlyDictionary<string, Term> Constraints { get; }

    public CompiledBindingScope(Dictionary<string, Term> bindings, IReadOnlyDictionary<string, Term> constraints)
        : base(bindings, StringComparer.Ordinal)
    {
        var combined = bindings is CompiledBindingScope parent
            ? new Dictionary<string, Term>(parent.Constraints, StringComparer.Ordinal)
            : new Dictionary<string, Term>(StringComparer.Ordinal);
        foreach (var binding in constraints)
            combined[binding.Key] = binding.Value;
        Constraints = combined;
    }

    public static Dictionary<string, Term> Copy(Dictionary<string, Term> bindings)
    {
        return bindings is CompiledBindingScope scope
            ? new CompiledBindingScope(bindings, scope.Constraints)
            : new Dictionary<string, Term>(bindings, StringComparer.Ordinal);
    }

    public static bool Match(
        Pattern pattern,
        Term value,
        Dictionary<string, Term> bindings,
        ProcessContext context
    )
    {
        if (bindings is not CompiledBindingScope scope)
            return pattern.Match(value, bindings, context);

        var candidate = new Dictionary<string, Term>(bindings, StringComparer.Ordinal);
        foreach (string name in Semantics.Variables(pattern).Distinct(StringComparer.Ordinal))
        {
            if (!scope.Constraints.TryGetValue(name, out var constraint))
                continue;
            if (candidate.TryGetValue(name, out var previous) && !previous.Equals(constraint))
                return false;
            candidate[name] = constraint;
        }
        if (!pattern.Match(
            value,
            candidate,
            context,
            bindings
        ))
            return false;
        foreach (var binding in candidate)
            bindings[binding.Key] = binding.Value;

        return true;
    }
}
