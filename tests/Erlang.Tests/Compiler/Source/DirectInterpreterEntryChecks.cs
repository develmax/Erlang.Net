using System.Reflection;
using Erlang.Compiler;

namespace Erlang.Tests;

internal static class DirectInterpreterEntryChecks
{
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    internal static Task Evaluate() => AcrossProfiles(EvaluateProfile);

    private static async Task EvaluateProfile(bool enabled)
    {
        await using var runtime = OwnedGuest506RuntimeFactory.Create(enabled);
        bool matched = false, invoked = false;
        Term? captured = null;
        var pattern = new DirectInterpreterPattern((context, value) =>
        {
            Require(Backed(context) && Portable(value));
            matched = true;
        });
        var callback = new FunctionTerm(
            1,
            (_, arguments) =>
        {
            Require(Portable(arguments[0]));
            invoked = true;
            captured = arguments[0];

            return ValueTask.FromResult(arguments[0]);
        }
        );
        Expr expression = new Expr.Sequence(new Expr[]
        {
            new Expr.Match(pattern, Value()),
            new Expr.Apply(new Expr.Literal(callback), new Expr[] { new Expr.Variable(DirectInterpreterEntryNames.Bound) })
        });
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        Term result = await actor.InvokeAsync(async context => await Execution.EvaluateAsync(expression, context));
        Require(matched && invoked && captured is not null && result.Equals(captured) && Portable(result));
        Require(result is TupleTerm { Items.Count: 2 } tuple && tuple.Items[1] is ReferenceTerm);
    }

    internal static Task Invoke() => AcrossProfiles(InvokeProfile);

    private static async Task InvokeProfile(bool enabled)
    {
        await using var runtime = OwnedGuest506RuntimeFactory.Create(enabled);
        ModuleDefinition module = new Parser(DirectInterpreterEntryNames.Source).ParseModule();
        bool observed = false, invoked = false;
        runtime.Modules.Register(
            DirectInterpreterEntryNames.Probe,
            DirectInterpreterEntryNames.Backing,
            0,
            (context, _) =>
        {
            observed = Backed(context);

            return ValueTask.FromResult<Term>(Term.A(observed ? ErlangBooleanAtoms.True : ErlangBooleanAtoms.False));
        }
        );
        Term environment = Term.Tuple(Term.A(DirectInterpreterEntryNames.Marker));
        var callback = new FunctionTerm(
            new LocalFunctionIdentity(
                Term.A(DirectInterpreterEntryNames.HostMetadata),
                null,
                DirectInterpreterEntryNames.Index,
                DirectInterpreterEntryNames.Index,
                new BitString(new byte[DirectInterpreterEntryNames.UniqueBytes])
            ),
            1,
            new[] { environment },
            (_, arguments) =>
        {
            invoked = true;
            Require(Portable(arguments[0]));

            return ValueTask.FromResult(arguments[0]);
        }
        );
        Require(!runtime.Modules.HasExport(module.Name, DirectInterpreterEntryNames.Run, 1));
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        Term result = await actor.InvokeAsync(async context => await Execution.InvokeAsync(
            module,
            DirectInterpreterEntryNames.Run,
            context,
            new Term[] { callback }
        ));
        Require(observed && invoked && Portable(result) && result is TupleTerm { Items.Count: 2 });
        Require(!runtime.Modules.HasExport(module.Name, DirectInterpreterEntryNames.Run, 1));
    }

    internal static Task Beam() => AcrossProfiles(BeamProfile);

    private static async Task BeamProfile(bool enabled)
    {
        await using var runtime = OwnedGuest506RuntimeFactory.Create(enabled);
        BeamModule module = BeamModule.Prepare(Resource(DirectInterpreterEntryNames.BeamFile));
        bool observed = false;
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        Term result = await actor.InvokeAsync(async context =>
        {
            using var checkpoint = Checkpoint(async (current, opcode) =>
            {
                if (!Backed(current))
                    return;
                observed = true;
                _ = await runtime.Modules.Call(current, ErlangModuleNames.Module, ProcessGarbageCollectionNames.Function);
            });

            return await module.Invoke(context, DirectInterpreterEntryNames.Run);
        });
        Term expected = ExternalTermFormat.Decode(Resource(DirectInterpreterEntryNames.BeamExpected));
        Require(observed && Portable(result) && Term.Tuple(Term.A(OrdinaryCollectorNames.Ok), result).Equals(expected));
        Require(!runtime.Modules.HasExport(module.Name, DirectInterpreterEntryNames.Run, 0));
    }

    internal static Task Error() => AcrossProfiles(ErrorProfile);

    private static async Task ErrorProfile(bool enabled)
    {
        await using var runtime = OwnedGuest506RuntimeFactory.Create(enabled);
        ModuleDefinition module = new Parser(DirectInterpreterEntryNames.Source).ParseModule();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        ErlangException? captured = null;
        await actor.InvokeAsync(async context =>
        {
            try
            {
                _ = await Execution.InvokeAsync(
                    module,
                    DirectInterpreterEntryNames.Fail,
                    context,
                    Array.Empty<Term>()
                );
            }
            catch (ErlangException error)
            {
                captured = error;
            }
            Require(captured is not null && Portable(captured.Reason));
            _ = await runtime.Modules.Call(context, ErlangModuleNames.Module, ProcessGarbageCollectionNames.Function);

            return true;
        });
        Require(captured!.Reason is TupleTerm { Items: [Atom marker, ReferenceTerm] } && marker.Name == DirectInterpreterEntryNames.Error);
        Require(captured.StackTraceTerm is Cons && ExternalTermFormat.Encode(captured.StackTraceTerm).Length != 0);
        BeamModule beam = BeamModule.Prepare(Resource(DirectInterpreterEntryNames.BeamFile));
        await actor.InvokeAsync(async context =>
        {
            try
            {
                _ = await beam.Invoke(context, DirectInterpreterEntryNames.Missing, Nil.Value);
            }
            catch (InvalidDataException error)
            {
                Require(error.Message == DirectInterpreterEntryNames.MissingDiagnostic);

                return true;
            }
            throw new InvalidOperationException(DirectInterpreterEntryNames.Failure);
        });
    }

    internal static Task Cancel() => AcrossProfiles(CancelProfile);

    private static async Task CancelProfile(bool enabled)
    {
        await using var runtime = OwnedGuest506RuntimeFactory.Create(enabled);
        var entered = new TaskCompletionSource<ProcessContext>(TaskCreationOptions.RunContinuationsAsynchronously);
        bool unwound = false, cancellationObserved = false;
        var callback = new FunctionTerm(
            1,
            async (execution, arguments) =>
        {
            var context = (ProcessContext)execution;
            entered.TrySetResult(context);
            Require(Backed(context) && Portable(arguments[0]));
            try
            {
                await Task.Delay(Timeout.Infinite, context.Cancellation);
            }
            finally
            {
                cancellationObserved = context.Cancellation.IsCancellationRequested;
            }

            return Nil.Value;
        }
        );
        ProcessHandle child = runtime.Spawn(async context =>
        {
            try
            {
                return await Execution.EvaluateAsync(new Expr.Apply(new Expr.Literal(callback), new Expr[] { Value() }), context);
            }
            finally
            {
                unwound = true;
            }
        });
        ProcessContext target = await entered.Task.WaitAsync(DirectInterpreterEntryNames.Deadline);
        await runtime.DisposeAsync().AsTask().WaitAsync(DirectInterpreterEntryNames.Deadline);
        await child.Completion.WaitAsync(DirectInterpreterEntryNames.Deadline);
        object completion = typeof(ProcessContext).GetProperty(DirectInterpreterEntryNames.Unwound, Members)!.GetValue(target)!;
        await ((Task)completion.GetType().GetProperty(DirectInterpreterEntryNames.Task)!.GetValue(completion)!).WaitAsync(DirectInterpreterEntryNames.Deadline);
        await ((Task)typeof(ProcessContext).GetProperty(DirectInterpreterEntryNames.Retirement, Members)!.GetValue(target)!).WaitAsync(DirectInterpreterEntryNames.Deadline);
        Require(unwound && cancellationObserved && HeapCount(target, DirectInterpreterEntryNames.Blocks) == 0 && HeapCount(target, DirectInterpreterEntryNames.Frames) == 0);
    }

    internal static async Task OffActor()
    {
        await using var runtime = OwnedGuest506RuntimeFactory.Create(false);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        ProcessContext context = await actor.Ready;
        object? originalHeap = typeof(ProcessContext).GetField(DirectInterpreterEntryNames.Heap, Members)!.GetValue(context);
        Require(VmStorageBirthAccess.HasRegisteredBacking(context) && !VmStorageBirthAccess.ExecutionEnabled(context));
        int originalFrames = HeapCount(context, DirectInterpreterEntryNames.Frames);
        int originalBlocks = HeapCount(context, DirectInterpreterEntryNames.Blocks);
        Term result = await Execution.EvaluateAsync(new Expr.Literal(Term.I(DirectInterpreterEntryNames.Operand)), context);
        Require(result.Equals(Term.I(DirectInterpreterEntryNames.Operand)));
        Require(ReferenceEquals(originalHeap, typeof(ProcessContext).GetField(DirectInterpreterEntryNames.Heap, Members)!.GetValue(context)));
        Require(!VmStorageBirthAccess.ExecutionEnabled(context) && VmStorageBirthAccess.Property(context, VmStorageBirthNames.CurrentInvocation) is null);
        Require(HeapCount(context, DirectInterpreterEntryNames.Frames) == originalFrames && HeapCount(context, DirectInterpreterEntryNames.Blocks) == originalBlocks);
    }

    private static async Task AcrossProfiles(Func<bool, Task> run)
    {
        List<Exception>? failures = null;
        foreach (bool enabled in new[] { false, true })
        {
            try
            {
                await run(enabled);
            }
            catch (Exception error)
            {
                (failures ??= new()).Add(new InvalidOperationException(
                    DirectInterpreterEntryNames.ProfileFailure + enabled, error));
            }
        }
        if (failures is not null)
            throw new AggregateException(failures);
    }

    private static Expr Value() => new Expr.Tuple(new Expr[]
    {
        new Expr.Literal(Term.A(DirectInterpreterEntryNames.Marker)),
        new Expr.Call(ErlangModuleNames.Module, DirectInterpreterEntryNames.MakeReference, Array.Empty<Expr>())
    });

    private static bool Backed(ProcessContext context) => HeapCount(context, DirectInterpreterEntryNames.Blocks) > 0;

    private static int HeapCount(ProcessContext context, string field)
    {
        object? heap = typeof(ProcessContext).GetField(DirectInterpreterEntryNames.Heap, Members)!.GetValue(context);
        if (heap is null)
            return 0;
        object snapshot = heap.GetType().GetProperty(DirectInterpreterEntryNames.Snapshot, Members)!.GetValue(heap)!;

        return (int)snapshot.GetType().GetProperty(field)!.GetValue(snapshot)!;
    }

    private static bool Portable(Term value) => value is not TupleTerm tuple
        || typeof(TupleTerm).GetProperty(DirectInterpreterEntryNames.Storage, Members)!.GetValue(tuple) is null;

    private static IDisposable Checkpoint(Func<ProcessContext, string, ValueTask> observe)
        => (IDisposable)typeof(ProcessRuntime).Assembly.GetType(DirectInterpreterEntryNames.CheckpointType, true)!
            .GetMethod(DirectInterpreterEntryNames.Enter, BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, new object[] { observe })!;

    private static byte[] Resource(string filename)
    {
        var assembly = typeof(DirectInterpreterEntryChecks).Assembly;
        string name = assembly.GetManifestResourceNames().Single(value => value.EndsWith(DirectInterpreterEntryNames.ResourceSeparator + filename, StringComparison.Ordinal));
        using Stream stream = assembly.GetManifestResourceStream(name)!;
        using var bytes = new MemoryStream();
        stream.CopyTo(bytes);

        return bytes.ToArray();
    }

    internal static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(DirectInterpreterEntryNames.Failure);
    }
}
