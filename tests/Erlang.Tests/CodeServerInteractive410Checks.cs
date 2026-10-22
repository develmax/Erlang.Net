using System.Text.Json;

namespace Erlang.Tests;

internal static class CodeServerInteractive410Checks
{
    internal static Task Empty() => Compare(false);

    internal static Task App() => Compare(true);

    private static async Task Compare(bool app)
    {
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await using ProcessRuntime runtime = await CodeServerRuntime410Checks.Create(
            [CodeServerInteractive410Names.Subject, CodeServerInteractive410Names.Observer]
        );
        await using (var environmentActor = new RuntimeNodeFoundationHostActor(runtime))
        {
            await environmentActor.InvokeAsync(async context =>
            {
                _ = await runtime.Modules.Call(
                    context,
                    CodeServerInteractive410Names.Os,
                    CodeServerInteractive410Names.UnsetEnv,
                    CodeServerInteractive410Fixture.Text(CodeServerInteractive410Names.ErlLibs)
                );
                Term value = await runtime.Modules.Call(
                    context,
                    CodeServerInteractive410Names.Os,
                    CodeServerInteractive410Names.GetEnv,
                    CodeServerInteractive410Fixture.Text(CodeServerInteractive410Names.ErlLibs)
                );
                CodeServerInteractive410Fixture.Require(value.Equals(Term.A(ErlangBooleanAtoms.False)));

                return true;
            });
        }
        await Run(runtime, app, cancellation.Token);
    }

    // Root supplies the real configured runtime (original init/loader/libraries and actual file native bodies).
    // This consumer never installs substitute init/loader handlers or imports expected observations into the VM.
    internal static async Task Run(ProcessRuntime runtime, bool app, CancellationToken cancellation)
    {
        CodeServerInteractive410Fixture.Require(runtime.Modules.SourceOwner(CodeServerInteractive410Names.Init,
            CodeServerInteractive410Names.Boot, 1) is PreparedBeamImage);
        foreach (string worker in new[] { CodeServerInteractive410Names.Subject, CodeServerInteractive410Names.Observer })
            CodeServerInteractive410Fixture.Require(!runtime.Modules.IsModuleLoaded(worker));
        string output = Path.GetFullPath(Path.Combine(
            CodeServerInteractive410Names.OutputRoot,
            app ? CodeServerInteractive410Names.App : CodeServerInteractive410Names.Empty,
            Guid.NewGuid().ToString(CodeServerInteractive410Names.GuidFormat)
        ));
        Directory.CreateDirectory(output);
        BitString[] arguments = CodeServerInteractive410Fixture.Create(output, app);
        await using (var fileActor = new RuntimeNodeFoundationHostActor(runtime))
        {
            string bootFile = Path.Combine(output, CodeServerInteractive410Names.BootBase + CodeServerInteractive410Names.BootExtension);
            Term read = await fileActor.InvokeAsync(async context => await runtime.Modules.Call(
                context,
                CodeServerRuntime410Names.PrimFile,
                CodeServerRuntime410Names.ReadFile,
                CodeServerInteractive410Fixture.Text(bootFile)
            ));
            File.WriteAllText(
                Path.Combine(output, CodeServerRuntime410Names.FileProbe),
                JsonSerializer.Serialize(OwnedFunctionOriginal407Terms.Typed(read))
            );
            CodeServerInteractive410Fixture.Require(read.Equals(Term.Tuple(Term.A(CodeServerRuntime410Names.Ok), new BitString(File.ReadAllBytes(bootFile)))));
            // Diagnostic only: the pinned default prim_state exercises the exported
            // filesystem path before SAFE2 can mask a missing dependency as error.
            Term primitive = await fileActor.InvokeAsync(async context => await runtime.Modules.Call(
                context,
                CodeServerInteractive410Names.Loader,
                CodeServerRuntime410Names.PrimitiveRead,
                Term.Tuple(
                    Term.A(CodeServerRuntime410Names.PrimitiveState),
                    Term.A(CodeServerRuntime410Names.False),
                    Term.A(CodeServerRuntime410Names.Undefined)
                ),
                CodeServerInteractive410Fixture.Text(bootFile)
            ));
            File.WriteAllText(
                Path.Combine(output, CodeServerRuntime410Names.LoaderProbe),
                JsonSerializer.Serialize(OwnedFunctionOriginal407Terms.Typed(primitive))
            );
        }
        Task<RuntimeOtpBootSession> startup = runtime.StartOtpBootAsync(arguments, cancellation);
        if (await Task.WhenAny(startup, Task.Delay(TimeSpan.FromSeconds(1), cancellation)) != startup)
            await CodeServerBootstrap410Diagnostic.Capture(runtime, output);
        RuntimeOtpBootSession session = await startup;
        CodeServerInteractive410Fixture.Require(!session.Completion.IsCompleted);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        Term actualInit = await actor.InvokeAsync(async context => await runtime.Modules.Call(
            context,
            CodeServerInteractive410Names.Erts,
            CodeServerInteractive410Names.Whereis,
            Term.A(CodeServerInteractive410Names.Init)
        ));
        CodeServerInteractive410Fixture.Require(actualInit.Equals(session.InitializationProcess));
        Term system = await actor.InvokeAsync(async context => await runtime.Modules.Call(
            context,
            CodeServerRuntime410Names.ErtsInternal,
            CodeServerRuntime410Names.IsSystemProcess,
            session.InitializationProcess
        ));
        CodeServerInteractive410Fixture.Require(system.Equals(Term.A(ErlangBooleanAtoms.True)));
        foreach (string worker in new[] { CodeServerInteractive410Names.Subject, CodeServerInteractive410Names.Observer })
            CodeServerInteractive410Fixture.Require(runtime.Modules.SourceOwner(worker, CodeServerInteractive410Names.ModuleInfo, 0) is PreparedBeamImage);

        async Task<(Term Term, byte[] Bytes)> Capture(string module, string function, string filename)
        {
            Term value = await actor.InvokeAsync(async context => await runtime.Modules.Call(context, module, function));
            BitString bytes = (BitString)await actor.InvokeAsync(async context => await runtime.Modules.Call(
                context,
                CodeServerInteractive410Names.Erts,
                CodeServerInteractive410Names.Encode,
                value
            ));
            File.WriteAllBytes(Path.Combine(output, CodeServerInteractive410Names.ActualPrefix + filename), bytes.ToArray());
            File.WriteAllText(
                Path.Combine(output, CodeServerInteractive410Names.ActualPrefix + filename + CodeServerInteractive410Names.Json),
                JsonSerializer.Serialize(OwnedFunctionOriginal407Terms.Typed(value))
            );

            return (value, bytes.ToArray());
        }
        var values = await Capture(CodeServerInteractive410Names.Observer, CodeServerInteractive410Names.Run, CodeServerInteractive410Names.ValuesFile);
        var stacks = await Capture(CodeServerInteractive410Names.Observer, CodeServerInteractive410Names.Stacks, CodeServerInteractive410Names.StacksFile);
        var shutdown = await Capture(
            CodeServerInteractive410Names.Subject,
            CodeServerInteractive410Names.Shutdown,
            CodeServerInteractive410Names.ShutdownFile
        );
        // All real guest routes and encoders have settled before passive original decode.
        var differences = new List<object>();
        int examined = 0, equal = 0;
        foreach (var pair in new[] { (Actual: values, Name: CodeServerInteractive410Names.ValuesFile),
            (Actual: stacks, Name: CodeServerInteractive410Names.StacksFile), (Actual: shutdown, Name: CodeServerInteractive410Names.ShutdownFile) })
        {
            byte[] bytes = CodeServerInteractive410Fixture.Resource((app ? CodeServerInteractive410Names.App : CodeServerInteractive410Names.Empty)
                + CodeServerInteractive410Names.Separator + pair.Name);
            Term expected = OwnedFunctionOriginal407Terms.Decode(bytes);
            File.WriteAllBytes(Path.Combine(output, CodeServerInteractive410Names.ExpectedPrefix + pair.Name), bytes);
            File.WriteAllText(
                Path.Combine(output, CodeServerInteractive410Names.ExpectedPrefix + pair.Name + CodeServerInteractive410Names.Json),
                JsonSerializer.Serialize(OwnedFunctionOriginal407Terms.Typed(expected))
            );
            if (pair.Name == CodeServerInteractive410Names.ValuesFile)
                equal += IoPrintableOriginal404Comparison.Rows(
                    expected,
                    pair.Actual.Term,
                    pair.Name,
                    CodeServerInteractive410Names.Rows,
                    differences,
                    ref examined
                );
            else if (pair.Name == CodeServerInteractive410Names.StacksFile)
                equal += IoPrintableOriginal404Comparison.Profiles(
                    expected,
                    pair.Actual.Term,
                    CodeServerInteractive410Names.Rows,
                    differences,
                    ref examined
                );
            else
            {
                examined++;
                if (expected.Equals(pair.Actual.Term)) equal++;
            }
            if (!expected.Equals(pair.Actual.Term) || !bytes.AsSpan().SequenceEqual(pair.Actual.Bytes))
                differences.Add(new { File = pair.Name, TypedEqual = expected.Equals(pair.Actual.Term), RawEqual = bytes.AsSpan().SequenceEqual(pair.Actual.Bytes) });
        }
        File.WriteAllText(
            Path.Combine(output, CodeServerInteractive410Names.Receipt),
            JsonSerializer.Serialize(new
            {
                Cohort = app ? CodeServerInteractive410Names.App : CodeServerInteractive410Names.Empty,
                ExpectedRows = CodeServerInteractive410Names.WholeRows,
                Examined = examined,
                Equal = equal,
                Differences = differences,
                SessionInitializationMatchesActualRegisteredPid = actualInit.Equals(session.InitializationProcess),
                OriginalNativeProviders = runtime.NativePrimitives,
                ExpectedDecodedAfterAllActualRoutes = true,
                StockCodeServerReplaced = false,
                FakeServiceReplies = false,
                FrameProjection = false
            })
        );
        CodeServerInteractive410Fixture.Require(examined == CodeServerInteractive410Names.WholeRows && equal == examined && differences.Count == 0);
    }
}
