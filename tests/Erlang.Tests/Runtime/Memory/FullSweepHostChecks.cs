namespace Erlang.Tests;

public static class FullSweepHostChecks
{
    public static async Task Run()
    {
        await using var runtime = new ProcessRuntime();
        Exception? failure = null;
        bool completed = false;
        var owner = runtime.Spawn(async context =>
        {
            try
            {
                await ChangeThreshold(context, ushort.MaxValue);
                object heap = VmStorageBirthAccess.Field(context, VmStorageBirthNames.Heap);
                TupleTerm old;
                using (var roots = (IDisposable)VmStorageBirthAccess.Call(heap, FullSweepHostNames.EnterFrame))
                {
                    old = (TupleTerm)VmStorageBirthAccess.Call(roots, FullSweepHostNames.Retain, Term.Tuple(Term.I(1)));
                    for (int index = 0; index < FullSweepHostNames.PromotionCollections; index++)
                        await CollectMinor(context);
                    object generation = VmStorageBirthAccess.Property(heap, FullSweepHostNames.GenerationSnapshot)!;
                    if ((long)VmStorageBirthAccess.Property(generation, FullSweepHostNames.OldWords)! <= 0)
                        throw new InvalidOperationException(FullSweepHostNames.Error);
                }
                // The actual unrooted old block survives a genuine minor pass.
                // This is the negative control for the forced full sweep below.
                await CollectMinor(context);
                if (!old.Items[0].Equals(Term.I(1)))
                    throw new InvalidOperationException(FullSweepHostNames.Error);
                await ChangeThreshold(context, 0);
                await CollectMinor(context);
                bool retired = false;
                try
                {
                    _ = old.Items[0];
                }
                catch (ObjectDisposedException)
                {
                    retired = true;
                }
                if (!retired)
                    throw new InvalidOperationException(FullSweepHostNames.Error);
                completed = true;
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
        if (!completed)
            throw new InvalidOperationException(FullSweepHostNames.Error);
    }

    private static ValueTask<Term> ChangeThreshold(ProcessContext context, ushort threshold)
        => context.Runtime.Modules.Call(
            context,
            ErlangModuleNames.Module,
            ErlangModuleNames.ProcessFlag,
            Term.A(FullSweepHostNames.Flag),
            Term.I(threshold)
        );

    private static async ValueTask CollectMinor(ProcessContext context)
    {
        Term result = await context.Runtime.Modules.Call(
            context,
            ErlangModuleNames.Module,
            ProcessGarbageCollectionNames.Function,
            context.Self,
            Term.List(Term.Tuple(Term.A(ProcessGarbageCollectionNames.TypeOption), Term.A(ProcessGarbageCollectionNames.Minor)))
        );
        if (!result.Equals(Term.A(ErlangBooleanAtoms.True)))
            throw new InvalidOperationException(FullSweepHostNames.Error);
    }
}
