namespace Erlang.Tests;

public static class FullSweepHostChecks
{
    public static async Task Run()
    {
        await using var runtime = new ProcessRuntime();
        Exception? failure = null;
        var owner = runtime.Spawn(async context =>
        {
            try
            {
                await runtime.Modules.Call(
                    context,
                    ErlangModuleNames.Module,
                    ErlangModuleNames.ProcessFlag,
                    Term.A(FullSweepHostNames.Flag),
                    Term.I(0)
                );
                int before = GC.CollectionCount(GC.MaxGeneration);
                Term result = await runtime.Modules.Call(
                    context,
                    ErlangModuleNames.Module,
                    ProcessGarbageCollectionNames.Function,
                    context.Self,
                    Term.List(Term.Tuple(Term.A(ProcessGarbageCollectionNames.TypeOption), Term.A(ProcessGarbageCollectionNames.Minor)))
                );
                if (!result.Equals(Term.A(ErlangBooleanAtoms.True)) || GC.CollectionCount(GC.MaxGeneration) <= before)
                    throw new InvalidOperationException(FullSweepHostNames.Error);
            }
            catch (Exception error)
            {
                failure = error;
            }

            return Nil.Value;
        });
        await owner.Completion.WaitAsync(FullSweepHostNames.Timeout);
        if (failure is not null)
            throw failure;
    }
}
