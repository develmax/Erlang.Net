namespace Erlang.Tests;

internal static class RuntimeNodeTextActivationHostChecks
{
    internal static async Task Run()
    {
        await RetainedTextIdentifiers();
        await ForeignRegisteredKernel();
    }

    private static async Task RetainedTextIdentifiers()
    {
        await using var runtime = new ProcessRuntime();
        await using var caller = new RuntimeNodeFoundationHostActor(runtime);
        await using var kernel = new RuntimeNodeFoundationHostActor(runtime);
        await kernel.Invoke(context =>
        {
            runtime.Register(RuntimeSetNodeHostNames.Kernel, context.Self);

            return true;
        });
        PortTerm port = (PortTerm)await caller.InvokeAsync(async context => await Call(
            context,
            PortNames.Open,
            Term.Tuple(Term.A(PortDescriptorNames.Kind), Term.I(0), Term.I(1)),
            Term.List(Term.A(PortNames.WriteOnlyOption))
        ));
        try
        {
            Pid pid = (await caller.Ready).Self;
            Pid parsedPid = (Pid)await caller.InvokeAsync(async context => await Call(context, ProcessIdentifierTextNames.ListToPid, await Call(context, ProcessIdentifierTextNames.PidToList, pid)));
            PortTerm parsedPort = (PortTerm)await caller.InvokeAsync(async context => await Call(context, ProcessIdentifierTextNames.ListToPort, await Call(context, ProcessIdentifierTextNames.PortToList, port)));
            Check(parsedPid.Equals(pid) && parsedPort.Equals(port));
            var retained = new Dictionary<Term, Term> { [parsedPid] = Nil.Value, [parsedPort] = Nil.Value };
            await caller.InvokeAsync(async context => await Call(
                context,
                RuntimeSetNodeHostNames.SetNode,
                Term.A(RuntimeSetNodeHostNames.First),
                Term.I(RuntimeSetNodeHostParameters.Requested)
            ));
            Check(parsedPid.Node == runtime.Node && parsedPid.Creation == RuntimeSetNodeHostParameters.MinimumCreation && parsedPid.Equals(pid));
            Check(parsedPort.Node == runtime.Node && parsedPort.Creation == RuntimeSetNodeHostParameters.MinimumCreation && parsedPort.Equals(port));
            Check(retained.ContainsKey(pid) && retained.ContainsKey(port));
            await caller.InvokeAsync(async context =>
            {
                Check((await Call(context, ProcessIdentifierTextNames.ListToPid, await Call(context, ProcessIdentifierTextNames.PidToList, pid))).Equals(pid));
                Check((await Call(context, ProcessIdentifierTextNames.ListToPort, await Call(context, ProcessIdentifierTextNames.PortToList, port))).Equals(port));
                await Call(
                    context,
                    PortNames.SetData,
                    parsedPort,
                    Nil.Value
                );
                Check((await Call(context, PortNames.GetData, port)).Equals(Nil.Value));

                return true;
            });
            await kernel.DisposeAsync();
            Check(parsedPid.Node == ProcessNodeDefaults.Local && parsedPid.Creation == 0 && parsedPid.Equals(pid));
            Check(parsedPort.Node == ProcessNodeDefaults.Local && parsedPort.Creation == 0 && parsedPort.Equals(port));
            Check(retained.ContainsKey(pid) && retained.ContainsKey(port));
        }
        finally
        {
            await caller.InvokeAsync(async context => await Call(context, PortNames.Close, port));
        }
    }

    private static async Task ForeignRegisteredKernel()
    {
        await using var runtime = new ProcessRuntime();
        await using var foreign = new ProcessRuntime();
        await using var caller = new RuntimeNodeFoundationHostActor(runtime);
        await using var foreignCaller = new RuntimeNodeFoundationHostActor(foreign);
        Pid foreignPid = (await foreignCaller.Ready).Self;
        Check(foreignPid.Equals((await caller.Ready).Self));
        Reject(() => runtime.Register(RuntimeSetNodeHostNames.Kernel, foreignPid));
        Reject(() => runtime.TryRegisterStartupName(RuntimeSetNodeHostNames.Kernel, foreignPid, out _));
        Check(runtime.WhereIs(RuntimeSetNodeHostNames.Kernel).Equals(Term.A(ProcessRegistryAtoms.Undefined)));
        await using var kernel = new RuntimeNodeFoundationHostActor(runtime);
        await kernel.Invoke(context =>
        {
            runtime.Register(RuntimeSetNodeHostNames.Kernel, context.Self);

            return true;
        });
        await caller.InvokeAsync(async context => await Call(
            context,
            RuntimeSetNodeHostNames.SetNode,
            Term.A(RuntimeSetNodeHostNames.First),
            Term.I(RuntimeSetNodeHostParameters.Requested)
        ));
        Check(runtime.IsDistributionAlive);
    }

    private static ValueTask<Term> Call(ProcessContext context, string name, params Term[] arguments) => context.Runtime.Modules.Call(
        context,
        ErlangModuleNames.Module,
        name,
        arguments
    );

    private static void Reject(Action action)
    {
        try
        {
            action();
        }
        catch (ErlangException exception) when (exception.Reason.Equals(Term.A(ErlangErrorReasons.BadArgument)))
        {
            return;
        }

        throw new InvalidOperationException(RuntimeSetNodeHostNames.Failure);
    }

    private static void Check(bool value)
    {
        if (!value)
            throw new InvalidOperationException(RuntimeSetNodeHostNames.Failure);
    }
}
