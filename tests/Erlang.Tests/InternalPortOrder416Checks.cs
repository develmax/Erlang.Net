using System.Reflection;
using Erlang;
namespace Erlang.Tests;

internal static class InternalPortOrder416Checks
{
    private static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException(InternalPortOrder416Names.Failure);
    }

    private static object Backend(ProcessRuntime runtime) => typeof(ProcessRuntime).GetProperty(BusyPortCancellationHostNames.Ports, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(runtime)!;

    private static void Send(ProcessRuntime runtime, Term port, Term message)
    {
        object backend = Backend(runtime);
        _ = backend.GetType().GetMethod(BusyPortCancellationHostNames.Send)!.Invoke(backend, [port, message]);
    }

    private static Task Commands(ProcessRuntime runtime, Term port)
    {
        object backend = Backend(runtime);

        return ((ValueTask)backend.GetType().GetMethod(BusyPortCancellationHostNames.AwaitCommands)!.Invoke(backend, [port, CancellationToken.None])!).AsTask();
    }

    internal static Task BadFlags() => Run(connect: false);

    internal static Task ConnectOrdering() => Run(connect: true);

    private static async Task Run(bool connect)
    {
        string folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(folder);
        string capture = Path.Combine(folder, BusyPortMonitorHostNames.CaptureLeaf), release = Path.Combine(folder, BusyPortMonitorHostNames.ReleaseLeaf);
        var errors = new List<Exception>();
        ProcessRuntime? runtime = null;
        RuntimeNodeFoundationHostActor? owner = null;
        RuntimeNodeFoundationHostActor? receiver = null;
        System.Diagnostics.Process? child = null;
        try
        {
            runtime = new ProcessRuntime();
            ProcessRuntime current = runtime;
            await current.InitializeModulesAsync(registry => RuntimeBootstrapInternalPorts416.Register(current, registry));
            owner = new RuntimeNodeFoundationHostActor(current);
            receiver = new RuntimeNodeFoundationHostActor(current);
            Pid target = (await receiver.Ready).Self;
            await owner.InvokeAsync(async context =>
            {
                string executable = Path.GetFullPath(
                    Path.Combine(PortHostParameters.RuntimeRoot,
        OperatingSystem.IsWindows() ? PortHostParameters.WindowsHost : PortHostParameters.UnixHost),
                    System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory()
                );
                Term port = await current.Modules.Call(
                    context,
                    InternalPortsOriginal416Names.Internal,
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
                var pid = (TupleTerm)await current.Modules.Call(
                    context,
                    InternalPortsOriginal416Names.Internal,
                    PortNames.Info,
                    port,
                    Term.A(PortNames.OsPid)
                );
                child = System.Diagnostics.Process.GetProcessById((int)((Integer)pid.Items[1]).Value);
                Require(!child.SafeHandle.IsInvalid);
                Term ready = Term.Tuple(port, Term.Tuple(Term.A(PortNames.Data), new BitString(new byte[] { BusyPortMonitorHostNames.ReadyMarker })));
                Require(await context.ReceiveAsync<Term>(m => m.Equals(ready) ? m : null, BusyPortMonitorHostNames.Timeout) is not null);
                _ = await current.Modules.Call(
                    context,
                    InternalPortsOriginal416Names.Internal,
                    PortNames.Command,
                    port,
                    new BitString(new byte[BusyPortMonitorHostNames.PayloadBytes]),
                    Nil.Value
                );
                // Existing backpressure fixture route: a real accepted protocol command,
                // not an injected Task, field assignment, fake cursor or sleep-based race.
                Send(
                    current,
                    port,
                    Term.Tuple(context.Self, Term.Tuple(Term.A(PortNames.ProtocolCommand), new BitString(new byte[] { BusyPortMonitorHostNames.Marker })))
                );
                Require(!Commands(current, port).IsCompleted && !File.Exists(release) && !child.HasExited);
                if (!connect)
                {
                    ValueTask<Term> malformed = current.Modules.Call(
                        context,
                        InternalPortsOriginal416Names.Internal,
                        PortNames.Command,
                        port,
                        new BitString(new byte[] { BusyPortMonitorHostNames.Marker }),
                        Term.A(InternalPortOrder416Names.InvalidFlags)
                    );
                    Require(malformed.IsCompleted);
                    Require((await malformed).Equals(Term.A(PortNames.BadOption)));
                    ValueTask<Term> publicMalformed = current.Modules.Call(
                        context,
                        ErlangModuleNames.Module,
                        PortNames.Command,
                        port,
                        new BitString(new byte[] { BusyPortMonitorHostNames.Marker }),
                        Term.A(InternalPortOrder416Names.InvalidFlags)
                    );
                    Require(publicMalformed.IsCompleted);
                    bool raised = false;
                    try
                    {
                        _ = await publicMalformed;
                    }
                    catch (ErlangException e) when (e.Reason.Equals(Term.A(ErlangErrorReasons.BadArgument)))
                    {
                        raised = true;
                    }
                    Require(raised && !Commands(current, port).IsCompleted && !File.Exists(release));
                    await File.WriteAllBytesAsync(release, []);
                    await Commands(current, port).WaitAsync(BusyPortMonitorHostNames.Timeout);
                    _ = await current.Modules.Call(
                        context,
                        InternalPortsOriginal416Names.Internal,
                        PortNames.Close,
                        port
                    );
                }
                else
                {
                    // Same actual sender queues close after its blocked command, before connect.
                    Send(current, port, Term.Tuple(context.Self, Term.A(PortNames.ProtocolClose)));
                    ValueTask<Term> malformed = current.Modules.Call(
                        context,
                        InternalPortsOriginal416Names.Internal,
                        PortNames.Connect,
                        port,
                        Term.A(InternalPortOrder416Names.InvalidPid)
                    );
                    Require(!malformed.IsCompleted);
                    ValueTask<Term> pending = current.Modules.Call(
                        context,
                        InternalPortsOriginal416Names.Internal,
                        PortNames.Connect,
                        port,
                        target
                    );
                    Require(!pending.IsCompleted && !Commands(current, port).IsCompleted && !File.Exists(release));
                    await File.WriteAllBytesAsync(release, []);
                    // io.c deliberately fails invalid PID in the ordered port operation.
                    Require((await malformed).Equals(Term.A(ErlangErrorReasons.BadArgument)));
                    Require((await pending).Equals(Term.A(ErlangErrorReasons.BadArgument)));
                    await Commands(current, port).WaitAsync(BusyPortMonitorHostNames.Timeout);
                }

                return true;
            });
        }
        catch (Exception error)
        {
            errors.Add(error);
        }
        finally
        {
            try
            {
                await File.WriteAllBytesAsync(release, []);
            }
            catch (Exception error)
            {
                errors.Add(error);
            }
            if (owner is not null) try
                {
                    await owner.DisposeAsync();
                }
                catch (Exception error)
                {
                    errors.Add(error);
                }
            if (receiver is not null) try
                {
                    await receiver.DisposeAsync();
                }
                catch (Exception error)
                {
                    errors.Add(error);
                }
            if (runtime is not null) try
                {
                    await runtime.DisposeAsync();
                }
                catch (Exception error)
                {
                    errors.Add(error);
                }
            if (child is not null)
            {
                try
                {
                    await child.WaitForExitAsync().WaitAsync(BusyPortMonitorHostNames.Timeout);
                    Require(child.ExitCode == 0);
                }
                catch (Exception error)
                {
                    errors.Add(error);
                }
                child.Dispose();
            }
            try
            {
                if (Directory.Exists(folder)) Directory.Delete(folder, true);
            }
            catch (Exception error)
            {
                errors.Add(error);
            }
        }
        if (errors.Count != 0) throw new AggregateException(errors);
    }
}
