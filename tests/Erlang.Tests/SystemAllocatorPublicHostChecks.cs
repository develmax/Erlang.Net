using Erlang;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Erlang.Tests;

internal static class SystemAllocatorPublicHostChecks
{
    internal static bool Owns(string module, string function, int arity)
        => module == ErlangModuleNames.Module && arity == SystemAllocatorPublicHostNames.QueryArity &&
            function is SystemAllocatorPublicHostNames.Info or SystemAllocatorPublicHostNames.Sizes;

    internal static async Task RunMfa(string function)
    {
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        Require((await Call(actor, function, Term.A(SystemAllocatorPublicHostNames.System))).Equals(Expected(function)));
    }

    internal static async Task Run()
    {
        var registry = new ModuleRegistry();
        Require(!registry.Exports.Any(export => Owns(export.Module, export.Function, export.Arity)));
        CoreModules.Register(registry);
        Require(registry.Exports.Count(export => Owns(export.Module, export.Function, export.Arity)) == SystemAllocatorPublicHostNames.PublicFunctionCount);
        Require(!registry.Exports.Any(export => export.Module == ErlangModuleNames.Module && export.Function == SystemAllocatorPublicHostNames.Memory));
        await ActualPublicResults();
        await ValidationBeforeCapability();
        await MailboxAndRuntimeIsolation();
    }

    private static async Task ActualPublicResults()
    {
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);

        async Task Both()
        {
            foreach (string function in new[] { SystemAllocatorPublicHostNames.Info, SystemAllocatorPublicHostNames.Sizes })
            {
                Term expected = Expected(function);
                Require((await Call(actor, function, Term.A(SystemAllocatorPublicHostNames.System))).Equals(expected));
                Require((await Call(actor, function, Term.List(Term.A(SystemAllocatorPublicHostNames.System)))).Equals(
                    Term.List(Term.Tuple(Term.A(SystemAllocatorPublicHostNames.System), expected))));
            }
        }
        await Both();
        using var owner = new NativePayloadProviderHostOwner(runtime);
        nint positive = owner.Payload.Allocate(SystemAllocatorPublicHostNames.Initial);
        Require(positive != 0);
        for (int index = 0; index < (int)SystemAllocatorPublicHostNames.Initial; index++)
            Marshal.WriteByte(positive, index, SystemAllocatorPublicHostNames.PayloadByte);
        nint zero = owner.Payload.Allocate(0);
        Require(zero != 0);
        await Both();
        positive = owner.Payload.Reallocate(positive, SystemAllocatorPublicHostNames.Grown);
        Require(positive != 0);
        for (int index = 0; index < (int)SystemAllocatorPublicHostNames.Initial; index++)
            Require(Marshal.ReadByte(positive, index) == SystemAllocatorPublicHostNames.PayloadByte);
        await Both();
        positive = owner.Payload.Reallocate(positive, SystemAllocatorPublicHostNames.Shrunk);
        Require(positive != 0);
        for (int index = 0; index < (int)SystemAllocatorPublicHostNames.Shrunk; index++)
            Require(Marshal.ReadByte(positive, index) == SystemAllocatorPublicHostNames.PayloadByte);
        await Both();
        Require(owner.Payload.Snapshot.RequestedBytes == (ulong)SystemAllocatorPublicHostNames.Shrunk &&
            owner.Payload.Snapshot.BackingRequestBytes == (ulong)SystemAllocatorPublicHostNames.Shrunk + 1);
        owner.Payload.Free(positive);
        owner.Payload.Free(zero);
        Require(owner.Payload.Snapshot.RequestedBytes == 0 && owner.Payload.Snapshot.BackingRequestBytes == 0);
        await Both();
    }

    private static async Task ValidationBeforeCapability()
    {
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        FieldInfo counter = typeof(ProcessRuntime).GetField(SystemAllocatorPublicHostNames.ReferenceCounter, SystemAllocatorPublicHostNames.Members)!;
        foreach (string function in new[] { SystemAllocatorPublicHostNames.Info, SystemAllocatorPublicHostNames.Sizes })
        {
            long before = (long)counter.GetValue(runtime)!;
            await Error(
                actor,
                function,
                Term.I(0),
                ErlangErrorReasons.FunctionClause
            );
            Require((long)counter.GetValue(runtime)! == before);
            await Error(
                actor,
                function,
                Nil.Value,
                ErlangErrorReasons.BadArgument
            );
            Require((long)counter.GetValue(runtime)! == before + 1);
            await Error(
                actor,
                function,
                Term.A(SystemAllocatorPublicHostNames.Unknown),
                ErlangErrorReasons.BadArgument
            );
            await Error(
                actor,
                function,
                Term.List(Term.A(SystemAllocatorPublicHostNames.System), Term.A(SystemAllocatorPublicHostNames.System)),
                ErlangErrorReasons.BadArgument
            );
            await Error(
                actor,
                function,
                Term.List(Term.I(0)),
                ErlangErrorReasons.BadArgument
            );
            await Error(
                actor,
                function,
                new Cons(Term.A(SystemAllocatorPublicHostNames.System), Term.A(SystemAllocatorPublicHostNames.Tail)),
                ErlangErrorReasons.BadArgument
            );
            await Error(
                actor,
                function,
                Term.List(Term.A(SystemAllocatorPublicHostNames.Unsupported), Term.A(SystemAllocatorPublicHostNames.Unknown)),
                ErlangErrorReasons.BadArgument
            );
            await Error(
                actor,
                function,
                Term.List(Term.A(SystemAllocatorPublicHostNames.Unsupported), Term.A(SystemAllocatorPublicHostNames.Unsupported)),
                ErlangErrorReasons.BadArgument
            );
            foreach (string unsupported in SystemAllocatorPublicHostNames.RecognizedUnsupported)
                await Error(
                    actor,
                    function,
                    Term.A(unsupported),
                    SystemAllocatorPublicHostNames.NotSupported
                );
            await Error(
                actor,
                function,
                Term.List(Term.A(SystemAllocatorPublicHostNames.System), Term.A(SystemAllocatorPublicHostNames.Unsupported)),
                SystemAllocatorPublicHostNames.NotSupported
            );
            Require((await actor.Ready).Mailbox.Count == 0);
        }
    }

    private static async Task MailboxAndRuntimeIsolation()
    {
        await using var first = new ProcessRuntime();
        await using var second = new ProcessRuntime();
        await using var firstActor = new RuntimeNodeFoundationHostActor(first);
        await using var secondActor = new RuntimeNodeFoundationHostActor(second);
        ProcessContext context = await firstActor.Ready;
        Term marker = Term.A(SystemAllocatorPublicHostNames.Marker);
        Term unrelated = Term.Tuple(first.MakeReference(), Nil.Value, Nil.Value);
        context.Mailbox.Send(marker);
        context.Mailbox.Send(unrelated);
        Task<Term> info = Call(firstActor, SystemAllocatorPublicHostNames.Info, Term.A(SystemAllocatorPublicHostNames.System));
        Task<Term> sizes = Call(secondActor, SystemAllocatorPublicHostNames.Sizes, Term.A(SystemAllocatorPublicHostNames.System));
        await Task.WhenAll(info, sizes).WaitAsync(SystemAllocatorPublicHostNames.Deadline);
        Require(info.Result.Equals(Expected(SystemAllocatorPublicHostNames.Info)) && sizes.Result.Equals(Expected(SystemAllocatorPublicHostNames.Sizes)));
        foreach (Term pending in new[] { marker, unrelated })
            Require((await firstActor.InvokeAsync(async caller => await caller.ReceiveAsync(value => value, TimeSpan.Zero)))!.Equals(pending));
        Require(context.Mailbox.Count == 0 && (await secondActor.Ready).Mailbox.Count == 0);
    }

    private static Task<Term> Call(RuntimeNodeFoundationHostActor actor, string function, Term input)
        => actor.InvokeAsync(context => context.Runtime.Modules.Call(
            context,
            ErlangModuleNames.Module,
            function,
            input
        ).AsTask());

    private static async Task Error(
        RuntimeNodeFoundationHostActor actor,
        string function,
        Term input,
        string reason
    )
    {
        try
        {
            _ = await Call(actor, function, input);
            throw new InvalidOperationException(SystemAllocatorPublicHostNames.Failure);
        }
        catch (ErlangException failure) when (failure.Reason.Equals(Term.A(reason)))
        {
        }
    }

    private static Term Expected(string function) => Term.List(Term.Tuple(
        Term.A(SystemAllocatorPublicHostNames.Instance),
        Term.I(SystemAllocatorPublicHostNames.SystemInstance),
        function == SystemAllocatorPublicHostNames.Sizes ? Nil.Value : Term.List(Term.Tuple(
            Term.A(SystemAllocatorPublicHostNames.Options),
            Term.List(
                Term.Tuple(Term.A(SystemAllocatorPublicHostNames.Enabled), Term.A(ErlangBooleanAtoms.True)),
                Term.Tuple(Term.A(SystemAllocatorPublicHostNames.Mechanism), Term.A(SystemAllocatorPublicHostNames.NativeMemory))
            )
        ))
    ));

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(SystemAllocatorPublicHostNames.Failure);
    }
}
