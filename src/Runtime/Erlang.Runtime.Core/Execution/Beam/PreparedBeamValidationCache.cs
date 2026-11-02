using System.Collections.Frozen;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;

namespace Erlang;

internal static class PreparedBeamValidationCache
{
    // CWT is reference keyed even though BeamLinkedCode is a record. A `with`
    // clone, mutable raw IR, or equal MFA from another image gets no certificate.
    private static readonly ConditionalWeakTable<BeamLinkedCode, PreparedBeamValidationCertificate> prepared = new();

    internal static bool TryGet(BeamLinkedCode code, out PreparedBeamValidationCertificate? certificate)
        => prepared.TryGetValue(code, out certificate);

    // Sole caller is ModuleRegistry.PrepareBeam's fresh owned reader pipeline,
    // AFTER the final dependency-selection clone and successful validations.
    // The readers privately allocate backing lists/arrays and expose readonly
    // views; no caller-owned IR or byte buffer reaches this certification seam.
    internal static void SealFreshPreparedCode(BeamLinkedCode code, IReadOnlyList<BeamLinkedFunction> validated)
    {
        if (!Readonly(code.Instructions) || !Readonly(code.Functions) || !Readonly(code.Exports)
            || !Readonly(code.Lambdas) || !Readonly(code.Types.Entries)
            || !Readonly(code.Structure.Chunks) || !Readonly(code.Structure.Atoms)
            || !Readonly(code.Structure.Imports) || !Readonly(code.Structure.Exports)
            || !Readonly(code.Structure.Literals)
            || code.Labels is not ReadOnlyDictionary<int, int>
            || code.RegistryModuleDependencies is not FrozenSet<string>
            || code.Instructions.Any(instruction => !Readonly(instruction.Operands)))
            return; // A future nonfrozen reader falls back to full validation.
        prepared.Add(code, new(code, validated));
    }

    private static bool Readonly<T>(IReadOnlyList<T> values)
        => values is ReadOnlyCollection<T> || values is T[] { Length: 0 };
}
