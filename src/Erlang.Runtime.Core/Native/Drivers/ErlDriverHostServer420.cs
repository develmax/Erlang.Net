using System.Diagnostics;
using System.Globalization;
using System.IO.Pipes;

namespace Erlang;

internal static class ErlDriverHostServer420
{
    internal static async Task<int> RunAsync(string[] arguments)
    {
        if (!OperatingSystem.IsWindows()) return 1;
        ErlDriverHostPeer420? peer = null;
        ErlDriverHostEngine420? engine = null;
        var peerReady = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var initialized = new SemaphoreSlim(1, 1);
        var shutdown = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Task nativeFailureObserver = Task.CompletedTask;
        try
        {
            var options = new Dictionary<string, string>(StringComparer.Ordinal);
            if (arguments.Length % 2 != 0) throw new InvalidDataException(ErlDriverHostNames420.InvalidFrame);
            for (int index = 0; index < arguments.Length; index += 2) options.Add(arguments[index], arguments[index + 1]);
            if (options.Count != 5) throw new InvalidDataException(ErlDriverHostNames420.InvalidFrame);
            string session = options[ErlDriverHostNames420.SessionArgument];
            if (session.Length != 64 || !session.All(character => char.IsAsciiHexDigit(character)))
                throw new InvalidDataException(ErlDriverHostNames420.InvalidFrame);
            int parentId = int.Parse(options[ErlDriverHostNames420.ParentArgument], CultureInfo.InvariantCulture);
            int maximumFrame = int.Parse(options[ErlDriverHostNames420.MaximumFrameArgument], CultureInfo.InvariantCulture);
            if (maximumFrame < ErlDriverHostNames420.MinimumFrameBytes || parentId <= 0 || parentId == Environment.ProcessId)
                throw new InvalidDataException(ErlDriverHostNames420.InvalidFrame);
            using var parent = Process.GetProcessById(parentId);
            var configuration = ErlDriverServerConfiguration420.Read();
            using var control = new NamedPipeClientStream(
                ".",
                options[ErlDriverHostNames420.ControlArgument],
                PipeDirection.InOut,
                PipeOptions.Asynchronous
            );
            using var events = new NamedPipeClientStream(
                ".",
                options[ErlDriverHostNames420.EventsArgument],
                PipeDirection.InOut,
                PipeOptions.Asynchronous
            );
            using var startup = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            await Task.WhenAll(control.ConnectAsync(startup.Token), events.ConnectAsync(startup.Token)).ConfigureAwait(false);
            ErlDriverHostPipeIdentity420.RequireParent(control, parentId);
            ErlDriverHostPipeIdentity420.RequireParent(events, parentId);
            peer = new(
                new(control, session, maximumFrame),
                new(events, session, maximumFrame),
                session,
                true,
                async (request, cancellation) =>
                {
                    await peerReady.Task.ConfigureAwait(false);
                    if (request.Operation == ErlDriverHostOperation420.Initialize)
                    {
                        await initialized.WaitAsync(cancellation).ConfigureAwait(false);
                        try
                        {
                            if (Volatile.Read(ref engine) is not null || request.Driver != 0 || request.Port != 0 || request.Actor != 0
                                || request.Command != 0 || request.Name is not null || request.SchedulerThread)
                                throw new InvalidDataException(ErlDriverHostNames420.InvalidFrame);
                            var input = ErlDriverPrimitiveWire420.Decode<ErlDriverInitializeRequest420>(request.Data);
                            if (!Path.IsPathFullyQualified(input.StartupDirectory))
                                throw new InvalidDataException(ErlDriverHostNames420.InvalidFrame);
                            Directory.SetCurrentDirectory(input.StartupDirectory);
                            ErlDriverWindowsLoader420.InitializeSearchPath(input.StartupDirectory);
                            var created = await ErlDriverHostEngine420.CreateAsync(peer!, session, configuration.Input(input.SystemInfo)).ConfigureAwait(false);
                            Volatile.Write(ref engine, created);
                            nativeFailureObserver = ObserveFailure(created, peer!, shutdown.Task);

                            return request with { Kind = ErlDriverHostKind420.Response, NativeResult = 0, Name = null, Data = null };
                        }
                        finally
                        {
                            initialized.Release();
                        }
                    }
                    var active = Volatile.Read(ref engine) ?? throw new InvalidDataException(ErlDriverHostNames420.InvalidFrame);
                    var response = await active.Handle(request, cancellation).ConfigureAwait(false);
                    if (request.Operation == ErlDriverHostOperation420.Shutdown)
                    {
                        await peer!.SealEventsAsync().ConfigureAwait(false);
                        shutdown.TrySetResult();
                    }

                    return response;
                },
                static (_, _) => throw new InvalidDataException(ErlDriverHostNames420.InvalidFrame)
            );
            peerReady.TrySetResult();
            Task parentExit = parent.WaitForExitAsync();
            Task first = await Task.WhenAny(peer.Completion, parentExit).ConfigureAwait(false);
            if (first == parentExit) return 1;
            await peer.Completion.ConfigureAwait(false);
            // Parent closes pipes only AFTER receiving the native cleanup reply
            // and all ordered events. EOF before that is a failed host session.
            if (!shutdown.Task.IsCompletedSuccessfully) return 1;
            await peer.DisposeAsync().ConfigureAwait(false);
            await nativeFailureObserver.ConfigureAwait(false);

            return 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine(error);

            return 1;
        }
        // On lost parent/transport do not wait indefinitely for callbacks that
        // need that parent. This dedicated process exits; its OS-owned images
        // and threads die with it. No successful unload/cleanup is advertised.
    }

    private static async Task ObserveFailure(ErlDriverHostEngine420 engine, ErlDriverHostPeer420 peer, Task shutdown)
    {
        var failure = engine.Failure;
        if (await Task.WhenAny(failure, shutdown).ConfigureAwait(false) == failure)
            await peer.AbortAsync(await failure.ConfigureAwait(false)).ConfigureAwait(false);
    }

}
