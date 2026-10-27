using System.Numerics;
using System.Reflection;
using static Erlang.Tests.BeamOwnedRoots507Names;

namespace Erlang.Tests;

internal static class BeamOwnedRoots507Checks
{
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    internal static async Task OriginalTryReplay()
    {
        await using var runtime = OwnedGuest506RuntimeFactory.Create();
        PreparedBeamImage image = BeamTryHostAssets.Select(runtime);
        await using var owner = new RuntimeNodeFoundationHostActor(runtime);
        var opcodes = new HashSet<string>(StringComparer.Ordinal);
        long maximumWords = 0;

        ValueTask Observe(ProcessContext context, string opcode)
        {
            opcodes.Add(opcode);
            Call(Get(context, Heap), Collect);
            maximumWords = Math.Max(maximumWords, (long)Get(Get(Get(context, Heap), Snapshot), Words));

            return ValueTask.CompletedTask;
        }
        await owner.InvokeAsync(async receiver =>
        {
            using (Checkpoint(Observe))
                _ = await runtime.Modules.Call(receiver, BeamTryHostNames.Module, BeamTryHostNames.Cases);
            foreach (string name in BeamTryHostCaseNames.Ordered)
            {
                TupleTerm expected = BeamTryHostAssets.Expected(name);
                var entered = new TaskCompletionSource<ProcessContext>(TaskCreationOptions.RunContinuationsAsynchronously);
                ProcessHandle child = runtime.Spawn(
                    async context =>
                {
                    entered.TrySetResult(context);
                    using var observation = Checkpoint(Observe);

                    return await runtime.Modules.Call(
                        context,
                        BeamTryHostNames.Module,
                        BeamTryHostNames.Capture,
                        receiver.Self,
                        Term.A(name)
                    );
                },
                    receiver
                );
                Term? report = await receiver.ReceiveAsync<Term>(message =>
                    message is TupleTerm { Items.Count: 2 } tuple && tuple.Items[0].Equals(expected.Items[0]) ? tuple : null,
                    Deadline);
                Check(report is not null && report.Equals(expected));
                Check((await child.Completion.WaitAsync(Deadline)).Equals(Term.A(ProcessExitReasons.Normal)));
                ProcessContext completed = await entered.Task.WaitAsync(Deadline);
                await Unwind(completed);
                RequireEmpty(completed);
            }

            return true;
        });
        string[] declaredOpcodes = ((System.Collections.IEnumerable)Get(Get(image, CodeProperty), InstructionsProperty))
            .Cast<object>().Select(instruction => (string)Get(Get(instruction, DefinitionProperty), OpcodeNameProperty))
            .Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
        string directory = Path.Combine(AppContext.BaseDirectory, Artifacts);
        Directory.CreateDirectory(directory);
        await File.WriteAllTextAsync(
            Path.Combine(directory, ReplayDiagnostics),
            System.Text.Json.JsonSerializer.Serialize(new
            {
                Opcodes = opcodes.Order(StringComparer.Ordinal).ToArray(),
                DeclaredOpcodes = declaredOpcodes,
                MaximumActualPayloadWords = maximumWords,
                CompleteOriginalCasesCompared = BeamTryHostCaseNames.Ordered.Count()
            })
        );
        Check(declaredOpcodes.Contains(TupleOpcode) && opcodes.Contains(TupleOpcode) && maximumWords > 0);
        Check(!declaredOpcodes.Contains(ListOpcode) || opcodes.Contains(ListOpcode));
    }

    internal static async Task AliasAndTrim()
    {
        await using var runtime = OwnedGuest506RuntimeFactory.Create();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            object words = Construct(WordType, Get(context, NativeArena));
            object state = Construct(StateType, words, context);
            try
            {
                Call(
                    state,
                    Write,
                    0,
                    Term.Tuple(Term.I(7))
                );
                Term child = (Term)Call(state, Read, 0)!;
                object destination = Construct(
                    OperandType,
                    X,
                    new BigInteger(1),
                    null
                );
                Call(
                    state,
                    CreateTuple,
                    destination,
                    new Term[] { child, child }
                );
                var pair = (TupleTerm)Call(state, Read, 1)!;
                Check(ReferenceEquals(pair.Items[0], child) && ReferenceEquals(pair.Items[1], child));
                object listDestination = Construct(
                    OperandType,
                    X,
                    new BigInteger(2),
                    null
                );
                Call(
                    state,
                    CreateCons,
                    listDestination,
                    child,
                    Nil.Value
                );
                var list = (Cons)Call(state, Read, 2)!;
                Check(ReferenceEquals(list.Head, child) && ReferenceEquals(list.Tail, Nil.Value));
                Call(state, Allocate, 3);
                Call(
                    state,
                    WriteY,
                    1,
                    child
                );
                Call(
                    state,
                    Write,
                    7,
                    Term.Tuple(Term.I(99))
                );
                Call(Get(context, Heap), Collect);
                long before = (long)Get(Get(Get(context, Heap), Snapshot), Words);
                Call(state, Live, 1);
                Call(
                    state,
                    Trim,
                    1,
                    2
                );
                Call(Get(context, Heap), Collect);
                Check(ReferenceEquals(Call(state, ReadY, 0), child));
                Check((long)Get(Get(Get(context, Heap), Snapshot), Words) < before);
                object clone = Call(state, Clone)!;
                using ((IDisposable)clone)
                {
                    Call(state, Deallocate, 2);
                    Call(state, Clear, Array.Empty<int>());
                    Call(Get(context, Heap), Collect);
                    Check(ReferenceEquals(Call(clone, Read, 0), child));
                    Check(ReferenceEquals(Call(clone, ReadY, 0), child));
                    Check((long)Get(Get(Get(context, Heap), Snapshot), Words) > 0);
                }
                Call(Get(context, Heap), Collect);
                Check((long)Get(Get(Get(context, Heap), Snapshot), Words) == 0);
            }
            finally
            {
                ((IDisposable)state).Dispose();
                await ((IAsyncDisposable)words).DisposeAsync();
            }
            object native = Get(Get(context, NativeArena), Snapshot);
            Check((long)Get(native, NativeReserved) == 0 && (long)Get(native, NativeRoots) == 0);

            return true;
        });
    }

    internal static async Task CancelAtTuple()
    {
        await using var runtime = OwnedGuest506RuntimeFactory.Create();
        _ = BeamTryHostAssets.Select(runtime);
        await using var owner = new RuntimeNodeFoundationHostActor(runtime);
        await using var killer = new RuntimeNodeFoundationHostActor(runtime);
        var entered = new TaskCompletionSource<ProcessContext>(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        ProcessContext receiver = await owner.Ready;
        ProcessHandle child = runtime.Spawn(async context =>
        {
            try
            {
                using var observation = Checkpoint(async (current, opcode) =>
                {
                    if (opcode != TupleOpcode)
                        return;
                    Call(Get(current, Heap), Collect);
                    Check((long)Get(Get(Get(current, Heap), Snapshot), Words) > 0);
                    entered.TrySetResult(current);
                    await release.Task.WaitAsync(current.Cancellation);
                });

                return await runtime.Modules.Call(
                    context,
                    BeamTryHostNames.Module,
                    BeamTryHostNames.Capture,
                    receiver.Self,
                    Term.A(BeamTryHostCaseNames.SavedTrace)
                );
            }
            catch (Exception failure)
            {
                entered.TrySetException(failure);
                throw;
            }
        });
        try
        {
            ProcessContext held = await entered.Task.WaitAsync(Deadline);
            await killer.Invoke(context =>
 {
     runtime.Exit(context, child.Pid, Term.A(ProcessExitSignals.Kill));

     return true;
 });
            await child.Completion.WaitAsync(Deadline);
            await Unwind(held);
            RequireEmpty(held);
        }
        finally
        {
            release.TrySetResult();
            await child.Completion.WaitAsync(Deadline);
        }
    }

    private static async Task Unwind(ProcessContext context)
        => await ((Task)Get(Get(context, Unwound), TaskProperty)).WaitAsync(Deadline);

    private static void RequireEmpty(ProcessContext context)
    {
        object heap = Get(Get(context, Heap), Snapshot);
        object native = Get(Get(context, NativeArena), Snapshot);
        Check((long)Get(heap, Words) == 0 && (long)Get(heap, Capacity) == 0);
        Check((long)Get(native, NativeReserved) == 0 && (long)Get(native, NativeRoots) == 0);
    }

    private static IDisposable Checkpoint(Func<ProcessContext, string, ValueTask> callback)
        => (IDisposable)typeof(ProcessRuntime).Assembly.GetType(ProbeType, true)!
            .GetMethod(ProbeEnter, BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, [callback])!;

    private static object Construct(string name, params object?[] arguments)
        => Activator.CreateInstance(
            typeof(ProcessRuntime).Assembly.GetType(name, true)!,
            Members,
            null,
            arguments,
            null
        )!;

    private static object? Call(object value, string name, params object?[] arguments)
    {
        MethodInfo method = value.GetType().GetMethods(Members).Single(candidate => candidate.Name == name
            && candidate.GetParameters().Length == arguments.Length
            && candidate.GetParameters().Select((parameter, index) => arguments[index] is null
                || parameter.ParameterType.IsInstanceOfType(arguments[index])).All(match => match));

        return method.Invoke(value, arguments);
    }

    private static object Get(object value, string name) => value.GetType().GetProperty(name, Members)!.GetValue(value)!;

    private static void Check(bool value)
    {
        if (!value)
            throw new InvalidOperationException(Failure);
    }
}
