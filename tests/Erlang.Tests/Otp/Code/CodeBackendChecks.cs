using System.Security.Cryptography;
using System.Text.Json;

namespace Erlang.Tests;

internal static class CodeBackendChecks
{
    internal static async Task Run(bool managed)
    {
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(CodeBackendNames.WatchdogSeconds));
        await using ProcessRuntime runtime = await CodeServerRuntime410Checks.Create([CodeBackendNames.Observer, CodeBackendNames.CodeModule, CodeBackendNames.InternalModule, CodeBackendNames.PurgerModule, CodeBackendNames.SubjectModule]);
        string[] dependencies = JsonSerializer.Deserialize<string[]>(
            LibraryDependencyOriginalHostChecks.ReadRetainedResource(LibraryDependencyOriginalNames.Dependencies))!;
        var selection = new BeamDependencySelection(dependencies.Concat([CodeBackendNames.Observer, CodeBackendNames.CodeModule, CodeBackendNames.InternalModule, CodeBackendNames.PurgerModule, CodeBackendNames.SubjectModule]));
        await runtime.InitializeModulesAsync(registry =>
        {
            if (managed)
            {
                CodeModule.Register(registry);
            }
        });
        var images = new List<PreparedBeamImage>();
        try
        {
            var inputs = new List<byte[]>();
            if (!managed)
            {
                inputs.Add(InterpreterCode411Resources.Read());
            }
            inputs.Add(Resource(CodeBackendNames.ObserverImage));
            inputs.Add(KernelBootOriginal453Access.Read(CodeBackendNames.InternalImage));
            inputs.Add(KernelBootOriginal453Access.Read(CodeBackendNames.PurgerImage));
            await PreparedBeamFixtureImages825.PrepareAndPublishAsync(
                runtime,
                inputs,
                selection,
                (registry, prepared) =>
            {
                images.AddRange(prepared);
                Require(registry.PublishPreparedBeam(prepared).Status == BeamPublicationStatus.Published);
            }
            );
            var expectedExports = Cons.Items(ExternalTermFormat.Decode(Resource(CodeBackendNames.ExportsFile))).Cast<TupleTerm>()
                .Select(row => (((Atom)row.Items[0]).Name, (int)((Integer)row.Items[1]).Value)).ToHashSet();
            Require(expectedExports.SetEquals(CodeModule.Exports));
            Require(expectedExports.SetEquals(runtime.Modules.Exports.Where(row => row.Module == CodeBackendNames.CodeModule && row.Function != CodeBackendNames.MetadataFunction)
                .Select(row => (row.Function, row.Arity))));
            foreach (var export in CodeModule.Exports)
            {
                Require(managed ? runtime.Modules.SourceOwner(CodeBackendNames.CodeModule, export.Name, export.Arity) is null
                    : runtime.Modules.SourceOwner(CodeBackendNames.CodeModule, export.Name, export.Arity) is PreparedBeamImage);
            }
            string output = Path.GetFullPath(Path.Combine(
                CodeBackendNames.Artifacts,
                managed ? CodeBackendNames.ManagedDirectory : CodeBackendNames.BeamDirectory,
                Guid.NewGuid().ToString(CodeBackendNames.GuidFormat)
            ));
            Directory.CreateDirectory(output);
            RuntimeOtpBootSession session = await runtime.StartOtpBootAsync(CodeServerOriginal410Fixture.Create(output), cancellation.Token);
            Require(!session.Completion.IsCompleted);
            await using var actor = new RuntimeNodeFoundationHostActor(runtime);
            Term[] rows = Cons.Items(ExternalTermFormat.Decode(Resource(CodeBackendNames.ContractsFile))).ToArray();
            var failures = new List<object>();
            int count = 0;
            foreach (TupleTerm row in rows)
            {
                Term actual = await actor.InvokeAsync(async context => await runtime.Modules.Call(
                    context,
                    CodeBackendNames.Observer,
                    CodeBackendNames.WorkerFunction,
                    row.Items[0],
                    row.Items[1],
                    context.Self
                ));
                actual = ((TupleTerm)actual).Items[1];
                if (!actual.Equals(row.Items[2]))
                {
                    failures.Add(new
                    {
                        Index = count,
                        Function = row.Items[0].ToString(),
                        Arguments = row.Items[1].ToString(),
                        Expected = row.Items[2].ToString(),
                        Actual = actual.ToString()
                    });
                }
                count++;
            }
            File.WriteAllText(
                Path.Combine(output, CodeBackendNames.ComparisonFile),
                JsonSerializer.Serialize(
                    new
                    {
                        Managed = managed,
                        Rows = count,
                        Failures = failures,
                        CorpusSha256 = Convert.ToHexString(SHA256.HashData(Resource(CodeBackendNames.ContractsFile))),
                        ManagedModuleMvid = typeof(CodeModule).Module.ModuleVersionId,
                        RuntimeModuleMvid = typeof(ProcessRuntime).Module.ModuleVersionId,
                        SelectedExports = expectedExports.Count,
                        OriginalCodeSha256 = Convert.ToHexString(SHA256.HashData(InterpreterCode411Resources.Read()))
                    },
                    new JsonSerializerOptions { WriteIndented = true }
                )
            );
            Require(failures.Count == 0);
            Term lifecycle = await actor.InvokeAsync(async context => await runtime.Modules.Call(
                context,
                CodeBackendNames.Observer,
                CodeBackendNames.LifecycleFunction,
                new BitString(Resource(CodeBackendNames.SubjectImage))
            ));
            Term expectedLifecycle = ExternalTermFormat.Decode(Resource(CodeBackendNames.LifecycleFile));
            File.WriteAllText(
                Path.Combine(output, CodeBackendNames.LifecycleReport),
                JsonSerializer.Serialize(
                    new
                    {
                        Equal = lifecycle.Equals(expectedLifecycle),
                        Actual = lifecycle.ToString(),
                        Expected = expectedLifecycle.ToString()
                    },
                    new JsonSerializerOptions { WriteIndented = true }
                )
            );
            Require(lifecycle.Equals(expectedLifecycle));
        }
        finally
        {
            foreach (PreparedBeamImage image in images)
            {
                image.Dispose();
            }
        }
    }

    internal static Task CheckRefusals()
    {
        bool refused = false;
        try
        {
            _ = RuntimeModuleSelection.Managed(CodeBackendNames.CodeModule);
        }
        catch (ArgumentException)
        {
            refused = true;
        }
        Require(refused);
        Require(CodeModule.Exports.Distinct().Count() == CodeModule.Exports.Count);

        return Task.CompletedTask;
    }

    internal static async Task CheckNativeProfile()
    {
        await using var runtime = new ProcessRuntime();
        await runtime.InitializeModulesAsync(CodeModule.Register);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Require((await runtime.Modules.Call(context, CodeModule.Name, CodeBackendNames.CoverageSupport)).Equals(Term.A(ErlangBooleanAtoms.False)));
            foreach (var export in CodeModule.Exports.Where(export => export.Name is CodeBackendNames.GetCoverageMode
                or CodeBackendNames.SetCoverageMode or CodeBackendNames.GetCoverage or CodeBackendNames.ResetCoverage or CodeBackendNames.GetDebugInfo))
            {
                bool failed = false;
                try
                {
                    await runtime.Modules.Call(
                        context,
                        CodeModule.Name,
                        export.Name,
                        Enumerable.Repeat<Term>(Term.Tuple(Term.A(CodeBackendNames.InvalidTag), Term.List(Term.I(1))), export.Arity).ToArray()
                    );
                }
                catch (ErlangException error)
                {
                    failed = error.ExceptionClass == ErlangExceptionClasses.Error && error.Reason.Equals(Term.A(ErlangErrorReasons.BadArgument));
                }
                Require(failed);
            }

            return true;
        });
    }

    private static byte[] Resource(string name)
    {
        byte[] Raw(string file)
        {
            using Stream input = typeof(CodeBackendChecks).Assembly.GetManifestResourceStream(CodeBackendNames.Prefix + file)
                ?? throw new InvalidOperationException(CodeBackendNames.Failure + CodeBackendNames.MissingResourceSeparator + file);
            using var output = new MemoryStream();
            input.CopyTo(output);

            return output.ToArray();
        }
        using JsonDocument provenance = JsonDocument.Parse(Raw(CodeBackendNames.ProvenanceFile));
        Require(provenance.RootElement.GetProperty(CodeBackendNames.BaselineField).GetString() == CodeBackendNames.Baseline
            && provenance.RootElement.GetProperty(CodeBackendNames.CommitField).GetString() == CodeBackendNames.BaselineCommit);
        string property = name switch
        {
            CodeBackendNames.ObserverImage => CodeBackendNames.ObserverHashField,
            CodeBackendNames.SubjectImage => CodeBackendNames.SubjectHashField,
            CodeBackendNames.ContractsFile => CodeBackendNames.CorpusHashField,
            CodeBackendNames.ExportsFile => CodeBackendNames.ExportsHashField,
            CodeBackendNames.LifecycleFile => CodeBackendNames.LifecycleHashField,
            _ => throw new InvalidOperationException(CodeBackendNames.Failure)
        };
        byte[] bytes = Raw(name);
        Require(Convert.ToHexString(SHA256.HashData(bytes)) == provenance.RootElement.GetProperty(property).GetString());

        return bytes;
    }

    private static void Require(bool value)
    {
        if (!value)
        {
            throw new InvalidOperationException(CodeBackendNames.Failure);
        }
    }
}
