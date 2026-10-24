using System.Reflection;

namespace Erlang.Tests;

public static class BusyPortMonitorHostChecks
{
    public static async Task Configuration()
    {
        await using var runtime = new ProcessRuntime();
        var process = SpawnChecked(
            runtime,
            async context =>
        {
            Term busy = Term.List(Term.A(SystemMonitorNames.BusyPort));
            Term undefined = Term.A(SystemMonitorNames.Undefined);
            Require((await Call(
                runtime,
                context,
                context.Self,
                busy
            )).Equals(undefined));
            Term expected = Term.Tuple(context.Self, busy);
            Require((await Call(runtime, context)).Equals(expected));
            try
            {
                await Call(
                    runtime,
                    context,
                    context.Self,
                    Term.List(Term.A(SystemMonitorNames.BusyPort), Term.I(BusyPortMonitorHostNames.Marker))
                );
                throw new InvalidOperationException(BusyPortMonitorHostNames.Failure);
            }
            catch (ErlangException error) when (error.Reason.Equals(Term.A(ErlangErrorReasons.BadArgument)))
            {
            }
            Require((await Call(runtime, context)).Equals(expected));
            Term off = Term.List(Term.Tuple(Term.A(SystemMonitorNames.BusyPort), CoreModules.Boolean(false)));
            Require((await Call(
                runtime,
                context,
                context.Self,
                off
            )).Equals(expected));
            Require((await Call(runtime, context)).Equals(Term.Tuple(context.Self, Nil.Value)));
            Require((await Call(runtime, context, undefined)).Equals(Term.Tuple(context.Self, Nil.Value)));
            Require((await Call(runtime, context)).Equals(undefined));

            return Nil.Value;
        }
        );
        Require(await Finish(process) is Nil);
    }

    public static async Task Run()
    {
        await Task.WhenAll(Suspension(false), Suspension(true));
    }

    private static async Task Suspension(bool protocolSend)
    {
        string folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(folder);
        string capture = Path.Combine(folder, BusyPortMonitorHostNames.CaptureLeaf);
        string release = Path.Combine(folder, BusyPortMonitorHostNames.ReleaseLeaf);
        System.Diagnostics.Process? child = null;
        try
        {
            await using var runtime = new ProcessRuntime();
            var owner = SpawnChecked(
                runtime,
                async context =>
            {
                string executable = Path.GetFullPath(
                    Path.Combine(
                        PortHostParameters.RuntimeRoot,
                        OperatingSystem.IsWindows() ? PortHostParameters.WindowsHost : PortHostParameters.UnixHost
                    ),
                    System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory()
                );
                Term port = await runtime.Modules.Call(
                    context,
                    ErlangModuleNames.Module,
                    PortNames.Open,
                    Term.Tuple(Term.A(PortNames.SpawnExecutable), Term.String(executable)),
                    Term.List(
                        Term.A(PortNames.Binary),
                        Term.A(PortNames.Hide),
                        Term.Tuple(Term.A(PortNames.Packet), Term.I(BusyPortMonitorHostNames.HeaderBytes)),
                        Term.Tuple(
                            Term.A(PortNames.BusyLimitsPort),
                            Term.Tuple(Term.I(BusyPortMonitorHostNames.LowWatermark), Term.I(BusyPortMonitorHostNames.HighWatermark))
                        ),
                        Term.Tuple(
                            Term.A(PortNames.Args),
                            Term.List(
                                Term.String(Path.GetFullPath(PortHostParameters.Probe)),
                                Term.String(BusyPortMonitorHostNames.HeldDrain),
                                Term.String(capture),
                                Term.String(release)
                            )
                        )
                    )
                );
                var pid = (TupleTerm)await runtime.Modules.Call(
                    context,
                    ErlangModuleNames.Module,
                    PortNames.Info,
                    port,
                    Term.A(PortNames.OsPid)
                );
                child = System.Diagnostics.Process.GetProcessById((int)((Integer)pid.Items[1]).Value);
                // Retain a handle while the owned child is still held; ExitCode requires it.
                Require(!child.SafeHandle.IsInvalid);
                Term ready = Term.Tuple(port, Term.Tuple(Term.A(PortNames.Data), new BitString(new byte[] { BusyPortMonitorHostNames.ReadyMarker })));
                Require(await context.ReceiveAsync<Term>(message => message.Equals(ready) ? message : null, BusyPortMonitorHostNames.Timeout) is not null);
                Require((await Call(
                    runtime,
                    context,
                    context.Self,
                    Term.List(Term.A(SystemMonitorNames.BusyPort))
                )).Equals(Term.A(SystemMonitorNames.Undefined)));
                var immediate = SpawnChecked(
                    runtime,
                    async sending => await runtime.Modules.Call(
                        sending,
                        ErlangModuleNames.Module,
                        PortNames.Command,
                        port,
                        new BitString(new byte[BusyPortMonitorHostNames.PayloadBytes])
                    )
                );
                Require((await Finish(immediate)).Equals(CoreModules.Boolean(true)));
                var refusing = SpawnChecked(
                    runtime,
                    async sending => await runtime.Modules.Call(
                        sending,
                        ErlangModuleNames.Module,
                        PortNames.Command,
                        port,
                        new BitString(new byte[] { BusyPortMonitorHostNames.Marker }),
                        Term.List(Term.A(BusyPortMonitorHostNames.NoSuspend))
                    )
                );
                Require((await Finish(refusing)).Equals(CoreModules.Boolean(false)));

                async ValueTask<Term> Send(ProcessContext sending)
                {
                    if (protocolSend)
                        return await runtime.Modules.Call(
                            sending,
                            ErlangModuleNames.Module,
                            ErlangBasicBifNames.Send,
                            port,
                            Term.Tuple(context.Self, Term.Tuple(Term.A(PortNames.ProtocolCommand), new BitString(new byte[] { BusyPortMonitorHostNames.Marker }))),
                            Nil.Value
                        );

                    return await runtime.Modules.Call(
                        sending,
                        ErlangModuleNames.Module,
                        PortNames.Command,
                        port,
                        new BitString(new byte[] { BusyPortMonitorHostNames.Marker })
                    );
                }
                var unwound = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                var cancelled = runtime.Spawn(async sending =>
                {
                    try
                    {
                        return await Send(sending);
                    }
                    finally
                    {
                        unwound.TrySetResult();
                    }
                });
                Term cancelledNotice = Term.Tuple(
                    Term.A(SystemMonitorNames.Notification),
                    cancelled.Pid,
                    Term.A(SystemMonitorNames.BusyPort),
                    port
                );
                Require((await context.ReceiveAsync<Term>(BusyNotice, BusyPortMonitorHostNames.Timeout))?.Equals(cancelledNotice) == true);
                Require(!cancelled.Completion.IsCompleted);
                runtime.Exit(context, cancelled.Pid, Term.A(ProcessExitSignals.Kill));
                await unwound.Task.WaitAsync(BusyPortMonitorHostNames.Timeout);
                await cancelled.Completion.WaitAsync(BusyPortMonitorHostNames.Timeout);
                var sender = SpawnChecked(runtime, Send);
                Term notice = Term.Tuple(
                    Term.A(SystemMonitorNames.Notification),
                    sender.Handle.Pid,
                    Term.A(SystemMonitorNames.BusyPort),
                    port
                );
                try
                {
                    Require((await context.ReceiveAsync<Term>(BusyNotice, BusyPortMonitorHostNames.Timeout))?.Equals(notice) == true);
                    Require(!sender.Handle.Completion.IsCompleted);
                }
                finally
                {
                    await File.WriteAllBytesAsync(release, []);
                }
                Term sent = await Finish(sender);
                Require(sent.Equals(protocolSend ? Term.A(BusyPortMonitorHostNames.Ok) : CoreModules.Boolean(true)));
                Require(await context.ReceiveAsync<Term>(BusyNotice, TimeSpan.Zero) is null);
                await runtime.Modules.Call(
                    context,
                    ErlangModuleNames.Module,
                    ErlangBasicBifNames.Send,
                    port,
                    Term.Tuple(context.Self, Term.A(PortNames.ProtocolClose))
                );
                Term closed = Term.Tuple(port, Term.A(PortNames.Closed));
                Require(await context.ReceiveAsync<Term>(message => message.Equals(closed) ? message : null, BusyPortMonitorHostNames.Timeout) is not null);

                return Nil.Value;
            }
            );
            try
            {
                Require(await Finish(owner) is Nil);
            }
            finally
            {
                await File.WriteAllBytesAsync(release, []);
            }
            await child!.WaitForExitAsync().WaitAsync(BusyPortMonitorHostNames.Timeout);
            Require(child.ExitCode == 0);
            byte[] observed = await File.ReadAllBytesAsync(capture);
            Require(observed.Length == BusyPortMonitorHostNames.PayloadBytes + 1 + BusyPortMonitorHostNames.HeaderBytes * 2);
            Require(observed[^1] == BusyPortMonitorHostNames.Marker);
        }
        finally
        {
            child?.Dispose();
            if (Directory.Exists(folder))
                Directory.Delete(folder, true);
        }
    }

    public static async Task QueuedCommandCancellation()
    {
        string folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(folder);
        string capture = Path.Combine(folder, BusyPortMonitorHostNames.CaptureLeaf);
        string release = Path.Combine(folder, BusyPortMonitorHostNames.ReleaseLeaf);
        System.Diagnostics.Process? child = null;
        try
        {
            await using var runtime = new ProcessRuntime();
            var owner = SpawnChecked(
                runtime,
                async context =>
            {
                string executable = Path.GetFullPath(
                    Path.Combine(
                        PortHostParameters.RuntimeRoot,
                        OperatingSystem.IsWindows() ? PortHostParameters.WindowsHost : PortHostParameters.UnixHost
                    ),
                    System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory()
                );
                Term port = await runtime.Modules.Call(
                    context,
                    ErlangModuleNames.Module,
                    PortNames.Open,
                    Term.Tuple(Term.A(PortNames.SpawnExecutable), Term.String(executable)),
                    Term.List(
                        Term.A(PortNames.Binary),
                        Term.A(PortNames.Hide),
                        Term.Tuple(Term.A(PortNames.Packet), Term.I(BusyPortMonitorHostNames.HeaderBytes)),
                        Term.Tuple(
                            Term.A(PortNames.BusyLimitsPort),
                            Term.Tuple(Term.I(BusyPortMonitorHostNames.LowWatermark), Term.I(BusyPortMonitorHostNames.HighWatermark))
                        ),
                        Term.Tuple(
                            Term.A(PortNames.Args),
                            Term.List(
                                Term.String(Path.GetFullPath(PortHostParameters.Probe)),
                                Term.String(BusyPortMonitorHostNames.HeldDrain),
                                Term.String(capture),
                                Term.String(release)
                            )
                        )
                    )
                );
                var pid = (TupleTerm)await runtime.Modules.Call(
                    context,
                    ErlangModuleNames.Module,
                    PortNames.Info,
                    port,
                    Term.A(PortNames.OsPid)
                );
                child = System.Diagnostics.Process.GetProcessById((int)((Integer)pid.Items[1]).Value);
                // Retain a handle while the owned child is still held; ExitCode requires it.
                Require(!child.SafeHandle.IsInvalid);
                Term ready = Term.Tuple(port, Term.Tuple(Term.A(PortNames.Data), new BitString(new byte[] { BusyPortMonitorHostNames.ReadyMarker })));
                Require(await context.ReceiveAsync<Term>(message => message.Equals(ready) ? message : null, BusyPortMonitorHostNames.Timeout) is not null);
                Require((await Call(
                    runtime,
                    context,
                    context.Self,
                    Term.List(Term.A(SystemMonitorNames.BusyPort))
                )).Equals(Term.A(SystemMonitorNames.Undefined)));
                var immediate = SpawnChecked(
                    runtime,
                    async sending => await runtime.Modules.Call(
                        sending,
                        ErlangModuleNames.Module,
                        PortNames.Command,
                        port,
                        new BitString(new byte[BusyPortMonitorHostNames.PayloadBytes])
                    )
                );
                Require((await Finish(immediate)).Equals(CoreModules.Boolean(true)));
                // Host fixture queues an accepted protocol command behind the held write.
                QueueProtocolCommand(
                    runtime,
                    port,
                    Term.Tuple(context.Self, Term.Tuple(
                    Term.A(PortNames.ProtocolCommand), new BitString(new byte[] { BusyPortMonitorHostNames.Marker })
                ))
                );
                Require(!PriorCommands(runtime, port).IsCompleted);
                var waiting = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                var unwound = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                var cancelled = runtime.Spawn(async sending =>
                {
                    try
                    {
                        ValueTask<Term> command = runtime.Modules.Call(
                            sending,
                            ErlangModuleNames.Module,
                            PortNames.Command,
                            port,
                            new BitString(new byte[] { BusyPortCancellationHostNames.CancelledMarker })
                        );
                        Require(!command.IsCompleted);
                        waiting.TrySetResult();

                        return await command;
                    }
                    finally
                    {
                        unwound.TrySetResult();
                    }
                });
                await waiting.Task.WaitAsync(BusyPortMonitorHostNames.Timeout);
                Require(!cancelled.Completion.IsCompleted);
                runtime.Exit(context, cancelled.Pid, Term.A(ProcessExitSignals.Kill));
                await unwound.Task.WaitAsync(BusyPortMonitorHostNames.Timeout);
                Require((await cancelled.Completion.WaitAsync(BusyPortMonitorHostNames.Timeout))
                    .Equals(Term.A(ProcessExitReasons.Killed)));
                Require(!File.Exists(release));
                Require(!child.HasExited);
                Require(!PriorCommands(runtime, port).IsCompleted);
                await File.WriteAllBytesAsync(release, []);
                await PriorCommands(runtime, port).WaitAsync(BusyPortMonitorHostNames.Timeout);
                await runtime.Modules.Call(
                    context,
                    ErlangModuleNames.Module,
                    ErlangBasicBifNames.Send,
                    port,
                    Term.Tuple(context.Self, Term.A(PortNames.ProtocolClose))
                );
                Term closed = Term.Tuple(port, Term.A(PortNames.Closed));
                Require(await context.ReceiveAsync<Term>(message => message.Equals(closed) ? message : null, BusyPortMonitorHostNames.Timeout) is not null);

                return Nil.Value;
            }
            );
            try
            {
                Require(await Finish(owner) is Nil);
            }
            finally
            {
                await File.WriteAllBytesAsync(release, []);
            }
            await child!.WaitForExitAsync().WaitAsync(BusyPortMonitorHostNames.Timeout);
            Require(child.ExitCode == 0);
            byte[] observed = await File.ReadAllBytesAsync(capture);
            Require(observed.Length == BusyPortMonitorHostNames.PayloadBytes + 1 + BusyPortMonitorHostNames.HeaderBytes * 2);
            Require(observed[^1] == BusyPortMonitorHostNames.Marker);
        }
        finally
        {
            child?.Dispose();
            if (Directory.Exists(folder))
                Directory.Delete(folder, true);
        }
    }

    public static Task QueuedInfoCancellation() => QueuedOperationCancellation(PortNames.Info, false);

    public static Task QueuedInfoItemCancellation() => QueuedOperationCancellation(PortNames.Info, true);

    public static Task QueuedCloseCancellation() => QueuedOperationCancellation(PortNames.Close, false);

    private static async Task QueuedOperationCancellation(string operation, bool infoItem)
    {
        string folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(folder);
        string capture = Path.Combine(folder, BusyPortMonitorHostNames.CaptureLeaf);
        string release = Path.Combine(folder, BusyPortMonitorHostNames.ReleaseLeaf);
        System.Diagnostics.Process? child = null;
        try
        {
            await using var runtime = new ProcessRuntime();
            var owner = SpawnChecked(
                runtime,
                async context =>
            {
                string executable = Path.GetFullPath(
                    Path.Combine(
                        PortHostParameters.RuntimeRoot,
                        OperatingSystem.IsWindows() ? PortHostParameters.WindowsHost : PortHostParameters.UnixHost
                    ),
                    System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory()
                );
                Term port = await runtime.Modules.Call(
                    context,
                    ErlangModuleNames.Module,
                    PortNames.Open,
                    Term.Tuple(Term.A(PortNames.SpawnExecutable), Term.String(executable)),
                    Term.List(
                        Term.A(PortNames.Binary),
                        Term.A(PortNames.Hide),
                        Term.Tuple(Term.A(PortNames.Packet), Term.I(BusyPortMonitorHostNames.HeaderBytes)),
                        Term.Tuple(
                            Term.A(PortNames.BusyLimitsPort),
                            Term.Tuple(Term.I(BusyPortMonitorHostNames.LowWatermark), Term.I(BusyPortMonitorHostNames.HighWatermark))
                        ),
                        Term.Tuple(
                            Term.A(PortNames.Args),
                            Term.List(
                                Term.String(Path.GetFullPath(PortHostParameters.Probe)),
                                Term.String(BusyPortMonitorHostNames.HeldDrain),
                                Term.String(capture),
                                Term.String(release)
                            )
                        )
                    )
                );
                var pid = (TupleTerm)await runtime.Modules.Call(
                    context,
                    ErlangModuleNames.Module,
                    PortNames.Info,
                    port,
                    Term.A(PortNames.OsPid)
                );
                child = System.Diagnostics.Process.GetProcessById((int)((Integer)pid.Items[1]).Value);
                // Retain a handle while the owned child is still held; ExitCode requires it.
                Require(!child.SafeHandle.IsInvalid);
                Term ready = Term.Tuple(port, Term.Tuple(Term.A(PortNames.Data), new BitString(new byte[] { BusyPortMonitorHostNames.ReadyMarker })));
                Require(await context.ReceiveAsync<Term>(message => message.Equals(ready) ? message : null, BusyPortMonitorHostNames.Timeout) is not null);
                Require((await Call(
                    runtime,
                    context,
                    context.Self,
                    Term.List(Term.A(SystemMonitorNames.BusyPort))
                )).Equals(Term.A(SystemMonitorNames.Undefined)));
                var immediate = SpawnChecked(
                    runtime,
                    async sending => await runtime.Modules.Call(
                        sending,
                        ErlangModuleNames.Module,
                        PortNames.Command,
                        port,
                        new BitString(new byte[BusyPortMonitorHostNames.PayloadBytes])
                    )
                );
                Require((await Finish(immediate)).Equals(CoreModules.Boolean(true)));
                // Host fixture queues an accepted protocol command behind the held write.
                QueueProtocolCommand(
                    runtime,
                    port,
                    Term.Tuple(context.Self, Term.Tuple(
                    Term.A(PortNames.ProtocolCommand), new BitString(new byte[] { BusyPortMonitorHostNames.Marker })
                ))
                );
                Require(!PriorCommands(runtime, port).IsCompleted);
                var waiting = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                var unwound = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                var cancelled = runtime.Spawn(async sending =>
                {
                    try
                    {
                        ValueTask<Term> command = infoItem
                            ? runtime.Modules.Call(
                                sending,
                                ErlangModuleNames.Module,
                                operation,
                                port,
                                Term.A(PortNames.OsPid)
                            )
                            : runtime.Modules.Call(
                                sending,
                                ErlangModuleNames.Module,
                                operation,
                                port
                            );
                        Require(!command.IsCompleted);
                        waiting.TrySetResult();

                        return await command;
                    }
                    finally
                    {
                        unwound.TrySetResult();
                    }
                });
                await waiting.Task.WaitAsync(BusyPortMonitorHostNames.Timeout);
                Require(!cancelled.Completion.IsCompleted);
                runtime.Exit(context, cancelled.Pid, Term.A(ProcessExitSignals.Kill));
                await unwound.Task.WaitAsync(BusyPortMonitorHostNames.Timeout);
                Require((await cancelled.Completion.WaitAsync(BusyPortMonitorHostNames.Timeout))
                    .Equals(Term.A(ProcessExitReasons.Killed)));
                Require(!File.Exists(release));
                Require(!child.HasExited);
                Require(!PriorCommands(runtime, port).IsCompleted);
                await File.WriteAllBytesAsync(release, []);
                await PriorCommands(runtime, port).WaitAsync(BusyPortMonitorHostNames.Timeout);
                if (operation == PortNames.Close)
                {
                    await child.WaitForExitAsync().WaitAsync(BusyPortMonitorHostNames.Timeout);
                    Require((await runtime.Modules.Call(
                        context,
                        ErlangModuleNames.Module,
                        PortNames.Info,
                        port
                    ))
                        .Equals(Term.A(SystemMonitorNames.Undefined)));
                }
                else
                {
                    await runtime.Modules.Call(
                        context,
                        ErlangModuleNames.Module,
                        ErlangBasicBifNames.Send,
                        port,
                        Term.Tuple(context.Self, Term.A(PortNames.ProtocolClose))
                    );
                    Term closed = Term.Tuple(port, Term.A(PortNames.Closed));
                    Require(await context.ReceiveAsync<Term>(message => message.Equals(closed) ? message : null, BusyPortMonitorHostNames.Timeout) is not null);
                }

                return Nil.Value;
            }
            );
            try
            {
                Require(await Finish(owner) is Nil);
            }
            finally
            {
                await File.WriteAllBytesAsync(release, []);
            }
            await child!.WaitForExitAsync().WaitAsync(BusyPortMonitorHostNames.Timeout);
            Require(child.ExitCode == 0);
            byte[] observed = await File.ReadAllBytesAsync(capture);
            Require(observed.Length == BusyPortMonitorHostNames.PayloadBytes + 1 + BusyPortMonitorHostNames.HeaderBytes * 2);
            Require(observed[^1] == BusyPortMonitorHostNames.Marker);
        }
        finally
        {
            child?.Dispose();
            if (Directory.Exists(folder))
                Directory.Delete(folder, true);
        }
    }

    private static object PortBackend(ProcessRuntime runtime) => typeof(ProcessRuntime)
        .GetProperty(BusyPortCancellationHostNames.Ports, BindingFlags.Instance | BindingFlags.NonPublic)!
        .GetValue(runtime)!;

    private static void QueueProtocolCommand(ProcessRuntime runtime, Term identity, Term message)
    {
        object ports = PortBackend(runtime);
        _ = ports.GetType().GetMethod(BusyPortCancellationHostNames.Send)!.Invoke(ports, [identity, message, null]);
    }

    private static Task PriorCommands(ProcessRuntime runtime, Term identity)
    {
        object ports = PortBackend(runtime);

        return ((ValueTask)ports.GetType().GetMethod(BusyPortCancellationHostNames.AwaitCommands)!
            .Invoke(ports, [identity, CancellationToken.None, null])!).AsTask();
    }

    // Runtime completion reports the exit reason; preserve guest delegate results separately.
    private static (ProcessHandle Handle, Task<Term> Result) SpawnChecked(
        ProcessRuntime runtime,
        Func<ProcessContext, ValueTask<Term>> body
    )
    {
        var result = new TaskCompletionSource<Term>(TaskCreationOptions.RunContinuationsAsynchronously);
        ProcessHandle handle = runtime.Spawn(async context =>
        {
            try
            {
                Term value = await body(context);
                result.TrySetResult(value);

                return value;
            }
            catch (Exception error)
            {
                result.TrySetException(error);
                throw;
            }
        });

        return (handle, result.Task);
    }

    private static async Task<Term> Finish((ProcessHandle Handle, Task<Term> Result) actor)
    {
        Term value = await actor.Result.WaitAsync(BusyPortMonitorHostNames.Timeout);
        Term reason = await actor.Handle.Completion.WaitAsync(BusyPortMonitorHostNames.Timeout);
        Require(reason.Equals(Term.A(ProcessExitReasons.Normal)));

        return value;
    }

    private static Term? BusyNotice(Term message) =>
        message is TupleTerm { Items: [Atom { Name: SystemMonitorNames.Notification }, _, Atom { Name: SystemMonitorNames.BusyPort }, _] } ? message : null;

    private static ValueTask<Term> Call(ProcessRuntime runtime, ProcessContext context, params Term[] arguments) =>
        runtime.Modules.Call(
            context,
            ErlangModuleNames.Module,
            SystemMonitorNames.Function,
            arguments
        );

    private static void Require(bool value)
    {
        if (!value)
            throw new InvalidOperationException(BusyPortMonitorHostNames.Failure);
    }
}
