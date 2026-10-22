namespace Erlang.Tests;

internal static class ManagedCollectors413Checks
{
    internal static async Task Authority()
    {
        await using var runtime = new ProcessRuntime();
        await using var foreign = new ProcessRuntime();
        await ManagedCollectors413Access.Initialize(runtime);
        await foreign.InitializeModulesAsync(_ => { });
        ErlangFunction check = ManagedCollectors413Access.Capture(
            runtime,
            ManagedCollectors413Names.Trace,
            ManagedCollectors413Names.Check,
            0
        );
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            ManagedCollectors413Access.Require((await check(context, [])).Equals(Term.A(ErlangBooleanAtoms.False)));
            foreach (var item in new[]
            {
                (ManagedCollectors413Names.Literal, ManagedCollectors413Names.Release, 0),
                (ManagedCollectors413Names.Literal, ManagedCollectors413Names.Copy, 3),
                (ManagedCollectors413Names.Trace, ManagedCollectors413Names.Clean, 1)
            })
            {
                ErlangFunction body = ManagedCollectors413Access.Capture(
                    runtime,
                    item.Item1,
                    item.Item2,
                    item.Item3
                );
                bool refused = false;
                try
                {
                    _ = await body(context, Enumerable.Repeat<Term>(Nil.Value, item.Item3).ToArray());
                }
                catch (ErlangException error)
                {
                    refused = error.Reason.Equals(Term.A(ManagedCollectors413Names.NotSupported));
                }
                ManagedCollectors413Access.Require(refused);
                ManagedCollectors413Access.Require(runtime.Modules.SourceOwner(item.Item1, item.Item2, item.Item3) is PreparedBeamImage);
            }

            return true;
        });
        await using var other = new RuntimeNodeFoundationHostActor(foreign);
        await other.InvokeAsync(async context =>
        {
            bool refused = false;
            try
            {
                _ = await check(context, []);
            }
            catch (ArgumentException)
            {
                refused = true;
            }
            ManagedCollectors413Access.Require(refused);

            return true;
        });
        await actor.InvokeAsync(async context =>
        {
            ManagedCollectors413Access.Cancel(context);
            bool refused = false;
            try
            {
                _ = await check(context, []);
            }
            catch (OperationCanceledException)
            {
                refused = true;
            }
            ManagedCollectors413Access.Require(refused);

            return true;
        });
    }

    internal static async Task Protocol()
    {
        await using var runtime = new ProcessRuntime();
        await ManagedCollectors413Access.Initialize(runtime);
        await using var owner = new RuntimeNodeFoundationHostActor(runtime);
        ProcessContext parent = await owner.Ready;
        ProcessHandle literalHandle = ManagedCollectors413Access.Spawn(runtime, parent, ManagedCollectors413Names.Literal);
        ProcessHandle traceHandle = ManagedCollectors413Access.Spawn(runtime, parent, ManagedCollectors413Names.Trace);
        ProcessContext literal = await ManagedCollectors413Access.Role(runtime, ManagedCollectors413Names.LiteralSlot);
        ProcessContext trace = await ManagedCollectors413Access.Role(runtime, ManagedCollectors413Names.TraceSlot);
        await owner.InvokeAsync(async context =>
        {
            _ = await runtime.Modules.Call(
                context,
                ManagedCollectors413Names.Erlang,
                ManagedCollectors413Names.Suspend,
                literalHandle.Pid
            );
            _ = await runtime.Modules.Call(
                context,
                ManagedCollectors413Names.Erlang,
                ManagedCollectors413Names.Suspend,
                traceHandle.Pid
            );

            return true;
        });
        ManagedCollectors413Access.Require(!literalHandle.Completion.IsCompleted && !traceHandle.Completion.IsCompleted);
        await using var target = new RuntimeNodeFoundationHostActor(runtime);
        ProcessContext recipient = await target.Ready;
        Term area = Term.Tuple(Term.A(ManagedCollectors413Names.Area), Term.List(Term.I(31), Term.Tuple(Term.I(7))));
        ErlangFunction copy = ManagedCollectors413Access.Capture(
            runtime,
            ManagedCollectors413Names.Literal,
            ManagedCollectors413Names.Copy,
            3
        );
        ErlangFunction clean = ManagedCollectors413Access.Capture(
            runtime,
            ManagedCollectors413Names.Trace,
            ManagedCollectors413Names.Clean,
            1
        );
        runtime.Send(recipient.Self, Term.A(ManagedCollectors413Names.Before));
        ManagedCollectors413Access.Require((await copy(literal, [recipient.Self, area, Term.A(ManagedCollectors413Names.Init)])).Equals(Term.A(ManagedCollectors413Names.Ok)));
        ManagedCollectors413Access.Require((await clean(trace, [recipient.Self])).Equals(Term.A(ErlangBooleanAtoms.True)));
        runtime.Send(recipient.Self, Term.A(ManagedCollectors413Names.After));
        ManagedCollectors413Access.Require(ManagedCollectors413Access.Requests(runtime).Length == 2);
        ManagedCollectors413Access.Require(ManagedCollectors413Access.Snapshot(literal).Count == 0
            && ManagedCollectors413Access.Snapshot(trace).Count == 0);
        await target.InvokeAsync(async context =>
        {
            Term? first = await context.ReceiveAsync(message => message);
            Term? second = await context.ReceiveAsync(message => message);
            ManagedCollectors413Access.Require(Term.A(ManagedCollectors413Names.Before).Equals(first)
                && Term.A(ManagedCollectors413Names.After).Equals(second));

            return true;
        });
        ManagedCollectors413Access.Require(ManagedCollectors413Access.Requests(runtime).Length == 0);
        Term expected = Term.Tuple(
            Term.A(ManagedCollectors413Names.CopyLiterals),
            Term.Tuple(area, Term.A(ManagedCollectors413Names.Init), recipient.Self),
            Term.A(ManagedCollectors413Names.Ok)
        );
        ManagedCollectors413Access.Require(ManagedCollectors413Access.Snapshot(literal).SequenceEqual([expected]));
        ManagedCollectors413Access.Require(ManagedCollectors413Access.Snapshot(trace).SequenceEqual<Term>([recipient.Self]));
    }

    internal static async Task Shutdown()
    {
        var runtime = new ProcessRuntime();
        try
        {
            await ManagedCollectors413Access.Initialize(runtime);
            var owner = new RuntimeNodeFoundationHostActor(runtime);
            ProcessContext parent = await owner.Ready;
            ProcessHandle literalHandle = ManagedCollectors413Access.Spawn(runtime, parent, ManagedCollectors413Names.Literal);
            ProcessContext literal = await ManagedCollectors413Access.Role(runtime, ManagedCollectors413Names.LiteralSlot);
            var target = new RuntimeNodeFoundationHostActor(runtime);
            ProcessContext recipient = await target.Ready;
            await owner.InvokeAsync(async context =>
            {
                _ = await runtime.Modules.Call(
                    context,
                    ManagedCollectors413Names.Erlang,
                    ManagedCollectors413Names.Suspend,
                    literalHandle.Pid
                );

                return true;
            });
            ErlangFunction copy = ManagedCollectors413Access.Capture(
                runtime,
                ManagedCollectors413Names.Literal,
                ManagedCollectors413Names.Copy,
                3
            );
            _ = await copy(literal, [recipient.Self, Term.Tuple(Term.I(43)), Term.A(ManagedCollectors413Names.Init)]);
            object request = ManagedCollectors413Access.Requests(runtime).Single();
            object slot = ManagedCollectors413Access.Field(runtime, ManagedCollectors413Names.LiteralSlot)!;
            await runtime.DisposeAsync();
            ManagedCollectors413Access.Require(literalHandle.Completion.IsCompleted && target.Process.Completion.IsCompleted
                && owner.Process.Completion.IsCompleted && ManagedCollectors413Access.Requests(runtime).Length == 0);
            foreach (string field in new[] { ManagedCollectors413Names.Requester, ManagedCollectors413Names.Target, ManagedCollectors413Names.Reply })
                ManagedCollectors413Access.Require(ManagedCollectors413Access.Property(request, field) is null);
            ManagedCollectors413Access.Require(ManagedCollectors413Access.Property(slot, ManagedCollectors413Names.Actor) is null
                && ManagedCollectors413Access.Property(slot, ManagedCollectors413Names.Source) is null);
            await target.DisposeAsync();
            await owner.DisposeAsync();
        }
        finally
        {
            await runtime.DisposeAsync();
        }
    }
}
