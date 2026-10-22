using System.Text.Json;

namespace Erlang.Tests;

internal static class CodeServerOriginal410Checks
{
    // Root supplies the real configured runtime (original init/loader/libraries and actual file native bodies).
    // This consumer never installs substitute init/loader handlers or imports expected observations into the VM.
    internal static async Task Run(ProcessRuntime runtime, CancellationToken cancellation)
    {
        CodeServerOriginal410Fixture.Require(runtime.Modules.SourceOwner(CodeServerOriginal410Names.Init,
            CodeServerOriginal410Names.Boot, 1) is PreparedBeamImage);
        foreach (string worker in new[] { CodeServerOriginal410Names.Subject, CodeServerOriginal410Names.Observer })
            CodeServerOriginal410Fixture.Require(!runtime.Modules.IsModuleLoaded(worker));
        string output = Path.GetFullPath(Path.Combine(CodeServerOriginal410Names.OutputRoot,
            Guid.NewGuid().ToString(CodeServerOriginal410Names.GuidFormat)));
        Directory.CreateDirectory(output);
        BitString[] arguments = CodeServerOriginal410Fixture.Create(output);
        await using (var fileActor = new RuntimeNodeFoundationHostActor(runtime))
        {
            string bootFile = Path.Combine(output, CodeServerOriginal410Names.BootBase + CodeServerOriginal410Names.BootExtension);
            Term read = await fileActor.InvokeAsync(async context => await runtime.Modules.Call(
                context,
                CodeServerRuntime410Names.PrimFile,
                CodeServerRuntime410Names.ReadFile,
                CodeServerOriginal410Fixture.Text(bootFile)
            ));
            File.WriteAllText(
                Path.Combine(output, CodeServerRuntime410Names.FileProbe),
                JsonSerializer.Serialize(OwnedFunctionOriginal407Terms.Typed(read))
            );
            CodeServerOriginal410Fixture.Require(read.Equals(Term.Tuple(Term.A(CodeServerRuntime410Names.Ok), new BitString(File.ReadAllBytes(bootFile)))));
            // Diagnostic only: the pinned default prim_state exercises the exported
            // filesystem path before SAFE2 can mask a missing dependency as error.
            Term primitive = await fileActor.InvokeAsync(async context => await runtime.Modules.Call(
                context,
                CodeServerOriginal410Names.Loader,
                CodeServerRuntime410Names.PrimitiveRead,
                Term.Tuple(
                    Term.A(CodeServerRuntime410Names.PrimitiveState),
                    Term.A(CodeServerRuntime410Names.False),
                    Term.A(CodeServerRuntime410Names.Undefined)
                ),
                CodeServerOriginal410Fixture.Text(bootFile)
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
        CodeServerOriginal410Fixture.Require(!session.Completion.IsCompleted);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        Term actualInit = await actor.InvokeAsync(async context => await runtime.Modules.Call(
            context,
            CodeServerOriginal410Names.Erts,
            CodeServerOriginal410Names.Whereis,
            Term.A(CodeServerOriginal410Names.Init)
        ));
        CodeServerOriginal410Fixture.Require(actualInit.Equals(session.InitializationProcess));
        Term system = await actor.InvokeAsync(async context => await runtime.Modules.Call(
            context,
            CodeServerRuntime410Names.ErtsInternal,
            CodeServerRuntime410Names.IsSystemProcess,
            session.InitializationProcess
        ));
        CodeServerOriginal410Fixture.Require(system.Equals(Term.A(ErlangBooleanAtoms.True)));
        foreach (string worker in new[] { CodeServerOriginal410Names.Subject, CodeServerOriginal410Names.Observer })
            CodeServerOriginal410Fixture.Require(runtime.Modules.SourceOwner(worker, CodeServerOriginal410Names.ModuleInfo, 0) is PreparedBeamImage);

        async Task<(Term Term, byte[] Bytes)> Capture(string module, string function, string filename)
        {
            Term value = await actor.InvokeAsync(async context => await runtime.Modules.Call(context, module, function));
            BitString bytes = (BitString)await actor.InvokeAsync(async context => await runtime.Modules.Call(
                context,
                CodeServerOriginal410Names.Erts,
                CodeServerOriginal410Names.Encode,
                value
            ));
            File.WriteAllBytes(Path.Combine(output, CodeServerOriginal410Names.ActualPrefix + filename), bytes.ToArray());
            File.WriteAllText(
                Path.Combine(output, CodeServerOriginal410Names.ActualPrefix + filename + CodeServerOriginal410Names.Json),
                JsonSerializer.Serialize(OwnedFunctionOriginal407Terms.Typed(value))
            );

            return (value, bytes.ToArray());
        }
        var values = await Capture(CodeServerOriginal410Names.Observer, CodeServerOriginal410Names.Run, CodeServerOriginal410Names.ValuesFile);
        var stacks = await Capture(CodeServerOriginal410Names.Observer, CodeServerOriginal410Names.Stacks, CodeServerOriginal410Names.StacksFile);
        var shutdown = await Capture(CodeServerOriginal410Names.Subject, CodeServerOriginal410Names.Shutdown, CodeServerOriginal410Names.ShutdownFile);
        // All real guest routes and encoders have settled before passive original decode.
        var differences = new List<object>();
        int examined = 0, equal = 0;
        foreach (var pair in new[] { (Actual: values, Name: CodeServerOriginal410Names.ValuesFile),
            (Actual: stacks, Name: CodeServerOriginal410Names.StacksFile), (Actual: shutdown, Name: CodeServerOriginal410Names.ShutdownFile) })
        {
            byte[] bytes = CodeServerOriginal410Fixture.Resource(pair.Name);
            Term expected = OwnedFunctionOriginal407Terms.Decode(bytes);
            File.WriteAllBytes(Path.Combine(output, CodeServerOriginal410Names.ExpectedPrefix + pair.Name), bytes);
            File.WriteAllText(
                Path.Combine(output, CodeServerOriginal410Names.ExpectedPrefix + pair.Name + CodeServerOriginal410Names.Json),
                JsonSerializer.Serialize(OwnedFunctionOriginal407Terms.Typed(expected))
            );
            if (pair.Name == CodeServerOriginal410Names.ValuesFile)
                equal += IoPrintableOriginal404Comparison.Rows(
                    expected,
                    pair.Actual.Term,
                    pair.Name,
                    CodeServerOriginal410Names.Rows,
                    differences,
                    ref examined
                );
            else if (pair.Name == CodeServerOriginal410Names.StacksFile)
                equal += IoPrintableOriginal404Comparison.Profiles(
                    expected,
                    pair.Actual.Term,
                    CodeServerOriginal410Names.Rows,
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
            Path.Combine(output, CodeServerOriginal410Names.Receipt),
            JsonSerializer.Serialize(new
            {
                ExpectedRows = CodeServerOriginal410Names.WholeRows,
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
        CodeServerOriginal410Fixture.Require(examined == CodeServerOriginal410Names.WholeRows && equal == examined && differences.Count == 0);
    }
}
