using System.Runtime.InteropServices;

using Erlang.Compiler;
using Erlang.Differential;

namespace Erlang.NifNativeBackend;

internal static class NifNativeDefaultRouteChecks
{
    internal static async Task Run()
    {
        EntryModuleDiagnostic();
        await PolicyAndFilename();
        await HostPrecedence();
        await NoPolicyFallback();
        await CurrentReservationAndOldCaller();
        await MissingFileAndHashAdmission();
        Console.WriteLine(NifNativeDefaultRouteNames.Pass);
    }

    private static void EntryModuleDiagnostic()
    {
        nint name = Marshal.StringToCoTaskMemUTF8(NifNativeDefaultRouteNames.Module);
        nint variant = Marshal.StringToCoTaskMemUTF8(NifNativeNames.Variant);
        try
        {
            var entry = new NifNativeAbiEntry
            {
                Name = name,
                Variant = variant,
                Options = NifNativeAbi.InitializerEntryOptions
            };
            var descriptor = new ModuleCodeDescriptor(
                [],
                [],
                new BitString(new byte[System.Security.Cryptography.MD5.HashSizeInBytes]),
                Nil.Value
            );
            NifNativeHarness.Require(NifNativeEntryFunctions.Read(entry, NifNativeDefaultRouteNames.Module, descriptor).Count == 0);
            try
            {
                NifNativeEntryFunctions.Read(entry, NifNativeDefaultRouteNames.ProbeModule, descriptor);
                throw new InvalidOperationException(NifNativeHarnessNames.Broken);
            }
            catch (NotSupportedException failure) when (failure.Message == NifNativeDiagnostics.ModuleIdentity)
            {
            }
        }
        finally
        {
            Marshal.FreeCoTaskMem(variant);
            Marshal.FreeCoTaskMem(name);
        }
    }

    private sealed class PolicyObserved : Exception { }

    private static async Task PolicyAndFilename()
    {
        using var compiled = GeneratedModuleCompiler.Compile(NifNativeDefaultRouteNames.Source);
        await using var runtime = new ProcessRuntime();
        compiled.Definition.Register(runtime.Modules);
        var sentinel = new PolicyObserved();
        string? observed = null;
        int calls = 0;
        runtime.Modules.InstallReviewedNativeExecutionPolicy(path =>
        {
            calls++;
            observed = path;
            throw sentinel;
        });
        await NifNativeHarness.Context(
            runtime,
            async context =>
        {
            Term nested = Term.List(Term.String(NifNativeDefaultRouteNames.RelativeStem), Nil.Value);
            try
            {
                await Load(compiled.Definition, context, nested);
                throw new InvalidOperationException(NifNativeHarnessNames.Broken);
            }
            catch (PolicyObserved error) when (ReferenceEquals(error, sentinel))
            {
            }
            string suffix = OperatingSystem.IsWindows()
                ? NifNativeDefaultLoaderNames.WindowsSuffix : NifNativeDefaultLoaderNames.UnixSuffix;
            string expected = Path.Combine(Environment.CurrentDirectory, NifNativeDefaultRouteNames.RelativeStem + suffix);
            NifNativeHarness.Require(calls == 1 && observed == expected);
            try
            {
                await Load(compiled.Definition, context, Term.I(42));
                throw new InvalidOperationException(NifNativeHarnessNames.Broken);
            }
            catch (ErlangException error) when (error.Reason.Equals(Term.A(ErlangErrorReasons.BadArgument)))
            {
            }
            NifNativeHarness.Require(calls == 1);
            // An actor context without an executing source image cannot reach the policy.
            try
            {
                await runtime.Modules.InvokeSourceLoadNif(context, [Term.String(NifNativeDefaultRouteNames.RelativeStem), Nil.Value]);
                throw new InvalidOperationException(NifNativeHarnessNames.Broken);
            }
            catch (NifImageAdmissionException error) when (error.Failure == NifImageAdmissionFailure.NoExecutingCaller)
            {
            }
            NifNativeHarness.Require(calls == 1);
        }
        );
    }

    private static async Task HostPrecedence()
    {
        using var compiled = GeneratedModuleCompiler.Compile(NifNativeDefaultRouteNames.Source);
        await using var runtime = new ProcessRuntime();
        compiled.Definition.Register(runtime.Modules);
        int approvals = 0;
        int overrides = 0;
        runtime.Modules.InstallReviewedNativeExecutionPolicy(_ =>
        {
            approvals++;
            throw new PolicyObserved();
        });
        runtime.Modules.InstallSourceLoadNifHandler((_, _, _) =>
        {
            overrides++;

            return ValueTask.FromResult<Term>(Term.A(NifNativeDefaultRouteNames.HostResult));
        });
        await NifNativeHarness.Context(
            runtime,
            async context =>
        {
            Term actual = await Load(compiled.Definition, context, Term.I(42));
            NifNativeHarness.Require(actual.Equals(Term.A(NifNativeDefaultRouteNames.HostResult)) && overrides == 1 && approvals == 0);
        }
        );
    }

    private static async Task NoPolicyFallback()
    {
        using var compiled = GeneratedModuleCompiler.Compile(NifNativeDefaultRouteNames.Source);
        await using var runtime = new ProcessRuntime();
        compiled.Definition.Register(runtime.Modules);
        await NifNativeHarness.Context(
            runtime,
            async context =>
        {
            NifNativeHarness.Require((await Load(
                compiled.Definition,
                context,
                Term.String(NifNativeDefaultRouteNames.RelativeStem)
            )).Equals(ErlangLoadNifTerms.IndirectCallError));
        }
        );
    }

    private static async Task CurrentReservationAndOldCaller()
    {
        using var original = GeneratedModuleCompiler.Compile(NifNativeDefaultRouteNames.Source);
        using var replacement = GeneratedModuleCompiler.Compile(NifNativeDefaultRouteNames.Source);
        await using var runtime = new ProcessRuntime();
        original.Definition.Register(runtime.Modules);
        ModuleCodeImage originalImage = runtime.Modules.CurrentCodeImage(NifNativeDefaultRouteNames.Module)!;
        int reservations = 0;
        runtime.Modules.RegisterIntrinsic(
            NifNativeDefaultRouteNames.ProbeModule,
            NifNativeDefaultRouteNames.Reserve,
            0,
            async (context, _) =>
        {
            await using var reservation = await runtime.Modules.ReserveNifImageAsync(context);
            NifNativeHarness.Require(ReferenceEquals(reservation.Image, originalImage));
            reservations++;

            return Nil.Value;
        }
        );
        await NifNativeHarness.Context(
            runtime,
            async context =>
        {
            for (int attempt = 0; attempt < 2; attempt++)
            {
                Term result = await Execution.InvokeAsync(
                    original.Definition,
                    NifNativeDefaultRouteNames.Reserve,
                    context,
                    []
                );
                NifNativeHarness.Require(result.Equals(Term.Tuple(Term.A(NifNativeDefaultRouteNames.Reserved), Nil.Value)));
            }
            NifNativeHarness.Require(reservations == 2);
        }
        );
        // Retain the actual old compiled image; no fake EnterSourceCode or constructor reexecution.
        runtime.Modules.DeleteModule(NifNativeDefaultRouteNames.Module);
        replacement.Definition.Register(runtime.Modules);
        runtime.Modules.InstallReviewedNativeExecutionPolicy(path => new NifNativeReviewedBasicAbi(
            path,
            NifNativeDefaultRouteNames.UnopenedHash,
            NifNativeDefaultRouteNames.UnopenedHash,
            OperatingSystem.IsLinux() ? Path.Combine(Environment.CurrentDirectory, NifNativeDefaultRouteNames.BridgeLeaf) : null,
            OperatingSystem.IsLinux() ? NifNativeDefaultRouteNames.UnopenedHash : null
        ));
        await NifNativeHarness.Context(
            runtime,
            async context =>
        {
            Term result = await Load(original.Definition, context, Term.String(NifNativeDefaultRouteNames.RelativeStem));
            NifNativeHarness.Require(result is TupleTerm { Items.Count: 2 } outer &&
                outer.Items[0].Equals(Term.A(NifNativeDefaultRouteNames.Error)) &&
                outer.Items[1] is TupleTerm { Items.Count: 2 } detail &&
                detail.Items[0].Equals(Term.A(NifNativeDefaultLoaderNames.OldCode)));
        }
        );
    }

    private static async Task MissingFileAndHashAdmission()
    {
        string unique = NifNativeDefaultRouteNames.MissingPrefix + Guid.NewGuid().ToString("N");
        string root = Path.Combine(Environment.CurrentDirectory, "artifacts");
        Directory.CreateDirectory(root);
        foreach (bool missingDirectory in new[] { false, true })
        {
            string stem = Path.Combine(root, missingDirectory ? Path.Combine(unique, unique) : unique);
            using var compiled = GeneratedModuleCompiler.Compile(NifNativeDefaultRouteNames.Source);
            await using var runtime = new ProcessRuntime();
            compiled.Definition.Register(runtime.Modules);
            runtime.Modules.InstallReviewedNativeExecutionPolicy(path => Capability(path));
            await NifNativeHarness.Context(
                runtime,
                async context =>
            {
                Term result = await Load(compiled.Definition, context, Term.String(stem));
                NifNativeHarness.Require(result is TupleTerm { Items.Count: 2 } outer &&
                    outer.Items[0].Equals(Term.A(NifNativeDefaultRouteNames.Error)) &&
                    outer.Items[1] is TupleTerm { Items.Count: 2 } detail &&
                    detail.Items[0].Equals(Term.A(NifNativeDefaultLoaderNames.LoadFailed)));
            }
            );
        }
        string suffix = OperatingSystem.IsWindows()
            ? NifNativeDefaultLoaderNames.WindowsSuffix : NifNativeDefaultLoaderNames.UnixSuffix;
        string badStem = Path.Combine(root, unique + NifNativeDefaultRouteNames.HashLeaf);
        string badPath = badStem + suffix;
        bool created = false;
        try
        {
            using (var file = new FileStream(
                badPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None
            ))
            {
                created = true;
                file.WriteByte(1);
            }
            using var compiled = GeneratedModuleCompiler.Compile(NifNativeDefaultRouteNames.Source);
            await using var runtime = new ProcessRuntime();
            compiled.Definition.Register(runtime.Modules);
            runtime.Modules.InstallReviewedNativeExecutionPolicy(path => Capability(path));
            await NifNativeHarness.Context(
                runtime,
                async context =>
            {
                try
                {
                    await Load(compiled.Definition, context, Term.String(badStem));
                    throw new InvalidOperationException(NifNativeHarnessNames.Broken);
                }
                catch (InvalidOperationException failure) when (failure.Message == NifNativeDiagnostics.UntrustedLibraryHash)
                {
                }
            }
            );
        }
        finally
        {
            if (created)
                File.Delete(badPath);
        }
    }

    private static NifNativeReviewedBasicAbi Capability(string path) => new(
        path,
        NifNativeDefaultRouteNames.UnopenedHash,
        NifNativeDefaultRouteNames.UnopenedHash,
        OperatingSystem.IsLinux() ? Path.Combine(Environment.CurrentDirectory, NifNativeDefaultRouteNames.BridgeLeaf) : null,
        OperatingSystem.IsLinux() ? NifNativeDefaultRouteNames.UnopenedHash : null
    );

    private static ValueTask<Term> Load(ModuleDefinition module, ProcessContext context, Term path) => Execution.InvokeAsync(
        module,
        NifNativeDefaultRouteNames.Load,
        context,
        [path]
    );
}
