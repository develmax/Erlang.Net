namespace Erlang.NifNativeBackend;

internal static class ResourceQueue593Observation
{
    internal static async ValueTask<Term> Execute(ProcessContext context, Term libraryPath)
    {
        var runtime = context.Runtime;
        Term loaded = await runtime.Modules.Call(
            context,
            Resource529Names.FamilyModule,
            ResourceQueue593Names.Load,
            libraryPath,
            Term.I(0)
        );
        NifNativeHarness.Require(loaded.Equals(Term.A(ResourceQueue593Names.LoadSucceeded)));

        return await runtime.Modules.Call(context, ResourceQueue593Names.Module, Resource529Names.Run);
    }

    internal static bool BothCasesReturned(Term observed)
    {
        Term[] rows = Cons.Items(observed).ToArray();
        string[] modes = [ResourceQueue593Names.Consume, ResourceQueue593Names.Abandon];
        if (rows.Length != modes.Length) return false;
        for (int index = 0; index < rows.Length; index++)
        {
            if (rows[index] is not TupleTerm { Items: [Atom mode, TupleTerm { Items: [TupleTerm result, var cleanup] }] }
                || mode.Name != modes[index]
                || result.Items is not [Atom { Name: ResourceQueue593Names.Returned }, _, _, _, _]) return false;
            Term[] actions = Cons.Items(cleanup).ToArray();
            if (actions is not [
                TupleTerm { Items: [Atom { Name: ResourceQueue593Names.SenderCleanup }, Atom { Name: ResourceQueue593Names.CleanupSucceeded }] },
                TupleTerm { Items: [Atom { Name: ResourceQueue593Names.ReceiverCleanup }, Atom { Name: ResourceQueue593Names.CleanupSucceeded }] }
            ]) return false;
        }

        return true;
    }
}
