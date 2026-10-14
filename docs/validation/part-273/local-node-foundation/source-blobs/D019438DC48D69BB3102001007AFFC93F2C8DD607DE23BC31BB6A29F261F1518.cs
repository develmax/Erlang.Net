using Erlang;
using System.Reflection;

namespace Erlang.Tests;

internal static class RuntimeNodeAdmissionFoundationHostChecks
{
    internal static async Task Run()
    {
        int checks = 0;
        await ValidationAndOwnedKernelDeath();
        await HaltAndDisposal();
        await Erlang.Tests.RuntimeNodeQueryHostChecks.Run();
        await Erlang.Tests.RuntimeNodeControlHostChecks.Run();
        Console.WriteLine("PASS two inherited named HOST bodies with unchanged assertions and actual kernel-role fixture setup");
        Console.WriteLine($"PASS {checks} owned kernel role/lifecycle controls; observed-epoch admission only, Core setnode unregistered");

        async Task ValidationAndOwnedKernelDeath()
        {
            await using var runtime = new ProcessRuntime();
            await using var foreign = new ProcessRuntime();
            await using var named = new ProcessRuntime(RuntimeNodeAdmissionFoundationHostNames.ConfiguredNode);
            Check(!runtime.IsDistributionAlive && !named.IsDistributionAlive);
            await using var caller = new RuntimeNodeFoundationHostActor(runtime);
            await using var kernel = new RuntimeNodeFoundationHostActor(runtime);
            await using var outsider = new RuntimeNodeFoundationHostActor(foreign);
            Check(await caller.InvokeAsync(async context => (await runtime.Modules.Call(context, ErlangModuleNames.Module, RuntimeNodeAdmissionFoundationHostNames.IsAlive)).Equals(Term.A(ErlangBooleanAtoms.False))));
            await caller.Invoke(context => Reject(() => Admit(runtime, context, Term.A(RuntimeNodeAdmissionFoundationHostNames.FirstNode), Term.I(0))));
            await kernel.Invoke(context =>
            {
                runtime.Register(RuntimeNodeControlNames.Kernel, context.Self);

                return true;
            });
            await caller.Invoke(context =>
            {
                Check(runtime.LocalNodeEvents.Monitor(context, true) == 0);
                Check(runtime.LocalNodeEvents.Monitor(context, true) == 1);
                Reject(() => Admit(runtime, context, Term.I(1), Term.I(0)));
                Reject(() => Admit(runtime, context, Term.A(RuntimeNodeAdmissionFoundationHostNames.InvalidNode), Term.I(0)));
                Reject(() => Admit(runtime, context, Term.A(ProcessNodeDefaults.Local), Term.I(0)));
                Reject(() => Admit(runtime, context, Term.A(RuntimeNodeAdmissionFoundationHostNames.FirstNode), Term.I(-1)));
                Reject(() => Admit(runtime, context, Term.A(RuntimeNodeAdmissionFoundationHostNames.FirstNode), new Integer((System.Numerics.BigInteger)uint.MaxValue + 1)));

                return true;
            });
            var external = new ReferenceTerm(RuntimeNodeAdmissionFoundationHostNames.FirstNode, new uint[] { 173, 29, 53 }, 4);
            Term retained = await caller.InvokeAsync(async context => await runtime.Modules.Call(context, ErlangModuleNames.Module, RuntimeNodeAdmissionFoundationHostNames.Decode, new BitString(ExternalTermFormat.Encode(external))));
            var pending = runtime.DistributionPending.BeginPending(Term.A(RuntimeNodeAdmissionFoundationHostNames.RemoteNode));
            Check(await caller.Invoke(context => Admit(runtime, context, Term.A(RuntimeNodeAdmissionFoundationHostNames.FirstNode), Term.I(0)).Equals(Term.A(ErlangBooleanAtoms.True))));
            Check(runtime.IsDistributionAlive && runtime.Node == RuntimeNodeAdmissionFoundationHostNames.FirstNode && caller.Process.Pid.Creation == 5);
            Check(await caller.InvokeAsync(async context => (await runtime.Modules.Call(context, ErlangModuleNames.Module, RuntimeNodeAdmissionFoundationHostNames.CreationQuery, Term.A(RuntimeNodeAdmissionFoundationHostNames.Creation))).Equals(Term.I(5))));
            await caller.InvokeAsync(async context =>
            {
                await Expect(context, RuntimeNodeAdmissionFoundationHostNames.NodeUp, RuntimeNodeAdmissionFoundationHostNames.FirstNode);
                await Expect(context, RuntimeNodeAdmissionFoundationHostNames.NodeUp, RuntimeNodeAdmissionFoundationHostNames.FirstNode);
                Reject(() => Admit(runtime, context, Term.A(RuntimeNodeAdmissionFoundationHostNames.SecondNode), Term.I(8)));
                await runtime.Modules.Call(context, ErlangModuleNames.Module, RuntimeNodeAdmissionFoundationHostNames.SendAfter, Term.I(0), context.Self, Term.A(RuntimeNodeAdmissionFoundationHostNames.TimerMessage));
                Check(await context.ReceiveAsync<Term>(message => message.Equals(Term.A(RuntimeNodeAdmissionFoundationHostNames.TimerMessage)) ? message : null, RuntimeNodeAdmissionFoundationHostNames.Deadline) is not null);

                return true;
            });
            await outsider.Invoke(context => Reject(() => Admit(runtime, context, Term.A(RuntimeNodeAdmissionFoundationHostNames.SecondNode), Term.I(8))));
            var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var held = kernel.InvokeAsync(async context =>
            {
                entered.SetResult();
                await release.Task;

                return Reject(() => Admit(runtime, context, Term.A(RuntimeNodeAdmissionFoundationHostNames.SecondNode), Term.I(8)));
            });
            try
            {
                await entered.Task.WaitAsync(RuntimeNodeAdmissionFoundationHostNames.Deadline);
                await caller.Invoke(context =>
                {
                    runtime.Exit(context, kernel.Process.Pid, Term.A(ProcessExitSignals.Kill));

                    return true;
                });
                await kernel.Process.Completion.WaitAsync(RuntimeNodeAdmissionFoundationHostNames.Deadline);
                Check(!runtime.IsDistributionAlive && runtime.Node == ProcessNodeDefaults.Local && caller.Process.Pid.Creation == 0);
                using var lease = runtime.DistributionPending.Acquire(pending);
                Check(lease.Snapshot().State == DistributionPendingState.Idle);
                await caller.InvokeAsync(async context =>
                {
                    await Expect(context, RuntimeNodeAdmissionFoundationHostNames.NodeDown, RuntimeNodeAdmissionFoundationHostNames.FirstNode);
                    await Expect(context, RuntimeNodeAdmissionFoundationHostNames.NodeDown, RuntimeNodeAdmissionFoundationHostNames.FirstNode);

                    return true;
                });
                await using var replacement = new RuntimeNodeFoundationHostActor(runtime);
                await replacement.Invoke(context =>
                {
                    runtime.Register(RuntimeNodeControlNames.Kernel, context.Self);

                    return true;
                });
                Check(await caller.Invoke(context => Admit(runtime, context, Term.A(RuntimeNodeAdmissionFoundationHostNames.SecondNode), Term.I(19)).Equals(Term.A(ErlangBooleanAtoms.True))));
                Check(runtime.IsDistributionAlive && caller.Process.Pid.Creation == 19);
                release.SetResult();
                Check(await held);
                Check(runtime.IsDistributionAlive && runtime.Node == RuntimeNodeAdmissionFoundationHostNames.SecondNode);
                await caller.Invoke(context =>
                {
                    Check(runtime.LocalNodeEvents.Monitor(context, false) == 2);
                    Check(runtime.LocalNodeEvents.Monitor(context, false) == 0);

                    return true;
                });
            }
            finally
            {
                release.TrySetResult();
            }
            GC.KeepAlive(retained);
        }

        async Task HaltAndDisposal()
        {
            await using var runtime = new ProcessRuntime();
            await using var caller = new RuntimeNodeFoundationHostActor(runtime);
            await using var kernel = new RuntimeNodeFoundationHostActor(runtime);
            await kernel.Invoke(context =>
            {
                runtime.Register(RuntimeNodeControlNames.Kernel, context.Self);

                return true;
            });
            await caller.Invoke(context => Admit(runtime, context, Term.A(RuntimeNodeAdmissionFoundationHostNames.FirstNode), Term.I(17)));
            await runtime.RequestImmediateHalt(0).WaitAsync(RuntimeNodeAdmissionFoundationHostNames.Deadline);
            await runtime.HaltQuiescence.WaitAsync(RuntimeNodeAdmissionFoundationHostNames.Deadline);
            Check(!runtime.IsDistributionAlive && runtime.Node == ProcessNodeDefaults.Local);
            await using var disposed = new ProcessRuntime();
            await using var disposeCaller = new RuntimeNodeFoundationHostActor(disposed);
            await disposeCaller.Invoke(context =>
            {
                disposed.Register(RuntimeNodeControlNames.Kernel, context.Self);

                return true;
            });
            await disposeCaller.Invoke(context => Admit(disposed, context, Term.A(RuntimeNodeAdmissionFoundationHostNames.FirstNode), Term.I(17)));
            await disposed.DisposeAsync();
            Check(!disposed.IsDistributionAlive && disposed.Node == ProcessNodeDefaults.Local);
        }

        Term Admit(ProcessRuntime runtime, ProcessContext caller, Term node, Term creation)
        {
            var method = typeof(ProcessRuntime).GetMethod(RuntimeNodeAdmissionFoundationHostNames.Admission, BindingFlags.Instance | BindingFlags.NonPublic)!;
            try
            {
                return (Term)method.Invoke(runtime, new object[] { caller, node, creation })!;
            }
            catch (TargetInvocationException exception) when (exception.InnerException is ErlangException error)
            {
                throw error;
            }
        }

        async Task Expect(ProcessContext context, string tag, string node)
        {
            var expected = Term.Tuple(Term.A(tag), Term.A(node));
            Check(await context.ReceiveAsync<Term>(message => message.Equals(expected) ? message : null, RuntimeNodeAdmissionFoundationHostNames.Deadline) is not null);
        }

        bool Reject(Action action)
        {
            try
            {
                action();
            }
            catch (ErlangException exception) when (exception.Reason.Equals(Term.A(ErlangErrorReasons.BadArgument)))
            {
                checks++;

                return true;
            }

            throw new Exception(RuntimeNodeAdmissionFoundationHostNames.Accepted);
        }

        void Check(bool condition)
        {
            if (!condition)
                throw new Exception(RuntimeNodeAdmissionFoundationHostNames.Failure);
            checks++;
        }
    }
}
