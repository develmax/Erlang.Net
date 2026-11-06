using Erlang.ErpcValidation;
using System.Security.Cryptography;
using System.Text.Json;
using Erlang;

try
{
    if (args.Length == 3 && args[2] == "Connected")
        return await ErpcConnectedChecks.Run(Path.GetFullPath(args[0]), Path.GetFullPath(args[1]));
    if (args.Length != 3 || args[2] is not ("Managed" or "Beam"))
        throw new ArgumentException(ErpcValidationDiagnostics.Usage);
    string repository = Path.GetFullPath(args[0]);
    string directory = Path.GetFullPath(args[1]);
    await using (var defaults = new ProcessRuntime())
    {
        await defaults.InitializeModulesAsync(Erlang.Otp.KernelModules.Register);
        var legacy = defaults.Modules.Exports.Where(row => row.Module == ErpcLibrary.Name)
            .Select(row => (row.Function, row.Arity)).ToHashSet();
        if (!legacy.SetEquals([("call", 4), ("call", 5), ("execute_call", 4)]))
            throw new InvalidOperationException(ErpcValidationDiagnostics.Exports);
    }
    int depth = ErpcValidationProvenance.Verify(repository, directory);
    bool managed = args[2] == "Managed";
    string kernel = Path.Combine(repository, "src/Otp/Original/Foundation/Erlang.Otp.Kernel.Beam/Otp/ebin");
    string stdlib = Path.Combine(repository, "src/Otp/Original/Foundation/Erlang.Otp.Stdlib.Beam/Otp");
    byte[] original = File.ReadAllBytes(Path.Combine(kernel, "erpc.beam"));
    var selections = new List<RuntimeModuleSelection>
{
    RuntimeModuleSelection.Managed("erlang"),
    RuntimeModuleSelection.Managed("maps"),
    managed ? RuntimeModuleSelection.Managed(ErpcLibrary.Name) : RuntimeModuleSelection.Beam(ErpcLibrary.Name, original, ["erlang", "maps"])
};
    await using var runtime = await ProcessRuntime.CreateAsync(new RuntimeLibrarySelection(selections));
    string[] dependencies = Directory.GetFiles(kernel, "*.beam").Concat(Directory.GetFiles(stdlib, "*.beam"))
        .Select(Path.GetFileNameWithoutExtension).Cast<string>().Concat(["erlang", "erpc_port_cases", "erpc_SUITE", "ct", "test_server", "os", "init", "peer"]).ToArray();
    var helperFiles = new List<string> { Path.Combine(stdlib, "lists.beam"), Path.Combine(directory, "erpc_port_cases.beam") };
    if (File.Exists(Path.Combine(directory, "upstream.etf")))
        helperFiles.Add(Path.Combine(directory, "erpc_SUITE.beam"));
    var images = helperFiles
        .Select(path => runtime.Modules.PrepareBeam(File.ReadAllBytes(path), new BeamDependencySelection(dependencies))).ToArray();
    await runtime.InitializeModulesAsync(registry =>
    {
        if (registry.PublishPreparedBeam(images).Status != BeamPublicationStatus.Published)
            throw new InvalidOperationException(ErpcValidationDiagnostics.Publication);
    });
    await runtime.InitializeModulesAsync(Erlang.Otp.KernelModules.Register);
    var expectedExports = Cons.Items(ExternalTermFormat.Decode(File.ReadAllBytes(Path.Combine(directory, "exports.etf"))))
        .Cast<TupleTerm>().Select(row => (((Atom)row.Items[0]).Name, (int)((Integer)row.Items[1]).Value)).ToHashSet();
    var actualExports = runtime.Modules.Exports.Where(row => row.Module == ErpcLibrary.Name && row.Function != "module_info")
        .Select(row => (row.Function, row.Arity)).ToHashSet();
    if (!expectedExports.SetEquals(ErpcLibrary.Exports) || !expectedExports.SetEquals(actualExports))
        throw new InvalidOperationException(ErpcValidationDiagnostics.Exports);
    foreach (var export in ErpcLibrary.Exports)
    {
        if ((runtime.Modules.SourceOwner(ErpcLibrary.Name, export.Name, export.Arity) is PreparedBeamImage) == managed)
            throw new InvalidOperationException(ErpcValidationDiagnostics.Owner);
    }
    _ = await Run(context => runtime.Modules.Call(
        context,
        "erlang",
        "system_flag",
        Term.A("backtrace_depth"),
        Term.I(depth)
    ));
    var failures = new List<object>();
    int passed = 0;
    int count = 0;
    foreach (TupleTerm row in Cons.Items(runtime.ImportExternalTerm(File.ReadAllBytes(Path.Combine(directory, "original.etf"))).Term))
    {
        Term actual = await Run(async context =>
        {
            _ = await runtime.Modules.Call(
                context,
                "erpc_port_cases",
                "worker",
                row.Items[0],
                row.Items[1],
                context.Self
            );

            return await Receive(context);
        });
        count++;
        if (actual.Equals(row.Items[2]))
            passed++;
        else
            failures.Add(new { Case = row.Items[0] + "/" + row.Items[1], Expected = row.Items[2].ToString(), Actual = actual.ToString() });
    }
    foreach (TupleTerm row in Cons.Items(runtime.ImportExternalTerm(File.ReadAllBytes(Path.Combine(directory, "effects.etf"))).Term))
    {
        Console.WriteLine("Scenario " + row.Items[0]);
        Term actual = await Run(async context =>
        {
            _ = await runtime.Modules.Call(
                context,
                "erpc_port_cases",
                "scenario_worker",
                row.Items[0],
                context.Self
            );

            return await Receive(context);
        });
        count++;
        if (actual.Equals(row.Items[1]))
            passed++;
        else
            failures.Add(new { Case = row.Items[0].ToString(), Expected = row.Items[1].ToString(), Actual = actual.ToString() });
    }
    if (File.Exists(Path.Combine(directory, "upstream.etf")))
    {
        foreach (TupleTerm row in Cons.Items(ExternalTermFormat.Decode(File.ReadAllBytes(Path.Combine(directory, "upstream.etf")))))
        {
            Console.WriteLine("Upstream " + row.Items[0]);
            Term actual = await Run(context => runtime.Modules.Call(
                context,
                "erpc_SUITE",
                ((Atom)row.Items[0]).Name,
                Nil.Value
            ));
            count++;
            if (actual.Equals(row.Items[1]))
                passed++;
            else
                failures.Add(new { Case = row.Items[0].ToString(), Expected = row.Items[1].ToString(), Actual = actual.ToString() });
        }
    }
    if (count != ErpcValidationProvenance.ExpectedCases(repository))
        throw new InvalidDataException(ErpcValidationDiagnostics.Capture);
    File.WriteAllText(
        Path.Combine(directory, args[2] + ".json"),
        JsonSerializer.Serialize(
            new
            {
                Backend = args[2],
                BacktraceDepth = depth,
                Exports = expectedExports.Count,
                Cases = count,
                Passed = passed,
                Failures = failures,
                Assemblies = AppDomain.CurrentDomain.GetAssemblies().Where(assembly => !assembly.IsDynamic && assembly.GetName().Name!.StartsWith("Erlang", StringComparison.Ordinal))
                    .Select(assembly => new { assembly.FullName, Sha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(assembly.Location))) }).ToArray(),
                OriginalBeamSha256 = Convert.ToHexString(SHA256.HashData(original)),
                ManagedAssemblySha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(typeof(ErpcLibrary).Assembly.Location)))
            },
            new JsonSerializerOptions { WriteIndented = true }
        )
    );
    Console.WriteLine($"{args[2]}: {passed}/{count}");

    return failures.Count == 0 ? 0 : 1;

    async ValueTask<Term> Receive(ProcessContext context)
    {
        TupleTerm? message = await context.ReceiveAsync(value => value is TupleTerm { Items.Count: 2 } tuple && tuple.Items[0].Equals(context.Self) ? tuple : null, TimeSpan.FromSeconds(15));

        return message?.Items[1] ?? throw new TimeoutException(ErpcValidationDiagnostics.Response);
    }

    async Task<Term> Run(Func<ProcessContext, ValueTask<Term>> body)
    {
        var result = new TaskCompletionSource<Term>(TaskCreationOptions.RunContinuationsAsynchronously);
        ProcessHandle worker = runtime.Spawn(async context =>
        {
            try
            {
                result.TrySetResult(await body(context));
            }
            catch (Exception error)
            {
                result.TrySetException(error);
            }

            return Nil.Value;
        });
        Term value = await result.Task.WaitAsync(TimeSpan.FromSeconds(20));
        await worker.Completion.WaitAsync(TimeSpan.FromSeconds(5));

        return value;
    }

}
catch (Exception error)
{
    Console.Error.WriteLine(error);

    return 2;
}
