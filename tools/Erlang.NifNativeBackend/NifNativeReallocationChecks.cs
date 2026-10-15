using System.Text.Json;

namespace Erlang.NifNativeBackend;

internal static partial class NifNativeReallocationChecks
{
    private static void Require(bool value)
    {
        if (!value)
            throw new InvalidOperationException(NifNativeReallocationHarnessNames.Broken);
    }

    internal static async Task Run(string[] args)
    {
        NifNativePlatformLoader.VerifyPlatform();
        if (args.Length != (OperatingSystem.IsWindows() ? NifNativeReallocationParameters.WindowsArguments : NifNativeReallocationParameters.LinuxArguments))
            throw new ArgumentException(NifNativeReallocationHarnessNames.Usage);
        var profile = new NifNativeCapabilityProfile(
            args[NifNativeReallocationParameters.NewFixturePathIndex],
            args[NifNativeReallocationParameters.NewFixtureHashIndex],
            NifNativeReallocationNames.ReviewedProfileDigest,
            OperatingSystem.IsLinux() ? args[NifNativeReallocationParameters.OldBridgePathIndex] : null,
            OperatingSystem.IsLinux() ? args[NifNativeReallocationParameters.OldBridgeHashIndex] : null,
            OperatingSystem.IsLinux() ? args[NifNativeReallocationParameters.ExtensionPathIndex] : null,
            OperatingSystem.IsLinux() ? args[NifNativeReallocationParameters.ExtensionHashIndex] : null
        );
        var legacy = new NifNativeCapabilityProfile(
            args[NifNativeReallocationParameters.OldFixturePathIndex],
            args[NifNativeReallocationParameters.OldFixtureHashIndex],
            NifNativeNames.SourceSha256,
            OperatingSystem.IsLinux() ? args[NifNativeReallocationParameters.OldBridgePathIndex] : null,
            OperatingSystem.IsLinux() ? args[NifNativeReallocationParameters.OldBridgeHashIndex] : null
        );
        if (OperatingSystem.IsWindows())
            NifNativeReallocationLayout.Check(
                args[NifNativeReallocationParameters.WindowsLayoutPathIndex],
                args[NifNativeReallocationParameters.WindowsLayoutHashIndex]
            );
        await OneShot(profile, legacy);
        foreach (NifNativeReallocationCase item in NifNativeReallocationParameters.Cases)
            await NativeCase(profile, item);
        await MixedProfile(legacy);
        Console.WriteLine(JsonSerializer.Serialize(new
        {
            Role = NifNativeReallocationHarnessNames.ControlsRole,
            OneShotInitialization = true,
            OldFiveArgumentConstructor = true,
            ActualOldLibraryOwnerMixedPermissionDenied = true,
            NestedPermissionRestored = true,
            LinuxGlobalSymbolResident = OperatingSystem.IsLinux(),
            WindowsHeaderLayoutVerified = OperatingSystem.IsWindows(),
            ActualAllocatorOom = false
        }));
    }

    private static (object SourceOwner, ModuleCodeImage Image, ModuleCodeDescriptor Descriptor) Register(ProcessRuntime runtime)
    {
        var sourceOwner = new object();
        var functions = new[] { (NifNativeNames.Increment, NifNativeNames.IncrementArity), (NifNativeNames.Greeting, NifNativeNames.GreetingArity) };
        foreach (var function in functions)
            runtime.Modules.RegisterSource(
                NifNativeNames.Module,
                function.Item1,
                function.Item2,
                (_, _) => ValueTask.FromResult<Term>(Nil.Value),
                sourceOwner
            );
        var descriptor = new ModuleCodeDescriptor(
            functions,
            functions,
            new BitString(new byte[System.Security.Cryptography.MD5.HashSizeInBytes]),
            Nil.Value,
            sourceOwner: sourceOwner
        )
        {
            DeclaredNifs = functions
        };
        runtime.Modules.PublishModuleCode(NifNativeNames.Module, descriptor);

        return (sourceOwner, runtime.Modules.CurrentCodeImage(NifNativeNames.Module)!, descriptor);
    }

    private static async Task WithLibrary(
        NifNativeCapabilityProfile profile,
        Func<ProcessRuntime, ProcessContext, ModuleCodeImage, NifNativeReallocationLibrary, Task> body
    )
    {
        await using var runtime = new ProcessRuntime();
        var source = Register(runtime);
        await NifNativeHarness.Context(
            runtime,
            async context =>
        {
            using var flow = context.EnterSourceCode(source.Image, source.SourceOwner);
            await using var reservation = await runtime.Modules.ReserveNifImageAsync(context);
            NifNativeLibraryOwner? owner = null;
            await reservation.InitializeAndCommitAsync(async cancellation =>
            {
                NifImageInitialized result = await NifNativeImageInitializer.InitializeAsync(
                    reservation,
                    profile,
                    Term.I(NifNativeReallocationParameters.LoadMarker),
                    cancellation
                );
                owner = (NifNativeLibraryOwner)result.Owner;

                return result;
            });
            await body(
                runtime,
                context,
                source.Image,
                new(owner!)
            );
        }
        );
        // Actual runtime/image disposal, not a detached callback owner, closes membership.
        await runtime.DisposeAsync();
        Require(runtime.NativeAllocationSnapshot.JoinedOwners == 0);
    }
}
