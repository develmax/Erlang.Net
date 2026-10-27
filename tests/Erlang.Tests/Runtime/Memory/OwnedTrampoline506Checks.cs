using System.Reflection;
using Erlang.Compiler;
using Erlang.Differential;
using static Erlang.Tests.OwnedTrampoline506Names;

namespace Erlang.Tests;

internal static class OwnedTrampoline506Checks
{
    private const BindingFlags Members = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

    internal static async Task AllCarriers()
    {
        using var guest = GeneratedModuleCompiler.Compile(GuestSource);
        using var target = GeneratedModuleCompiler.Compile(TargetSource);
        await using var runtime = OwnedGuest506RuntimeFactory.Create();
        guest.Definition.Register(runtime.Modules);
        target.Definition.Register(runtime.Modules);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        Term expected = Term.Tuple(Term.I(506), Term.List(Term.I(1), Term.I(2)));
        await actor.InvokeAsync(async context =>
        {
            using var observation = Observe((current, carrier) =>
            {
                seen.Add(carrier);
                object heap = Get(current, Heap);
                Call(heap, Collect);
                Check((long)Get(Get(heap, Snapshot), Words) > 0);

                return ValueTask.CompletedTask;
            });
            foreach (string function in new[] { RemoteEntry, PrivateEntry })
            {
                Term result = await runtime.Modules.Call(
                    context,
                    GuestModule,
                    function,
                    Term.I(506)
                );
                Check(result.Equals(expected));
            }
            Term closure = await runtime.Modules.Call(
                context,
                GuestModule,
                FunctionEntry,
                Term.I(506)
            );
            Check(closure.Equals(Term.Tuple(expected, Term.I(506))));
            try
            {
                await runtime.Modules.Call(
                    context,
                    GuestModule,
                    RuntimeEntry,
                    Term.I(506)
                );
                throw new InvalidOperationException(Failure);
            }
            catch (ErlangException error)
            {
                Check(error.Reason.Equals(Term.A(ErlangErrorReasons.BadArgument)));
                // It must remain readable after another real owned collection.
                Call(Get(context, Heap), Collect);
                _ = error.StackTraceTerm.ToString();
            }

            return Nil.Value;
        });
        foreach (string carrier in new[] { Private, Source, Runtime, Function, SourceFunction })
            Check(seen.Contains(carrier));
    }

    internal static async Task TailPlateau()
    {
        using var guest = GeneratedModuleCompiler.Compile(GuestSource);
        await using var runtime = OwnedGuest506RuntimeFactory.Create();
        guest.Definition.Register(runtime.Modules);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        var snapshots = new List<(long Words, int Roots)>();
        Term result = await actor.InvokeAsync(async context =>
        {
            using var observation = Observe((current, carrier) =>
            {
                if (carrier == Private)
                {
                    object heap = Get(current, Heap);
                    Call(heap, Collect);
                    object snapshot = Get(heap, Snapshot);
                    snapshots.Add(((long)Get(snapshot, Words), (int)Get(snapshot, Roots)));
                }

                return ValueTask.CompletedTask;
            });

            return await runtime.Modules.Call(
                context,
                GuestModule,
                DeepEntry,
                Term.I(Iterations)
            );
        });
        Check(result.Equals(Term.Tuple(Term.I(1), Term.List(Term.I(1), Term.I(2)))));
        Check(snapshots.Count >= Iterations);
        Check(snapshots.All(row => row.Words > 0 && row.Words <= PlateauPayloadBudget));
        Check(snapshots.Select(row => row.Roots).Distinct().Count() == 1);
        ProcessContext owner = await actor.Ready;
        Call(Get(owner, Heap), Collect);
        Check((long)Get(Get(Get(owner, Heap), Snapshot), Words) == 0);
    }

    internal static Task CancelHandoff() => CancelAt(Source, RemoteEntry);

    internal static Task CancelFunctionTransfer() => CancelAt(SourceFunction, FunctionEntry);

    private static async Task CancelAt(string selectedCarrier, string entry)
    {
        using var guest = GeneratedModuleCompiler.Compile(GuestSource);
        using var target = GeneratedModuleCompiler.Compile(TargetSource);
        await using var runtime = OwnedGuest506RuntimeFactory.Create();
        guest.Definition.Register(runtime.Modules);
        target.Definition.Register(runtime.Modules);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await using var killer = new RuntimeNodeFoundationHostActor(runtime);
        var entered = new TaskCompletionSource<ProcessContext>(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Task<Term> pending = actor.InvokeAsync(async context =>
        {
            using var observation = Observe(async (current, carrier) =>
            {
                if (carrier != selectedCarrier)
                    return;
                Call(Get(current, Heap), Collect);
                Check((long)Get(Get(Get(current, Heap), Snapshot), Words) > 0);
                entered.TrySetResult(current);
                await release.Task.WaitAsync(current.Cancellation);
            });

            return await runtime.Modules.Call(
                context,
                GuestModule,
                entry,
                Term.I(506)
            );
        });
        try
        {
            ProcessContext held = await entered.Task.WaitAsync(Deadline);
            await killer.Invoke(context =>
            {
                runtime.Exit(context, actor.Process.Pid, Term.A(ProcessExitSignals.Kill));

                return true;
            });
            try
            {
                await pending.WaitAsync(Deadline);
                throw new InvalidOperationException(Failure);
            }
            catch (OperationCanceledException) { }
            await ((Task)Get(Get(held, Unwound), TaskProperty)).WaitAsync(Deadline);
            object snapshot = Get(Get(held, Heap), Snapshot);
            Check((long)Get(snapshot, Words) == 0 && (long)Get(snapshot, Capacity) == 0);
        }
        finally
        {
            release.TrySetResult();
            try
            {
                await pending.WaitAsync(Deadline);
            }
            catch (OperationCanceledException) { }
        }
    }

    private static IDisposable Observe(Func<ProcessContext, string, ValueTask> callback)
        => (IDisposable)typeof(Execution).Assembly.GetType(ProbeType, true)!
            .GetMethod(ProbeEnter, BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, [callback])!;

    private static object Get(object value, string name) => value.GetType().GetProperty(name, Members)!.GetValue(value)!;

    private static object? Call(object value, string name) => value.GetType().GetMethod(name, Members)!.Invoke(value, null);

    private static void Check(bool value)
    {
        if (!value)
            throw new InvalidOperationException(Failure);
    }
}
