namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private static bool HasTupleAtomEntryPartition(
        FunctionDefinition function,
        int argument,
        int size,
        string tag,
        string atom
    )
    {
        if (function.Clauses is not [
            { Guard: null } tuple,
            { Guard: null } scalar,
            { Guard: null } fallback
        ] || tuple.Patterns[argument] is not Pattern.Tuple { Items: [Pattern.Literal { Value: Atom tupleTag }, ..] } pattern
            || pattern.Items.Count != size
            || tupleTag.Name != tag || scalar.Patterns[argument] is not Pattern.Literal { Value: Atom scalarTag }
            || scalarTag.Name != atom || !HasUnconditionalParameters(fallback))
        {
            return false;
        }

        // Strip the selectors and account for every other binding. Equality
        // between two head bindings would create another clause failure.
        return HasUnconditionalParameters(tuple with
        {
            Patterns = tuple.Patterns.Where((_, index) => index != argument).Concat(pattern.Items.Skip(1)).ToArray()
        }) && HasUnconditionalParameters(scalar with
        {
            Patterns = scalar.Patterns.Where((_, index) => index != argument).ToArray()
        });
    }
}
