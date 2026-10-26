using Erlang.Compiler;
using Erlang.Differential;

namespace Erlang.NifNativeBackend;

// Explicit test/embedding handler, never a production default erlang:load_nif implementation.
internal static class NifNativeEntryControlledChecks
{
    internal static async Task Run(
        string source,
        string module,
        NifNativeReviewedBasicAbi capability,
        Term loadInfo,
        Term? expectedMarker,
        Term? expectedThird,
        string? expectedFailureDiagnostic
    )
    {
        var profile = NifNativeCapabilityProfile.ForReviewedBasicAbi(capability);
        using var compiled = GeneratedModuleCompiler.Compile(source);
        await using var runtime = new ProcessRuntime();
        compiled.Definition.Register(runtime.Modules);
        ModuleCodeImage image = runtime.Modules.CurrentCodeImage(module)
            ?? throw new InvalidOperationException(NifNativeHarnessNames.Broken);
        bool initialized = false;
        runtime.Modules.InstallSourceLoadNifHandler(async (context, captured, arguments) =>
        {
            var caller = captured.Consume(context);
            NifNativeHarness.Require(ReferenceEquals(caller.Image, image));
            NifNativeHarness.Require(arguments.Length == 2 && arguments[0].Equals(Term.String(profile.LibraryPath)));
            await using var reservation = await runtime.Modules.ReserveNifImageAsync(context);
            NifNativeHarness.Require(ReferenceEquals(reservation.Image, caller.Image));
            await reservation.InitializeAndCommitAsync(cancellation => NifNativeImageInitializer.InitializeAsync(
                reservation,
                profile,
                arguments[1],
                cancellation
            ));
            initialized = true;

            return Term.A(NifNativeEntryControlledNames.Ok);
        });
        try
        {
            await NifNativeHarness.Context(
                runtime,
                async context =>
            {
                await AssertStub(compiled.Definition, context);
                NifNativeHarness.Require((await Entry(
                    compiled.Definition,
                    context,
                    NifNativeEntryControlledNames.Ordinary,
                    []
                )).Equals(Term.A(NifNativeEntryControlledNames.Untouched)));
                Exception? failure = null;
                try
                {
                    Term result = await Entry(
                        compiled.Definition,
                        context,
                        NifNativeEntryControlledNames.Load,
                        [Term.String(profile.LibraryPath), loadInfo]
                    );
                    NifNativeHarness.Require(result.Equals(Term.A(NifNativeEntryControlledNames.Ok)));
                }
                catch (Exception error) when (
                    expectedFailureDiagnostic is not null &&
                    error is NotSupportedException or InvalidOperationException &&
                    error.Message == expectedFailureDiagnostic
                )
                {
                    failure = error;
                }
                if (expectedFailureDiagnostic is not null)
                {
                    NifNativeHarness.Require(failure is not null && !initialized);
                    await AssertStub(compiled.Definition, context);
                }
                else
                {
                    NifNativeHarness.Require(failure is null && initialized);
                    Term payload = Term.Tuple(Term.A(NifNativeEntryControlledNames.Payload), Term.List(Term.I(17), Term.I(23)));
                    NifNativeHarness.Require((await Entry(
                        compiled.Definition,
                        context,
                        NifNativeEntryControlledNames.Identity,
                        [payload]
                    )).Equals(payload));
                    NifNativeHarness.Require((await Entry(
                        compiled.Definition,
                        context,
                        NifNativeEntryControlledNames.Marker,
                        []
                    )).Equals(expectedMarker));
                    NifNativeHarness.Require((await Entry(
                        compiled.Definition,
                        context,
                        NifNativeEntryControlledNames.Third,
                        []
                    )).Equals(expectedThird));
                }
                NifNativeHarness.Require((await Entry(
                    compiled.Definition,
                    context,
                    NifNativeEntryControlledNames.Ordinary,
                    []
                )).Equals(Term.A(NifNativeEntryControlledNames.Untouched)));
            }
            );
        }
        finally
        {
            await image.NifBindings.RetireAsync().WaitAsync(NifNativeHarnessNames.Deadline);
        }
    }

    private static async Task AssertStub(ModuleDefinition module, ProcessContext context)
    {
        try
        {
            await Entry(
                module,
                context,
                NifNativeEntryControlledNames.Marker,
                []
            );
        }
        catch (ErlangException failure) when (failure.Reason.Equals(Term.A(NifNativeEntryControlledNames.NotLoaded)))
        {
            return;
        }
        throw new InvalidOperationException(NifNativeHarnessNames.Broken);
    }

    private static ValueTask<Term> Entry(
        ModuleDefinition module,
        ProcessContext context,
        string function,
        IReadOnlyList<Term> arguments
    ) => Execution.InvokeAsync(
        module,
        function,
        context,
        arguments
    );
}
