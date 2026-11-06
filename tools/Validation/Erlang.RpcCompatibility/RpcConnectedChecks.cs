using Erlang.Otp;
using Erlang.Tests;

internal static class RpcConnectedChecks
{
    internal static async Task Run(string repository, string directory, bool managed)
    {
        await using var a = new ConnectedSpawnHostEndpoint();
        await using var b = new ConnectedSpawnHostEndpoint();
        await Install(
            a.Runtime,
            repository,
            directory,
            managed
        );
        await Install(
            b.Runtime,
            repository,
            directory,
            managed
        );
        await a.Initialize(ConnectedSpawnHostNames.NodeA);
        await b.Initialize(ConnectedSpawnHostNames.NodeB);
        ulong flags = ConnectedSpawnHostNames.Flags | RpcCompatibilityLimits.PublishedNodeFlag;
        await a.Connect(b, flags);
        await b.Connect(a, flags);
        using var stop = new CancellationTokenSource();
        Task forward = Pump(a, b, stop.Token);
        Task backward = Pump(b, a, stop.Token);
        var results = new List<string>();

        void Passed(string check)
        {
            results.Add(check);
            Console.WriteLine(check);
        }
        try
        {
            await using var caller = new RuntimeNodeFoundationHostActor(a.Runtime);
            await using var remote = new RuntimeNodeFoundationHostActor(b.Runtime);
            await caller.InvokeAsync(async context => await a.Runtime.Modules.Call(context, "rpc", "start"));
            await remote.InvokeAsync(async context => await b.Runtime.Modules.Call(context, "rpc", "start"));
            await caller.InvokeAsync(async context =>
            {
                async ValueTask<Term> Rpc(string function, params Term[] arguments)
                    => await a.Runtime.Modules.Call(
                        context,
                        "rpc",
                        function,
                        arguments
                    );

                Term node = b.Runtime.NodeAtom;
                Term[] mfa = [node, Term.A("erlang"), Term.A("abs"), Term.List(Term.I(-17))];
                Require((await Rpc("call", mfa)).Equals(Term.I(17)));
                Passed("remote call");
                Require((await Rpc("block_call", mfa)).Equals(Term.I(17)));
                Passed("remote rex block_call");
                Require((await Rpc(
                    "call",
                    node,
                    Term.A("erlang"),
                    Term.A("throw"),
                    Term.List(Term.Tuple(Term.A("EXIT"), Term.A("thrown")))
                ))
                    .Equals(Term.Tuple(Term.A("badrpc"), Term.Tuple(Term.A("EXIT"), Term.A("thrown")))));
                Passed("remote throw EXIT conversion");
                Term key = await Rpc("async_call", mfa);
                Require((await Rpc("yield", key)).Equals(Term.I(17)));
                Passed("remote async/yield");
                Term key2 = await Rpc(
                    "async_call",
                    node,
                    Term.A("rpc_port_cases"),
                    Term.A("delay"),
                    Term.List(Term.I(80))
                );
                Require((await Rpc("nb_yield", key2, Term.I(0))).Equals(Term.A("timeout")));
                Require((await Rpc("nb_yield", key2, Term.A("infinity"))).Equals(Term.Tuple(Term.A("value"), Term.A("ok"))));
                Passed("remote non-abandoning nb_yield timeout");
                Term all = Term.List(
                    a.Runtime.NodeAtom,
                    node,
                    Term.A("absent@localhost"),
                    node
                );
                Require((await Rpc(
                    "multicall",
                    all,
                    Term.A("erlang"),
                    Term.A("abs"),
                    Term.List(Term.I(-17)),
                    Term.I(5000)
                ))
                    .Equals(Term.Tuple(Term.List(Term.I(17), Term.I(17), Term.I(17)), Term.List(Term.A("absent@localhost")))));
                Passed("mixed local/remote/missing duplicate multicall");
                Require((await Rpc(
                    "cast",
                    node,
                    Term.A("erlang"),
                    Term.A("send"),
                    Term.List(context.Self, Term.A("remote_cast"))
                ))
                    .Equals(Term.A("true")));
                Require(await context.ReceiveAsync<Term>(value => value.Equals(Term.A("remote_cast")) ? value : null, TimeSpan.FromSeconds(5)) is not null);
                Passed("remote cast and payload delivery");
                Require((await Rpc(
                    "eval_everywhere",
                    Term.List(node),
                    Term.A("erlang"),
                    Term.A("send"),
                    Term.List(context.Self, Term.A("remote_eval"))
                ))
                    .Equals(Term.A("abcast")));
                Require(await context.ReceiveAsync<Term>(value => value.Equals(Term.A("remote_eval")) ? value : null, TimeSpan.FromSeconds(5)) is not null);
                Passed("remote eval_everywhere");

                return Nil.Value;
            });
            var ready = new TaskCompletionSource<Pid>(TaskCreationOptions.RunContinuationsAsynchronously);
            ProcessHandle echo = b.Runtime.Spawn(async context =>
            {
                b.Runtime.Register("rpc_connected_echo", context.Self);
                ready.TrySetResult(context.Self);
                while (true)
                {
                    Term message = (await context.ReceiveAsync<Term>(value => value))!;
                    if (message is Atom { Name: "stop" })
                    {
                        return Nil.Value;
                    }
                    if (message is TupleTerm { Items: [var from, var value] })
                    {
                        await b.Runtime.Modules.Call(
                            context,
                            "erlang",
                            "send",
                            from,
                            Term.Tuple(Term.A("rpc_connected_echo"), b.Runtime.NodeAtom, value)
                        );
                    }
                }
            });
            Pid pid = await ready.Task.WaitAsync(TimeSpan.FromSeconds(10));
            try
            {
                await caller.InvokeAsync(async context =>
                {
                    async ValueTask<Term> Rpc(string function, params Term[] arguments)
                        => await a.Runtime.Modules.Call(
                            context,
                            "rpc",
                            function,
                            arguments
                        );
                    Term node = b.Runtime.NodeAtom, name = Term.A("rpc_connected_echo");
                    Require((await Rpc(
                        "server_call",
                        node,
                        name,
                        name,
                        Term.A("answer")
                    )).Equals(Term.A("answer")));
                    Passed("remote server_call");
                    Require((await Rpc(
                        "multi_server_call",
                        Term.List(node),
                        name,
                        Term.A("answer")
                    ))
                        .Equals(Term.Tuple(Term.List(Term.A("answer")), Nil.Value)));
                    Passed("remote multi_server_call");
                    Require((await Rpc(
                        "sbcast",
                        Term.List(node),
                        name,
                        Term.A("broadcast")
                    ))
                        .Equals(Term.Tuple(Term.List(node), Nil.Value)));
                    Passed("remote sbcast rex acknowledgement");
                    Require((await Rpc(
                        "sbcast",
                        Term.List(node),
                        Term.A("rpc_absent"),
                        Term.A("broadcast")
                    ))
                        .Equals(Term.Tuple(Nil.Value, Term.List(node))));
                    Passed("remote sbcast missing name");
                    Term foreign = new Pid(b.Runtime.Node, pid.Id, pid.Creation);
                    Require((await Rpc("pinfo", foreign, Term.A("registered_name")))
                        .Equals(Term.Tuple(Term.A("registered_name"), name)));
                    Passed("remote pinfo through blocking rex");

                    return Nil.Value;
                });
            }
            finally
            {
                b.Runtime.Send(echo.Pid, Term.A("stop"));
                await echo.Completion.WaitAsync(TimeSpan.FromSeconds(10));
            }
        }
        finally
        {
            stop.Cancel();
            await Task.WhenAll(forward, backward);
        }
        File.WriteAllText(
            Path.Combine(directory, (managed ? "Managed" : "Beam") + "-connected.json"),
            System.Text.Json.JsonSerializer.Serialize(new { Passed = results.Count, Checks = results }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true })
        );
        Console.WriteLine($"Connected RPC: {results.Count} checks passed");
    }

    private static async Task Install(
        ProcessRuntime runtime,
        string repository,
        string directory,
        bool managed
    )
    {
        await runtime.InitializeModulesAsync(registry =>
        {
            RuntimeNativePrimitiveProviders.Register(
                runtime,
                registry,
                RuntimeNativePrimitives.EtsStorageCore | RuntimeNativePrimitives.InternalScalars413 | RuntimeNativePrimitives.PersistentTermCore
            );
            RpcCompatibilityBootstrap.Register(runtime, registry);
            if (managed)
            {
                RpcLibrary.Register(registry);
            }
        });
        string stdlib = Path.Combine(repository, "src/Otp/Original/Foundation/Erlang.Otp.Stdlib.Beam/Otp");
        string kernel = Path.Combine(repository, "src/Otp/Original/Foundation/Erlang.Otp.Kernel.Beam/Otp/ebin");
        string[] dependencies = Directory.GetFiles(Path.Combine(repository, "src/Otp/Original"), "*.beam", SearchOption.AllDirectories)
            .Select(Path.GetFileNameWithoutExtension).Cast<string>().Append("rpc_port_cases").Append("rpc_SUITE").Append("erlang").ToArray();
        string[] helpers = ["net_kernel", "erpc", "gen_server", "gen", "proc_lib", "sys", "lists", "maps", "ets", "queue",
            "logger", "logger_config", "logger_filters", "logger_backend", "logger_server", "logger_proxy", "proplists", "unicode"];
        var images = new List<PreparedBeamImage>();
        foreach (string module in helpers.Concat(managed ? [] : new[] { "rpc" }).Append("rpc_port_cases"))
        {
            string file = module == "rpc_port_cases" ? Path.Combine(directory, module + ".beam")
                : File.Exists(Path.Combine(stdlib, module + ".beam")) ? Path.Combine(stdlib, module + ".beam") : Path.Combine(kernel, module + ".beam");
            images.Add(runtime.Modules.PrepareBeam(File.ReadAllBytes(file), new BeamDependencySelection(dependencies)));
        }
        await runtime.InitializeModulesAsync(registry =>
        {
            Require(registry.PublishPreparedBeam(images.ToArray()).Status == BeamPublicationStatus.Published);
        });
    }

    private static async Task Pump(ConnectedSpawnHostEndpoint from, ConnectedSpawnHostEndpoint to, CancellationToken cancellation)
    {
        while (!cancellation.IsCancellationRequested)
        {
            HostDistributionBuffer? buffer = await from.Controller.Invoke(context => from.Runtime.DistributionPending.TakeConnectedOutput(from.Handle!, context, out _));
            if (buffer is not null)
            {
                await to.PutPacket(buffer.Segments.SelectMany(segment => segment.ToArray()).ToArray());
            }
            else
            {
                try
                {
                    await Task.Delay(1, cancellation);
                }
                catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
                {
                }
            }
        }
    }

    private static void Require(bool value)
    {
        if (!value)
        {
            throw new InvalidOperationException(RpcCompatibilityDiagnostics.ConnectedRpcContractFailed);
        }
    }
}
