using System.Collections;
using System.Buffers.Binary;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Security.Cryptography;
using System.Text;

namespace Erlang.Tests;

public static class NifBeamEntryHostChecks
{
    public static async Task Run()
    {
        LambdaPublicationValidation();
        await LocalEntry();
        await ClosuresAndGenerations();
        await OwnedDrain();
        await SyntheticPendingClosure();
    }

    private static void Require(bool value)
    {
        if (!value)
            throw new InvalidOperationException(NifBeamEntryHostNames.Broken);
    }

    private static void LambdaPublicationValidation()
    {
        byte[] malformed = Resource(NifBeamEntryHostNames.ClosureModule);
        bool changed = false;
        for (int offset = BeamClosureMutationWire.FormHeaderBytes; offset < malformed.Length;)
        {
            int length = BinaryPrimitives.ReadInt32BigEndian(malformed.AsSpan(offset + BeamClosureMutationWire.LengthFieldOffset));
            if (Encoding.ASCII.GetString(malformed, offset, BeamClosureMutationWire.IdentifierBytes) == BeamClosureMutationWire.LambdaChunk)
            {
                int entry = offset + BeamClosureMutationWire.ChunkHeaderBytes + NifBeamEntryHostNames.LambdaCountBytes;
                int arity = BinaryPrimitives.ReadInt32BigEndian(malformed.AsSpan(entry + NifBeamEntryHostNames.LambdaArityOffset));
                BinaryPrimitives.WriteInt32BigEndian(malformed.AsSpan(entry + NifBeamEntryHostNames.LambdaFreeVariablesOffset), checked(arity + 1));
                changed = true;
                break;
            }
            offset += BeamClosureMutationWire.ChunkHeaderBytes + ((length + BeamClosureMutationWire.AlignmentMask) & ~BeamClosureMutationWire.AlignmentMask);
        }
        Require(changed);
        try
        {
            new ModuleRegistry().PrepareBeam(malformed);
            throw new InvalidOperationException(NifBeamEntryHostNames.Broken);
        }
        catch (InvalidDataException)
        {
        }

        BeamModule standalone = BeamModule.Prepare(Resource(NifBeamEntryHostNames.ClosureModule));
        object code = typeof(BeamModule).GetField(NifBeamEntryHostNames.StandaloneCode, NifBeamEntryHostNames.InternalMembers)!.GetValue(standalone)!;
        object lambda = ((IEnumerable)Property(code, NifBeamEntryHostNames.Lambdas)).Cast<object>().First();
        object target = ((IEnumerable)Property(code, NifBeamEntryHostNames.Functions)).Cast<object>()
            .Single(function => Property(function, NifBeamEntryHostNames.Entry).Equals(Property(lambda, NifBeamEntryHostNames.Label)));
        object[] instructions = ((IEnumerable)Property(code, NifBeamEntryHostNames.Instructions)).Cast<object>().ToArray();
        object instruction = instructions[(int)Property(target, NifBeamEntryHostNames.FirstInstruction)];
        object definition = Property(instruction, NifBeamEntryHostNames.Definition);
        object unsupported = Activator.CreateInstance(
            definition.GetType(),
            [
            Property(definition, NifBeamEntryHostNames.Opcode),
            NifBeamEntryHostNames.UnsupportedEntry,
            Property(definition, NifBeamEntryHostNames.Arity),
            false
        ]
        )!;
        instruction.GetType().GetProperty(NifBeamEntryHostNames.Definition)!.SetValue(instruction, unsupported);
        // Managed fault injection: leave the declared target unreachable from exports.
        object[] exports = ((IEnumerable)Property(code, NifBeamEntryHostNames.Exports)).Cast<object>().ToArray();
        code.GetType().GetProperty(NifBeamEntryHostNames.Exports)!.SetValue(code, Array.CreateInstance(exports[0].GetType(), 0));
        Type metadata = typeof(ModuleRegistry).Assembly.GetType(NifBeamEntryHostNames.MetadataType)!;
        try
        {
            metadata.GetMethod(NifBeamEntryHostNames.Read, BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, [code, null]);
            throw new InvalidOperationException(NifBeamEntryHostNames.Broken);
        }
        catch (TargetInvocationException failure) when (failure.InnerException is NotSupportedException)
        {
        }
    }

    private static object Property(object target, string name)
        => target.GetType().GetProperty(name, NifBeamEntryHostNames.AllMembers)!.GetValue(target)!;

    private static object? Call(object target, string name, params object?[] arguments)
    {
        try
        {
            return target.GetType().GetMethod(name, NifBeamEntryHostNames.InternalMembers)!.Invoke(target, arguments);
        }
        catch (TargetInvocationException failure) when (failure.InnerException is { } inner)
        {
            ExceptionDispatchInfo.Capture(inner).Throw();
            throw;
        }
    }

    private static byte[] Resource(string module)
    {
        Assembly assembly = typeof(NifBeamEntryHostChecks).Assembly;
        string suffix = NifBeamEntryHostNames.Separator + module + NifBeamEntryHostNames.Extension;
        string name = assembly.GetManifestResourceNames().Single(value => value.EndsWith(suffix, StringComparison.Ordinal));
        using Stream input = assembly.GetManifestResourceStream(name)!;
        using var output = new MemoryStream();
        input.CopyTo(output);
        byte[] bytes = output.ToArray();
        string expected = module == NifBeamEntryHostNames.LocalModule ? NifBeamEntryHostNames.LocalHash : NifBeamEntryHostNames.ClosureHash;
        Require(Convert.ToHexString(SHA256.HashData(bytes)) == expected);

        return bytes;
    }

    private static object Bind(
        object execution,
        NifBeamEntryTestOwner owner,
        params KeyValuePair<(string Name, int Arity), ErlangFunction>[] functions
    )
    {
        object image = Property(execution, NifBeamEntryHostNames.Image);
        object table = Property(image, NifBeamEntryHostNames.Table);
        var descriptor = new ModuleCodeDescriptor(
            functions.Select(function => function.Key),
            [],
            new BitString(new byte[MD5.HashSizeInBytes]),
            Nil.Value
        );
        using var reservation = (IDisposable)Call(table, NifBeamEntryHostNames.Begin, descriptor)!;
        Call(
            reservation,
            NifBeamEntryHostNames.Publish,
            functions,
            owner
        );

        return table;
    }

    private static async Task<Term> Invoke(ProcessRuntime runtime, Func<ProcessContext, ValueTask<Term>> body)
    {
        var result = new TaskCompletionSource<Term>(TaskCreationOptions.RunContinuationsAsynchronously);
        var actor = runtime.Spawn(async context =>
        {
            try
            {
                Term value = await body(context);
                result.TrySetResult(value);

                return value;
            }
            catch (Exception failure)
            {
                result.TrySetException(failure);
                throw;
            }
        });
        try
        {
            return await result.Task.WaitAsync(NifBeamEntryHostNames.Deadline);
        }
        finally
        {
            await actor.Completion.WaitAsync(NifBeamEntryHostNames.Deadline);
        }
    }

    private static async Task LocalEntry()
    {
        await using var runtime = new ProcessRuntime();
        PreparedBeamImage prepared = runtime.Modules.PrepareBeam(Resource(NifBeamEntryHostNames.LocalModule));
        runtime.Modules.PublishPreparedBeam(prepared);
        object execution = Property(prepared, NifBeamEntryHostNames.Execution);
        Require(ReferenceEquals(Property(execution, NifBeamEntryHostNames.Image), runtime.Modules.CurrentCodeImage(prepared.Name)));
        var owner = new NifBeamEntryTestOwner();
        object table = Bind(
            execution,
            owner,
            new KeyValuePair<(string Name, int Arity), ErlangFunction>(
                (NifBeamEntryHostNames.Loop, 2),
                (context, arguments) =>
        {
            Require(ReferenceEquals(context.ExecutingSourceImage(prepared), Property(execution, NifBeamEntryHostNames.Image)));
            Require(arguments.Count == 2);

            return ValueTask.FromResult<Term>(Term.I(NifBeamEntryHostNames.Bound));
        }
            )
        );
        try
        {
            Term result = await Invoke(runtime, context => runtime.Modules.Call(context, prepared.Name, NifBeamEntryHostNames.Run));
            Require(result.Equals(Term.I(NifBeamEntryHostNames.Bound)));
        }
        finally
        {
            await ((Task)Call(table, NifBeamEntryHostNames.Retire)!).WaitAsync(NifBeamEntryHostNames.Deadline);
        }
        Require(owner.Calls == 1);
    }

    private static async Task ClosuresAndGenerations()
    {
        await using var runtime = new ProcessRuntime();
        byte[] bytes = Resource(NifBeamEntryHostNames.ClosureModule);
        PreparedBeamImage first = runtime.Modules.PrepareBeam(bytes);
        runtime.Modules.PublishPreparedBeam(first);
        var captured = (FunctionTerm)await Invoke(
            runtime,
            context => runtime.Modules.Call(
                context,
                first.Name,
                NifBeamEntryHostNames.Make,
                Term.I(NifBeamEntryHostNames.Capture)
            )
        );
        var oldOwner = new NifBeamEntryTestOwner();
        int makeCalls = 0;
        int lambdaCalls = 0;
        object oldTable = Bind(
            Property(first, NifBeamEntryHostNames.Execution),
            oldOwner,
            new KeyValuePair<(string Name, int Arity), ErlangFunction>(
                (NifBeamEntryHostNames.Make, 1),
                (_, _) =>
            {
                Interlocked.Increment(ref makeCalls);

                return ValueTask.FromResult<Term>(captured);
            }
            ),
            new KeyValuePair<(string Name, int Arity), ErlangFunction>(
                (NifBeamEntryHostNames.Generated, 2),
                (_, arguments) =>
            {
                Require(arguments[0].Equals(Term.I(NifBeamEntryHostNames.Argument)) && arguments[1].Equals(Term.I(NifBeamEntryHostNames.Capture)));
                Interlocked.Increment(ref lambdaCalls);

                return ValueTask.FromResult<Term>(Term.I(NifBeamEntryHostNames.Bound));
            }
            )
        );
        try
        {
            Term result = await Invoke(runtime, context => runtime.Modules.Call(context, first.Name, NifBeamEntryHostNames.Run));
            Require(result.Equals(Term.Tuple(Term.A(NifBeamEntryHostNames.Success), Term.I(NifBeamEntryHostNames.Bound), Term.A(NifBeamEntryHostNames.True))));
            Require(makeCalls == 1 && lambdaCalls == 1);
            PreparedBeamImage second = runtime.Modules.PrepareBeam(bytes);
            runtime.Modules.PublishPreparedBeam(second);
            var currentOwner = new NifBeamEntryTestOwner();
            object currentTable = Bind(
                Property(second, NifBeamEntryHostNames.Execution),
                currentOwner,
                new KeyValuePair<(string Name, int Arity), ErlangFunction>((NifBeamEntryHostNames.Generated, 2), (_, _) => ValueTask.FromResult<Term>(Term.I(NifBeamEntryHostNames.Replacement)))
            );
            try
            {
                var current = (FunctionTerm)await Invoke(
                    runtime,
                    context => runtime.Modules.Call(
                        context,
                        second.Name,
                        NifBeamEntryHostNames.Make,
                        Term.I(NifBeamEntryHostNames.Capture)
                    )
                );
                Term oldResult = await Invoke(runtime, context => captured.Invoke(context, [Term.I(NifBeamEntryHostNames.Argument)]));
                Term newResult = await Invoke(runtime, context => current.Invoke(context, [Term.I(NifBeamEntryHostNames.Argument)]));
                Require(oldResult.Equals(Term.I(NifBeamEntryHostNames.Bound)) && newResult.Equals(Term.I(NifBeamEntryHostNames.Replacement)));

                BeamModule standalone = BeamModule.Prepare(bytes);
                var isolated = (FunctionTerm)await Invoke(runtime, context => standalone.Invoke(context, NifBeamEntryHostNames.Make, Term.I(NifBeamEntryHostNames.Capture)));
                Term isolatedResult = await Invoke(runtime, context => isolated.Invoke(context, [Term.I(NifBeamEntryHostNames.Argument)]));
                Require(isolatedResult.Equals(Term.I(NifBeamEntryHostNames.Fallback)));
                object standaloneExecution = typeof(BeamModule).GetField(NifBeamEntryHostNames.StandaloneExecution, NifBeamEntryHostNames.InternalMembers)!.GetValue(standalone)!;
                var standaloneOwner = new NifBeamEntryTestOwner();
                object standaloneTable = Bind(
                    standaloneExecution,
                    standaloneOwner,
                    new KeyValuePair<(string Name, int Arity), ErlangFunction>((NifBeamEntryHostNames.Generated, 2), (_, _) => ValueTask.FromResult<Term>(Term.I(NifBeamEntryHostNames.Bound)))
                );
                try
                {
                    Term bound = await Invoke(runtime, context => isolated.Invoke(context, [Term.I(NifBeamEntryHostNames.Argument)]));
                    Require(bound.Equals(Term.I(NifBeamEntryHostNames.Bound)));
                }
                finally
                {
                    await ((Task)Call(standaloneTable, NifBeamEntryHostNames.Retire)!).WaitAsync(NifBeamEntryHostNames.Deadline);
                }
                Require(standaloneOwner.Calls == 1);
            }
            finally
            {
                await ((Task)Call(currentTable, NifBeamEntryHostNames.Retire)!).WaitAsync(NifBeamEntryHostNames.Deadline);
            }
            Require(currentOwner.Calls == 1);
        }
        finally
        {
            await ((Task)Call(oldTable, NifBeamEntryHostNames.Retire)!).WaitAsync(NifBeamEntryHostNames.Deadline);
        }
        Require(oldOwner.Calls == 1);
    }

    private static async Task OwnedDrain()
    {
        await using var runtime = new ProcessRuntime();
        PreparedBeamImage prepared = runtime.Modules.PrepareBeam(Resource(NifBeamEntryHostNames.LocalModule));
        runtime.Modules.PublishPreparedBeam(prepared);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var unwinding = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var owner = new NifBeamEntryTestOwner();
        object table = Bind(
            Property(prepared, NifBeamEntryHostNames.Execution),
            owner,
            new KeyValuePair<(string Name, int Arity), ErlangFunction>(
                (NifBeamEntryHostNames.Loop, 2),
                async (context, _) =>
        {
            entered.TrySetResult();
            try
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, context.Cancellation);

                return Nil.Value;
            }
            finally
            {
                unwinding.TrySetResult();
                await release.Task.WaitAsync(NifBeamEntryHostNames.Deadline);
            }
        }
            )
        );
        var actor = runtime.Spawn(context => runtime.Modules.Call(context, prepared.Name, NifBeamEntryHostNames.Run));
        Task? retirement = null;
        try
        {
            await entered.Task.WaitAsync(NifBeamEntryHostNames.Deadline);
            retirement = (Task)Call(table, NifBeamEntryHostNames.Retire)!;
            Require(!retirement.IsCompleted && owner.Calls == 0);
            var killer = runtime.Spawn(context =>
            {
                runtime.Exit(context, actor.Pid, Term.A(NifBeamEntryHostNames.Kill));

                return ValueTask.FromResult<Term>(Nil.Value);
            });
            await killer.Completion.WaitAsync(NifBeamEntryHostNames.Deadline);
            await unwinding.Task.WaitAsync(NifBeamEntryHostNames.Deadline);
            Require(!retirement.IsCompleted && owner.Calls == 0);
        }
        finally
        {
            release.TrySetResult();
            await actor.Completion.WaitAsync(NifBeamEntryHostNames.Deadline);
            retirement ??= (Task)Call(table, NifBeamEntryHostNames.Retire)!;
            await retirement.WaitAsync(NifBeamEntryHostNames.Deadline);
        }
        Require(owner.Calls == 1);
    }

    private static async Task RequireRecognizedOwner(ProcessContext context)
    {
        object operation = Call(context.Runtime.Modules, NifBeamEntryHostNames.Reserve, context)!;
        Task task = (Task)operation.GetType().GetMethod(NifBeamEntryHostNames.AsTask)!.Invoke(operation, null)!;
        try
        {
            await task.WaitAsync(NifBeamEntryHostNames.Deadline);
            throw new InvalidOperationException(NifBeamEntryHostNames.Broken);
        }
        catch (InvalidOperationException failure) when (
            failure.GetType().GetProperty(NifBeamEntryHostNames.Failure, NifBeamEntryHostNames.InternalMembers)?.GetValue(failure)?.ToString() == NifBeamEntryHostNames.Reload)
        {
        }
    }

    private static async Task SyntheticPendingClosure()
    {
        await using var runtime = new ProcessRuntime();
        PreparedBeamImage prepared = runtime.Modules.PrepareBeam(Resource(NifBeamEntryHostNames.ClosureModule));
        object code = Property(prepared, NifBeamEntryHostNames.Code);
        object[] exports = ((IEnumerable)Property(code, NifBeamEntryHostNames.Exports)).Cast<object>().ToArray();
        object run = exports.Single(function => (string)Property(function, NifBeamEntryHostNames.Name) == NifBeamEntryHostNames.Run);
        // Managed metadata injection only: raw pinned fixture bytes remain unchanged.
        code.GetType().GetProperty(NifBeamEntryHostNames.OnLoadEntry)!.SetValue(code, Property(run, NifBeamEntryHostNames.Entry));
        BeamOnLoadTicket ticket = runtime.Modules.StagePreparedOnLoad(prepared);
        object execution = Property(prepared, NifBeamEntryHostNames.Execution);
        Require(ReferenceEquals(Property(execution, NifBeamEntryHostNames.Owner), prepared));
        var bodies = ((IEnumerable)Property(prepared, NifBeamEntryHostNames.Bodies)).Cast<ErlangFunction>().ToArray();
        int makeIndex = Array.FindIndex(exports, function => (string)Property(function, NifBeamEntryHostNames.Name) == NifBeamEntryHostNames.Make);
        ErlangFunction make = bodies[makeIndex];
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        FunctionTerm? escaped = null;
        var owner = new NifBeamEntryTestOwner();
        object table = Bind(
            execution,
            owner,
            new KeyValuePair<(string Name, int Arity), ErlangFunction>(
                (NifBeamEntryHostNames.Run, 0),
                async (context, _) =>
            {
                Require(ReferenceEquals(context.ExecutingSourceImage(prepared), Property(execution, NifBeamEntryHostNames.Image)));
                await RequireRecognizedOwner(context);
                escaped = (FunctionTerm)await make(context, [Term.I(NifBeamEntryHostNames.Capture)]);
                entered.TrySetResult();
                await release.Task.WaitAsync(NifBeamEntryHostNames.Deadline);

                return Term.A(NifBeamEntryHostNames.Success);
            }
            ),
            new KeyValuePair<(string Name, int Arity), ErlangFunction>(
                (NifBeamEntryHostNames.Generated, 2),
                async (context, _) =>
            {
                Require(ReferenceEquals(context.ExecutingSourceImage(prepared), Property(execution, NifBeamEntryHostNames.Image)));
                await RequireRecognizedOwner(context);

                return Term.I(NifBeamEntryHostNames.Bound);
            }
            )
        );
        ProcessHandle callback = runtime.Modules.StartPendingOnLoad(runtime, ticket);
        bool published = false;
        try
        {
            await entered.Task.WaitAsync(NifBeamEntryHostNames.Deadline);
            Require(!runtime.Modules.IsModuleLoaded(prepared.Name));
            release.TrySetResult();
            BeamOnLoadCallbackOutcome outcome = await ticket.Outcome.WaitAsync(NifBeamEntryHostNames.Deadline);
            await callback.Completion.WaitAsync(NifBeamEntryHostNames.Deadline);
            await ticket.CallbackQuiescence.WaitAsync(NifBeamEntryHostNames.Deadline);
            Require(outcome.Failure is null && outcome.Returned!.Equals(Term.A(NifBeamEntryHostNames.Success)));
            Require(runtime.Modules.FinishPendingOnLoad(ticket, true));
            published = true;
            Require(ReferenceEquals(runtime.Modules.CurrentCodeImage(prepared.Name), Property(execution, NifBeamEntryHostNames.Image)));
            Require(ReferenceEquals(runtime.Modules.SourceOwner(prepared.Name, NifBeamEntryHostNames.Make, 1), prepared));
            Term result = await Invoke(runtime, context => escaped!.Invoke(context, [Term.I(NifBeamEntryHostNames.Argument)]));
            Require(result.Equals(Term.I(NifBeamEntryHostNames.Bound)));
        }
        finally
        {
            release.TrySetResult();
            await callback.Completion.WaitAsync(NifBeamEntryHostNames.Deadline);
            await ticket.CallbackQuiescence.WaitAsync(NifBeamEntryHostNames.Deadline);
            if (!published)
                runtime.Modules.FinishPendingOnLoad(ticket, false);
            await ((Task)Call(table, NifBeamEntryHostNames.Retire)!).WaitAsync(NifBeamEntryHostNames.Deadline);
        }
        Require(owner.Calls == 1);
    }
}
