namespace Erlang.NifNativeBackend;

internal static class NifNativeOwnershipChecks
{
    internal static async Task Run(NifNativeCapabilityProfile profile)
    {
        NifNativePlatformLoader.VerifyPlatform();
        NifNativeExportRegistration.EnsureRegistered(profile);
        using var file = NifNativeCapabilityProfile.OpenVerified(profile.LibraryPath, profile.LibrarySha256);
        nint observer = NifNativePlatformLoader.Open(file, profile.LibraryPath, global: false);
        try
        {
            nint counter = NifNativePlatformLoader.Symbol(observer, NifNativeHarnessNames.UnloadCounter);
            int before = Counter(counter);
            await LoadVeto(profile);
            NifNativeHarness.Require(Counter(counter) == before);
            await SuccessfulImage(profile);
            NifNativeHarness.Require(Counter(counter) == before + 1);
            await NifNativeLifecycleChecks.Run(profile, counter);
        }
        finally
        {
            NifNativePlatformLoader.Close(observer);
        }
    }

    private static unsafe int Counter(nint pointer) => ((delegate* unmanaged[Cdecl]<int>)pointer)();

    private static (object Owner, ModuleCodeImage Image) Register(ProcessRuntime runtime)
    {
        var owner = new object();
        var functions = new[] { (NifNativeNames.Increment, NifNativeNames.IncrementArity), (NifNativeNames.Greeting, NifNativeNames.GreetingArity) };
        foreach (var function in functions)
            runtime.Modules.RegisterSource(
                NifNativeNames.Module,
                function.Item1,
                function.Item2,
                (_, _) => ValueTask.FromResult<Term>(Nil.Value),
                owner
            );
        var descriptor = new ModuleCodeDescriptor(
            functions,
            functions,
            new BitString(new byte[System.Security.Cryptography.MD5.HashSizeInBytes]),
            Nil.Value,
            sourceOwner: owner
        )
        {
            DeclaredNifs = functions
        };
        runtime.Modules.PublishModuleCode(NifNativeNames.Module, descriptor);

        return (owner, runtime.Modules.CurrentCodeImage(NifNativeNames.Module)!);
    }

    private static async Task LoadVeto(NifNativeCapabilityProfile profile)
    {
        await using var runtime = new ProcessRuntime();
        var source = Register(runtime);
        await NifNativeHarness.Context(
            runtime,
            async context =>
        {
            using var flow = context.EnterSourceCode(source.Image, source.Owner);
            var reservation = await runtime.Modules.ReserveNifImageAsync(context);
            Exception? expectedVeto = null;
            try
            {
                await reservation.InitializeAndCommitAsync(cancellation => NifNativeImageInitializer.InitializeAsync(
                    reservation,
                    profile,
                    Term.I(17),
                    cancellation
                ));
                throw new InvalidOperationException(NifNativeHarnessNames.Broken);
            }
            catch (InvalidOperationException failure) when (failure.Message == NifNativeDiagnostics.LoadVeto)
            {
                expectedVeto = failure;
                NifNativeHarness.Require(source.Image.NifBindings.Phase == NifImageBindingPhase.Empty);
                NifNativeHarness.Require(runtime.Modules.GetNifTaints().SequenceEqual(new[] { NifNativeNames.Module }));
            }
            finally
            {
                try
                {
                    await reservation.DisposeAsync();
                }
                catch (Exception failure) when (ReferenceEquals(failure, expectedVeto))
                {
                    // The join rethrows the exact already-observed initialization failure.
                }
            }
        }
        );
    }

    private static async Task SuccessfulImage(NifNativeCapabilityProfile profile)
    {
        await using var runtime = new ProcessRuntime();
        var source = Register(runtime);
        await NifNativeHarness.Context(
            runtime,
            async context =>
        {
            using var flow = context.EnterSourceCode(source.Image, source.Owner);
            await using var reservation = await runtime.Modules.ReserveNifImageAsync(context);
            NifNativeLibraryOwner? nativeOwner = null;
            await reservation.InitializeAndCommitAsync(async cancellation =>
            {
                var initialized = await NifNativeImageInitializer.InitializeAsync(
                    reservation,
                    profile,
                    Term.I(42),
                    cancellation
                );
                nativeOwner = (NifNativeLibraryOwner)initialized.Owner;

                return initialized;
            });
            NifNativeHarness.Require(runtime.Modules.GetNifTaints().SequenceEqual(new[] { NifNativeNames.Module }));
            using (var call = source.Image.NifBindings.Admit(NifNativeNames.Increment, 1)!)
                NifNativeHarness.Require((await call.InvokeAsync(context, [Term.I(41)])).Equals(Term.I(42)));
            using (var call = source.Image.NifBindings.Admit(NifNativeNames.Greeting, 0)!)
                NifNativeHarness.Require((await call.InvokeAsync(context, [])).Equals(Term.String(NifNativeHarnessNames.Greeting)));
            using (var call = source.Image.NifBindings.Admit(NifNativeNames.Increment, 1)!)
            {
                try
                {
                    await call.InvokeAsync(context, [NifNativeHarnessNames.WrongInput]);
                    throw new InvalidOperationException(NifNativeHarnessNames.Broken);
                }
                catch (ErlangException failure) when (failure.Reason.Equals(NifNativeNames.BadArgument))
                {
                }
            }
            using var held = source.Image.NifBindings.Admit(NifNativeNames.Greeting, 0)!;
            Task retirement = source.Image.NifBindings.RetireAsync();
            NifNativeHarness.Require(!retirement.IsCompleted);
            held.Dispose();
            await retirement.WaitAsync(NifNativeHarnessNames.Deadline);
            await source.Image.NifBindings.RetireAsync();
            NifNativeHarness.Require(nativeOwner!.OutstandingAfterNativeUnload == 0);
        }
        );
    }
}
