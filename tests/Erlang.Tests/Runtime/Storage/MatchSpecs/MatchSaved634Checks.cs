using System.Numerics;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using static Erlang.Tests.MatchSaved634Names;

namespace Erlang.Tests;

// Inactive finite component controls. No public match-spec producer is redirected.
internal static class MatchSaved634Checks
{
    internal static async Task Protocol()
    {
        string folder = Path.GetFullPath(Path.Combine(Output, Guid.NewGuid().ToString(GuidFormat)));
        Directory.CreateDirectory(folder);
        var errors = new List<Exception>();
        bool captured = false, actorDisposed = false, runtimeDisposed = false;
        int compiled = 0, runs = 0, freed = 0;
        int countBefore = -1, countAfter = -1;
        string runtimeSha = string.Empty, hostSha = string.Empty, actualSha = string.Empty;
        string runtimeConfiguration = string.Empty, hostConfiguration = string.Empty;
        try
        {
            runtimeSha = Sha634(typeof(ProcessRuntime).Assembly.Location);
            hostSha = Sha634(typeof(MatchSaved634Checks).Assembly.Location);
            runtimeConfiguration = typeof(ProcessRuntime).Assembly.GetCustomAttribute<AssemblyConfigurationAttribute>()?.Configuration ?? string.Empty;
            hostConfiguration = typeof(MatchSaved634Checks).Assembly.GetCustomAttribute<AssemblyConfigurationAttribute>()?.Configuration ?? string.Empty;
            countBefore = NativeCount();
            await InRuntime(
                async (runtime, actor, context) =>
            {
                var observations = new List<Term>();
                Term[] values = Values();
                Require(values.Length == ProtocolEntries);
                foreach (Term value in values)
                {
                    object program = await actor.Invoke(owner => CompileValue(owner, value));
                    compiled++;
                    try
                    {
                        MatchSpecResult result = await RunValue(program, context);
                        runs++;
                        Require(result.Matched && result.Value.Equals(value));
                        observations.Add(Term.Tuple(Term.A(MatchSpecNames.Constant), result.Value));
                    }
                    finally
                    {
                        await CloseProgram(program);
                        Require((bool)Property(program, Freed));
                        freed++;
                    }
                }
                byte[] bytes = ExternalTermFormat.Encode(
                    Term.Tuple(Term.A(MatchSpecNames.Ok), Cons.From(observations)),
                    new EtfEncodingOptions { MinorVersion = 2, FunctionOwner = runtime.InitializationPid }
                );
                System.IO.File.WriteAllBytes(Path.Combine(folder, OutcomeFile), bytes);
                actualSha = Convert.ToHexString(SHA256.HashData(bytes));
                captured = true;
            },
                (actor, runtime) =>
            {
                actorDisposed = actor;
                runtimeDisposed = runtime;
            }
            );
        }
        catch (Exception error)
        {
            errors.Add(error);
        }
        finally
        {
            try
            {
                countAfter = NativeCount();
                Require(countBefore >= 0 && countBefore == countAfter);
                Require(runtimeSha == Sha634(typeof(ProcessRuntime).Assembly.Location));
                Require(hostSha == Sha634(typeof(MatchSaved634Checks).Assembly.Location));
            }
            catch (Exception error)
            {
                errors.Add(error);
            }
            try
            {
                System.IO.File.WriteAllText(
                    Path.Combine(folder, ReceiptFile),
                    JsonSerializer.Serialize(
                        new
                        {
                            Schema = ProtocolSchema,
                            OriginalSourcePin = OriginalPin,
                            OriginalFixtureSha256 = OriginalObserverSha256,
                            RuntimeSha256 = runtimeSha,
                            HostSha256 = hostSha,
                            RuntimeConfiguration = runtimeConfiguration,
                            HostConfiguration = hostConfiguration,
                            Framework = RuntimeInformation.FrameworkDescription,
                            ProcessArchitecture = RuntimeInformation.ProcessArchitecture.ToString(),
                            PointerBytes = IntPtr.Size,
                            LittleEndian = BitConverter.IsLittleEndian,
                            CompiledPrograms = compiled,
                            CompletedRuns = runs,
                            PhysicallyFreedPrograms = freed,
                            ExpectedEntries = ProtocolEntries,
                            NativeProgramCountBefore = countBefore,
                            NativeProgramCountAfter = countAfter,
                            NativeProgramCountRestored = countBefore >= 0 && countBefore == countAfter,
                            ActorDisposeCompleted = actorDisposed,
                            RuntimeDisposeCompleted = runtimeDisposed,
                            WholeOutcomeCaptured = captured,
                            ActualSha256 = actualSha,
                            Passed = errors.Count == 0 && captured && actorDisposed && runtimeDisposed
                        && compiled == ProtocolEntries && runs == ProtocolEntries && freed == ProtocolEntries
                        && countBefore >= 0 && countBefore == countAfter,
                            InternalComponent = true,
                            PublicProducerRedirected = false,
                            RawEtfRewritten = false,
                            FrameProjection = false,
                            GoldenCompared = false,
                            ExceptionalFrameCompatibilityClaimed = false,
                            Errors = errors.Select(error => error.ToString()).ToArray()
                        },
                        new JsonSerializerOptions { WriteIndented = true }
                    )
                );
            }
            catch (Exception error)
            {
                errors.Add(error);
            }
        }
        if (errors.Count == 1)
            ExceptionDispatchInfo.Capture(errors[0]).Throw();
        if (errors.Count > 1)
            throw new AggregateException(Failure, errors);
    }

    private static string Sha634(string path) => Convert.ToHexString(SHA256.HashData(System.IO.File.ReadAllBytes(path)));

    internal static Task MutationAndCopy() => InRuntime(async (runtime, actor, context) =>
    {
        object program = await actor.Invoke(owner => CompileValue(owner, Term.I(First)));
        IDisposable? reader = null;
        nint text = 0;
        long before = 0;
        bool patched = false;
        try
        {
            reader = (IDisposable)Call(program, Acquire)!;
            text = (nint)Call(reader, Text)!;
            before = Marshal.ReadInt64(text, RootOffsetBytes);
            Marshal.WriteInt64(text, RootOffsetBytes, ((long)Second << SmallShift) | SmallTag);
            patched = true;
            MatchSpecResult result = await RunValue(program, context);
            Require(result.Matched && result.Value.Equals(Term.I(Second)));
            Term copy = result.Value;
            Marshal.WriteInt64(text, RootOffsetBytes, before);
            patched = false;
            Task pending = (Task)Call(program, Close)!;
            Require(!pending.IsCompleted && !(bool)Property(program, Freed));
            Term committed = (Term)Call(
                reader,
                Copy,
                runtime,
                CancellationToken.None
            )!;
            Require(committed.Equals(Term.I(First)));
            reader.Dispose();
            reader = null;
            await pending.WaitAsync(Deadline);
            Require((bool)Property(program, Freed) && copy.Equals(Term.I(Second)));
        }
        finally
        {
            if (patched && reader is not null)
                Marshal.WriteInt64(text, RootOffsetBytes, before);
            reader?.Dispose();
            await CloseProgram(program);
        }
    });

    internal static Task HeaderRejection() => InRuntime(async (runtime, actor, context) =>
    {
        object program = await actor.Invoke(owner => CompileValue(owner, Compound()));
        IDisposable? reader = null;
        nint fragment = 0;
        int selectedOffset = -1;
        long before = 0;
        try
        {
            reader = (IDisposable)Call(program, Acquire)!;
            fragment = (nint)Call(reader, Fragment)!;
            foreach (int offset in new[] { NextOffsetBytes, OffHeapOffsetBytes, UsedWordsOffsetBytes })
            {
                selectedOffset = offset;
                before = Marshal.ReadInt64(fragment, offset);
                long changed = offset == NextOffsetBytes ? (long)fragment : before + 1;
                Marshal.WriteInt64(fragment, offset, changed);
                bool refused = false;
                try
                {
                    _ = Call(
                        reader,
                        Copy,
                        runtime,
                        CancellationToken.None
                    );
                }
                catch (InvalidOperationException)
                {
                    refused = true;
                }
                Require(refused);
                await using var foreign = new ProcessRuntime();
                bool foreignFirst = false;
                try
                {
                    _ = Call(
                        reader,
                        Copy,
                        foreign,
                        CancellationToken.None
                    );
                }
                catch (ArgumentException)
                {
                    foreignFirst = true;
                }
                Require(foreignFirst);
                Marshal.WriteInt64(fragment, offset, before);
                selectedOffset = -1;
            }
            Require(((Term)Call(
                reader,
                Copy,
                runtime,
                CancellationToken.None
            )!).Equals(Compound()));
        }
        finally
        {
            if (reader is not null && selectedOffset >= 0)
                Marshal.WriteInt64(fragment, selectedOffset, before);
            reader?.Dispose();
            await CloseProgram(program);
        }
    });

    internal static Task SourceReleased() => InRuntime(async (runtime, actor, context) =>
    {
        var pair = await actor.Invoke(BuildFresh);
        try
        {
            for (int index = 0; index < 3 && pair.Source.IsAlive; index++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }
            Require(!pair.Source.IsAlive);
            MatchSpecResult result = await RunValue(pair.Program, context);
            Require(result.Matched && result.Value.Equals(Compound()));
        }
        finally
        {
            await CloseProgram(pair.Program);
        }
    });

    internal static Task RollbackAndCancellation() => InRuntime(async (runtime, actor, context) =>
    {
        int initial = NativeCount();
        object fragment = CaptureValue(runtime, Compound());
        IDisposable? fragmentReader = null;
        try
        {
            fragmentReader = (IDisposable)Call(fragment, ReadFragment)!;
            bool refused = false;
            try
            {
                _ = NewProgram(runtime, fragment);
            }
            catch (InvalidOperationException)
            {
                refused = true;
            }
            Require(refused && NativeCount() == initial);
            Require(((Term)Call(fragmentReader, CopyFragment, CancellationToken.None)!).Equals(Compound()));
        }
        finally
        {
            fragmentReader?.Dispose();
            await ((Task)Call(fragment, CloseFragment)!).WaitAsync(Deadline);
        }
        foreach (Term unsupported in new Term[]
        {
            new MapTerm([]),
            new BitString([1, 2, 3]),
            new TupleTerm([]),
            Term.Tuple(new TupleTerm([])),
            new Cons(new TupleTerm([]), Nil.Value)
        })
        {
            object?[] args = [context, Specification(unsupported), MatchSpecDialect.Table, null];
            bool accepted = await actor.Invoke(owner =>
            {
                args[0] = owner;

                return (bool)Static(MatchSaved634Names.Compiler, Compile, args)!;
            });
            Require(!accepted && args[3] is null && NativeCount() == initial);
        }
        object program = await actor.Invoke(owner => CompileValue(owner, Compound()));
        IDisposable? reader = null;
        try
        {
            reader = (IDisposable)Call(program, Acquire)!;
            using var cancelled = new CancellationTokenSource();
            await cancelled.CancelAsync();
            bool observed = false;
            try
            {
                _ = Call(
                    reader,
                    Copy,
                    runtime,
                    cancelled.Token
                );
            }
            catch (OperationCanceledException)
            {
                observed = true;
            }
            Require(observed && !(bool)Property(program, Freed));
            Require(((Term)Call(
                reader,
                Copy,
                runtime,
                CancellationToken.None
            )!).Equals(Compound()));
        }
        finally
        {
            reader?.Dispose();
            await CloseProgram(program);
        }
        Require(NativeCount() == initial);
    });

    internal static Task DisposeWaitsReader() => Shutdown(false);

    internal static Task HaltWaitsReader() => Shutdown(true);

    private static Task Shutdown(bool halt) => InRuntime(async (runtime, actor, context) =>
    {
        object program = await actor.Invoke(owner => CompileValue(owner, Term.I(First)));
        IDisposable? reader = null;
        Task? stopping = null;
        try
        {
            reader = (IDisposable)Call(program, Acquire)!;
            if (halt)
            {
                _ = runtime.RequestImmediateHalt(0);
                stopping = runtime.HaltQuiescence;
            }
            else
            {
                stopping = runtime.DisposeAsync().AsTask();
            }
            using var deadline = new CancellationTokenSource(Deadline);
            while (!(bool)Property(runtime, StopAdmission))
                await Task.Delay(PollMilliseconds, deadline.Token);
            Require(!stopping.IsCompleted && !(bool)Property(program, Freed));
            bool admissionRefused = false;
            try
            {
                using var extra = (IDisposable)Call(program, Acquire)!;
            }
            catch (ObjectDisposedException)
            {
                admissionRefused = true;
            }
            Require(admissionRefused);
            Require(((Term)Call(
                reader,
                Copy,
                runtime,
                CancellationToken.None
            )!).Equals(Term.I(First)));
        }
        finally
        {
            reader?.Dispose();
            if (stopping is not null)
                await stopping.WaitAsync(Deadline);
            await CloseProgram(program);
        }
        Require((bool)Property(program, Freed));
    });

    private static async Task InRuntime(Func<ProcessRuntime, RuntimeNodeFoundationHostActor, ProcessContext, Task> body, Action<bool, bool>? cleanupObserved = null)
    {
        var runtime = OwnedGuest506RuntimeFactory.Create();
        RuntimeNodeFoundationHostActor? actor = null;
        var errors = new List<Exception>();
        bool actorDisposed = false, runtimeDisposed = false;
        try
        {
            actor = new(runtime);
            ProcessContext context = await actor.Ready.WaitAsync(Deadline);
            await body(runtime, actor, context);
        }
        catch (Exception error)
        {
            errors.Add(error);
        }
        finally
        {
            if (actor is not null)
            {
                try
                {
                    await actor.DisposeAsync().AsTask().WaitAsync(Deadline);
                    actorDisposed = true;
                }
                catch (Exception error)
                {
                    errors.Add(error);
                }
            }
            try
            {
                await runtime.DisposeAsync().AsTask().WaitAsync(Deadline);
                runtimeDisposed = true;
            }
            catch (Exception error)
            {
                errors.Add(error);
            }
        }
        cleanupObserved?.Invoke(actorDisposed, runtimeDisposed);
        if (errors.Count == 1)
            ExceptionDispatchInfo.Capture(errors[0]).Throw();
        if (errors.Count > 1)
            throw new AggregateException(Failure, errors);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static (object Program, WeakReference Source) BuildFresh(ProcessContext context)
    {
        Term value = Compound();
        Term specification = Specification(value);

        return (CompileSpecification(context, specification), new WeakReference(specification));
    }

    private static Term Compound() => Term.Tuple(new Integer(BigInteger.One << 70),
        new FloatTerm(-2.5), new Cons(Term.I(9), Term.I(11)));

    private static Term[] Values() => [Nil.Value, Term.A(MatchSaved634Names.Atom), Term.I(First),
        new FloatTerm(1.25), new Integer(BigInteger.One << 70), new Integer(-(BigInteger.One << 77)),
        Term.Tuple(Term.I(7), Nil.Value), Term.List(Term.I(3), Term.I(4)),
        new Cons(Term.I(9), Term.I(11)), Compound()];

    private static Term Specification(Term value) => Term.List(Term.Tuple(
        Term.A(MatchSpecNames.Wildcard), Nil.Value, Term.List(Term.Tuple(Term.A(MatchSpecNames.Constant), value))));

    private static object CompileValue(ProcessContext context, Term value) => CompileSpecification(context, Specification(value));

    private static object CompileSpecification(ProcessContext context, Term specification)
    {
        object?[] args = [context, specification, MatchSpecDialect.Table, null];
        Require((bool)Static(MatchSaved634Names.Compiler, Compile, args)!);

        return args[3] ?? throw new InvalidOperationException(Failure);
    }

    private static object CaptureValue(ProcessRuntime runtime, Term value)
    {
        object?[] args = [runtime, value, CancellationToken.None, null];
        Require((bool)Static(FragmentType, CaptureFragment, args)!);

        return args[3] ?? throw new InvalidOperationException(Failure);
    }

    private static object NewProgram(ProcessRuntime runtime, object fragment)
    {
        try
        {
            return Activator.CreateInstance(
                typeof(ProcessRuntime).Assembly.GetType(ProgramType, true)!,
                Members,
                null,
                [runtime, fragment],
                null
            )!;
        }
        catch (TargetInvocationException error) when (error.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(error.InnerException).Throw();
            throw;
        }
    }

    private static Task<MatchSpecResult> RunValue(object program, ProcessContext context)
        => (Task<MatchSpecResult>)Static(MatchSaved634Names.Compiler, Run, [program, context])!;

    private static async Task CloseProgram(object program) => await ((Task)Call(program, Close)!).WaitAsync(Deadline);

    private static int NativeCount()
        => (int)typeof(ProcessRuntime).Assembly.GetType(ProgramType, true)!.GetProperty(Allocations, Members)!.GetValue(null)!;

    private static object Property(object owner, string name) => owner.GetType().GetProperty(name, Members)!.GetValue(owner)!;

    private static object? Static(string type, string method, object?[] args)
        => Invoke(typeof(ProcessRuntime).Assembly.GetType(type, true)!.GetMethod(method, Members)!, null, args);

    private static object? Call(object owner, string method, params object?[] args)
        => Invoke(owner.GetType().GetMethod(method, Members)!, owner, args);

    private static object? Invoke(MethodInfo method, object? owner, object?[] args)
    {
        try
        {
            return method.Invoke(owner, args);
        }
        catch (TargetInvocationException error) when (error.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(error.InnerException).Throw();
            throw;
        }
    }

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(Failure);
    }
}
