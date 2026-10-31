namespace Erlang.Tests;

public static class PortHostChecks
{
    public static async Task Run()
    {
        Require(ErlangSourceWrapperSignatures.Contains(PortNames.Open, 2));
        Require(!ErlangSourceWrapperSignatures.Contains(PortNames.Open, 3));
        Require(ErlangSourceWrapperSignatures.Contains(PortNames.Control, 3));
        Require(!ErlangSourceWrapperSignatures.Contains(PortNames.Control, 2));
        await using var runtime = new ProcessRuntime();
        Exception? failure = null;
        PortTerm? identity = null;
        var owner = runtime.Spawn(async context =>
        {
            try
            {
                await VerifyIo(context, 0);
                string executable = Path.GetFullPath(
                    Path.Combine(PortHostParameters.RuntimeRoot, OperatingSystem.IsWindows() ? PortHostParameters.WindowsHost : PortHostParameters.UnixHost),
                    System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory()
                );
                identity = (PortTerm)await runtime.Modules.Call(
                    context,
                    ErlangModuleNames.Module,
                    PortNames.Open,
                    Term.Tuple(Term.A(PortNames.SpawnExecutable), Text(executable)),
                    Term.List(
                        Term.A(PortNames.Binary),
                        Term.Tuple(Term.A(PortNames.Args), Term.List(Text(Path.GetFullPath(PortHostParameters.Probe))))
                    )
                );
                var info = (TupleTerm)await runtime.Modules.Call(
                    context,
                    ErlangModuleNames.Module,
                    PortNames.Info,
                    identity,
                    Term.A(PortNames.OsPid)
                );
                int pid = (int)((Integer)info.Items[1]).Value;
                using var child = System.Diagnostics.Process.GetProcessById(pid);
                Require(!child.HasExited);
                await VerifyIo(context, 0);
                await runtime.Modules.Call(
                    context,
                    ErlangModuleNames.Module,
                    PortNames.Command,
                    identity,
                    new BitString(new byte[] { 1, 2, 3 })
                );
                Term? reply = await context.Mailbox.ReceiveAsync<Term>(message => message, PortHostParameters.Timeout, context.Cancellation);
                Require(reply is TupleTerm { Items.Count: 2 } tuple && tuple.Items[0].Equals(identity) && tuple.Items[1].Equals(Term.Tuple(Term.A(PortNames.Data), new BitString(new byte[] { 1, 2, 3 }))));
                await VerifyIo(context, PortHostParameters.EchoPayloadBytes);
            }
            catch (Exception exception)
            {
                failure = exception;
            }

            return Term.I(0);
        });
        await owner.Completion.WaitAsync(PortHostParameters.Timeout);
        if (failure is not null)
            throw failure;
        Term? observed = null;
        var observer = runtime.Spawn(async context =>
        {
            await VerifyIo(context, PortHostParameters.EchoPayloadBytes);
            observed = await runtime.Modules.Call(
                context,
                ErlangModuleNames.Module,
                PortNames.Info,
                identity!
            );

            return Term.I(0);
        });
        await observer.Completion.WaitAsync(PortHostParameters.Timeout);
        Require(observed!.Equals(Term.A(ProcessRegistryAtoms.Undefined)));
    }

    private static async ValueTask VerifyIo(ProcessContext context, int bytes)
    {
        Term sample = await context.Runtime.Modules.Call(
            context,
            ErlangModuleNames.Module,
            RuntimeStatisticsNames.Function,
            Term.A(RuntimeStatisticsNames.Io)
        );
        Require(sample.Equals(Term.Tuple(
            Term.Tuple(Term.A(PortHostParameters.StatisticsInput), Term.I(bytes)),
            Term.Tuple(Term.A(PortHostParameters.StatisticsOutput), Term.I(bytes))
        )));
    }

    private static Term Text(string text) => Cons.From(text.Select(character => Term.I(character)));

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(PortHostParameters.Failure);
    }
}
