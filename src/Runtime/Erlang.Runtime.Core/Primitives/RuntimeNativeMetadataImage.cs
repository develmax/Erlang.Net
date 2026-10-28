using System.Security.Cryptography;

namespace Erlang;

public sealed class RuntimeNativeMetadataImage
{
    public string OriginalBeamSha256 { get; }

    public BitString NativeMd5 { get; }

    internal Term LoadModuleErrorLocation527 { get; }

    public RuntimeNativeMetadataImage(ReadOnlyMemory<byte> originalErlangBeam)
    {
        byte[] owned = originalErlangBeam.ToArray();
        BeamLinkedCode code = BeamLinkedCodeReader.Read(BeamStructureReader.Read(owned));
        if (code.Structure.Atoms[1] != RuntimeNativeMetadataNames527.Module)
            throw new InvalidDataException(RuntimeNativeMetadataNames527.Invalid);
        BeamLinkedFunction function = code.Exports.Single(value =>
            value.Name == RuntimeNativeMetadataNames527.LoadModule
            && value.Arity == RuntimeNativeMetadataNames527.LoadModuleArity);
        IReadOnlyList<Term> locations = BeamInstructionLocations.Read(code);
        Term? selected = null;
        bool found = false;
        for (int index = function.FirstInstructionIndex; index < function.EndInstructionIndex; index++)
        {
            BeamInstruction instruction = code.Instructions[index];
            if (instruction.Definition.Name is not (BeamLinkNames.ExternalCall
                or BeamLinkNames.ExternalTailCall or BeamOnLoadNames.ExternalLast))
                continue;
            BeamImport import = code.Structure.Imports[(int)instruction.Operands[1].Value];
            if (code.Structure.Atoms[import.ModuleAtomIndex] != RuntimeNativeMetadataNames527.Module
                || code.Structure.Atoms[import.FunctionAtomIndex] != RuntimeNativeMetadataNames527.Error
                || import.Arity != RuntimeNativeMetadataNames527.ErrorArity)
                continue;
            Term location = locations[index];
            if (found && !selected!.Equals(location))
                throw new InvalidDataException(RuntimeNativeMetadataNames527.Invalid);
            selected = location;
            found = true;
        }
        if (!found)
            throw new InvalidDataException(RuntimeNativeMetadataNames527.Invalid);
        LoadModuleErrorLocation527 = selected!;
        NativeMd5 = BeamChecksumReader.Read(code);
        OriginalBeamSha256 = Convert.ToHexString(SHA256.HashData(owned));
    }
}
