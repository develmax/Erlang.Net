using System.Reflection;
using System.Numerics;
using Erlang;
using Erlang.Tests;

namespace Erlang.Tests;

internal static class ConnectedDistributionEventsHostChecks
{
    internal static async Task<int> Run()
    {
        int checks = 0;
        await using var runtime = new ProcessRuntime();
        await using var kernel = new RuntimeNodeFoundationHostActor(runtime);
        await using var controller = new RuntimeNodeFoundationHostActor(runtime);
        await using var observer = new RuntimeNodeFoundationHostActor(runtime);
        await kernel.Invoke(context =>
        {
            runtime.Register(ConnectedDistributionHostNames.Kernel, context.Self);
            var admission = typeof(ProcessRuntime).GetMethod(ConnectedDistributionHostNames.Admission, BindingFlags.Instance | BindingFlags.NonPublic)!;
            _ = admission.Invoke(runtime, [context, Term.A(ConnectedDistributionHostNames.Local), Term.I(0)]);

            return true;
        });
        var options = new MapTerm([
            new(Term.A(ConnectedDistributionEventsHostNames.ConnectionId), Term.A(ErlangBooleanAtoms.True)),
            new(Term.A(ConnectedDistributionEventsHostNames.Reason), Term.A(ErlangBooleanAtoms.True)),
            new(Term.A(ConnectedDistributionEventsHostNames.NodeType), Term.A(ConnectedDistributionEventsHostNames.All))
        ]);
        Type type = typeof(ProcessRuntime).Assembly.GetType(ConnectedDistributionEventsHostNames.OptionsType)!;
        object parsed = type.GetMethod(ConnectedDistributionEventsHostNames.Parse, BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, [options])!;
        MethodInfo monitor = typeof(RuntimeLocalNodeEvents).GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
            .Single(method => method.Name == ConnectedDistributionEventsHostNames.Monitor && method.GetParameters().Length == ConnectedDistributionEventsHostNames.MonitorArguments);
        await observer.Invoke(context =>
        {
            Check(runtime.LocalNodeEvents.Monitor(context, true) == 0);
            Check(runtime.LocalNodeEvents.Monitor(context, true) == 1);
            Check((BigInteger)monitor.Invoke(runtime.LocalNodeEvents, [context, true, parsed])! == BigInteger.Zero);
            Check((BigInteger)monitor.Invoke(runtime.LocalNodeEvents, [context, true, parsed])! == BigInteger.One);

            return true;
        });
        Atom remote = Term.A(ConnectedDistributionHostNames.Remote);
        var pending = runtime.DistributionPending;
        TupleTerm handle = pending.BeginPending(remote);
        await controller.Invoke(context => pending.AdoptConnected(
            handle,
            context,
            DistributionPassThroughFlags.Mandatory,
            1
        ));
        await ExpectInfo(
            ConnectedDistributionEventsHostNames.NodeUp,
            ConnectedDistributionEventsHostNames.Hidden,
            null,
            handle
        );
        await ExpectInfo(
            ConnectedDistributionEventsHostNames.NodeUp,
            ConnectedDistributionEventsHostNames.Hidden,
            null,
            handle
        );
        await ExpectEmpty();
        pending.CloseConnected(handle);
        await ExpectInfo(
            ConnectedDistributionEventsHostNames.NodeDown,
            ConnectedDistributionEventsHostNames.Hidden,
            Term.A(ConnectedDistributionEventsHostNames.Closed),
            handle
        );
        await ExpectInfo(
            ConnectedDistributionEventsHostNames.NodeDown,
            ConnectedDistributionEventsHostNames.Hidden,
            Term.A(ConnectedDistributionEventsHostNames.Closed),
            handle
        );
        await ExpectEmpty();
        handle = pending.BeginPending(remote);
        await controller.Invoke(context => pending.AdoptConnected(
            handle,
            context,
            DistributionPassThroughFlags.Mandatory | ConnectedDistributionEventsHostNames.Published,
            2
        ));
        await ExpectDefault(ConnectedDistributionEventsHostNames.NodeUp);
        await ExpectDefault(ConnectedDistributionEventsHostNames.NodeUp);
        await ExpectInfo(
            ConnectedDistributionEventsHostNames.NodeUp,
            ConnectedDistributionEventsHostNames.Visible,
            null,
            handle
        );
        await ExpectInfo(
            ConnectedDistributionEventsHostNames.NodeUp,
            ConnectedDistributionEventsHostNames.Visible,
            null,
            handle
        );
        await ExpectEmpty();
        await kernel.Invoke(context =>
        {
            runtime.Exit(
                context,
                controller.Process.Pid,
                Term.Tuple(Term.A(ProcessExitReasons.Shutdown), Term.A(ConnectedDistributionEventsHostNames.Reset))
            );

            return true;
        });
        _ = await controller.Process.Completion.WaitAsync(ConnectedDistributionHostNames.Deadline);
        await ExpectDefault(ConnectedDistributionEventsHostNames.NodeDown);
        await ExpectDefault(ConnectedDistributionEventsHostNames.NodeDown);
        await ExpectInfo(
            ConnectedDistributionEventsHostNames.NodeDown,
            ConnectedDistributionEventsHostNames.Visible,
            Term.A(ConnectedDistributionEventsHostNames.Reset),
            handle
        );
        await ExpectInfo(
            ConnectedDistributionEventsHostNames.NodeDown,
            ConnectedDistributionEventsHostNames.Visible,
            Term.A(ConnectedDistributionEventsHostNames.Reset),
            handle
        );
        await ExpectEmpty();
        await using var normalController = new RuntimeNodeFoundationHostActor(runtime);
        handle = pending.BeginPending(remote);
        await normalController.Invoke(context => pending.AdoptConnected(
            handle,
            context,
            DistributionPassThroughFlags.Mandatory,
            3
        ));
        await ExpectInfo(
            ConnectedDistributionEventsHostNames.NodeUp,
            ConnectedDistributionEventsHostNames.Hidden,
            null,
            handle
        );
        await ExpectInfo(
            ConnectedDistributionEventsHostNames.NodeUp,
            ConnectedDistributionEventsHostNames.Hidden,
            null,
            handle
        );
        await normalController.DisposeAsync();
        await ExpectInfo(
            ConnectedDistributionEventsHostNames.NodeDown,
            ConnectedDistributionEventsHostNames.Hidden,
            Term.A(ConnectedDistributionEventsHostNames.Closed),
            handle
        );
        await ExpectInfo(
            ConnectedDistributionEventsHostNames.NodeDown,
            ConnectedDistributionEventsHostNames.Hidden,
            Term.A(ConnectedDistributionEventsHostNames.Closed),
            handle
        );
        await ExpectEmpty();
        await using var secondController = new RuntimeNodeFoundationHostActor(runtime);
        handle = pending.BeginPending(remote);
        await secondController.Invoke(context => pending.AdoptConnected(
            handle,
            context,
            DistributionPassThroughFlags.Mandatory,
            3
        ));
        await ExpectInfo(
            ConnectedDistributionEventsHostNames.NodeUp,
            ConnectedDistributionEventsHostNames.Hidden,
            null,
            handle
        );
        await ExpectInfo(
            ConnectedDistributionEventsHostNames.NodeUp,
            ConnectedDistributionEventsHostNames.Hidden,
            null,
            handle
        );
        await observer.Invoke(context =>
        {
            runtime.Exit(context, kernel.Process.Pid, Term.A(ConnectedDistributionEventsHostNames.NoNetwork));

            return true;
        });
        _ = await kernel.Process.Completion.WaitAsync(ConnectedDistributionHostNames.Deadline);
        await ExpectInfo(
            ConnectedDistributionEventsHostNames.NodeDown,
            ConnectedDistributionEventsHostNames.Hidden,
            Term.A(ConnectedDistributionEventsHostNames.NoNetwork),
            handle
        );
        await ExpectInfo(
            ConnectedDistributionEventsHostNames.NodeDown,
            ConnectedDistributionEventsHostNames.Hidden,
            Term.A(ConnectedDistributionEventsHostNames.NoNetwork),
            handle
        );
        await ExpectEmpty();
        Atom local = Term.A(ConnectedDistributionHostNames.Local);
        await Expect(Term.Tuple(Term.A(ConnectedDistributionEventsHostNames.NodeDown), local));
        await Expect(Term.Tuple(Term.A(ConnectedDistributionEventsHostNames.NodeDown), local));
        var localInfo = new MapTerm([
            new(Term.A(ConnectedDistributionEventsHostNames.ConnectionId), Term.A(ConnectedDistributionEventsHostNames.Undefined)),
            new(Term.A(ConnectedDistributionEventsHostNames.NodeType), Term.A(ConnectedDistributionEventsHostNames.Visible)),
            new(Term.A(ConnectedDistributionEventsHostNames.Reason), Term.A(ConnectedDistributionEventsHostNames.NoNetwork))
        ]);
        await Expect(Term.Tuple(Term.A(ConnectedDistributionEventsHostNames.NodeDown), local, localInfo));
        await Expect(Term.Tuple(Term.A(ConnectedDistributionEventsHostNames.NodeDown), local, localInfo));
        await observer.Invoke(context =>
        {
            Check((BigInteger)monitor.Invoke(runtime.LocalNodeEvents, [context, false, parsed])! == new BigInteger(2));
            Check((BigInteger)monitor.Invoke(runtime.LocalNodeEvents, [context, false, parsed])! == BigInteger.Zero);

            return true;
        });

        return checks;

        void Check(bool value)
        {
            checks++;
            if (!value)
                throw new Exception(ConnectedDistributionHostNames.Failure);
        }

        async Task ExpectEmpty() => await observer.InvokeAsync(async context =>
        {
            Check(await context.ReceiveAsync<Term>(message => message is TupleTerm tuple && tuple.Items.Count >= 2 &&
                tuple.Items[1].Equals(remote) ? message : null, TimeSpan.Zero) is null);

            return true;
        });

        async Task ExpectDefault(string eventName) => await Expect(Term.Tuple(Term.A(eventName), remote));

        async Task ExpectInfo(
            string eventName,
            string visibility,
            Term? reason,
            TupleTerm captured
        )
        {
            var fields = new List<KeyValuePair<Term, Term>>
            {
                new(Term.A(ConnectedDistributionEventsHostNames.ConnectionId), captured.Items[0]),
                new(Term.A(ConnectedDistributionEventsHostNames.NodeType), Term.A(visibility))
            };
            if (reason is not null)
                fields.Add(new(Term.A(ConnectedDistributionEventsHostNames.Reason), reason));
            await Expect(Term.Tuple(Term.A(eventName), remote, new MapTerm(fields)));
        }

        async Task Expect(Term expected) => await observer.InvokeAsync(async context =>
        {
            Check(await context.ReceiveAsync<Term>(message => message.Equals(expected) ? message : null, ConnectedDistributionHostNames.Deadline) is not null);

            return true;
        });
    }
}
