using System.Collections;
using System.Reflection;
using System.Runtime.ExceptionServices;
using static Erlang.Tests.NativeResource524Access;
using static Erlang.Tests.NativeResource524Names;

namespace Erlang.Tests;

// All values come from actual existing producers, never synthetic resource kinds.
internal static class SuspensionAck286Checks
{
    internal static Task RunAll() => RunProfiles([false, true]);

    internal static Task Run(bool owned) => RunProfiles([owned]);

    private static async Task RunProfiles(IEnumerable<bool> profiles)
    {
        var failures = new List<Exception>();
        foreach (bool owned in profiles)
            foreach (SuspensionAckFamily286 family in Enum.GetValues<SuspensionAckFamily286>())
            {
                string label = $"{SuspensionAck286Names.Mode}/{(owned ? SuspensionAck286Names.PrivateProfile : SuspensionAck286Names.OrdinaryProfile)}/{family}";
                Console.WriteLine($"{SuspensionAck286Names.Begin} {label}");
                try
                {
                    await One(owned, family);
                    Console.WriteLine($"{SuspensionAck286Names.Pass} {label}");
                }
                catch (Exception error)
                {
                    Console.WriteLine($"{SuspensionAck286Names.Fail} {label}");
                    Console.WriteLine(error);
                    failures.Add(new InvalidOperationException(label, error));
                }
            }
        foreach (bool owned in profiles)
        {
            string label = $"{SuspensionAck286Names.Mode}/{(owned ? SuspensionAck286Names.PrivateProfile : SuspensionAck286Names.OrdinaryProfile)}/{nameof(PreparedMailboxRead292Checks)}";
            Console.WriteLine($"{SuspensionAck286Names.Begin} {label}");
            try
            {
                await PreparedMailboxRead292Checks.Run(owned);
                Console.WriteLine($"{SuspensionAck286Names.Pass} {label}");
            }
            catch (Exception error)
            {
                Console.WriteLine($"{SuspensionAck286Names.Fail} {label}");
                Console.WriteLine(error);
                failures.Add(new InvalidOperationException(label, error));
            }
        }
        if (failures.Count != 0) throw new AggregateException(failures);
    }

    private static async Task One(bool owned, SuspensionAckFamily286 family)
    {
        await using var runtime = OwnedGuest506RuntimeFactory.Create(owned);
        RuntimeRegexEngine411? engine = family == SuspensionAckFamily286.Regex
            ? await RuntimeNativePrimitiveProviders.RegisterRegexAsync411(runtime, runtime.Modules, RegexNative411Access.Admission()) : null;
        PreparedBeamImage? image = family == SuspensionAckFamily286.PreparedLoader
            ? runtime.Modules.PrepareBeam(BeamLiteralExportHostBytes.Image(ExternalTermFormat.Encode(Term.I(SuspensionAck286Names.PreparedLiteral)))) : null;
        await using var requester = new RuntimeNodeFoundationHostActor(runtime);
        ResourceReferenceTerm? source = null;
        IDisposable? host = null;
        object? backing = null;
        Task? retired = null;
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var target = runtime.Spawn(async context =>
        {
            entered.TrySetResult();
            await release.Task.WaitAsync(context.Cancellation);
            await context.ExecutionCheckpoint();

            return Nil.Value;
        });
        Exception? original = null;
        var cleanupFailures = new List<Exception>(SuspensionAck286Names.CleanupStages);
        try
        {
            await entered.Task.WaitAsync(Deadline);
            await requester.InvokeAsync(async context =>
            {
                Term produced;
                switch (family)
                {
                    case SuspensionAckFamily286.Atomics:
                        object binding = Activator.CreateInstance(
                            TypeOf(SuspensionAck286Names.AtomicsProducer),
                            Members,
                            null,
                            [runtime, runtime.Modules],
                            null
                        )!;
                        produced = await ((ErlangFunction)Property(binding, SuspensionAck286Names.ProducerBody))(context, [Term.I(SuspensionAck286Names.AtomicLength), Term.I(SuspensionAck286Names.AtomicOptions)]);
                        break;
                    case SuspensionAckFamily286.Counters:
                        produced = (Term)Static(
                            SuspensionAck286Names.CountersProducer,
                            SuspensionAck286Names.CreateCounters,
                            runtime,
                            Property(runtime, SuspensionAck286Names.CountersOwner),
                            context,
                            SuspensionAck286Names.CounterLength
                        )!;
                        break;
                    case SuspensionAckFamily286.Regex:
                        var compiled = (TupleTerm)await engine!.CompileAsync(context, new BitString(new byte[] { SuspensionAck286Names.PatternByte }), Nil.Value);
                        Check(compiled.Items[SuspensionAck286Names.RegexResultStatus].Equals(Term.A(RegexNative411Names.Ok)));
                        produced = ((TupleTerm)compiled.Items[SuspensionAck286Names.RegexResultPattern]).Items[SuspensionAck286Names.RegexReference];
                        break;
                    case SuspensionAckFamily286.BinaryProgram:
                        produced = ((TupleTerm)await (ValueTask<Term>)Static(
                            SuspensionAck286Names.BinaryProducer,
                            SuspensionAck286Names.CompileBinary,
                            context,
                            new BitString(new byte[] { SuspensionAck286Names.PatternByte })
                        )!).Items[SuspensionAck286Names.BinaryReference];
                        break;
                    default:
                        produced = runtime.ReferenceResources.CreatePrepared(image!);
                        break;
                }
                source = (ResourceReferenceTerm)produced;
                // Direct producer calls above deliberately return their real
                // independently-owned Host carrier, not an unrooted guest cell.
                Check(source.GetType().GetProperty(SuspensionAck286Names.Storage, Members)!.GetValue(source) is null);
                host = (IDisposable)Property(source, SuspensionAck286Names.Hosts[(int)family]);
                backing = Direct(runtime, source, family);
                retired = (Task)Property(backing, SuspensionAck286Names.Retired[(int)family]);
                using var escape = new NativeTermHostScope524(runtime);
                Term result = await runtime.Modules.Call(
                    context,
                    ErlangModuleNames.Module,
                    SuspensionAck172Names.Suspend,
                    target.Pid,
                    Term.List(Term.Tuple(Term.A(SuspensionAck172Names.Asynchronous), source))
                );
                Check(result.Equals(Term.A(SuspensionAck172Names.True)));

                return Nil.Value;
            });
            Task retirement = retired ?? throw new InvalidOperationException(nameof(SuspensionAck286Checks));
            uint[] expectedWords = source!.Words.ToArray();
            host!.Dispose(); // Drop the explicit producer carrier; account opaque escapes separately.
            host = null;
            FinalizeOpaqueHostEscapes();
            // Host finalizers cannot retire a still-pending real ACK graph.
            Check(!retirement.IsCompleted);
            release.TrySetResult();
            await requester.InvokeAsync(async context =>
            {
                using var escape = new NativeTermHostScope524(runtime);
                bool checkedPayload = false;
                Atom? ack = await context.ReceiveAsync(
                    message =>
                {
                    if (message is not TupleTerm { Items.Count: SuspensionAck172Names.AckArity } tuple) return null;
                    Check(tuple.Items[SuspensionAck172Names.StatusIndex].Equals(Term.A(SuspensionAck172Names.Suspended)));
                    var actual = (ResourceReferenceTerm)tuple.Items[SuspensionAck172Names.TagIndex];
                    // Opaque receive exposes an independently-owned Host copy.
                    // Dispose that exact exposed carrier after its last read;
                    // NativeTermHostScope owns a separate graph interest.
                    using var receivedHost = (IDisposable)Property(actual, SuspensionAck286Names.Hosts[(int)family]);
                    Check(actual.Words.SequenceEqual(expectedWords));
                    object receivedBacking = Direct(runtime, actual, family);
                    Check(ReferenceEquals(receivedBacking, backing) && !retirement.IsCompleted);
                    Read(
                        runtime,
                        actual,
                        receivedBacking,
                        family,
                        image
                    );
                    checkedPayload = true;

                    return Term.A(SuspensionAck286Names.CheckedAck);
                },
                    Deadline
                );
                Check(ack is not null && checkedPayload);
                await runtime.Modules.Call(
                    context,
                    ErlangModuleNames.Module,
                    SuspensionAck172Names.Resume,
                    target.Pid
                );

                return Nil.Value;
            });
            await target.Completion.WaitAsync(Deadline);
            ProcessContext requesterContext = await requester.Ready.WaitAsync(Deadline);
            await AwaitMailbox(requesterContext);
            await requester.InvokeAsync(context =>
            {
                Call(
                    Property(context, SuspensionAck175Names.Heap),
                    SuspensionAck175Names.Collect,
                    true,
                    SuspensionAck175Names.FullSweep
                );

                return Task.FromResult<Term>(Nil.Value);
            });
            // The opaque BIF argument export commits its own Host wrapper.
            // Its internal DTO is now unreachable after ACK; finalize Host
            // escapes separately from actual receiver major collection above.
            FinalizeOpaqueHostEscapes();
            // This must settle before requester or VM disposal can rescue it.
            await retirement.WaitAsync(Deadline);
            if (family is SuspensionAckFamily286.Atomics or SuspensionAckFamily286.Counters)
                Check(Call(backing!, SuspensionAck286Names.AcquireArray) is null);
            else if (family == SuspensionAckFamily286.Regex)
            {
                Check((bool)Property(backing!, SuspensionAck286Names.RegexFreed));
                Check(engine!.Report.CodesCreated == SuspensionAck286Names.CreatedCodeCount && engine.Report.CodesFreed == SuspensionAck286Names.CreatedCodeCount && engine.Report.Active == SuspensionAck286Names.NoActiveRegex);
                await engine.DisposeAsync();
                RegexNative411Access.Empty(engine);
            }
            else if (family == SuspensionAckFamily286.BinaryProgram) Check((bool)Property(backing!, SuspensionAck286Names.BinaryFreed));
            else Check((long)Property(backing!, PreparedLoader807Names.Bytes) == SuspensionAck286Names.NoLoaderBytes && !image!.IsConsumed);
        }
        catch (Exception error)
        {
            original = error;
        }
        finally
        {
            release.TrySetResult();
            try
            {
                host?.Dispose();
            }
            catch (Exception error)
            {
                cleanupFailures.Add(error);
            }
            try
            {
                await runtime.DisposeAsync();
            }
            catch (Exception error)
            {
                cleanupFailures.Add(error);
            }
            try
            {
                if (retired is not null) await retired.WaitAsync(Deadline);
            }
            catch (Exception error)
            {
                cleanupFailures.Add(error);
            }
            try
            {
                if (image is not null) await image.DisposeAsync();
            }
            catch (Exception error)
            {
                cleanupFailures.Add(error);
            }
            GC.KeepAlive(source);
        }
        if (original is not null)
        {
            if (cleanupFailures.Count != 0)
                throw new AggregateException(new[] { original! }.Concat(cleanupFailures));
            ExceptionDispatchInfo.Capture(original).Throw();
        }
        if (cleanupFailures.Count != 0) throw new AggregateException(cleanupFailures);
    }

    private static void FinalizeOpaqueHostEscapes()
    {
        // Same existing AtomicsHost413/CountersHost421 Host contract. Actual
        // guest graph leases have no finalizer and remain independently tested.
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }

    private static object Direct(ProcessRuntime runtime, Term reference, SuspensionAckFamily286 family)
    {
        object value = Static(
            SuspensionAck286Names.Roots[(int)family],
            SuspensionAck286Names.Direct[(int)family],
            reference,
            runtime
        )!;

        return family is SuspensionAckFamily286.Atomics or SuspensionAckFamily286.Counters ? Property(value, SuspensionAck286Names.Array) : value;
    }

    private static void Read(
        ProcessRuntime runtime,
        ResourceReferenceTerm reference,
        object backing,
        SuspensionAckFamily286 family,
        PreparedBeamImage? image
    )
    {
        if (family is SuspensionAckFamily286.Atomics or SuspensionAckFamily286.Counters)
        {
            using var pin = (IDisposable)Call(backing, SuspensionAck286Names.AcquireArray)!;
            Check(pin is not null && (ulong)Property(Property(pin, SuspensionAck286Names.ArrayValue), SuspensionAck286Names.ArrayLength) == SuspensionAck286Names.ArrayElements);
        }
        else if (family == SuspensionAckFamily286.Regex)
        {
            object kind = Enum.Parse(TypeOf(SuspensionAck286Names.RegexLeaseKind), SuspensionAck286Names.RegexGuestLease);
            using var pin = (IDisposable)Call(backing, SuspensionAck286Names.RetainRegex, kind)!;
            Check(((IReadOnlyList<uint>)Call(backing, SuspensionAck286Names.RegexNumbers, pin)!).SequenceEqual(reference.Words));
            Check((nint)Call(backing, SuspensionAck286Names.RegexPointer, pin)! != SuspensionAck286Names.NullNativePointer);
        }
        else if (family == SuspensionAckFamily286.BinaryProgram)
        {
            using var pin = (IDisposable)Call(backing, SuspensionAck286Names.ReadBinary)!;
            object arena = Property(backing, SuspensionAck286Names.BinaryArena);
            Check((int)Property(arena, SuspensionAck286Names.NeedleLength) == SuspensionAck286Names.PatternLength && (byte)Call(arena, SuspensionAck286Names.ReadNeedle, SuspensionAck286Names.NeedleStart)! == SuspensionAck286Names.PatternByte);
        }
        else
        {
            using var pin = runtime.ReferenceResources.AcquirePrepared(reference);
            Check(pin is not null && ReferenceEquals(pin.Value, image) && (long)Property(backing, PreparedLoader807Names.Bytes) > SuspensionAck286Names.NoLoaderBytes);
        }
    }

    private static async Task AwaitMailbox(ProcessContext context)
    {
        object tracker = Property(context.Mailbox, SuspensionAck175Names.Retirements);
        var tasks = new List<Task>();
        lock (Field(tracker, SuspensionAck175Names.Gate))
            foreach (string list in new[] { SuspensionAck175Names.Active, SuspensionAck175Names.Failed })
                foreach (object work in (IEnumerable)Field(tracker, list))
                    tasks.Add((Task)Property(Property(work, SuspensionAck175Names.Completion), SuspensionAck175Names.Task));
        await Task.WhenAll(tasks).WaitAsync(Deadline);
    }

    private static Type TypeOf(string name) => typeof(ProcessRuntime).Assembly.GetType(SuspensionAck286Names.RuntimeNamespace + name, true)!;

    private static object Field(object value, string name) => value.GetType().GetField(name, Members)!.GetValue(value)!;

    private static object? Static(string type, string name, params object?[] arguments)
    {
        try
        {
            return TypeOf(type).GetMethod(name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)!.Invoke(null, arguments);
        }
        catch (TargetInvocationException error) when (error.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(error.InnerException).Throw();
            throw;
        }
    }
}
