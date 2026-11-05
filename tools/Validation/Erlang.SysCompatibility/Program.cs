using System.Security.Cryptography;
using System.Text.Json;
using Erlang;

if (args.Length is not (3 or 4) || args[2] is not (SysValidationBackends.Managed or SysValidationBackends.Beam))
{
    Console.Error.WriteLine(SysValidationDiagnostics.Usage);

    return 2;
}
bool errorsOnly = args.Length == 4 && args[3] == SysValidationProtocol.Errors;
if (args.Length == 4 && !errorsOnly)
{
    Console.Error.WriteLine(SysValidationDiagnostics.Usage);

    return 2;
}
string repository = Path.GetFullPath(args[0]);
string directory = Path.GetFullPath(args[1]);
bool managed = args[2] == SysValidationBackends.Managed;
string[] roots = [SysValidationFiles.StdlibRoot, SysValidationFiles.KernelRoot, SysValidationFiles.ErtsRoot];
var files = roots.SelectMany(root => Directory.GetFiles(Path.Combine(repository, root), SysValidationFiles.BeamPattern))
    .GroupBy(Path.GetFileNameWithoutExtension).ToDictionary(group => group.Key!, group => group.First());
foreach (string fixture in new[] { SysValidationModules.SysPortCases, SysValidationModules.SysBehaviourCases, SysValidationModules.UpstreamSuite, SysValidationModules.UpstreamSpecialOne, SysValidationModules.UpstreamSpecialTwo })
{
    files[fixture] = Path.Combine(directory, fixture + SysValidationFiles.BeamExtension);
}
SysAdmission.Verify(repository, directory, files);
await using var runtime = await ProcessRuntime.CreateAsync(
    new RuntimeLibrarySelection(managed ? [RuntimeModuleSelection.Managed(SysModule.Name)] : []),
    RuntimeNativePrimitives.EtsStorageCore | RuntimeNativePrimitives.PersistentTermCore,
    CancellationToken.None
);
string[] modules = [SysValidationModules.Gen, SysValidationModules.ProcLib, SysValidationModules.Lists, SysValidationModules.Maps, SysValidationModules.Queue, SysValidationModules.SysPortCases, SysValidationModules.SysBehaviourCases, SysValidationModules.UpstreamSuite, SysValidationModules.UpstreamSpecialOne, SysValidationModules.UpstreamSpecialTwo, SysValidationModules.GenServer, SysValidationModules.GenStatem, SysValidationModules.GenEvent, SysValidationModules.Proplists, SysValidationModules.Io, SysValidationModules.IoLib, SysValidationModules.IoLibFormat, SysValidationModules.IoLibPretty, SysValidationModules.Unicode, SysValidationModules.UnicodeUtil, SysValidationModules.String, SysValidationModules.Logger, SysValidationModules.NetKernel, SysValidationModules.File, SysValidationModules.FileServer, SysValidationModules.FileIoServer, SysValidationModules.Filename, SysValidationModules.PrimFile, SysValidationModules.PrimBuffer, SysValidationModules.Os, SysValidationModules.RawFileIo, SysValidationModules.Epp, SysValidationModules.ErlScan, SysValidationModules.ErlParse, SysValidationModules.ErlAnno, SysValidationModules.ErlFeatures];
await runtime.InitializeModulesAsync(registry => RuntimeBootstrapFileSystem.Register(runtime, registry));
var images = new List<PreparedBeamImage>();
foreach (string module in modules.Concat(managed ? [] : new[] { SysModule.Name }))
{
    byte[] bytes = File.ReadAllBytes(files[module]);
    images.Add(runtime.Modules.PrepareBeam(bytes, new BeamDependencySelection(files.Keys.Concat(SysBeamImports.Read(runtime.Modules, bytes)))));
}
await runtime.InitializeModulesAsync(registry =>
{
    if (registry.PublishPreparedBeam(images.ToArray()).Status != BeamPublicationStatus.Published)
    {
        throw new InvalidOperationException(SysValidationDiagnostics.Publication);
    }
});
await Execute(async context =>
{
    foreach (TupleTerm feature in Cons.Items(ExternalTermFormat.Decode(File.ReadAllBytes(Path.Combine(directory, SysValidationFiles.Features)))))
    {
        await runtime.Modules.Call(
            context,
            SysValidationModules.PersistentTerm,
            SysValidationProtocol.Put,
            Term.Tuple(Term.A(SysValidationModules.ErlFeatures), feature.Items[0]),
            feature.Items[1]
        );
    }

    return Nil.Value;
});
if (!errorsOnly)
{
    await Execute(context => runtime.Modules.Call(context, SysValidationModules.PrimBuffer, SysValidationProtocol.OnLoad));
    await Execute(context => runtime.Modules.Call(context, SysValidationModules.PrimFile, SysValidationProtocol.OnLoad));
    // Start the actual file_server callback with gen_server. The minimal validation
    // runtime has no command-line master node, so no initializer wrapper is needed.
    await Execute(context => runtime.Modules.Call(
        context,
        SysValidationModules.GenServer,
        SysValidationProtocol.Start,
        Term.Tuple(Term.A(SysValidationProtocol.Local), Term.A(SysValidationProtocol.FileServer2)),
        Term.A(SysValidationModules.FileServer),
        Nil.Value,
        Nil.Value
    ));
}
using JsonDocument provenance = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(directory, SysValidationFiles.Provenance)));
int depth = provenance.RootElement.GetProperty(SysProvenanceFields.BacktraceDepth).GetInt32();
await Execute(context => runtime.Modules.Call(
    context,
    SysValidationModules.Erlang,
    SysValidationProtocol.SystemFlag,
    Term.A(SysValidationProtocol.BacktraceDepth),
    Term.I(depth)
));
var exports = Cons.Items(ExternalTermFormat.Decode(File.ReadAllBytes(Path.Combine(directory, SysValidationFiles.Exports))))
    .Cast<TupleTerm>().Select(row => (((Atom)row.Items[0]).Name, (int)((Integer)row.Items[1]).Value)).ToHashSet();
if (!exports.SetEquals(SysModule.Exports))
{
    throw new InvalidOperationException(SysValidationDiagnostics.Exports);
}
foreach (var export in SysModule.Exports)
{
    if (!runtime.Modules.HasExport(SysModule.Name, export.Name, export.Arity)
        || (runtime.Modules.SourceOwner(SysModule.Name, export.Name, export.Arity) is PreparedBeamImage) == managed)
    {
        throw new InvalidOperationException(SysValidationDiagnostics.Owner);
    }
}
Term[] corpusRows = Cons.Items(runtime.ImportExternalTerm(File.ReadAllBytes(Path.Combine(directory, SysValidationFiles.Corpus))).Term).ToArray();
Term[] rows = errorsOnly ? corpusRows.Where(value =>
{
    var row = (TupleTerm)value;

    return row.Items[1] is TupleTerm { Items: [Atom { Name: SysValidationProtocol.Exception }, _, _, _] }
        || row.Items[0] is TupleTerm { Items: [Atom { Name: SysValidationProtocol.SystemCase }, Atom { Name: SysValidationProtocol.CodeError }] };
}).ToArray() : corpusRows;
var failures = new List<object>();
int passed = 0;
foreach (TupleTerm row in rows)
{
    Console.WriteLine(row.Items[0]);
    var result = new TaskCompletionSource<Term>(TaskCreationOptions.RunContinuationsAsynchronously);
    ProcessHandle worker = runtime.Spawn(async context =>
    {
        try
        {
            result.TrySetResult(await runtime.Modules.Call(
                context,
                SysValidationModules.SysPortCases,
                SysValidationProtocol.Observe,
                row.Items[0]
            ));
        }
        catch (ErlangException error)
        {
            foreach (Pid pid in runtime.ProcessSnapshot())
            {
                Console.Error.WriteLine(await runtime.Modules.Call(
                    context,
                    SysValidationModules.Erlang,
                    SysValidationProtocol.ProcessInfo,
                    pid,
                    Term.List(
                        Term.A(SysValidationProtocol.CurrentStacktrace),
                        Term.A(SysValidationProtocol.Messages),
                        Term.A(SysValidationProtocol.RegisteredName)
                    )
                ));
            }
            result.TrySetResult(Term.Tuple(
                Term.A(SysValidationProtocol.Uncaught),
                Term.A(error.ExceptionClass),
                error.Reason,
                error.StackTraceTerm ?? Nil.Value
            ));
        }
        catch (Exception error)
        {
            result.TrySetException(error);
        }

        return Nil.Value;
    });
    Term actual = await result.Task.WaitAsync(SysValidationLimits.OperationDeadline);
    await worker.Completion.WaitAsync(SysValidationLimits.ExitDeadline);
    if (actual.Equals(row.Items[1]))
    {
        passed++;
    }
    else
    {
        failures.Add(new { Case = row.Items[0].ToString(), Expected = row.Items[1].ToString(), Actual = actual.ToString() });
    }
}
var identities = SysModule.Exports.Select(export => runtime.Modules.SourceOwner(SysModule.Name, export.Name, export.Arity) is null).Distinct().ToArray();
File.WriteAllText(
    Path.Combine(directory, args[2] + (errorsOnly ? SysValidationFiles.ErrorReportQualifier : string.Empty) + SysValidationFiles.ReportSuffix),
    JsonSerializer.Serialize(
        new
        {
            Backend = args[2],
            Mode = errorsOnly ? SysValidationProtocol.Errors : SysValidationProtocol.Full,
            CorpusTotal = corpusRows.Length,
            Passed = passed,
            Total = rows.Length,
            Failures = failures,
            Baseline = SysValidationBaseline.Tag,
            Commit = SysValidationBaseline.Commit,
            Assembly = typeof(SysModule).Module.ModuleVersionId,
            BacktraceDepth = depth,
            Assemblies = new[] { typeof(SysModule).Assembly, typeof(ProcessRuntime).Assembly, typeof(Term).Assembly, typeof(SysAdmission).Assembly }
        .Select(assembly => new { assembly.FullName, Mvid = assembly.ManifestModule.ModuleVersionId, Sha256 = SysAdmission.Hash(assembly.Location) }),
            Sources = Directory.GetFiles(Path.Combine(repository, SysValidationFiles.ManagedSources), SysValidationFiles.ManagedPattern)
        .Select(path => new { Path = Path.GetRelativePath(repository, path), Sha256 = SysAdmission.Hash(path) }),
            CorpusSha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(directory, SysValidationFiles.Corpus))))
        },
        new JsonSerializerOptions { WriteIndented = true }
    )
);
Console.WriteLine($"{args[2]}: {passed}/{rows.Length}, {failures.Count} failures");
return failures.Count == 0 ? 0 : 1;

async Task<Term> Execute(Func<ProcessContext, ValueTask<Term>> body)
{
    var completion = new TaskCompletionSource<Term>(TaskCreationOptions.RunContinuationsAsynchronously);
    ProcessHandle process = runtime.Spawn(async context =>
    {
        try
        {
            completion.TrySetResult(await body(context));
        }
        catch (ErlangException error)
        {
            Console.Error.WriteLine(Term.Tuple(Term.A(error.ExceptionClass), error.Reason, error.StackTraceTerm ?? Nil.Value));
            completion.TrySetException(error);
        }
        catch (Exception error)
        {
            completion.TrySetException(error);
        }

        return Nil.Value;
    });
    Term result = await completion.Task.WaitAsync(SysValidationLimits.OperationDeadline);
    await process.Completion.WaitAsync(SysValidationLimits.ExitDeadline);

    return result;
}
