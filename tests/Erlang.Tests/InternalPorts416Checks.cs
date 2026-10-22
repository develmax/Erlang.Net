namespace Erlang.Tests;

internal static class InternalPorts416Checks
{
    private static void Require(bool condition)
    {
        if (!condition) throw new InvalidOperationException(InternalPorts416Names.Failure);
    }

    internal static async Task Admission()
    {
        await using var runtime = new ProcessRuntime();
        await using var foreign = new ProcessRuntime();
        await runtime.InitializeModulesAsync(registry =>
        {
            RuntimeBootstrapInternalPorts416.Register(runtime, registry);
            int count = registry.Exports.Count();
            RuntimeBootstrapInternalPorts416.Register(runtime, registry);
            Require(registry.Exports.Count() == count);
            foreach (var signature in InternalPortsOriginal416Names.NativeSignatures)
                Require(registry.HasExport(InternalPortsOriginal416Names.Internal, signature.Name, signature.Arity));
        });
        await foreign.InitializeModulesAsync(_ => { });
        int before = foreign.Modules.Exports.Count();
        bool refused = false;
        try
        {
            RuntimeBootstrapInternalPorts416.Register(runtime, foreign.Modules);
        }
        catch (ArgumentException)
        {
            refused = true;
        }
        Require(refused && foreign.Modules.Exports.Count() == before);
        await using var actor = new RuntimeNodeFoundationHostActor(foreign);
        await actor.InvokeAsync(async context =>
        {
            bool rejected = false;
            try
            {
                _ = await runtime.Modules.Call(
                    context,
                    InternalPortsOriginal416Names.Internal,
                    PortNames.Info,
                    Nil.Value
                );
            }
            catch (ArgumentException)
            {
                rejected = true;
            }
            Require(rejected);

            return true;
        });
        await using var conflict = new ProcessRuntime();
        await conflict.InitializeModulesAsync(registry =>
        {
            registry.Register(
                InternalPortsOriginal416Names.Internal,
                PortNames.Info,
                1,
                (_, _) => ValueTask.FromResult<Term>(Nil.Value)
            );
            bool rejected = false;
            try
            {
                RuntimeBootstrapInternalPorts416.Register(conflict, registry);
            }
            catch (InvalidOperationException)
            {
                rejected = true;
            }
            Require(rejected && registry.HasExport(InternalPortsOriginal416Names.Internal, PortNames.Info, 1));
            foreach (var signature in InternalPortsOriginal416Names.NativeSignatures)
                if (signature != (PortNames.Info, 1))
                    Require(!registry.HasExport(InternalPortsOriginal416Names.Internal, signature.Name, signature.Arity));
        });
    }

    internal static async Task Live()
    {
        await using var runtime = new ProcessRuntime();
        await runtime.InitializeModulesAsync(registry => RuntimeBootstrapInternalPorts416.Register(runtime, registry));
        await using var owner = new RuntimeNodeFoundationHostActor(runtime);
        await using var next = new RuntimeNodeFoundationHostActor(runtime);
        ProcessContext receiver = await next.Ready;
        string executable = Path.GetFullPath(
            Path.Combine(PortHostParameters.RuntimeRoot,
            OperatingSystem.IsWindows() ? PortHostParameters.WindowsHost : PortHostParameters.UnixHost),
            System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory()
        );
        PortTerm port = await owner.InvokeAsync(async context => (PortTerm)await runtime.Modules.Call(
            context,
            InternalPortsOriginal416Names.Internal,
            PortNames.Open,
            Term.Tuple(Term.A(PortNames.SpawnExecutable), Text(executable)),
            Term.List(
                Term.A(PortNames.Binary),
                Term.Tuple(Term.A(PortNames.Args), Term.List(Text(Path.GetFullPath(PortHostParameters.Probe))))
            )
        ));
        var osPid = (TupleTerm)await owner.InvokeAsync(async context => await runtime.Modules.Call(
            context,
            InternalPortsOriginal416Names.Internal,
            PortNames.Info,
            port,
            Term.A(PortNames.OsPid)
        ));
        using var child = System.Diagnostics.Process.GetProcessById((int)((Integer)osPid.Items[1]).Value);
        Require(!child.HasExited);
        Term status = await owner.InvokeAsync(async context => await runtime.Modules.Call(
            context,
            InternalPortsOriginal416Names.Internal,
            PortNames.Command,
            port,
            new BitString(InternalPorts416Names.First),
            Nil.Value
        ));
        Require(status.Equals(Term.A(ErlangBooleanAtoms.True)));
        await owner.InvokeAsync(async context =>
        {
            Term expected = Term.Tuple(port, Term.Tuple(Term.A(PortNames.Data), new BitString(InternalPorts416Names.First)));
            Require(expected.Equals(await context.ReceiveAsync<Term>(value => value.Equals(expected) ? value : null, InternalPorts416Names.Timeout)));

            return true;
        });
        status = await owner.InvokeAsync(async context => await runtime.Modules.Call(
            context,
            InternalPortsOriginal416Names.Internal,
            PortNames.Connect,
            port,
            receiver.Self
        ));
        Require(status.Equals(Term.A(ErlangBooleanAtoms.True)));
        Term connected = await owner.InvokeAsync(async context => await runtime.Modules.Call(
            context,
            InternalPortsOriginal416Names.Internal,
            PortNames.Info,
            port,
            Term.A(PortNames.Connected)
        ));
        Require(connected.Equals(Term.Tuple(Term.A(PortNames.Connected), receiver.Self)));
        Term all = await owner.InvokeAsync(async context => await runtime.Modules.Call(
            context,
            InternalPortsOriginal416Names.Internal,
            PortNames.Info,
            port
        ));
        Require(Cons.Items(all).Any(item => item.Equals(connected)));
        status = await next.InvokeAsync(async context => await runtime.Modules.Call(
            context,
            InternalPortsOriginal416Names.Internal,
            PortNames.Command,
            port,
            new BitString(InternalPorts416Names.Second),
            Nil.Value
        ));
        Require(status.Equals(Term.A(ErlangBooleanAtoms.True)));
        await next.InvokeAsync(async context =>
        {
            Term expected = Term.Tuple(port, Term.Tuple(Term.A(PortNames.Data), new BitString(InternalPorts416Names.Second)));
            Require(expected.Equals(await context.ReceiveAsync<Term>(value => value.Equals(expected) ? value : null, InternalPorts416Names.Timeout)));

            return true;
        });
        status = await owner.InvokeAsync(async context => await runtime.Modules.Call(
            context,
            InternalPortsOriginal416Names.Internal,
            PortNames.Close,
            port
        ));
        Require(status.Equals(Term.A(ErlangBooleanAtoms.True)));
        await child.WaitForExitAsync().WaitAsync(InternalPorts416Names.Timeout);
        Require(child.HasExited);
        Term missing = await next.InvokeAsync(async context => await runtime.Modules.Call(
            context,
            InternalPortsOriginal416Names.Internal,
            PortNames.Info,
            port
        ));
        Require(missing.Equals(Term.A(ProcessRegistryAtoms.Undefined)));
    }

    private static Term Text(string value) => Cons.From(value.Select(character => Term.I(character)));
}
