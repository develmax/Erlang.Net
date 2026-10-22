using Erlang;
namespace Erlang.Tests;

internal static class EtsRename418HostChecks
{
    private static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException(EtsRename418HostNames.Failure);
    }

    private static ValueTask<Term> Call(
        ProcessRuntime runtime,
        ProcessContext context,
        string function,
        params Term[] args
    )
     => runtime.Modules.Call(
         context,
         EtsRename418HostNames.Ets,
         function,
         args
     );

    private static void Register(ProcessRuntime runtime)
    {
        RuntimeNativePrimitiveProviders.Register(runtime, runtime.Modules, RuntimeNativePrimitives.EtsStorageCore);
        RuntimeBootstrapEtsRename418.Register(runtime, runtime.Modules);
    }

    private static async ValueTask BadArgument(Func<Task<Term>> operation)
    {
        bool failed = false;
        try
        {
            await operation();
        }
        catch (ErlangException error)
        {
            failed = error.Reason.Equals(Term.A(ErlangErrorReasons.BadArgument));
        }
        Require(failed);
    }

    internal static async Task Basics()
    {
        await using var runtime = new ProcessRuntime();
        Register(runtime);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term old = Term.A(EtsRename418HostNames.Old), next = Term.A(EtsRename418HostNames.Next);
            _ = await Call(
                runtime,
                context,
                EtsRename418HostNames.New,
                old,
                Term.List(Term.A(EtsRename418HostNames.Named))
            );
            Term original = await Call(
                runtime,
                context,
                EtsRename418HostNames.Whereis,
                old
            );
            Term? replacement = null, anonymous = null;
            try
            {
                await BadArgument(async () => await Call(
                    runtime,
                    context,
                    EtsRename418HostNames.Rename,
                    old,
                    old
                ));
                Require((await Call(
                    runtime,
                    context,
                    EtsRename418HostNames.Rename,
                    original,
                    next
                )).Equals(next));
                Require((await Call(
                    runtime,
                    context,
                    EtsRename418HostNames.Whereis,
                    old
                )).Equals(Term.A(EtsRename418HostNames.Undefined)));
                Require((await Call(
                    runtime,
                    context,
                    EtsRename418HostNames.Whereis,
                    next
                )).Equals(original));
                _ = await Call(
                    runtime,
                    context,
                    EtsRename418HostNames.New,
                    old,
                    Term.List(Term.A(EtsRename418HostNames.Named))
                );
                replacement = await Call(
                    runtime,
                    context,
                    EtsRename418HostNames.Whereis,
                    old
                );
                _ = await Call(
                    runtime,
                    context,
                    EtsRename418HostNames.Insert,
                    original,
                    Term.Tuple(Term.I(EtsRename418HostNames.Key), Term.I(EtsRename418HostNames.Initial))
                );
                _ = await Call(
                    runtime,
                    context,
                    EtsRename418HostNames.Insert,
                    replacement,
                    Term.Tuple(Term.I(EtsRename418HostNames.Key), Term.I(EtsRename418HostNames.Replacement))
                );
                anonymous = await Call(
                    runtime,
                    context,
                    EtsRename418HostNames.New,
                    next,
                    Nil.Value
                );
                Require(ReferenceEquals(await Call(
                    runtime,
                    context,
                    EtsRename418HostNames.Rename,
                    anonymous,
                    old
                ), anonymous));
                Require((await Call(
                    runtime,
                    context,
                    EtsRename418HostNames.Whereis,
                    old
                )).Equals(replacement));
                await BadArgument(async () => await Call(
                    runtime,
                    context,
                    EtsRename418HostNames.Rename,
                    original,
                    old
                ));
                Require((await Call(
                    runtime,
                    context,
                    EtsRename418HostNames.Whereis,
                    next
                )).Equals(original));
                Require((await Call(
                    runtime,
                    context,
                    EtsRename418HostNames.Whereis,
                    old
                )).Equals(replacement));
                Require((await Call(
                    runtime,
                    context,
                    EtsRename418HostNames.Lookup,
                    original,
                    Term.I(EtsRename418HostNames.Key)
                )).Equals(Term.List(Term.Tuple(Term.I(EtsRename418HostNames.Key), Term.I(EtsRename418HostNames.Initial)))));
                Require((await Call(
                    runtime,
                    context,
                    EtsRename418HostNames.Lookup,
                    replacement,
                    Term.I(EtsRename418HostNames.Key)
                )).Equals(Term.List(Term.Tuple(Term.I(EtsRename418HostNames.Key), Term.I(EtsRename418HostNames.Replacement)))));
                _ = await Call(
                    runtime,
                    context,
                    EtsRename418HostNames.Delete,
                    original
                );
                Require((await Call(
                    runtime,
                    context,
                    EtsRename418HostNames.Whereis,
                    next
                )).Equals(Term.A(EtsRename418HostNames.Undefined)));
                Require((await Call(
                    runtime,
                    context,
                    EtsRename418HostNames.Whereis,
                    old
                )).Equals(replacement));
            }
            finally
            {
                if (anonymous is not null) _ = await Call(
                    runtime,
                    context,
                    EtsRename418HostNames.Delete,
                    anonymous
                );
                if (replacement is not null) _ = await Call(
                    runtime,
                    context,
                    EtsRename418HostNames.Delete,
                    replacement
                );
                try
                {
                    _ = await Call(
                        runtime,
                        context,
                        EtsRename418HostNames.Delete,
                        original
                    );
                }
                catch (ErlangException) { }
            }

            return true;
        });
    }

    internal static async Task Access()
    {
        await using var runtime = new ProcessRuntime();
        Register(runtime);
        await using var owner = new RuntimeNodeFoundationHostActor(runtime);
        await using var borrower = new RuntimeNodeFoundationHostActor(runtime);
        foreach (string protection in EtsRename418HostNames.Protections)
        {
            Term old = Term.A(EtsRename418HostNames.Old), next = Term.A(EtsRename418HostNames.Next);
            _ = await owner.InvokeAsync(async context => await Call(
                runtime,
                context,
                EtsRename418HostNames.New,
                old,
                Term.List(Term.A(EtsRename418HostNames.Named), Term.A(protection))
            ));
            Term handle = await owner.InvokeAsync(async context => await Call(
                runtime,
                context,
                EtsRename418HostNames.Whereis,
                old
            ));
            try
            {
                if (protection == EtsRename418HostNames.Public)
                    Require((await borrower.InvokeAsync(async context => await Call(
                        runtime,
                        context,
                        EtsRename418HostNames.Rename,
                        old,
                        next
                    ))).Equals(next));
                else
                    await BadArgument(() => borrower.InvokeAsync(async context => await Call(
                        runtime,
                        context,
                        EtsRename418HostNames.Rename,
                        old,
                        next
                    )));
            }
            finally
            {
                _ = await owner.InvokeAsync(async context => await Call(
                    runtime,
                    context,
                    EtsRename418HostNames.Delete,
                    handle
                ));
            }
        }
    }

    internal static async Task RetiredName()
    {
        await using var runtime = new ProcessRuntime();
        Register(runtime);
        await using var deadOwner = new RuntimeNodeFoundationHostActor(runtime);
        await using var survivor = new RuntimeNodeFoundationHostActor(runtime);
        Term old = Term.A(EtsRename418HostNames.Old), next = Term.A(EtsRename418HostNames.Next);
        _ = await deadOwner.InvokeAsync(async context => await Call(
            runtime,
            context,
            EtsRename418HostNames.New,
            next,
            Term.List(Term.A(EtsRename418HostNames.Named))
        ));
        _ = await survivor.InvokeAsync(async context => await Call(
            runtime,
            context,
            EtsRename418HostNames.New,
            old,
            Term.List(Term.A(EtsRename418HostNames.Named))
        ));
        Term handle = await survivor.InvokeAsync(async context => await Call(
            runtime,
            context,
            EtsRename418HostNames.Whereis,
            old
        ));
        object gate = typeof(ProcessRuntime).GetField(EtsRename418HostNames.Gate, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(runtime)!;
        try
        {
            await survivor.Invoke(context =>
            {
                lock (gate)
                {
                    runtime.Exit(context, deadOwner.Process.Pid, Term.A(EtsRename418HostNames.Kill));
                    // Actual Terminate has retired its group. Retirement cannot reacquire gate yet.
                    var rename = Call(
                        runtime,
                        context,
                        EtsRename418HostNames.Rename,
                        handle,
                        next
                    );
                    Require(rename.IsCompletedSuccessfully && rename.GetAwaiter().GetResult().Equals(next));
                }

                return true;
            });
            await deadOwner.Process.Completion.WaitAsync(RuntimeNodeFoundationHostActorParameters.Deadline);
            Require((await survivor.InvokeAsync(async context => await Call(
                runtime,
                context,
                EtsRename418HostNames.Whereis,
                next
            ))).Equals(handle));
        }
        finally
        {
            _ = await survivor.InvokeAsync(async context => await Call(
                runtime,
                context,
                EtsRename418HostNames.Delete,
                handle
            ));
        }
    }

    internal static async Task Admission()
    {
        await using var runtime = new ProcessRuntime();
        Register(runtime);
        int count = runtime.Modules.Exports.Count();
        RuntimeBootstrapEtsRename418.Register(runtime, runtime.Modules);
        Require(count == runtime.Modules.Exports.Count());
        await using var foreign = new ProcessRuntime();
        bool refused = false;
        try
        {
            RuntimeBootstrapEtsRename418.Register(runtime, foreign.Modules);
        }
        catch (ArgumentException)
        {
            refused = true;
        }
        Require(refused);
        foreign.Modules.Register(
            EtsRename418HostNames.Ets,
            EtsRename418HostNames.Rename,
            2,
            (_, _) => ValueTask.FromResult<Term>(Nil.Value)
        );
        refused = false;
        try
        {
            RuntimeBootstrapEtsRename418.Register(foreign, foreign.Modules);
        }
        catch (InvalidOperationException)
        {
            refused = true;
        }
        Require(refused);
    }
}
