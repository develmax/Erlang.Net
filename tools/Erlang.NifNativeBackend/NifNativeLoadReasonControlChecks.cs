using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Erlang.NifNativeBackend;

internal static class NifNativeLoadReasonControlChecks
{
    internal static async Task Run()
    {
        await using var runtime = new ProcessRuntime();
        Entry(runtime, NifNativeLoadReasonCheckNames.DuplicateCase);
        Entry(runtime, NifNativeLoadReasonCheckNames.UndeclaredCase);
        Entry(runtime, NifNativeLoadReasonCheckNames.MissingCase);
        Entry(runtime, NifNativeLoadReasonCheckNames.MismatchCase);
        var veto = new NifNativeLoadVetoException(NifNativeLoadReasonCheckNames.VetoCode);
        NifNativeHarness.Require(veto.Message == NifNativeDiagnostics.LoadVeto && veto.ReturnCode == NifNativeLoadReasonCheckNames.VetoCode);
        AssertReason(runtime, NifNativeLoadReasonCheckNames.VetoCase, veto.PublicReason);
        NifNativeHarness.Require(new NifNativeLoadVetoException(NifNativeLoadReasonCheckNames.NegativeVetoCode).PublicReason.Format(runtime) == NifNativeLoadReasonCheckNames.NegativeVetoReason);
        var quoted = new NifNativeLoadReason(
            NifNativeLoadReasonKind.MissingFunction,
            NifNativeLoadReasonCheckNames.QuotedModule,
            string.Empty,
            NifNativeLoadReasonCheckNames.MissingFunction,
            0,
            0
        );
        NifNativeHarness.Require(quoted.Format(runtime) == NifNativeLoadReasonCheckNames.QuotedMissingReason);
        var control = new NifNativeLoadReason(
            NifNativeLoadReasonKind.ModuleMismatch,
            NifNativeLoadReasonCheckNames.ControlModule,
            NifNativeLoadReasonCheckNames.AlphaModule,
            string.Empty,
            0,
            0
        );
        NifNativeHarness.Require(control.Format(runtime) == NifNativeLoadReasonCheckNames.ControlMismatchReason);
        Entry(runtime, NifNativeLoadReasonCheckNames.MissingCase, unsignedArity: true);
    }

    private static unsafe void Entry(
        ProcessRuntime runtime,
        string caseName,
        bool unsignedArity = false
    )
    {
        bool duplicate = caseName == NifNativeLoadReasonCheckNames.DuplicateCase;
        bool missing = caseName == NifNativeLoadReasonCheckNames.MissingCase;
        bool mismatch = caseName == NifNativeLoadReasonCheckNames.MismatchCase;
        string module = duplicate ? NifNativeLoadReasonCheckNames.AlphaModule : NifNativeLoadReasonCheckNames.BetaModule;
        string libraryModule = mismatch ? NifNativeLoadReasonCheckNames.AlphaModule : module;
        string function = missing ? NifNativeLoadReasonCheckNames.MissingFunction : duplicate ? NifNativeElevenNames.Identity : NifNativeElevenNames.Ordinary;
        uint arity = unsignedArity ? uint.MaxValue : duplicate ? NifNativeLoadReasonCheckNames.IdentityArity : 0;
        var descriptor = new ModuleCodeDescriptor(
            [(NifNativeElevenNames.Identity, (int)NifNativeLoadReasonCheckNames.IdentityArity), (NifNativeElevenNames.Ordinary, 0)],
            [],
            new BitString(new byte[MD5.HashSizeInBytes]),
            Nil.Value
        )
        { DeclaredNifs = [(NifNativeElevenNames.Identity, (int)NifNativeLoadReasonCheckNames.IdentityArity)] };
        int count = duplicate ? NifNativeLoadReasonCheckNames.DuplicateEntries : NifNativeLoadReasonCheckNames.SingleEntry;
        nint moduleName = 0;
        nint variant = 0;
        nint functionName = 0;
        nint entries = 0;
        try
        {
            moduleName = Marshal.StringToCoTaskMemUTF8(libraryModule);
            variant = Marshal.StringToCoTaskMemUTF8(NifNativeNames.Variant);
            functionName = Marshal.StringToCoTaskMemUTF8(function);
            entries = Marshal.AllocHGlobal(checked(sizeof(NifNativeAbiFunction) * count));
            for (int index = 0; index < count; index++)
                ((NifNativeAbiFunction*)entries)[index] = new NifNativeAbiFunction
                {
                    Name = functionName,
                    Arity = arity,
                    Function = NifNativeLoadReasonCheckNames.NeverInvokedFunctionAddress
                };
            var entry = new NifNativeAbiEntry
            {
                Major = NifNativeAbi.MajorVersion,
                Minor = NifNativeAbi.MinorVersion,
                Name = moduleName,
                Variant = variant,
                Options = NifNativeAbi.InitializerEntryOptions,
                FunctionCount = count,
                Functions = entries
            };
            try
            {
                NifNativeEntryFunctions.Read(entry, module, descriptor);
            }
            catch (NifNativeEntryAdmissionException failure)
            {
                string legacy = mismatch ? NifNativeDiagnostics.ModuleIdentity : duplicate ? NifNativeDiagnostics.UntrustedFunction : NifNativeDiagnostics.NoDeclaredNif;
                NifNativeHarness.Require(failure.Message == legacy);
                if (unsignedArity)
                    NifNativeHarness.Require(failure.PublicReason.Kind == NifNativeLoadReasonKind.MissingFunction && failure.PublicReason.Format(runtime) == NifNativeLoadReasonCheckNames.UnsignedArityReason);
                else
                    AssertReason(runtime, caseName, failure.PublicReason);

                return;
            }
            throw new InvalidOperationException(NifNativeHarnessNames.Broken);
        }
        finally
        {
            Marshal.FreeHGlobal(entries);
            Marshal.FreeCoTaskMem(functionName);
            Marshal.FreeCoTaskMem(variant);
            Marshal.FreeCoTaskMem(moduleName);
        }
    }

    private static void AssertReason(
        ProcessRuntime runtime,
        string caseName,
        NifNativeLoadReason reason
    ) => NifNativeLoadReasonChecks.AssertOriginal(
        caseName,
        Term.Tuple(
            Term.A(NifNativeElevenNames.Error),
            Term.Tuple(Term.A(reason.Category), Term.String(reason.Format(runtime)))
        )
    );
}
