using System.Collections.Frozen;

namespace Erlang.NifNativeBackend;

public static class PreparedBeamValidationChecks
{
    public static async Task Isolation()
    {
        foreach (bool owned in new[] { false, true })
        {
            await using var runtime = NativeOwned516Profile.Create(owned);
            await runtime.InitializeModulesAsync(Check);
            byte[] expected = Convert.FromBase64String(NativeRecordBeamImages.Original);
            for (int replay = 0; replay < PreparedBeamValidationCaseNames.Replays; replay++)
            {
                // Genuine published original BEAM, repeated across distinct
                // actual process contexts using the same immutable image.
                Term actual = await CpuStatisticsAccess.Actor<Term>(
                    runtime,
                    async context => await runtime.Modules.Call(context, NativeRecordBeamCaseNames.Observer, NativeRecordBeamCaseNames.Run)
                );
                Require(actual.Equals(ExternalTermFormat.Decode(expected))
                    && ExternalTermFormat.Encode(actual).AsSpan().SequenceEqual(expected));
            }
        }
    }

    private static void Check(ModuleRegistry registry)
    {
        byte[] source = Convert.FromBase64String(NativeRecordBeamImages.NativeRecordPhysical);
        var dependencies = new BeamDependencySelection([NativeRecordBeamCaseNames.Module, NativeRecordBeamCaseNames.Probe, ErlangModuleNames.Module]);
        PreparedBeamImage image = registry.PrepareBeam(source, dependencies);
        BeamLinkedCode code = image.Code;
        BeamLinkedFunction entry = code.Exports.Single(function => function.Name == NativeRecordBeamCaseNames.Run);
        Require(PreparedBeamValidationCache.TryGet(code, out var proof) && proof!.HasSuccess(entry));
        byte firstByte = code.Structure.Code.Instructions.Span[0];
        Array.Clear(source); // Caller-owned input can change after PrepareBeam.
        Require(code.Structure.Code.Instructions.Span[0] == firstByte);
        BeamRegisterValidation.Validate(code, entry);
        BeamInstruction original = code.Instructions[entry.FirstInstructionIndex];
        var invalid = new BeamInstruction(
            original.Offset,
            BeamOpcodeCatalog.All.Single(definition => definition.Name == BeamLinkNames.Deallocate),
            Array.AsReadOnly(new[] { new BeamOperand(BeamOperandParameters.Unsigned, 1) })
        );
        RejectReadonly(() => ((IList<BeamInstruction>)code.Instructions)[entry.FirstInstructionIndex] = invalid);
        RejectReadonly(() => ((IList<BeamOperand>)original.Operands)[0] = new(BeamOperandParameters.Unsigned, 1));
        var mutable = code.Instructions.ToList();
        BeamLinkedCode clone = code with { Instructions = mutable };
        Require(!PreparedBeamValidationCache.TryGet(clone, out _));
        BeamRegisterValidation.Validate(clone, entry); // Valid but caller-mutable IR stays uncached.
        mutable[entry.FirstInstructionIndex] = invalid;
        RejectInvalid(() => BeamRegisterValidation.Validate(clone, entry));
        mutable[entry.FirstInstructionIndex] = original;
        BeamRegisterValidation.Validate(clone, entry); // A prior failure must not poison a mutable IR.
        BeamRegisterValidation.Validate(code, entry); // Original immutable image is unaffected.
        // A readonly wrapper over someone else's mutable list is NOT proof.
        var aliased = code.Instructions.ToList();
        BeamLinkedCode wrappedClone = code with { Instructions = aliased.AsReadOnly() };
        Require(!PreparedBeamValidationCache.TryGet(wrappedClone, out _));
        BeamRegisterValidation.Validate(wrappedClone, entry);
        aliased[entry.FirstInstructionIndex] = invalid;
        RejectInvalid(() => BeamRegisterValidation.Validate(wrappedClone, entry));
        BeamLinkedCode changedDependencies = code with { RegistryModuleDependencies = FrozenSet<string>.Empty };
        Require(!PreparedBeamValidationCache.TryGet(changedDependencies, out _));
        RejectInvalid(() => BeamRegisterValidation.Validate(changedDependencies, entry));
        PreparedBeamImage other = registry.PrepareBeam(Convert.FromBase64String(NativeRecordBeamImages.NativeRecordPhysical), dependencies);
        BeamLinkedFunction equalMfa = other.Code.Exports.Single(function => function.Name == entry.Name && function.Arity == entry.Arity);
        Require(!ReferenceEquals(code, other.Code) && entry == equalMfa && !ReferenceEquals(entry, equalMfa));
        Require(!proof!.HasSuccess(equalMfa));
        Require(PreparedBeamValidationCache.TryGet(other.Code, out var otherProof) && otherProof!.HasSuccess(equalMfa));
        PreparedBeamImage probe = registry.PrepareBeam(Convert.FromBase64String(NativeRecordBeamImages.NativeRecordPhysicalProbe), dependencies);
        PreparedBeamImage observer = registry.PrepareBeam(Convert.FromBase64String(NativeRecordBeamImages.NativeRecordPhysicalObserver), dependencies);
        Require(registry.PublishPreparedBeam(image, probe, observer).Status == BeamPublicationStatus.Published);
    }

    private static void RejectReadonly(Action action)
    {
        try
        {
            action();
        }
        catch (NotSupportedException)
        {
            return;
        }
        throw new InvalidOperationException(PreparedBeamValidationCaseNames.Failure);
    }

    private static void RejectInvalid(Action action)
    {
        try
        {
            action();
        }
        catch (Exception error) when (error is InvalidDataException or NotSupportedException)
        {
            return;
        }
        throw new InvalidOperationException(PreparedBeamValidationCaseNames.Failure);
    }

    private static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException(PreparedBeamValidationCaseNames.Failure);
    }
}
