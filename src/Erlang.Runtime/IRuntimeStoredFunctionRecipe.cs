namespace Erlang;

// Internal trusted runtime/compiler metadata only. Implementations must never
// retain the source FunctionTerm, its environment, invocation, or delegate.
internal interface IRuntimeStoredFunctionRecipe
{
    FunctionTerm Rebind(IReadOnlyList<Term> environment);
}
