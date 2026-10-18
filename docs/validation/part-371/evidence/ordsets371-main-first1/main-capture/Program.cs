using System.Text.Json;
using System.Security.Cryptography;
using Erlang;
using Erlang.Compiler;
using Erlang.Differential;
if (args.Length != 3) return 2;
string repository = Path.GetFullPath(args[0]);
string tool = Path.GetFullPath(args[1]);
string output = Path.GetFullPath(args[2]);
if (Directory.Exists(output)) return 2;
Directory.CreateDirectory(output);
using var profile = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(tool, "profile.json")));
var groups = new List<object>();
int failures = 0;
foreach (var fixture in profile.RootElement.GetProperty("Fixtures").EnumerateArray())
{
    string id = fixture.GetProperty("Id").GetString()!;
    string directory = Path.Combine(output, id);
    Directory.CreateDirectory(directory);
    string source = Path.Combine(fixture.GetProperty("Location").GetString() == "tool" ? tool : repository, fixture.GetProperty("Path").GetString()!);
    string[] paths = [source, .. fixture.GetProperty("Companions").EnumerateArray().Select(x => Path.Combine(tool, x.GetString()!))];
    string[] names = fixture.GetProperty("Names").EnumerateArray().Select(x => x.GetString()!).ToArray();
    var compiled = new List<GeneratedModuleArtifact>();
    var compileResults = new List<object>();
    bool compileFailed = false;
    foreach (string path in paths)
    {
        try
        {
            var module = GeneratedModuleCompiler.Compile(await File.ReadAllTextAsync(path), Path.GetFileName(path));
            compiled.Add(module);
            compileResults.Add(new { Path = path, SourceSha256 = Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(path))), DeclaredSourceFile = Path.GetFileName(path), module.CSharpSha256, Passed = true });
        }
        catch (Exception error)
        {
            compileFailed = true;
            compileResults.Add(new { Path = path, Passed = false, Error = error.ToString() });
        }
    }
    try
    {
        if (compileFailed)
        {
            failures++;
            groups.Add(new { Id = id, Status = "FAIL_COMPILE_DEPENDENCY", Compile = compileResults, ExecutionSkipped = true });
            continue;
        }
        await using var runtime = new ProcessRuntime();
        foreach (var module in compiled) module.Definition.Register(runtime.Modules);
        Term runtimeMetadata = Term.Tuple(
            Term.A(FamilyRuntimeMetadataNames.Tag),
            runtime.Modules.ModuleInformation(FamilyRuntimeMetadataNames.Module, FamilyRuntimeMetadataNames.Fingerprint),
            runtime.InitializationPid
        );
        await File.WriteAllBytesAsync(
            Path.Combine(directory, FamilyRuntimeMetadataNames.File),
            ExternalTermFormat.Encode(runtimeMetadata)
        );
        Term sourceMetadata = Cons.From(compiled.Select(module => Term.Tuple(
            Term.A(module.Definition.Name),
            runtime.Modules.ModuleInformation(module.Definition.Name, FamilyRuntimeMetadataNames.Fingerprint)
        )));
        await File.WriteAllBytesAsync(
            Path.Combine(directory, FamilyRuntimeMetadataNames.SourceModulesFile),
            ExternalTermFormat.Encode(sourceMetadata)
        );
        var completed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var process = runtime.Spawn(async context =>
        {
            var encoding = new EtfEncodingOptions { FunctionOwner = context.Runtime.InitializationPid };
            try
            {
                var main = compiled.Single(x => x.Definition.Name == fixture.GetProperty("Module").GetString());
                var worker = compiled.Single(x => x.Definition.Name == fixture.GetProperty(FamilyCaptureNames.WorkerModule).GetString());
                if (fixture.GetProperty(FamilyCaptureNames.ValueAcquisition).GetString() != FamilyCaptureNames.MatchedWorkerPolicy)
                    throw new InvalidOperationException(FamilyCaptureNames.InvalidValuePolicy);
                Term rows = await Execution.InvokeAsync(
                    worker.Definition,
                    fixture.GetProperty(FamilyCaptureNames.WorkerEntry).GetString()!,
                    context,
                    [Term.A(main.Definition.Name), Term.A(fixture.GetProperty("Run").GetString()!)]
                );
                await File.WriteAllBytesAsync(Path.Combine(directory, "returned.etf"), ExternalTermFormat.Encode(rows, encoding));
                var values = Cons.Items(rows).ToArray();
                if (values.Length != names.Length) throw new InvalidOperationException("Unexpected contract row count.");
                for (int index = 0; index < names.Length; index++)
                    if (values[index] is not TupleTerm { Items.Count: 2 } pair || !pair.Items[0].Equals(Term.A(names[index])))
                        throw new InvalidOperationException("Missing, reordered or malformed row " + names[index]);
                Term[] workerArguments = [Term.A(fixture.GetProperty(FamilyCaptureNames.StackModule).GetString()!), Term.A(fixture.GetProperty(FamilyCaptureNames.StackEntry).GetString()!)];
                Term stacks = await Execution.InvokeAsync(
                    worker.Definition,
                    fixture.GetProperty(FamilyCaptureNames.WorkerEntry).GetString()!,
                    context,
                    workerArguments
                );
                await File.WriteAllBytesAsync(Path.Combine(directory, "stack-observations.etf"), ExternalTermFormat.Encode(stacks, encoding));
                await File.WriteAllTextAsync(
                    Path.Combine(directory, SourceFunctionObservationNames.File),
                    JsonSerializer.Serialize(
                        SourceFunctionObservation.Read(compiled.Select(module => module.Definition), rows, stacks),
                        new JsonSerializerOptions { WriteIndented = true }
                    )
                );

                completed.TrySetResult();
            }
            catch (ErlangException error)
            {
                await File.WriteAllBytesAsync(Path.Combine(directory, "uncaught-stack.etf"), ExternalTermFormat.Encode(error.StackTraceTerm, encoding));
                completed.TrySetException(error);
            }
            catch (Exception error)
            {
                completed.TrySetException(error);
            }

            return Nil.Value;
        });
        await completed.Task.WaitAsync(TimeSpan.FromSeconds(fixture.GetProperty("ObserveTimeoutSeconds").GetInt32()));
        await process.Completion.WaitAsync(TimeSpan.FromSeconds(10));
        groups.Add(new { Id = id, Status = "CAPTURED_PENDING_PASSIVE_CHECKS", Compile = compileResults, ExecutionSkipped = false, ReturnedSha256 = Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(Path.Combine(directory, "returned.etf")))), StacksSha256 = Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(Path.Combine(directory, "stack-observations.etf")))) });
    }
    catch (Exception error)
    {
        failures++;
        await File.WriteAllTextAsync(Path.Combine(directory, "failure.txt"), error.ToString());
        groups.Add(new { Id = id, Status = "FAIL_CAPTURE_RAW_RETAINED", Compile = compileResults, ExecutionSkipped = false, Error = error.ToString() });
    }
    finally
    {
        foreach (var module in compiled) module.Dispose();
    }
}
await File.WriteAllTextAsync(
    Path.Combine(output, "completion.json"),
    JsonSerializer.Serialize(
        new { Status = failures == 0 ? "CAPTURED_PENDING_PASSIVE_CHECKS" : "FAILED_GROUPS_RAW_RETAINED", FailedGroups = failures, Groups = groups, OriginalExecuted = false, Published = false },
        new JsonSerializerOptions { WriteIndented = true }
    )
);
var captureFiles = Directory.EnumerateFiles(output, FamilyCaptureNames.AllFiles, SearchOption.AllDirectories).OrderBy(path => path, StringComparer.Ordinal).Select(path => new
{
    Path = Path.GetRelativePath(output, path).Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
    Bytes = new FileInfo(path).Length,
    Sha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)))
}).ToArray();
await File.WriteAllTextAsync(
    Path.Combine(output, FamilyCaptureNames.CaptureSeal),
    JsonSerializer.Serialize(new { Files = captureFiles }, new JsonSerializerOptions { WriteIndented = true })
);
return failures == 0 ? 0 : 1;
