using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Erlang;
using static Erlang.Tests.KernelBootOriginal453Names;

namespace Erlang.Tests;

internal static class KernelBootOriginal453Checks
{
    internal static async Task Run()
    {
        string admission = RegexNative411Access.Admission();
        string output = Path.Combine(Output, Guid.NewGuid().ToString(ReUnicodeOriginal453Names.GuidFormat));
        Directory.CreateDirectory(output);
        var failures = new System.Collections.Concurrent.ConcurrentQueue<string>();
        EventHandler<System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs> diagnostic = (_, item) =>
        {
            if (failures.Count < 256 && item.Exception is not OperationCanceledException)
                failures.Enqueue(item.Exception.ToString() + (item.Exception is ErlangException guest ? guest.StackTraceTerm : null));
        };
        AppDomain.CurrentDomain.FirstChanceException += diagnostic;
        try
        {
            await Check(admission, output);
        }
        finally
        {
            AppDomain.CurrentDomain.FirstChanceException -= diagnostic;
            File.WriteAllText(Path.Combine(output, StandardBootOriginal412Names.DiagnosticFailures), JsonSerializer.Serialize(failures.ToArray()));
        }
    }

    private static async Task Check(string admission, string output)
    {
        await using ProcessRuntime runtime = await KernelBootOriginal453Runtime.Create(admission);
        var prepared = new TaskCompletionSource<Term>(TaskCreationOptions.RunContinuationsAsynchronously);
        ProcessHandle preload = runtime.Spawn(async context =>
        {
            try
            {
                prepared.TrySetResult(await runtime.Modules.Call(context, PrimFile, OnLoad));
            }
            catch (Exception error)
            {
                prepared.TrySetException(error);
            }

            return Nil.Value;
        });
        await prepared.Task.WaitAsync(ReUnicodeOriginal453Names.Deadline);
        await preload.Completion.WaitAsync(ReUnicodeOriginal453Names.Deadline);
        using var document = JsonDocument.Parse(File.ReadAllBytes(admission));
        string installed = document.RootElement.GetProperty(Installed).GetString()!;
        string[] arguments = [RootOption, installed, BinOption, Path.Combine(installed, ErtsBin),
            BootOption, Path.Combine(installed, BootBase), ModeOption, Interactive, NoShell, NoInput,
            CookieOption, Cookie, StartEpmdOption, False, ListenOption, False, NoEpmd,
            ProtocolOption, Protocol, ShortNameOption, Node];
        using var deadline = new CancellationTokenSource(ReUnicodeOriginal453Names.Deadline);
        RuntimeOtpBootSession session = await runtime.StartOtpBootAsync(
            arguments.Select(value => new BitString(Encoding.UTF8.GetBytes(value))), deadline.Token);
        KernelBootOriginal453Access.Require(!session.Completion.IsCompleted && runtime.Node == Node);
        await global::Erlang.Tests.PreparedBeamFixtureScope171.RunAsync(
            runtime,
            (registry, scope) =>
        {
            string[] modules = JsonSerializer.Deserialize<string[]>(StandardBootOriginal412Access.Resource(StandardBootOriginal412Names.Dependencies))!;
            PreparedBeamImage image = scope.Prepare(() => registry.PrepareBeam(KernelBootOriginal453Access.Read(Worker + Beam),
                new BeamDependencySelection(modules.Append(Worker))));

            return ValueTask.FromResult<Action<ModuleRegistry>>(_ =>
            {
                KernelBootOriginal453Access.Require(registry.PublishPreparedBeam(image).Status == BeamPublicationStatus.Published);
            });
        }
        );
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            KernelBootOriginal453Access.Require((await runtime.Modules.Call(
                context,
                Erts,
                WhereIs,
                Term.A(Init)
            )).Equals(session.InitializationProcess));
            KernelBootOriginal453Access.Require(await runtime.Modules.Call(
                context,
                Erts,
                WhereIs,
                Term.A(Kernel)
            ) is Pid);
            KernelBootOriginal453Access.Require((await runtime.Modules.Call(context, Erts, Alive)).Equals(Term.A(True)));

            return true;
        });
        var owners = new List<object>();
        foreach ((string module, string function, int arity) in new[]
        {
            (Kernel, Init, 1), (Init, Boot, 1), (Internal, SpawnSystem, 3), (Purger, Start, 0)
        })
        {
            KernelBootOriginal453Access.Require(runtime.Modules.SourceOwner(module, function, arity) is PreparedBeamImage);
            owners.Add(new { Module = module, Original = true });
        }
        var captured = new Dictionary<string, Term>();
        foreach ((string route, string file) in new[]
        {
            (ReUnicodeOriginal453Names.Run, ReUnicodeOriginal453Names.Values),
            (ReUnicodeOriginal453Names.Stacks, ReUnicodeOriginal453Names.StackValues)
        })
        {
            Term actual = await actor.InvokeAsync(async context => await runtime.Modules.Call(context, Worker, route));
            captured.Add(file, actual);
            File.WriteAllBytes(Path.Combine(output, Actual + file), ExternalTermFormat.Encode(actual));
        }
        var comparisons = new List<object>();
        var originalFileComparisons = new List<object>();
        bool equal = true;
        foreach ((string file, Term actual) in captured)
        {
            Term expected = ExternalTermFormat.Decode(KernelBootOriginal453Access.Read(file));
            equal &= expected.Equals(actual);
            originalFileComparisons.Add(new
            {
                File = file,
                StrictOriginalBytesEqual = KernelBootOriginal453Access.Read(file)
                .AsSpan().SequenceEqual(ExternalTermFormat.Encode(actual))
            });
            Term[] ep = file == ReUnicodeOriginal453Names.Values ? [expected] : Cons.Items(expected).Cast<TupleTerm>().Select(item => item.Items[1]).ToArray();
            Term[] ap = file == ReUnicodeOriginal453Names.Values ? [actual] : Cons.Items(actual).Cast<TupleTerm>().Select(item => item.Items[1]).ToArray();
            KernelBootOriginal453Access.Require(ep.Length == ap.Length && ep.Length == (file == ReUnicodeOriginal453Names.Values ? 1 : 5));
            for (int profile = 0; profile < ep.Length; profile++)
            {
                Term[] er = Cons.Items(ep[profile]).ToArray(), ar = Cons.Items(ap[profile]).ToArray();
                KernelBootOriginal453Access.Require(er.Length == Cases && ar.Length == Cases);
                for (int index = 0; index < er.Length; index++)
                {
                    bool passed = er[index].Equals(ar[index]) && ExternalTermFormat.Encode(er[index]).AsSpan().SequenceEqual(ExternalTermFormat.Encode(ar[index]));
                    comparisons.Add(new
                    {
                        File = file,
                        Profile = profile,
                        Index = index,
                        Passed = passed,
                        Expected = passed ? null : er[index].ToString(),
                        Actual = passed ? null : ar[index].ToString()
                    });
                    equal &= passed;
                }
            }
        }
        await actor.DisposeAsync();
        await runtime.DisposeAsync().AsTask().WaitAsync(ReUnicodeOriginal453Names.Deadline);
        bool passedAll = equal && comparisons.Count == Rows;
        File.WriteAllText(
            Path.Combine(output, Receipt),
            JsonSerializer.Serialize(new
            {
                Passed = passedAll,
                Examined = comparisons.Count,
                WholeTyped = true,
                ReencodedWholeRowsEqual = equal,
                OriginalFileComparisons = originalFileComparisons,
                FrameFieldsProjected = false,
                NoListener = true,
                Epmd = false,
                FullTcpBoot = false,
                Cleanup = true,
                RuntimeSha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(typeof(ProcessRuntime).Assembly.Location))),
                SourceOwners = owners,
                Arguments = arguments,
                Comparisons = comparisons
            })
        );
        KernelBootOriginal453Access.Require(passedAll);
    }
}
