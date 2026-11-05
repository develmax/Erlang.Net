using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Erlang;

namespace Erlang.Tests;

internal static class DetsBackendChecks
{
    internal const string ManagedTest = "runtime/dets/managed-public-otp-contracts";
    internal const string OriginalTest = "runtime/dets/original-public-otp-contracts";
    internal const string ManagedSuiteTest = "runtime/dets/managed-upstream-cases";
    internal const string OriginalSuiteTest = "runtime/dets/original-upstream-cases";
    private static readonly TimeSpan Deadline = TimeSpan.FromSeconds(300);

    internal static Task Managed() => Run(true);

    internal static Task Original() => Run(false);

    internal static Task ManagedSuite() => Run(true, true);

    internal static Task OriginalSuite() => Run(false, true);

    private static async Task<Term> Execute(ProcessRuntime runtime, Func<ProcessContext, ValueTask<Term>> operation)
    {
        var result = new TaskCompletionSource<Term>(TaskCreationOptions.RunContinuationsAsynchronously);
        ProcessHandle worker = runtime.Spawn(async context =>
        {
            try
            {
                result.TrySetResult(await operation(context));
            }
            catch (Exception error)
            {
                result.TrySetException(error);
            }

            return Nil.Value;
        });
        Term value = await result.Task.WaitAsync(Deadline);
        _ = await worker.Completion.WaitAsync(Deadline);

        return value;
    }

    internal static string Repository()
    {
        for (DirectoryInfo? directory = new(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "reference-baseline.json")))
            {
                return directory.FullName;
            }
        }
        throw new DirectoryNotFoundException("DETS repository was not found");
    }

    private static async Task Run(bool managed, bool suite = false)
    {
        string repository = Repository();
        string capture = Path.Combine(repository, "tests/Erlang.Tests/Resources", suite ? "DetsSuiteBackend" : "DetsBackend");
        string observerModule = suite ? "dets_suite_cases" : "dets_port_cases";
        byte[] observer = File.ReadAllBytes(Path.Combine(capture, observerModule + ".beam"));
        using (JsonDocument provenance = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(capture, "provenance.json"))))
        {
            foreach (var (property, name) in new[]
            {
                ("ObserverSourceSha256", observerModule + ".erl"), ("ObserverBeamSha256", observerModule + ".beam"),
                ("CorpusSha256", "original.etf"), ("ErrorsSha256", "errors.etf"), ("ExportsSha256", "exports.etf")
            })
            {
                string actualHash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(capture, name))));
                if (actualHash != provenance.RootElement.GetProperty(property).GetString())
                {
                    throw new InvalidOperationException("DETS fixture identity changed: " + name);
                }
            }
        }
        Term declared = ExternalTermFormat.Decode(File.ReadAllBytes(Path.Combine(capture, "exports.etf")));
        var exports = Cons.Items(declared).Cast<TupleTerm>()
            .Where(item => item.Items[0] is not Atom { Name: "module_info" })
            .Select(item => (((Atom)item.Items[0]).Name, (int)((Integer)item.Items[1]).Value)).ToHashSet();
        if (!exports.SetEquals(DetsModule.Exports))
        {
            throw new InvalidOperationException("DETS pinned export surface differs");
        }
        Term expected = ExternalTermFormat.Decode(File.ReadAllBytes(Path.Combine(capture, "original.etf")));
        string output = Path.Combine(
            repository,
            "artifacts/dets-acceptance",
            (suite ? "Suite-" : string.Empty) + (managed ? "Managed-" : "Beam-") + Guid.NewGuid().ToString("N")
        );
        Directory.CreateDirectory(output);

        void Progress(string stage) => File.AppendAllText(Path.Combine(output, "progress.log"), DateTime.UtcNow.ToString("O") + " " + stage + Environment.NewLine);
        Progress("Creating kernel runtime");
        foreach (string legacy in suite ? Array.Empty<string>() : Directory.EnumerateFiles(Path.Combine(capture, "legacy")))
        {
            File.Copy(legacy, Path.Combine(output, Path.GetFileName(legacy)));
        }
        await using ProcessRuntime runtime = await KernelBootOriginal453Runtime.Create(
            RegexNative411Access.Admission(),
            (owner, registry) =>
        {
            owner.InitializeModulesAsync(_ =>
            {
                RuntimeBootstrapEtsQueries414.Register(owner, registry);
                RuntimeBootstrapEtsMutations414.Register(owner, registry);
                RuntimeBootstrapEtsTraversal416.Register(owner, registry);
                RuntimeBootstrapEtsCounter417.Register(owner, registry);
                RuntimeBootstrapEtsRename418.Register(owner, registry);
                RuntimeBootstrapEtsHeir419.Register(owner, registry);
                RuntimeBootstrapEtsCompiled419.Register(owner, registry);
                RuntimeBootstrapEtsAll420.Register(owner, registry);
                RuntimeBootstrapEtsFixation420.Register(owner, registry);
                RuntimeBootstrapEtsCursor420.Register(owner, registry);
                RuntimeBootstrapEtsReplace420.Register(owner, registry);
                RuntimeBootstrapEtsInspection420.Register(owner, registry);
                RuntimeBootstrapEtsInfo420.Register(owner, registry);
            }).GetAwaiter().GetResult();

            return registry.PrepareBeam(
                File.ReadAllBytes(Path.Combine(repository,
                "src/Otp/Original/Foundation/Erlang.Otp.Stdlib.Beam/Otp/ets.beam")),
                new BeamDependencySelection(new[]
                { "erlang", "ets", "dets", "disk_log", "qlc", "erl_eval", "ms_transform", "io", "io_lib", "lists", "file", "re", "string", "unicode" })
            );
        }
        );
        Progress("Kernel runtime created");
        await runtime.InitializeModulesAsync(registry =>
        {
            if (managed)
            {
                DetsModule.Register(registry);
            }
        });
        var dependencies = new BeamDependencySelection(new[]
        {
            "dets", "dets_server", "dets_sup", "dets_v9", "dets_utils", "dets_port_cases", "ets", "erlang", "prim_buffer", "binary",
            "file", "filename", "lists", "qlc", "sys", "proc_lib", "logger", "io", "io_lib",
            "dets_SUITE", "dets_suite_cases", "test_server", "rpc", "peer", "timer", "c", "proplists", "ct", "sofs", "os", "net_adm"
        });
        if (suite)
        {
            using PreparedBeamImage suiteImage = runtime.Modules.PrepareBeam(File.ReadAllBytes(Path.Combine(capture, "dets_SUITE.beam")), dependencies);
            await runtime.InitializeModulesAsync(registry =>
            {
                if (registry.PublishPreparedBeam(suiteImage).Status != BeamPublicationStatus.Published)
                {
                    throw new InvalidOperationException("DETS upstream suite publication failed");
                }
            });
        }
        using PreparedBeamImage prepared = runtime.Modules.PrepareBeam(observer, dependencies);
        await runtime.InitializeModulesAsync(registry =>
        {
            if (registry.PublishPreparedBeam(prepared).Status != BeamPublicationStatus.Published)
            {
                throw new InvalidOperationException("DETS observer publication failed");
            }
        });
        _ = await Execute(runtime, context => runtime.Modules.Call(context, "prim_file", "on_load"));
        using JsonDocument admission = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(repository,
            "artifacts/library376-official-compiler/official-admission.json")));
        string installed = admission.RootElement.GetProperty("InstalledOtpRoot").GetString()!;
        using var cancellation = new CancellationTokenSource(Deadline);
        RuntimeOtpBootSession session = await runtime.StartOtpBootAsync(
            new[]
        {
            "-root", installed, "-bindir", Path.Combine(installed, "erts-17.1/bin"),
            "-boot", Path.Combine(installed, "bin/no_dot_erlang"), "-mode", "interactive", "-noshell", "-noinput"
        }.Select(value => new BitString(Encoding.UTF8.GetBytes(value))),
            cancellation.Token
        );
        if (session.Completion.IsCompleted)
        {
            throw new InvalidOperationException("DETS kernel boot stopped");
        }
        _ = await Execute(runtime, context => runtime.Modules.Call(context, "prim_buffer", "on_load"));
        Progress("Kernel boot completed; starting observer");
        Term actual;
        try
        {
            actual = await Execute(
                runtime,
                async context =>
        {
            Term tag = runtime.MakeReference();
            _ = await runtime.Modules.Call(
                context,
                observerModule,
                "run_worker",
                context.Self,
                tag,
                Term.String(output.Replace('\\', '/'))
            );
            var message = await context.ReceiveAsync<TupleTerm>(value => value is TupleTerm { Items.Count: 2 } pair && pair.Items[0].Equals(tag) ? pair : null);

            return message!.Items[1];
        }
            );
        }
        catch (ErlangException error)
        {
            Term failure = Term.Tuple(Term.A(error.ExceptionClass), error.Reason, error.StackTraceTerm);
            File.WriteAllBytes(Path.Combine(output, "observer-failure.etf"), ExternalTermFormat.Encode(failure));
            File.WriteAllText(Path.Combine(output, "observer-failure.txt"), failure.ToString());
            throw new InvalidOperationException("DETS observer failed: " + output, error);
        }
        Progress("Observer completed");
        File.WriteAllBytes(Path.Combine(output, "actual.etf"), ExternalTermFormat.Encode(actual));
        File.WriteAllText(
            Path.Combine(output, "receipt.json"),
            JsonSerializer.Serialize(
                new
                {
                    Backend = managed ? "Managed" : "Beam",
                    ObserverSha256 = Convert.ToHexString(SHA256.HashData(observer)),
                    AssemblySha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(typeof(DetsModule).Assembly.Location))),
                    ImplementationMvid = typeof(DetsModule).Module.ModuleVersionId,
                    HasBeamSourceOwner = runtime.Modules.SourceOwner("dets", "open_file", 2) is PreparedBeamImage,
                    Equal = actual.Equals(expected),
                    Expected = expected.ToString(),
                    Actual = actual.ToString()
                },
                new JsonSerializerOptions { WriteIndented = true }
            )
        );
        if (!actual.Equals(expected))
        {
            throw new InvalidOperationException("DETS differs from pinned OTP: " + output);
        }
        Term expectedErrors = ExternalTermFormat.Decode(File.ReadAllBytes(Path.Combine(capture, "errors.etf")));
        Term actualErrors = await Execute(
            runtime,
            async context =>
        {
            Term tag = runtime.MakeReference();
            _ = await runtime.Modules.Call(
                context,
                observerModule,
                "error_worker",
                context.Self,
                tag
            );
            var message = await context.ReceiveAsync<TupleTerm>(value => value is TupleTerm { Items.Count: 2 } pair && pair.Items[0].Equals(tag) ? pair : null);

            return message!.Items[1];
        }
        );
        File.WriteAllBytes(Path.Combine(output, "errors-actual.etf"), ExternalTermFormat.Encode(actualErrors));
        File.WriteAllText(
            Path.Combine(output, "errors.json"),
            JsonSerializer.Serialize(
                new
                {
                    Equal = actualErrors.Equals(expectedErrors),
                    Expected = expectedErrors.ToString(),
                    Actual = actualErrors.ToString()
                },
                new JsonSerializerOptions { WriteIndented = true }
            )
        );
        if (!actualErrors.Equals(expectedErrors))
        {
            throw new InvalidOperationException("DETS complete error stacks differ: " + output);
        }
        if ((runtime.Modules.SourceOwner("dets", "open_file", 2) is PreparedBeamImage) == managed)
        {
            throw new InvalidOperationException("DETS executed a different backend");
        }
        if (managed && runtime.Modules.Exports.Count(export => export.Module == "dets" && export.Function != "module_info") != DetsModule.Exports.Count)
        {
            throw new InvalidOperationException("DETS selected surface changed during execution");
        }
    }
}
