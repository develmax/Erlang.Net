namespace Erlang.Tests;

public static class PortDrainHostChecks
{
    public static async Task Run()
    {
        string path = Path.GetFullPath(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), PortDrainHostParameters.Filename));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        System.Diagnostics.Process? child = null;
        try
        {
            await using var runtime = new ProcessRuntime();
            Exception? failure = null;
            var process = runtime.Spawn(async context =>
            {
                try
                {
                    string executable = Path.GetFullPath(
                        Path.Combine(PortHostParameters.RuntimeRoot, OperatingSystem.IsWindows() ? PortHostParameters.WindowsHost : PortHostParameters.UnixHost),
                        System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory()
                    );
                    Term port = await runtime.Modules.Call(
                        context,
                        ErlangModuleNames.Module,
                        PortNames.Open,
                        Term.Tuple(Term.A(PortNames.SpawnExecutable), Text(executable)),
                        Term.List(
                            Term.A(PortNames.Binary),
                            Term.A(PortNames.Hide),
                            Term.Tuple(Term.A(PortNames.Packet), Term.I(PortDrainHostParameters.HeaderBytes)),
                            Term.Tuple(
                                Term.A(PortNames.BusyLimitsPort),
                                Term.Tuple(Term.I(PortDrainHostParameters.LowWatermark), Term.I(PortDrainHostParameters.HighWatermark))
                            ),
                            Term.Tuple(
                                Term.A(PortNames.Args),
                                Term.List(Text(Path.GetFullPath(PortHostParameters.Probe)), Text(PortDrainHostParameters.ProbeMode), Text(path))
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
                    await context.ReceiveAsync<Term>(message => message, PortHostParameters.Timeout);
                    await runtime.Modules.Call(
                        context,
                        ErlangModuleNames.Module,
                        PortNames.Command,
                        port,
                        new BitString(new byte[PortDrainHostParameters.PayloadBytes])
                    );
                    await runtime.Modules.Call(
                        context,
                        ErlangModuleNames.Module,
                        ErlangBasicBifNames.Send,
                        port,
                        Term.Tuple(context.Self, Term.A(PortNames.ProtocolClose))
                    );
                    Term closed = Term.Tuple(port, Term.A(PortNames.Closed));
                    Term? reply = await context.ReceiveAsync<Term>(message => message.Equals(closed) ? message : null, PortHostParameters.Timeout);
                    if (reply is null)
                        throw new InvalidOperationException(PortDrainHostParameters.Failure);
                }
                catch (Exception exception)
                {
                    failure = exception;
                }

                return Term.I(0);
            });
            await process.Completion.WaitAsync(PortHostParameters.Timeout);
            if (failure is not null)
                throw failure;
            await child!.WaitForExitAsync().WaitAsync(PortHostParameters.Timeout);
            byte[] bytes = await File.ReadAllBytesAsync(path);
            if (bytes.Length == PortDrainHostParameters.PayloadBytes + PortDrainHostParameters.HeaderBytes &&
                bytes.AsSpan(0, PortDrainHostParameters.HeaderBytes).SequenceEqual(new byte[] { 0, 16, 0, 0 }) &&
                bytes.Skip(PortDrainHostParameters.HeaderBytes).All(value => value == 0))
                return;

            throw new InvalidOperationException(PortDrainHostParameters.Failure);
        }
        finally
        {
            child?.Dispose();
            if (File.Exists(path))
                File.Delete(path);
            Directory.Delete(Path.GetDirectoryName(path)!);
        }
    }

    private static Term Text(string text) => Cons.From(text.Select(character => Term.I(character)));
}
