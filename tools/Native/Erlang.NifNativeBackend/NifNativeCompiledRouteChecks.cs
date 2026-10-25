using Erlang.Compiler;
using Erlang.Differential;

namespace Erlang.NifNativeBackend;

internal static class NifNativeCompiledRouteChecks
{
    internal static async Task Run(NifNativeCapabilityProfile profile)
    {
        NifNativeExportRegistration.EnsureRegistered(profile);
        using var verifiedFile = NifNativeCapabilityProfile.OpenVerified(profile.LibraryPath, profile.LibrarySha256);
        nint observer = NifNativePlatformLoader.Open(verifiedFile, profile.LibraryPath, global: false);
        try
        {
            int before = Counter(observer);
            using var compiled = GeneratedModuleCompiler.Compile(NifNativeCompiledRouteNames.Source);
            await using var runtime = new ProcessRuntime();
            compiled.Definition.Register(runtime.Modules);
            ModuleCodeImage image = runtime.Modules.CurrentCodeImage(NifNativeNames.Module)!;
            NifNativeLibraryOwner? nativeOwner = null;
            // A real host intrinsic preserves the actual calling source image flow.
            // It is a development-only bridge, not erlang:load_nif or an artificial source scope.
            runtime.Modules.RegisterIntrinsic(
                NifNativeCompiledRouteNames.BridgeModule,
                NifNativeCompiledRouteNames.Install,
                0,
                async (context, _) =>
                {
                    var caller = context.CaptureExecutingCode();
                    NifNativeHarness.Require(caller is not null && ReferenceEquals(caller.Image, image) &&
                        ReferenceEquals(caller.Owner, compiled.Definition));
                    await using var reservation = await runtime.Modules.ReserveNifImageAsync(context);
                    await reservation.InitializeAndCommitAsync(async cancellation =>
                    {
                        var initialized = await NifNativeImageInitializer.InitializeAsync(
                            reservation,
                            profile,
                            Term.I(NifNativeCompiledRouteNames.Expected),
                            cancellation
                        );
                        nativeOwner = (NifNativeLibraryOwner)initialized.Owner;

                        return initialized;
                    });

                    return NifNativeCompiledRouteNames.Installed;
                }
            );
            try
            {
                await NifNativeHarness.Context(
                    runtime,
                    async context =>
                {
                    try
                    {
                        await Entry(
                            compiled.Definition,
                            context,
                            NifNativeNames.Increment,
                            [Term.I(NifNativeCompiledRouteNames.Input)]
                        );
                        throw new InvalidOperationException(NifNativeHarnessNames.Broken);
                    }
                    catch (ErlangException failure) when (failure.Reason.Equals(NifNativeCompiledRouteNames.NotLoadedReason))
                    {
                    }
                    NifNativeHarness.Require((await Entry(
                        compiled.Definition,
                        context,
                        NifNativeCompiledRouteNames.Plain,
                        []
                    )).Equals(Term.I(NifNativeCompiledRouteNames.PlainValue)));
                    NifNativeHarness.Require((await Entry(
                        compiled.Definition,
                        context,
                        NifNativeCompiledRouteNames.Install,
                        []
                    )).Equals(NifNativeCompiledRouteNames.Installed));
                    foreach (string route in NifNativeCompiledRouteNames.Routes)
                        NifNativeHarness.Require((await Entry(
                            compiled.Definition,
                            context,
                            route,
                            [Term.I(NifNativeCompiledRouteNames.Input)]
                        )).Equals(Term.I(NifNativeCompiledRouteNames.Expected)));
                    NifNativeHarness.Require((await runtime.Modules.Call(
                        context,
                        NifNativeNames.Module,
                        NifNativeNames.Increment,
                        Term.I(NifNativeCompiledRouteNames.Input)
                    )).Equals(Term.I(NifNativeCompiledRouteNames.Expected)));
                    NifNativeHarness.Require((await Entry(
                        compiled.Definition,
                        context,
                        NifNativeNames.Greeting,
                        []
                    )).Equals(Term.String(NifNativeHarnessNames.Greeting)));
                    NifNativeHarness.Require((await Entry(
                        compiled.Definition,
                        context,
                        NifNativeCompiledRouteNames.Local,
                        [Term.I(NifNativeCompiledRouteNames.Input)]
                    )).Equals(
                        Term.Tuple(Term.I(NifNativeCompiledRouteNames.Expected), Term.String(NifNativeHarnessNames.Greeting))));
                    try
                    {
                        await Entry(
                            compiled.Definition,
                            context,
                            NifNativeNames.Increment,
                            [NifNativeHarnessNames.WrongInput]
                        );
                        throw new InvalidOperationException(NifNativeHarnessNames.Broken);
                    }
                    catch (ErlangException failure) when (failure.Reason.Equals(NifNativeNames.BadArgument))
                    {
                    }
                    NifNativeHarness.Require((await Entry(
                        compiled.Definition,
                        context,
                        NifNativeCompiledRouteNames.Plain,
                        []
                    )).Equals(Term.I(NifNativeCompiledRouteNames.PlainValue)));
                }
                );
            }
            finally
            {
                await image.NifBindings.RetireAsync().WaitAsync(NifNativeHarnessNames.Deadline);
            }
            NifNativeHarness.Require(nativeOwner is not null && nativeOwner.OutstandingAfterNativeUnload == 0);
            NifNativeHarness.Require(Counter(observer) == before + 1);
            Console.WriteLine(NifNativeCompiledRouteNames.SourceHashLabel + compiled.CSharpSha256);
            Console.WriteLine(NifNativeCompiledRouteNames.Pass);
        }
        finally
        {
            NifNativePlatformLoader.Close(observer);
        }
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

    private static unsafe int Counter(nint observer)
    {
        var counter = (delegate* unmanaged[Cdecl]<int>)NifNativePlatformLoader.Symbol(observer, NifNativeHarnessNames.UnloadCounter);

        return counter();
    }
}
