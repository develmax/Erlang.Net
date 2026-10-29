using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using static Erlang.Tests.NativeResource524Access;
using static Erlang.Tests.RegexLifecycle599Names;

namespace Erlang.Tests;

internal static class RegexLifecycle599Checks
{
    internal static async Task PublishedRollbackRefused()
    {
        var failures = new List<Exception>();
        ProcessRuntime? runtime = null;
        RuntimeNodeFoundationHostActor? actor = null;
        RuntimeRegexEngine411? engine = null;
        try
        {
            runtime = OwnedGuest506RuntimeFactory.Create();
            engine = await RuntimeNativePrimitiveProviders.RegisterRegexAsync411(runtime, runtime.Modules, RegexNative411Access.Admission());
            bool refused = false;
            try
            {
                await ((ValueTask)Call(engine, FailedDispose)!);
            }
            catch (InvalidOperationException)
            {
                refused = true;
            }
            Require(refused && !engine.Report.Closed && engine.Report.CodesCreated == 0);
            actor = new(runtime);
            RuntimeRegexEngine411 actual = engine;
            Term version = await actor.Invoke(context => actual.Version(context));
            Require(version is BitString { BitLength: > 0 });
        }
        catch (Exception failure)
        {
            failures.Add(failure);
        }
        finally
        {
            if (actor is not null)
                try
                {
                    await actor.DisposeAsync().AsTask().WaitAsync(RegexCustody597Names.Deadline);
                }
                catch (Exception failure)
                {
                    failures.Add(failure);
                }
            if (runtime is not null)
                try
                {
                    await runtime.DisposeAsync().AsTask().WaitAsync(RegexCustody597Names.Deadline);
                }
                catch (Exception failure)
                {
                    failures.Add(failure);
                }
            if (engine is not null)
                try
                {
                    RegexNative411Access.Empty(engine);
                }
                catch (Exception failure)
                {
                    failures.Add(failure);
                }
        }
        Finish(failures);
    }

    internal static async Task FailedInstallRollback()
    {
        var failures = new List<Exception>();
        ProcessRuntime? runtime = null;
        RuntimeNodeFoundationHostActor? actor = null;
        RuntimeRegexEngine411? unpublished = null;
        try
        {
            runtime = OwnedGuest506RuntimeFactory.Create();
            actor = new(runtime);
            ProcessRuntime actual = runtime;
            // Real no-code engine opened and closed on its owning actor. Public
            // self-dispose must still refuse; the narrow creation rollback must work.
            await actor.InvokeAsync(async context =>
            {
                unpublished = (RuntimeRegexEngine411)InvokeStatic(
                    typeof(RuntimeRegexEngine411),
                    EngineOpen,
                    actual,
                    RegexNative411Access.Admission()
                )!;
                bool publicRefused = false;
                try
                {
                    await unpublished.DisposeAsync();
                }
                catch (InvalidOperationException)
                {
                    publicRefused = true;
                }
                Require(publicRefused && !unpublished.Report.Closed);
                await ((ValueTask)Call(unpublished, FailedDispose)!).ConfigureAwait(false);
                RegexNative411Access.Empty(unpublished);

                return Term.A(ErlangBooleanAtoms.True);
            });
            // Exercise the actual public installer catch after a successful Open.
            // This public conflict reliably fails provider publication, not the
            // earlier argument/admission checks, and must preserve that failure.
            runtime.Modules.Register(
                ReModule,
                VersionFunction,
                0,
                static (_, _) => ValueTask.FromResult<Term>(Nil.Value)
            );
            string displaced = (string)typeof(ProcessRuntime).Assembly.GetType(NativeNamesType, true)!
                .GetField(DisplacedField, Members)!.GetRawConstantValue()!;
            await actor.InvokeAsync(async context =>
            {
                bool originalPreserved = false;
                try
                {
                    _ = await RuntimeNativePrimitiveProviders.RegisterRegexAsync411(actual, actual.Modules, RegexNative411Access.Admission());
                }
                catch (InvalidOperationException error) when (error.Message == displaced)
                {
                    originalPreserved = true;
                }
                Require(originalPreserved);
                Require(typeof(ProcessRuntime).GetField(RuntimeEngine, Members)!.GetValue(actual) is null
                    && typeof(ProcessRuntime).GetField(RuntimeInstalling, Members)!.GetValue(actual) is null);

                return Term.A(ErlangBooleanAtoms.True);
            });
        }
        catch (Exception failure)
        {
            failures.Add(failure);
        }
        finally
        {
            if (unpublished is not null)
                try
                {
                    await unpublished.DisposeAsync().AsTask().WaitAsync(RegexCustody597Names.Deadline);
                }
                catch (Exception failure)
                {
                    failures.Add(failure);
                }
            if (actor is not null)
                try
                {
                    await actor.DisposeAsync().AsTask().WaitAsync(RegexCustody597Names.Deadline);
                }
                catch (Exception failure)
                {
                    failures.Add(failure);
                }
            if (runtime is not null)
                try
                {
                    await runtime.DisposeAsync().AsTask().WaitAsync(RegexCustody597Names.Deadline);
                }
                catch (Exception failure)
                {
                    failures.Add(failure);
                }
        }
        Finish(failures);
    }

    internal static Task HostReaderHandoff() => RegexCustody597Fixture.Run(async fixture =>
    {
        var failures = new List<Exception>();
        IDisposable? host = null;
        object? pinnedCode = null;
        try
        {
            var exported = (ResourceReferenceTerm)InvokeStatic(
                typeof(ProcessRuntime).Assembly.GetType(RootsType, true)!,
                HostCopy,
                fixture.Reference,
                null
            )!;
            host = (IDisposable)Property(exported, RegexCustody597Names.HostField);
            Term pattern = Pattern(fixture, exported);
            fixture.Release();
            pinnedCode = await fixture.Actor!.Invoke(context => Call(
                fixture.Engine!,
                Authenticate,
                context,
                pattern,
                true
            )!);
            host.Dispose();
            host = null;
            Require(!((Task)Property(fixture.Backing!, RegexCustody597Names.Retired)).IsCompleted);
            // Consume the real PCRE code through the acquired reader, after the
            // only Host carrier and Producer are gone. No CLR collection proof.
            uint actual = (uint)Call(
                fixture.Engine!,
                PatternInfo,
                pinnedCode,
                CaptureCount
            )!;
            Require(actual == (uint)Property(fixture.Code, Captures));
            Call(fixture.Engine!, EndRead, pinnedCode);
            pinnedCode = null;
            await ((Task)Property(fixture.Backing!, RegexCustody597Names.Retired)).WaitAsync(RegexCustody597Names.Deadline);
            Require((bool)Property(fixture.Backing!, RegexCustody597Names.Freed));
            GC.KeepAlive(pattern);
            GC.KeepAlive(exported);
        }
        catch (Exception failure)
        {
            failures.Add(failure);
        }
        finally
        {
            if (pinnedCode is not null)
                try
                {
                    Call(fixture.Engine!, EndRead, pinnedCode);
                }
                catch (Exception failure)
                {
                    failures.Add(failure);
                }
            try
            {
                host?.Dispose();
            }
            catch (Exception failure)
            {
                failures.Add(failure);
            }
        }
        Finish(failures);
    });

    internal static async Task AdmissionOrders()
    {
        await RegexCustody597Fixture.Run(async fixture =>
        {
            object frame = fixture.Frame();
            int slot = (int)Call(frame, RegexCustody597Names.Reserve)!;
            Call(
                frame,
                RegexCustody597Names.Set,
                slot,
                fixture.Reference
            );
            var header = (ReferenceTerm)Call(frame, RegexCustody597Names.Read, slot)!;
            fixture.Release();
            bool refused = false;
            try
            {
                await fixture.Engine!.DisposeAsync();
            }
            catch (InvalidOperationException)
            {
                refused = true;
            }
            Require(refused && !fixture.Engine!.Report.Closed);
            Term result = await fixture.Actor!.InvokeAsync(async context => await fixture.Engine!.RunAsync(
                context,
                Term.String(RegexCustody597Names.Word),
                Pattern(fixture, header),
                Nil.Value
            ));
            Require(result.Equals(RegexNative411Access.Match(0, 1)));
            Call(frame, RegexCustody597Names.Clear, slot);
            Call(fixture.Heap, RegexCustody597Names.Collect);
            await ((Task)Property(fixture.Backing!, RegexCustody597Names.Retired)).WaitAsync(RegexCustody597Names.Deadline);
        });
        await RegexCustody597Fixture.Run(async fixture =>
        {
            var failures = new List<Exception>();
            IDisposable? accidentallyAdmitted = null;
            Task? closing = null;
            try
            {
                object backing = fixture.Backing!;
                object owner = backing.GetType().GetField(BackingOwner, Members)!.GetValue(backing)!;
                object gate = owner.GetType().GetField(OwnerGate, Members)!.GetValue(owner)!;
                bool rejected = false;
                // Prevent the later owner-close worker from reaching CloseVm.
                // On old source, the public Guest pre-scan has already returned
                // but Retain(Guest) does not consult this owner decision: RED.
                // On corrected source it observes the latched admission: GREEN.
                lock (gate)
                {
                    closing = fixture.Engine!.DisposeAsync().AsTask();
                    object kind = Enum.Parse(typeof(ProcessRuntime).Assembly.GetType(LeaseKindType, true)!, GuestKind);
                    try
                    {
                        accidentallyAdmitted = (IDisposable)Call(backing, Retain, kind)!;
                    }
                    catch (ObjectDisposedException)
                    {
                        rejected = true;
                    }
                    nint address = (nint)backing.GetType().GetField(Memory, Members)!.GetValue(backing)!;
                    Require(address != 0 && fixture.Engine.Report.Closed);
                    Require(rejected && accidentallyAdmitted is null && Marshal.ReadInt64(address, RefCountOffset) == 0);
                }
            }
            catch (Exception failure)
            {
                failures.Add(failure);
            }
            finally
            {
                try
                {
                    accidentallyAdmitted?.Dispose();
                }
                catch (Exception failure)
                {
                    failures.Add(failure);
                }
                try
                {
                    fixture.Release();
                }
                catch (Exception failure)
                {
                    failures.Add(failure);
                }
                if (closing is not null)
                    try
                    {
                        await closing.WaitAsync(RegexCustody597Names.Deadline);
                    }
                    catch (Exception failure)
                    {
                        failures.Add(failure);
                    }
            }
            try
            {
                Require((bool)Property(fixture.Backing!, RegexCustody597Names.Freed));
                RegexNative411Access.Empty(fixture.Engine!);
            }
            catch (Exception failure)
            {
                failures.Add(failure);
            }
            Finish(failures);
        });
    }

    private static Term Pattern(RegexCustody597Fixture fixture, ReferenceTerm reference)
        => Term.Tuple(
            Term.A(PatternTag),
            new Integer((uint)Property(fixture.Code, Captures)),
            Term.I((byte)Property(fixture.Code, UnicodeMetadata)),
            Term.I((bool)Property(fixture.Code, Crlf) ? 1 : 0),
            reference
        );

    internal static object? InvokeStatic(Type type, string method, params object?[] args)
    {
        try
        {
            return type.GetMethod(method, Members)!.Invoke(null, args);
        }
        catch (TargetInvocationException failure) when (failure.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(failure.InnerException).Throw();
            throw;
        }
    }

    private static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException(Failure);
    }

    private static void Finish(List<Exception> failures)
    {
        if (failures.Count == 1) ExceptionDispatchInfo.Capture(failures[0]).Throw();
        if (failures.Count > 1) throw new AggregateException(CleanupFailure, failures);
    }
}
