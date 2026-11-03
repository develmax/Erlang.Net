using Erlang;
namespace Erlang.Tests;

internal static class CodePurger432HostChecks
{
    internal static async Task RoleAndWindow()
    {
        await using var fixture = await CodePurger432Fixture.Create();
        var body = CodePurger432HostAccess.Body(fixture.Runtime);
        await fixture.Actor.InvokeAsync(async context =>
        {
            bool refused = false;
            try
            {
                await body(context, [Term.I(17), Nil.Value]);
            }
            catch (ErlangException error)
            {
                refused = error.Reason.Equals(Term.A(CodePurger432HostNames.NotSupported));
            }
            CodePurger432HostAccess.Require(refused);
            Term old = await fixture.Runtime.Modules.Call(
                context,
                CodePurger432HostNames.ErLang,
                CodePurger432HostNames.SystemInfo,
                Term.A(CodePurger432HostNames.Outstanding)
            );
            CodePurger432HostAccess.Require(old.Equals(Term.I(2 * CodePurger432HostNames.Schedulers)));
            Term exchanged = await fixture.Runtime.Modules.Call(
                context,
                CodePurger432HostNames.ErLang,
                CodePurger432HostNames.SystemFlag,
                Term.A(CodePurger432HostNames.Outstanding),
                Term.I(1)
            );
            CodePurger432HostAccess.Require(exchanged.Equals(old));
            Term current = await fixture.Runtime.Modules.Call(
                context,
                CodePurger432HostNames.ErLang,
                CodePurger432HostNames.SystemInfo,
                Term.A(CodePurger432HostNames.Outstanding)
            );
            CodePurger432HostAccess.Require(current.Equals(Term.I(1)));
            bool bad = false;
            try
            {
                await fixture.Runtime.Modules.Call(
                    context,
                    CodePurger432HostNames.ErLang,
                    CodePurger432HostNames.SystemFlag,
                    Term.A(CodePurger432HostNames.Outstanding),
                    Term.I(0)
                );
            }
            catch (ErlangException error)
            {
                bad = error.Reason.Equals(Term.A(CodePurger432HostNames.BadArgument));
            }
            CodePurger432HostAccess.Require(bad);

            return true;
        });
        bool badOperation = false;
        try
        {
            await CodePurger432HostAccess.OnPurger(fixture, CodePurgerOriginal432Names.Invalid);
        }
        catch (ErlangException error)
        {
            badOperation = error.Reason.Equals(Term.A(CodePurger432HostNames.BadArgument));
        }
        CodePurger432HostAccess.Require(badOperation);
        await CodePurger432HostAccess.Publish(fixture, CodePurgerOriginal432Names.FirstBeam);
        await CodePurger432HostAccess.Publish(fixture, CodePurgerOriginal432Names.SecondBeam);
        Term sourceResult = await fixture.Actor.InvokeAsync(async context => await fixture.Runtime.Modules.Call(
            context,
            CodePurger432HostNames.Module,
            CodePurger432HostNames.SoftPurge,
            Term.A(CodePurger432HostNames.Subject)
        ));
        CodePurger432HostAccess.Require(sourceResult.Equals(Term.A(CodePurger432HostNames.True)));

        await using var altered = new ProcessRuntime(1);
        await global::Erlang.Tests.PreparedBeamFixtureScope171.RunAsync(
            altered,
            (registry, scope) =>
        {
            byte[] raw = CodePurgerOriginal432Access.Resource(CodePurgerOriginal432Names.PurgerBeam);
            byte[] changed = BeamMetadataModeledBytes.WithAttributes(raw, Cons.From([Term.Tuple(Term.A(CodePurger432HostNames.AlteredAuthority), Cons.From([Term.I(1)]))]));
            var image = scope.Prepare(() => registry.PrepareBeam(changed, fixture.Dependencies));

            return ValueTask.FromResult<Action<ModuleRegistry>>(_ =>
            {
                CodePurger432HostAccess.Require(registry.PublishPreparedBeam([image]).Status == BeamPublicationStatus.Published);
            });
        }
        );
        await using var parent = new RuntimeNodeFoundationHostActor(altered);
        await parent.Invoke(context =>
        {
            object?[] args = [context, CodePurger432HostNames.Module, CodePurger432HostNames.Start, Array.Empty<Term>(), null];
            bool started = (bool)typeof(ProcessRuntime).GetMethod(CodePurger432HostNames.Spawn, CodePurger432HostNames.Flags)!.Invoke(altered, args)!;
            CodePurger432HostAccess.Require(!started && args[4] is null && CodePurger432HostAccess.Field(altered, CodePurger432HostNames.Actor) is null);

            return true;
        });
    }

    internal static async Task PrepareAbortFence()
    {
        await using var fixture = await CodePurger432Fixture.Create();
        PreparedBeamImage first = await CodePurger432HostAccess.Publish(fixture, CodePurgerOriginal432Names.FirstBeam);
        Term fun = await fixture.Actor.InvokeAsync(async context => await fixture.Runtime.Modules.Call(context, CodePurger432HostNames.Subject, CodePurger432HostNames.FunValue));
        await CodePurger432HostAccess.Publish(fixture, CodePurgerOriginal432Names.SecondBeam);
        ModuleCodeImage old = CodePurger432HostAccess.Code(first);
        CodePurger432HostAccess.Require((await CodePurger432HostAccess.OnPurger(fixture, CodePurger432HostNames.Prepare)).Equals(Term.A(CodePurger432HostNames.True)));
        Task pending = fixture.Runtime.Modules.PendingPurge(old)
            ?? throw new InvalidOperationException(CodePurger432HostNames.Failure);
        CodePurger432HostAccess.Require(!pending.IsCompleted);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Task<Term> applying = fixture.Actor.InvokeAsync(async context =>
        {
            entered.TrySetResult();

            return await fixture.Runtime.Modules.Call(
                context,
                CodePurger432HostNames.ErLang,
                CodePurger432HostNames.Apply,
                fun,
                Nil.Value
            );
        });
        await entered.Task.WaitAsync(CodePurger432HostNames.Deadline);
        await CodePurger432HostAccess.ActorTurn(await fixture.Actor.Ready).WaitAsync(CodePurger432HostNames.Deadline);
        CodePurger432HostAccess.Require(!applying.IsCompleted && !pending.IsCompleted);
        CodePurger432HostAccess.Require((await CodePurger432HostAccess.OnPurger(fixture, CodePurger432HostNames.Abort)).Equals(Term.A(CodePurger432HostNames.False)));
        CodePurger432HostAccess.Require((await applying.WaitAsync(CodePurger432HostNames.Deadline)).Equals(Term.I(1)) && pending.IsCompletedSuccessfully && fixture.Runtime.Modules.PendingPurge(old) is null);
        CodePurger432HostAccess.Require((await CodePurger432HostAccess.OnPurger(fixture, CodePurger432HostNames.Prepare)).Equals(Term.A(CodePurger432HostNames.True)));
        CodePurger432HostAccess.Require((await CodePurger432HostAccess.OnPurger(fixture, CodePurger432HostNames.Complete)).Equals(Term.A(CodePurger432HostNames.True)) && !fixture.Runtime.Modules.HasOldCode(CodePurger432HostNames.Subject));
    }

    internal static async Task OnLoadSavedOld()
    {
        await using var fixture = await CodePurger432Fixture.Create();
        PreparedBeamImage first = await CodePurger432HostAccess.Publish(fixture, CodePurgerOriginal432Names.FirstBeam);
        Term savedFun = await fixture.Actor.InvokeAsync(async context => await fixture.Runtime.Modules.Call(context, CodePurger432HostNames.Subject, CodePurger432HostNames.FunValue));
        await fixture.Actor.Invoke(context => fixture.Runtime.Modules.DeleteModule(CodePurger432HostNames.Subject));
        PreparedBeamImage rejected = fixture.Runtime.Modules.PrepareBeam(CodePurgerOriginal432Access.Resource(CodePurgerOriginal432Names.RejectedBeam), fixture.Dependencies);
        typeof(ModuleRegistry).GetMethod(CodePurger432HostNames.Stage, CodePurger432HostNames.Flags)!.Invoke(fixture.Runtime.Modules, [rejected]);
        Term result = await fixture.Actor.InvokeAsync(async context => await fixture.Runtime.Modules.InvokeLoaderOnLoad(context, Term.A(CodePurger432HostNames.Subject)));
        CodePurger432HostAccess.Require(result.Equals(Term.A(CodePurger432HostNames.Rejected)));
        typeof(ModuleRegistry).GetMethod(CodePurger432HostNames.Finish, CodePurger432HostNames.Flags)!.Invoke(fixture.Runtime.Modules, [CodePurger432HostNames.Subject, false]);
        CodePurger432HostAccess.Require((await CodePurger432HostAccess.OnPurger(fixture, CodePurger432HostNames.PrepareOnLoad)).Equals(Term.A(CodePurger432HostNames.True)));
        ModuleCodeImage candidate = CodePurger432HostAccess.Code(rejected), old = CodePurger432HostAccess.Code(first);
        CodePurger432HostAccess.Require(fixture.Runtime.Modules.PendingPurge(candidate) is not null && fixture.Runtime.Modules.PendingPurge(old) is null);
        await fixture.Actor.InvokeAsync(async context =>
        {
            Term wire = await fixture.Runtime.Modules.Call(
                context,
                CodePurger432HostNames.ErLang,
                CodePurger432HostNames.Encode,
                savedFun
            );
            Term restored = await fixture.Runtime.Modules.Call(
                context,
                CodePurger432HostNames.ErLang,
                CodePurger432HostNames.Decode,
                wire
            );
            Term value = await fixture.Runtime.Modules.Call(
                context,
                CodePurger432HostNames.ErLang,
                CodePurger432HostNames.Apply,
                restored,
                Nil.Value
            );
            CodePurger432HostAccess.Require(restored.Equals(savedFun) && value.Equals(Term.I(1)));

            return true;
        });
        CodePurger432HostAccess.Require((await CodePurger432HostAccess.OnPurger(fixture, CodePurger432HostNames.Complete)).Equals(Term.A(CodePurger432HostNames.True)));
        CodePurger432HostAccess.Require(fixture.Runtime.Modules.HasOldCode(CodePurger432HostNames.Subject) && fixture.Runtime.Modules.PendingPurge(candidate) is null);
        await CodePurger432HostAccess.OnPurger(fixture, CodePurger432HostNames.Prepare);
        CodePurger432HostAccess.Require(fixture.Runtime.Modules.PendingPurge(old) is not null);
        await CodePurger432HostAccess.OnPurger(fixture, CodePurger432HostNames.Complete);
        CodePurger432HostAccess.Require(!fixture.Runtime.Modules.HasOldCode(CodePurger432HostNames.Subject));
    }

    internal static async Task OwnerDeath()
    {
        await using var fixture = await CodePurger432Fixture.Create();
        PreparedBeamImage first = await CodePurger432HostAccess.Publish(fixture, CodePurgerOriginal432Names.FirstBeam);
        await CodePurger432HostAccess.Publish(fixture, CodePurgerOriginal432Names.SecondBeam);
        ModuleCodeImage old = CodePurger432HostAccess.Code(first);
        ReferenceTerm reference = await CodePurger432HostAccess.Monitor(fixture);
        await CodePurger432HostAccess.OnPurger(fixture, CodePurger432HostNames.Prepare);
        CodePurger432HostAccess.Require(fixture.Runtime.Modules.PendingPurge(old) is not null);
        await fixture.Actor.Invoke(context =>
{
    fixture.Runtime.Exit(context, fixture.Purger.Pid, Term.A(CodePurger432HostNames.Kill));

    return true;
});
        await CodePurger432HostAccess.Down(fixture, reference);
        CodePurger432HostAccess.Empty(fixture.Runtime, old);
        CodePurger432HostAccess.Require(fixture.Runtime.Modules.HasOldCode(CodePurger432HostNames.Subject));
    }

    internal static async Task Shutdown()
    {
        for (int mode = 0; mode < 3; mode++)
        {
            await using var fixture = await CodePurger432Fixture.Create();
            PreparedBeamImage first = await CodePurger432HostAccess.Publish(fixture, CodePurgerOriginal432Names.FirstBeam);
            await CodePurger432HostAccess.Publish(fixture, CodePurgerOriginal432Names.SecondBeam);
            ModuleCodeImage old = CodePurger432HostAccess.Code(first);
            await CodePurger432HostAccess.OnPurger(fixture, CodePurger432HostNames.Prepare);
            Task pending = fixture.Runtime.Modules.PendingPurge(old)!;
            if (mode == 0) await fixture.Runtime.DisposeAsync();
            else if (mode == 1)
            {
                await fixture.Runtime.RequestHalt(0, new(false));
                await fixture.Runtime.HaltQuiescence.WaitAsync(CodePurger432HostNames.Deadline);
            }
            else
            {
                await fixture.Actor.Invoke(context =>
{
    fixture.Runtime.Exit(context, fixture.Runtime.InitializationPid, Term.A(CodePurger432HostNames.Kill));

    return true;
});
                await fixture.Runtime.DisposeAsync();
            }
            CodePurger432HostAccess.Require(pending.IsCompletedSuccessfully);
            CodePurger432HostAccess.Empty(fixture.Runtime, old);
        }
    }
}
