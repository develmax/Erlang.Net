using Erlang;
namespace Erlang.Tests;

internal static class CodePurger432HostAccess
{
    internal static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException(CodePurger432HostNames.Failure);
    }

    internal static object? Field(ProcessRuntime runtime, string name) => typeof(ProcessRuntime).GetField(name, CodePurger432HostNames.Flags)!.GetValue(runtime);

    internal static ErlangFunction Body(ProcessRuntime runtime)
    {
        object?[] args = [runtime, CodePurger432HostNames.Internal, CodePurger432HostNames.Native, 2, null];
        var type = typeof(ProcessRuntime).Assembly.GetType(CodePurger432HostNames.NativeType, true)!;
        Require((bool)type.GetMethod(CodePurger432HostNames.Capture, CodePurger432HostNames.Flags)!.Invoke(null, args)!);

        return (ErlangFunction)args[4]!;
    }

    internal static Task<Term> OnPurger(CodePurger432Fixture fixture, string operation, string? module = null)
    {
        var context = (ProcessContext)Field(fixture.Runtime, CodePurger432HostNames.Actor)!;
        var scheduler = (TaskScheduler)typeof(ProcessContext).GetProperty(CodePurger432HostNames.Scheduler, CodePurger432HostNames.Flags)!.GetValue(context)!;
        var body = Body(fixture.Runtime);

        return Task.Factory.StartNew(
            async () => await body(context, [Term.A(module ?? CodePurger432HostNames.Subject), Term.A(operation)]),
            CancellationToken.None,
            TaskCreationOptions.DenyChildAttach,
            scheduler
        ).Unwrap();
    }

    internal static Task<Term> ActorTurn(ProcessContext context)
    {
        var scheduler = (TaskScheduler)typeof(ProcessContext).GetProperty(CodePurger432HostNames.Scheduler, CodePurger432HostNames.Flags)!.GetValue(context)!;

        return Task.Factory.StartNew<Term>(
            () => Term.A(CodePurger432HostNames.Ok),
            CancellationToken.None,
            TaskCreationOptions.DenyChildAttach,
            scheduler
        );
    }

    internal static ModuleCodeImage Code(PreparedBeamImage prepared)
    {
        object execution = typeof(PreparedBeamImage).GetProperty(CodePurger432HostNames.Execution, CodePurger432HostNames.Flags)!.GetValue(prepared)!;

        return (ModuleCodeImage)execution.GetType().GetProperty(CodePurger432HostNames.Image, CodePurger432HostNames.Flags)!.GetValue(execution)!;
    }

    internal static async Task<PreparedBeamImage> Publish(CodePurger432Fixture fixture, string resource)
    {
        return await fixture.Actor.Invoke(context =>
        {
            var image = fixture.Runtime.Modules.PrepareBeam(CodePurgerOriginal432Access.Resource(resource), fixture.Dependencies);
            Require(fixture.Runtime.Modules.PublishPreparedBeam([image]).Status == BeamPublicationStatus.Published);

            return image;
        });
    }

    internal static void Empty(ProcessRuntime runtime, ModuleCodeImage old)
    {
        Require(Field(runtime, CodePurger432HostNames.Actor) is null && Field(runtime, CodePurger432HostNames.Source) is null
            && Field(runtime, CodePurger432HostNames.Session) is null && Field(runtime, CodePurger432HostNames.Operation) is null
            && ((Task)Field(runtime, CodePurger432HostNames.Retirement)!).IsCompletedSuccessfully
            && runtime.Modules.PendingPurge(old) is null);
    }

    internal static async Task<ReferenceTerm> Monitor(CodePurger432Fixture fixture)
        => await fixture.Actor.Invoke(context => fixture.Runtime.Monitor(context, fixture.Purger.Pid));

    internal static async Task Down(CodePurger432Fixture fixture, ReferenceTerm reference)
    {
        await fixture.Actor.InvokeAsync(async context =>
        {
            Term? value = await context.Mailbox.ReceiveAsync<Term>(
                message => message is TupleTerm { Items.Count: 5 } tuple
                && tuple.Items[0].Equals(Term.A(CodePurger432HostNames.Down)) && tuple.Items[1].Equals(reference) ? message : null,
                CodePurger432HostNames.Deadline,
                context.Cancellation
            );
            Require(value is not null);

            return true;
        });
    }
}
