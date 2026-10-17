using System.Diagnostics;
using System.Reflection;

namespace Erlang.Tests;

internal static class HaltPortHostChecks
{
    public static async Task Run()
    {
        await OrderedDrain();
        await ImmediateDiscard();
        await Deadline();
        await ImmediatePreemptsOrderly();
        await HeldOwnerCancellation(false);
        await HeldOwnerCancellation(true);
    }

    public static async Task ImmediateEcho()
    {
        await using var fixture = await Open(false);
        await Call(
            fixture,
            HaltPortHostNames.Profile,
            fixture.Handle.Pid,
            Term.List(Term.A(HaltPortHostNames.Timestamp))
        );
        var payload = new BitString(new byte[] { HaltPortHostNames.EchoByte });
        await Call(
            fixture,
            PortNames.Command,
            fixture.Port,
            payload
        );
        Term expected = Term.Tuple(fixture.Port, Term.Tuple(Term.A(PortNames.Data), payload));
        Require((await fixture.Context.ReceiveAsync<Term>(value => value.Equals(expected) ? value : null, HaltPortHostNames.Timeout))!.Equals(expected));
        Require((await fixture.Runtime.RequestImmediateHalt(HaltPortHostNames.Status)).Status == HaltPortHostNames.Status);
        await fixture.Runtime.HaltQuiescence.WaitAsync(HaltPortHostNames.Timeout);
        await fixture.Child.WaitForExitAsync().WaitAsync(HaltPortHostNames.Timeout);
        Require(!fixture.Runtime.IsAlive(fixture.Handle.Pid));
        Require(fixture.Child.HasExited);
        RequireProfileCleared(fixture.Runtime);
    }

    private static async Task OrderedDrain()
    {
        await using var fixture = await Open(true);
        await Call(
            fixture,
            HaltPortHostNames.Profile,
            fixture.Port,
            Term.List(Term.A(HaltPortHostNames.Timestamp))
        );
        byte[] expected = await Queue(fixture);
        Task<RuntimeHaltResult> selected = fixture.Runtime.RequestHalt(HaltPortHostNames.Status);
        Require(!selected.IsCompleted);
        Require((await selected.WaitAsync(HaltPortHostNames.Timeout)).Status == HaltPortHostNames.Status);
        await fixture.Runtime.HaltQuiescence.WaitAsync(HaltPortHostNames.Timeout);
        Require((await File.ReadAllBytesAsync(fixture.Capture!)).SequenceEqual(expected));
        Require(fixture.Child.HasExited);
        RequireProfileCleared(fixture.Runtime);
    }

    private static async Task ImmediateDiscard()
    {
        await using var fixture = await Open(true);
        byte[] expected = await Queue(fixture);
        Require((await fixture.Runtime.RequestImmediateHalt(HaltPortHostNames.Status)).Status == HaltPortHostNames.Status);
        Require(!fixture.Runtime.HaltQuiescence.IsCompleted);
        await fixture.Runtime.HaltQuiescence.WaitAsync(HaltPortHostNames.Timeout);
        Require((await File.ReadAllBytesAsync(fixture.Capture!)).Length < expected.Length);
        Require(fixture.Child.HasExited);
    }

    private static async Task Deadline()
    {
        await using var fixture = await Open(true);
        _ = await Queue(fixture);
        Require((await fixture.Runtime.RequestHalt(HaltPortHostNames.Status, new RuntimeHaltOptions(true, HaltPortHostNames.TimeoutMilliseconds)).WaitAsync(HaltPortHostNames.Timeout)).Status == HaltPortHostNames.TimeoutStatus);
        Require(!fixture.Runtime.HaltQuiescence.IsCompleted);
        await fixture.Runtime.HaltQuiescence.WaitAsync(HaltPortHostNames.Timeout);
        Require(fixture.Child.HasExited);
    }

    private static async Task ImmediatePreemptsOrderly()
    {
        await using var fixture = await Open(true);
        _ = await Queue(fixture);
        Task<RuntimeHaltResult> orderly = fixture.Runtime.RequestHalt(HaltPortHostNames.Status);
        Require(!orderly.IsCompleted);
        Require((await fixture.Runtime.RequestImmediateHalt(HaltPortHostNames.ImmediateStatus)).Status == HaltPortHostNames.ImmediateStatus);
        Require((await orderly).Status == HaltPortHostNames.ImmediateStatus);
        await fixture.Runtime.HaltQuiescence.WaitAsync(HaltPortHostNames.Timeout);
        Require(fixture.Child.HasExited);
    }

    private static async Task HeldOwnerCancellation(bool deadline)
    {
        await using var fixture = await Open(true, held: true);
        ProcessContext? cooperativeContext = null;
        ProcessContext? callbackContext = null;
        var cooperativeReady = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var cooperativeStopped = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var callbackReady = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var callbackEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseCallback = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        ProcessHandle cooperative = fixture.Runtime.Spawn(async context =>
        {
            cooperativeContext = context;
            DiagnosticGate.Emit("cooperative-ready", context);
            cooperativeReady.SetResult();
            try
            {
                await Task.Delay(Timeout.Infinite, context.Cancellation);
            }
            catch (OperationCanceledException) when (context.Cancellation.IsCancellationRequested)
            {
                DiagnosticGate.Emit("cooperative-cancel-observed", context);
                cooperativeStopped.SetResult();
            }

            return Nil.Value;
        });
        ProcessHandle callback = fixture.Runtime.Spawn(async context =>
        {
            callbackContext = context;
            DiagnosticGate.Emit("callback-ready", context);
            using var registration = context.Cancellation.Register(() =>
            {
                DiagnosticGate.Emit("cancellation-callback-entered", context);
                callbackEntered.SetResult();
                releaseCallback.Task.GetAwaiter().GetResult();
                DiagnosticGate.Emit("cancellation-callback-released", context);
            });
            callbackReady.SetResult();
            try
            {
                await Task.Delay(Timeout.Infinite, context.Cancellation);
            }
            catch (OperationCanceledException) when (context.Cancellation.IsCancellationRequested)
            {
            }

            DiagnosticGate.Emit("callback-body-before-registration-dispose", context);
            return Nil.Value;
        });
        try
        {
            await Task.WhenAll(cooperativeReady.Task, callbackReady.Task).WaitAsync(HaltPortHostNames.Timeout);
            byte[] expected = await Queue(fixture);
            Task<RuntimeHaltResult> selected = fixture.Runtime.RequestHalt(
                HaltPortHostNames.Status,
                new RuntimeHaltOptions(true, deadline ? HaltPortHostNames.TimeoutMilliseconds : null)
            );
            try
            {
            await Task.WhenAll(cooperativeStopped.Task, callbackEntered.Task).WaitAsync(HaltPortHostNames.Timeout);
            }
            catch
            {
                DiagnosticGate.Snapshot(cooperativeStopped.Task, callbackEntered.Task, cooperative.Completion, callback.Completion,
                    cooperativeContext, callbackContext);
                throw;
            }
            DiagnosticGate.Snapshot(cooperativeStopped.Task, callbackEntered.Task, cooperative.Completion, callback.Completion,
                cooperativeContext, callbackContext);
            Require(!File.Exists(fixture.Release!));
            Require(!fixture.Runtime.HaltQuiescence.IsCompleted);
            if (deadline)
                Require((await selected.WaitAsync(HaltPortHostNames.Timeout)).Status == HaltPortHostNames.TimeoutStatus);
            else
                Require(!selected.IsCompleted);
            fixture.ReleaseChild();
            if (!deadline)
            {
                Require((await selected.WaitAsync(HaltPortHostNames.Timeout)).Status == HaltPortHostNames.Status);
                Require((await File.ReadAllBytesAsync(fixture.Capture!)).SequenceEqual(expected));
            }
            Require(!fixture.Runtime.HaltQuiescence.IsCompleted);
        }
        finally
        {
            fixture.ReleaseChild();
            DiagnosticGate.Snapshot(cooperativeStopped.Task, callbackEntered.Task, cooperative.Completion, callback.Completion,
                cooperativeContext, callbackContext);
            releaseCallback.TrySetResult();
        }
        await fixture.Runtime.HaltQuiescence.WaitAsync(HaltPortHostNames.Timeout);
        Require(fixture.Child.HasExited);
    }

    private static async Task<byte[]> Queue(HaltPortFixture fixture)
    {
        byte[] payload = Enumerable.Range(0, HaltPortHostNames.PayloadBytes).Select(index => (byte)(index % HaltPortHostNames.PayloadPattern)).ToArray();
        Require((await Call(
            fixture,
            PortNames.Command,
            fixture.Port,
            new BitString(payload)
        )).Equals(Term.A(ErlangBooleanAtoms.True)));
        var queue = (TupleTerm)await Call(
            fixture,
            PortNames.Info,
            fixture.Port,
            Term.A(PortNames.QueueSize)
        );
        Require(((Integer)queue.Items[1]).Value > 0);

        return HaltPortHostNames.ExpectedHeader.Concat(payload).ToArray();
    }

    private static async Task<HaltPortFixture> Open(bool capture, bool held = false)
    {
        var runtime = new ProcessRuntime();
        HaltPortFixture? fixture = null;
        Process? child = null;
        string? file = null;
        string? release = null;
        try
        {
            var ready = new TaskCompletionSource<ProcessContext>(TaskCreationOptions.RunContinuationsAsynchronously);
            var hold = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            ProcessHandle handle = runtime.Spawn(async context =>
            {
                ready.SetResult(context);
                await hold.Task.WaitAsync(context.Cancellation);

                return Nil.Value;
            });
            ProcessContext owner = await ready.Task.WaitAsync(HaltPortHostNames.Timeout);
            string executable = Path.GetFullPath(
                Path.Combine(PortHostParameters.RuntimeRoot, OperatingSystem.IsWindows() ? PortHostParameters.WindowsHost : PortHostParameters.UnixHost),
                System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory()
            );
            var arguments = new List<Term> { Text(Path.GetFullPath(PortHostParameters.Probe)) };
            if (capture)
            {
                Directory.CreateDirectory(HaltPortHostNames.FileDirectory);
                file = Path.GetFullPath(Path.Combine(HaltPortHostNames.FileDirectory, Guid.NewGuid().ToString("N")));
                arguments.Add(Text(held ? HaltPortHostNames.HeldDrainFile : HaltPortHostNames.DrainFile));
                arguments.Add(Text(file));
                if (held)
                {
                    release = file + HaltPortHostNames.ReleaseSuffix;
                    arguments.Add(Text(release));
                }
            }
            PortTerm port = (PortTerm)await runtime.Modules.Call(
                owner,
                ErlangModuleNames.Module,
                PortNames.Open,
                Term.Tuple(Term.A(PortNames.SpawnExecutable), Text(executable)),
                Term.List(
                    Term.A(PortNames.Binary),
                    Term.Tuple(Term.A(PortNames.Packet), Term.I(HaltPortHostNames.PacketBytes)),
                    Term.Tuple(Term.A(PortNames.Args), Cons.From(arguments))
                )
            );
            var info = (TupleTerm)await runtime.Modules.Call(
                owner,
                ErlangModuleNames.Module,
                PortNames.Info,
                port,
                Term.A(PortNames.OsPid)
            );
            child = Process.GetProcessById((int)((Integer)info.Items[1]).Value);
            fixture = new HaltPortFixture(
                runtime,
                handle,
                owner,
                port,
                child,
                file,
                release
            );
            if (capture)
            {
                Term expected = Term.Tuple(port, Term.Tuple(Term.A(PortNames.Data), new BitString(HaltPortHostNames.Ready)));
                Require((await owner.ReceiveAsync<Term>(value => value.Equals(expected) ? value : null, HaltPortHostNames.Timeout))!.Equals(expected));
            }

            return fixture;
        }
        catch
        {
            if (fixture is not null)
                await fixture.DisposeAsync();
            else
            {
                if (release is not null)
                    File.WriteAllText(release, string.Empty);
                await runtime.DisposeAsync();
                child?.Dispose();
                if (file is not null)
                    File.Delete(file);
                if (release is not null)
                    File.Delete(release);
            }
            throw;
        }
    }

    private static ValueTask<Term> Call(HaltPortFixture fixture, string function, params Term[] arguments) => fixture.Runtime.Modules.Call(
        fixture.Context,
        ErlangModuleNames.Module,
        function,
        arguments
    );

    private static Term Text(string value) => Cons.From(value.Select(character => Term.I(character)));

    private static void RequireProfileCleared(ProcessRuntime runtime)
    {
        object profiler = typeof(ProcessRuntime).GetProperty(HaltPortHostNames.ProfilingOwner, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(runtime)!;
        Require(((Term)profiler.GetType().GetMethod(HaltPortHostNames.Get)!.Invoke(profiler, null)!).Equals(Term.A(HaltPortHostNames.Undefined)));
    }

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(HaltPortHostNames.Failure);
    }
}
