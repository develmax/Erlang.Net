using System.Diagnostics;
using System.IO.Pipes;
using System.Security.Cryptography;

namespace Erlang;

// One actual operating-system child owns one VM's native driver namespace.
// Callbacks/port data never move to another runtime's Windows DLL statics.
internal sealed class ErlDriverHostChild420 : IAsyncDisposable
{
    private readonly Process process;
    private readonly NamedPipeServerStream control;
    private readonly NamedPipeServerStream events;
    private readonly ErlDriverHostLaunchOptions420 options;
    private readonly TaskCompletionSource<bool> launched = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource<bool> retire = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource prepared = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly Task retirement;
    private readonly Task processObserver;
    private readonly TaskCompletionSource physicalExit423 = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly Task physicalCleanup423;
    private readonly TaskCompletionSource physicalAccessEnded423 = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private bool startAttempted423;
    private bool startReturnedNoProcess423;
    private readonly string session;
    private ErlDriverHostPeer420? peer;
    private ErlDriverHostDiagnostics420? output;
    private ErlDriverHostDiagnostics420? error;
    private Task outputDrain = Task.CompletedTask;
    private Task errorDrain = Task.CompletedTask;

    private ErlDriverHostChild420(
        Process process,
        NamedPipeServerStream control,
        NamedPipeServerStream events,
        ErlDriverHostLaunchOptions420 options,
        string session
    )
    {
        this.process = process;
        this.control = control;
        this.events = events;
        this.options = options;
        this.session = session;
        bool suppress = !ExecutionContext.IsFlowSuppressed();
        AsyncFlowControl flow = default;
        try
        {
            if (suppress) flow = ExecutionContext.SuppressFlow();
            retirement = RetireAsync();
            processObserver = ObserveExitAsync();
            physicalCleanup423 = CompletePhysicalCleanup423();
        }
        finally
        {
            if (suppress) flow.Undo();
        }
    }

    internal ErlDriverHostPeer420 Peer => peer ?? throw new InvalidOperationException(ErlDriverHostNames420.Closed);
    internal Task Completion => retirement;
    // Cleanup errors never complete this fence. It is successful only after an
    // actual owned-process exit or a proven no-process start outcome.
    internal Task PhysicalExit => physicalExit423.Task;
    internal Task PhysicalCleanup => physicalCleanup423;
    internal string StandardOutput => retirement.IsCompleted ? output?.Text ?? string.Empty : string.Empty;
    internal string StandardError => retirement.IsCompleted ? error?.Text ?? string.Empty : string.Empty;

    internal static async ValueTask<ErlDriverHostChild420> StartAsync(
        ErlDriverHostPackage420 package,
        ErlDriverHostLaunchOptions420 options,
        Func<ErlDriverHostFrame420, CancellationToken, ValueTask<ErlDriverHostFrame420>> onRequest,
        Func<ErlDriverHostFrame420, CancellationToken, ValueTask> onEvent,
        CancellationToken cancellation
    )
    {
        cancellation.ThrowIfCancellationRequested();
        if (options.StartupTimeout <= TimeSpan.Zero || options.ShutdownTimeout <= TimeSpan.Zero
            || options.MaximumFrameBytes < ErlDriverHostNames420.MinimumFrameBytes)
            throw new ArgumentOutOfRangeException(nameof(options));
        package.Verify();
        string session = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        string stem = ErlDriverHostNames420.PipePrefix + session;
        NamedPipeServerStream? control = null;
        NamedPipeServerStream? events = null;
        Process? process = null;
        ErlDriverHostChild420? owner = null;
        try
        {
            control = CreatePipe(stem + ErlDriverHostNames420.ControlSuffix);
            events = CreatePipe(stem + ErlDriverHostNames420.EventsSuffix);
            var start = new ProcessStartInfo(package.EntryPoint)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = Path.GetDirectoryName(package.EntryPoint)!
            };
            start.ArgumentList.Add(ErlDriverHostNames420.ControlArgument);
            start.ArgumentList.Add(stem + ErlDriverHostNames420.ControlSuffix);
            start.ArgumentList.Add(ErlDriverHostNames420.EventsArgument);
            start.ArgumentList.Add(stem + ErlDriverHostNames420.EventsSuffix);
            start.ArgumentList.Add(ErlDriverHostNames420.SessionArgument);
            start.ArgumentList.Add(session);
            start.ArgumentList.Add(ErlDriverHostNames420.MaximumFrameArgument);
            start.ArgumentList.Add(options.MaximumFrameBytes.ToString(System.Globalization.CultureInfo.InvariantCulture));
            start.ArgumentList.Add(ErlDriverHostNames420.ParentArgument);
            start.ArgumentList.Add(Environment.ProcessId.ToString(System.Globalization.CultureInfo.InvariantCulture));
            process = new Process { StartInfo = start };
            owner = new(
                process,
                control,
                events,
                options,
                session
            );
            owner.startAttempted423 = true;
            if (!process.Start())
            {
                owner.startReturnedNoProcess423 = true;
                throw new IOException(ErlDriverHostNames420.Closed);
            }
            owner.launched.TrySetResult(true);
            owner.output = new(process.StandardOutput);
            owner.error = new(process.StandardError);
            bool suppress = !ExecutionContext.IsFlowSuppressed();
            AsyncFlowControl flow = default;
            try
            {
                if (suppress) flow = ExecutionContext.SuppressFlow();
                owner.outputDrain = owner.output.DrainAsync();
                owner.errorDrain = owner.error.DrainAsync();
            }
            finally
            {
                if (suppress) flow.Undo();
            }
            using var startup = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
            startup.CancelAfter(options.StartupTimeout);
            Task connected = Task.WhenAll(control.WaitForConnectionAsync(startup.Token), events.WaitForConnectionAsync(startup.Token));
            Task first = await Task.WhenAny(connected, owner.processObserver).ConfigureAwait(false);
            if (first != connected) startup.Cancel();
            await connected.ConfigureAwait(false);
            ErlDriverHostPipeIdentity420.RequireChild(control, process.Id);
            ErlDriverHostPipeIdentity420.RequireChild(events, process.Id);
            cancellation.ThrowIfCancellationRequested();
            owner.peer = new(
                new(control, session, options.MaximumFrameBytes),
                new(events, session, options.MaximumFrameBytes),
                session,
                false,
                onRequest,
                onEvent
            );
            owner.prepared.TrySetResult();

            return owner;
        }
        catch (Exception failure)
        {
            if (owner is not null)
            {
                owner.launched.TrySetResult(false);
                owner.prepared.TrySetResult();
                owner.retire.TrySetResult(false);
                try
                {
                    await owner.retirement.ConfigureAwait(false);
                }
                catch (Exception cleanup)
                {
                    if (!owner.PhysicalExit.IsCompletedSuccessfully)
                        throw new ErlDriverHostStartupFailure423(owner, failure, cleanup);
                    throw new AggregateException(failure, cleanup);
                }
                if (!owner.PhysicalExit.IsCompletedSuccessfully)
                    throw new ErlDriverHostStartupFailure423(owner, failure,
                        new IOException(ErlDriverPhysicalExitNames423.Unsettled));
            }
            else
            {
                var failures = new List<Exception> { failure };
                try
                {
                    control?.Dispose();
                }
                catch (Exception cleanup)
                {
                    failures.Add(cleanup);
                }
                try
                {
                    events?.Dispose();
                }
                catch (Exception cleanup)
                {
                    failures.Add(cleanup);
                }
                try
                {
                    process?.Dispose();
                }
                catch (Exception cleanup)
                {
                    failures.Add(cleanup);
                }
                if (failures.Count > 1) throw new AggregateException(failures);
            }
            throw;
        }
    }

    private static NamedPipeServerStream CreatePipe(string name) => new(
        name,
        PipeDirection.InOut,
        1,
        PipeTransmissionMode.Byte,
        PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly
    );

    private async Task ObserveExitAsync()
    {
        if (!await launched.Task.ConfigureAwait(false))
        {
            // An exception from inside Process.Start is not automatically proof
            // that no process was created. Keep that unknown outcome explicit.
            if (!startAttempted423 || startReturnedNoProcess423) physicalExit423.TrySetResult();

            return;
        }
        try
        {
            await process.WaitForExitAsync().ConfigureAwait(false);
            physicalExit423.TrySetResult();
        }
        finally
        {
            retire.TrySetResult(false);
        }
    }

    private async Task CompletePhysicalCleanup423()
    {
        await physicalExit423.Task.ConfigureAwait(false);
        await physicalAccessEnded423.Task.ConfigureAwait(false);
        await prepared.Task.ConfigureAwait(false);
        var failures = new List<Exception>();
        try
        {
            await processObserver.ConfigureAwait(false);
        }
        catch (Exception failure)
        {
            failures.Add(failure);
        }
        try
        {
            await outputDrain.ConfigureAwait(false);
        }
        catch (Exception failure)
        {
            failures.Add(failure);
        }
        try
        {
            await errorDrain.ConfigureAwait(false);
        }
        catch (Exception failure)
        {
            failures.Add(failure);
        }
        try
        {
            process.Dispose();
        }
        catch (Exception failure)
        {
            failures.Add(failure);
        }
        if (failures.Count != 0) throw new AggregateException(failures);
    }

    private async Task RetireAsync()
    {
        bool graceful = await retire.Task.ConfigureAwait(false);
        bool started = await launched.Task.ConfigureAwait(false);
        await prepared.Task.ConfigureAwait(false);
        var failures = new List<Exception>();
        if (started && !graceful && peer is not null)
            failures.Add(new IOException(ErlDriverHostNames420.UnexpectedExit));
        if (started && graceful && peer is not null)
        {
            using var timeout = new CancellationTokenSource(options.ShutdownTimeout);
            try
            {
                await peer.RequestAsync(
                    ErlDriverHostOperation420.Shutdown,
                    0,
                    0,
                    0,
                    0,
                    null,
                    null,
                    timeout.Token
                )
                    .AsTask().WaitAsync(timeout.Token).ConfigureAwait(false);
                await peer.EventsDrained.WaitAsync(timeout.Token).ConfigureAwait(false);
                // The child has acknowledged genuine native cleanup. Closing
                // both pipes is the final parent receipt before its graceful exit.
                await peer.DisposeAsync().AsTask().WaitAsync(timeout.Token).ConfigureAwait(false);
                await process.WaitForExitAsync(timeout.Token).ConfigureAwait(false);
                physicalExit423.TrySetResult();
                if (process.ExitCode != 0) throw new IOException(ErlDriverHostNames420.UnexpectedExit);
            }
            catch (Exception failure)
            {
                failures.Add(failure);
            }
        }
        // Stop transport first, then force a noncooperative/faulted native
        // child to exit BEFORE joining managed callbacks which may await it.
        if (peer is not null)
            try
            {
                await peer.AbortAsync(new ObjectDisposedException(ErlDriverHostNames420.Closed)).ConfigureAwait(false);
            }
            catch (Exception failure)
            {
                failures.Add(failure);
            }
        if (started)
        {
            try
            {
                if (!process.HasExited) process.Kill(entireProcessTree: true);
            }
            catch (Exception failure)
            {
                failures.Add(failure);
            }
            try
            {
                using var physicalWait = new CancellationTokenSource(options.ShutdownTimeout);
                await process.WaitForExitAsync(physicalWait.Token).ConfigureAwait(false);
                physicalExit423.TrySetResult();
            }
            catch (Exception failure)
            {
                failures.Add(failure);
                try
                {
                    if (process.HasExited) physicalExit423.TrySetResult();
                }
                catch (Exception confirmation)
                {
                    failures.Add(confirmation);
                }
            }
        }
        if (peer is not null)
            try
            {
                await peer.DisposeAsync().ConfigureAwait(false);
            }
            catch (Exception failure)
            {
                failures.Add(failure);
            }
        try
        {
            control.Dispose();
        }
        catch (Exception failure)
        {
            failures.Add(failure);
        }
        try
        {
            events.Dispose();
        }
        catch (Exception failure)
        {
            failures.Add(failure);
        }
        // No retirement path may inspect the Process object after this fence;
        // its prepared physical cleanup can now dispose the actual handle.
        physicalAccessEnded423.TrySetResult();
        if (physicalExit423.Task.IsCompletedSuccessfully)
        {
            try
            {
                await physicalCleanup423.ConfigureAwait(false);
            }
            catch (Exception failure)
            {
                failures.Add(failure);
            }
        }
        else failures.Add(new IOException(ErlDriverPhysicalExitNames423.Unsettled));
        if (failures.Count != 0) throw new AggregateException(failures);
    }

    public ValueTask DisposeAsync()
    {
        retire.TrySetResult(true);

        return new(retirement);
    }
}
