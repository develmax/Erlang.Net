namespace Erlang;

internal sealed partial class RuntimePersistentTerms
{
    internal async ValueTask<Term> EraseAll419(ProcessContext context)
    {
        await arena.EraseAll419(context).ConfigureAwait(false);

        return PersistentTermNativeAtoms.True;
    }
}
