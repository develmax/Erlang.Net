using System.Reflection;

namespace Erlang.Tests;

public static class RemoteSpawnMetadataHostChecks
{
    private static readonly List<Term> observations = new();
    public static IReadOnlyList<Term> Observations => observations;

    public static Task ParserMetadata()
    {
        observations.Clear();
        Type type = typeof(ProcessRuntime).Assembly.GetType(RemoteSpawnMetadataHostNames.TypeName, throwOnError: true)!;
        MethodInfo parse = type.GetMethod(RemoteSpawnMetadataHostNames.ParseMethod, BindingFlags.Static | BindingFlags.NonPublic)!;
        PropertyInfo property = type.GetProperty(RemoteSpawnMetadataHostNames.AsyncProperty)!;
        bool all = true;
        foreach (var (options, expected) in AsyncCases())
        {
            object parsed = parse.Invoke(null, [options])!;
            bool actual = (bool)property.GetValue(parsed)!;
            observations.Add(Term.Tuple(
                Term.A(RemoteSpawnMetadataHostNames.Parsed),
                options,
                CoreModules.Boolean(expected),
                CoreModules.Boolean(actual)
            ));
            all &= actual == expected;
        }
        Require(all);

        return Task.CompletedTask;
    }

    public static async Task ConnectedAsync()
    {
        observations.Clear();
        foreach (bool request in new[] { false, true })
            foreach (var (options, expected) in AsyncCases())
                await Connected(
                    request,
                    options,
                    expected,
                    null
                );
    }

    public static async Task PriorityDuplicates()
    {
        observations.Clear();
        Term priority = Term.Tuple(Term.A(ConnectedSpawnHostNames.Link), Term.List(Term.A(ConnectedSpawnOptionHostNames.Priority)));
        Term plain = Term.A(ConnectedSpawnHostNames.Link);
        Term empty = Term.Tuple(Term.A(ConnectedSpawnHostNames.Link), Nil.Value);
        foreach (bool request in new[] { false, true })
        {
            await Connected(
                request,
                Term.List(empty),
                false,
                false
            );
            await Connected(
                request,
                Term.List(empty, plain),
                false,
                false
            );
            await Connected(
                request,
                Term.List(priority, empty),
                false,
                true
            );
            await Connected(
                request,
                Term.List(priority, plain),
                false,
                true
            );
            await Connected(
                request,
                Term.List(empty, priority),
                false,
                true
            );
            await Connected(
                request,
                Term.List(priority, empty, plain),
                false,
                true
            );
        }
    }

    private static IEnumerable<(Term Options, bool Expected)> AsyncCases()
    {
        Term yes = Term.Tuple(Term.A(RemoteSpawnMetadataHostNames.Async), CoreModules.Boolean(true));
        Term no = Term.Tuple(Term.A(RemoteSpawnMetadataHostNames.Async), CoreModules.Boolean(false));
        yield return (Term.List(yes), true);
        yield return (Term.List(no), false);
        yield return (Term.List(yes, no), false);
        yield return (Term.List(no, yes), true);
        yield return (Term.List(yes, Term.Tuple(Term.A(RemoteSpawnMetadataHostNames.SpawnPriority), Term.A(RemoteSpawnMetadataHostNames.NormalPriority))), true);
    }

    private static async Task Connected(
        bool request,
        Term options,
        bool asyncExpected,
        bool? priorityExpected
    )
    {
        await using var a = new ConnectedSpawnHostEndpoint();
        await using var b = new ConnectedSpawnHostEndpoint();
        await a.Initialize(ConnectedSpawnHostNames.NodeA);
        await b.Initialize(ConnectedSpawnHostNames.NodeB);
        await a.Connect(b);
        await b.Connect(a);
        await using var caller = new RuntimeNodeFoundationHostActor(a.Runtime);
        var entered = new TaskCompletionSource<(Pid Pid, Term Info)>(TaskCreationOptions.RunContinuationsAsynchronously);
        b.Runtime.Modules.Register(
            ConnectedSpawnHostNames.TargetModule,
            ConnectedSpawnHostNames.TargetFunction,
            0,
            async (context, _) =>
            {
                Term info = await b.Runtime.Modules.Call(
                    context,
                    ConnectedSpawnHostNames.Erlang,
                    ConnectedSpawnHostNames.ProcessInfo,
                    context.Self,
                    Term.A(RemoteSpawnMetadataHostNames.Async)
                );
                entered.TrySetResult((context.Self, info));
                await context.ReceiveAsync(value => value.Equals(Term.A(ConnectedSpawnHostNames.Release)) ? value : null);
                context.Exit(Term.A(ConnectedSpawnHostNames.ChildReason));

                return Nil.Value;
            }
        );
        var spawning = caller.InvokeAsync(async context =>
        {
            context.TrapExits = true;

            return await a.Runtime.Modules.Call(
                context,
                ConnectedSpawnHostNames.Erlang,
                request ? ConnectedSpawnHostNames.SpawnRequest : RemoteSpawnMetadataHostNames.SpawnOpt,
                Term.A(b.Runtime.Node),
                Term.A(ConnectedSpawnHostNames.TargetModule),
                Term.A(ConnectedSpawnHostNames.TargetFunction),
                Nil.Value,
                options
            );
        });
        await b.PutPacket(await a.TakePacket());
        var child = await entered.Task.WaitAsync(ConnectedSpawnHostNames.Deadline);
        try
        {
            await a.PutPacket(await b.TakePacket());
            Term result = await spawning;
            if (request)
            {
                Require(result is ReferenceTerm);
                await caller.InvokeAsync(async context =>
                {
                    Term? reply = await context.ReceiveAsync(
                        value => value is TupleTerm
                        {
                            Items: [Atom { Name: ConnectedSpawnHostNames.SpawnReply }, var reference, _, _]
                        } && reference.Equals(result) ? value : null,
                        ConnectedSpawnHostNames.Deadline
                    );
                    Require(reply is TupleTerm { Items: [_, _, Atom { Name: ConnectedSpawnHostNames.Success }, var actual] } && actual.Equals(child.Pid));

                    return true;
                });
            }
            else
                Require(result.Equals(child.Pid));
            observations.Add(Term.Tuple(
                Term.A(RemoteSpawnMetadataHostNames.Child),
                CoreModules.Boolean(request),
                options,
                child.Info
            ));
            Require(child.Info.Equals(Term.Tuple(Term.A(RemoteSpawnMetadataHostNames.Async), CoreModules.Boolean(asyncExpected))));
            if (priorityExpected is null)
                return;
            Term priority = await caller.InvokeAsync(async context =>
            {
                Term info = await a.Runtime.Modules.Call(
                    context,
                    ConnectedSpawnHostNames.Erlang,
                    ConnectedSpawnHostNames.ProcessInfo,
                    context.Self,
                    Term.A(RemoteSpawnMetadataHostNames.PriorityMessages)
                );
                a.Runtime.Send(context.Self, Term.A(ConnectedSpawnHostNames.Unrelated));

                return info;
            });
            observations.Add(Term.Tuple(
                Term.A(RemoteSpawnMetadataHostNames.Parent),
                CoreModules.Boolean(request),
                options,
                priority
            ));
            b.Runtime.Send(child.Pid, Term.A(ConnectedSpawnHostNames.Release));
            await a.PutPacket(await b.TakePacket());
            var mail = await caller.InvokeAsync(async context =>
            {
                Term? first = await context.ReceiveAsync(value => value, ConnectedSpawnHostNames.Deadline);
                Term? second = await context.ReceiveAsync(value => value, ConnectedSpawnHostNames.Deadline);
                Require(first is not null && second is not null);

                return Term.Tuple(first!, second!);
            });
            observations.Add(Term.Tuple(
                Term.A(RemoteSpawnMetadataHostNames.Mailbox),
                CoreModules.Boolean(request),
                options,
                mail
            ));
            Term exit = Term.Tuple(Term.A(ConnectedSpawnHostNames.Exit), child.Pid, Term.A(ConnectedSpawnHostNames.ChildReason));
            Term unrelated = Term.A(ConnectedSpawnHostNames.Unrelated);
            Require(priority.Equals(Term.Tuple(Term.A(RemoteSpawnMetadataHostNames.PriorityMessages), CoreModules.Boolean(priorityExpected.Value))));
            Require(mail.Equals(priorityExpected.Value ? Term.Tuple(exit, unrelated) : Term.Tuple(unrelated, exit)));
        }
        finally
        {
            b.Runtime.Send(child.Pid, Term.A(ConnectedSpawnHostNames.Release));
        }
    }

    private static void Require(bool passed)
    {
        if (!passed)
            throw new InvalidOperationException(RemoteSpawnMetadataHostNames.Failure);
    }
}
