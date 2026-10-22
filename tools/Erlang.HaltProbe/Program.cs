using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Erlang;
using Erlang.Compiler;
using Erlang.Differential;
using Erlang.HaltProbe;

if (args.Length > 0 && args[0] == HaltWindowsDumpNames437.Command)
    return HaltWindowsDumpHelper437.Run(args);

if (args.Length > 0 && args[0] == NativeHaltTrackNames.Command)
    return await NativeHaltTrack.Run(args);

HaltProbeOptions? options = null;
var hashes = new List<string>();
try
{
    options = HaltProbeOptions.Parse(args);
    var runtime = new ProcessRuntime();
    Erlang.Otp.OtpModules.Register(runtime.Modules);
    IoCharacterRequests.Register(runtime);
    string? module = options.Module;
    var generated = new List<GeneratedModuleArtifact>();
    foreach (string path in options.Sources)
    {
        string source = await File.ReadAllTextAsync(path);
        hashes.Add(Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(source))));
        ModuleDefinition definition;
        if (options.Compiled)
        {
            GeneratedModuleArtifact artifact = GeneratedModuleCompiler.Compile(source);
            generated.Add(artifact);
            definition = artifact.Definition;
        }
        else
            definition = new Parser(source).ParseModule();
        definition.Register(runtime.Modules);
        module ??= definition.Name;
    }
    ProcessHandle process = runtime.Spawn(context => runtime.Modules.Call(context, module!, options.Entry));
    Task winner = await Task.WhenAny(runtime.HaltCompletion, process.Completion);
    if (runtime.Lifecycle is RuntimeLifecycleState.Stopping or RuntimeLifecycleState.Stopped)
    {
        RuntimeHaltResult result = await runtime.HaltCompletion;
        var evidence = new HaltProbeEvidence(
            HaltProbeNames.RequestObserved,
            options.Compiled ? HaltProbeNames.Compiled : HaltProbeNames.Interpreted,
            result.Kind,
            result.Kind == RuntimeHaltKind.Abort ? null : result.Status,
            result.Slogan,
            null,
            null,
            result.Kind == RuntimeHaltKind.CrashDump ? HaltProbeNames.DumpFormatGap : null,
            hashes.AsReadOnly()
        )
        { LogicalStack = result.LogicalStack?.ToString() };
        await File.WriteAllTextAsync(options.Evidence, JsonSerializer.Serialize(evidence));

        return HaltPhysicalEndpoint.Finish(result, options.Dump);
    }
    Term returned = await process.Completion;
    await File.WriteAllTextAsync(
        options.Evidence,
        JsonSerializer.Serialize(new HaltProbeEvidence(
            HaltProbeNames.Returned,
            options.Compiled ? HaltProbeNames.Compiled : HaltProbeNames.Interpreted,
            null,
            null,
            null,
            returned.ToString(),
            null,
            null,
            hashes.AsReadOnly()
        ))
    );
    await runtime.DisposeAsync();
    foreach (GeneratedModuleArtifact artifact in generated)
        artifact.Dispose();

    return returned is Atom { Name: ProcessExitReasons.Normal } ? 0 : HaltProbeNames.AbnormalEntryExit;
}
catch (Exception exception)
{
    if (options is not null)
        await File.WriteAllTextAsync(
            options.Evidence,
            JsonSerializer.Serialize(new HaltProbeEvidence(
                HaltProbeNames.Infrastructure,
                options.Compiled ? HaltProbeNames.Compiled : HaltProbeNames.Interpreted,
                null,
                null,
                null,
                null,
                exception.ToString(),
                null,
                hashes.AsReadOnly()
            ))
        );
    Console.Error.WriteLine(exception);

    return HaltProbeNames.InfrastructureExit;
}
