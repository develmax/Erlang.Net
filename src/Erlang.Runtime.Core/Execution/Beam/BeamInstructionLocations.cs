using System.Runtime.CompilerServices;

namespace Erlang;

internal static class BeamInstructionLocations
{
    private static readonly ConditionalWeakTable<BeamLinkedCode, Term[]> locations = new();

    internal static IReadOnlyList<Term> Read(BeamLinkedCode code)
        => locations.GetValue(code, Create);

    private static Term[] Create(BeamLinkedCode code)
    {
        var result = new Term[code.Instructions.Count];
        Term current = Nil.Value;
        for (int index = 0; index < result.Length; index++)
        {
            BeamInstruction instruction = code.Instructions[index];
            if (instruction.Definition.Name == BeamLinkNames.FunctionInfo
                && (index == 0 || code.Instructions[index - 1].Definition.Name != BeamLinkNames.Line))
                current = Nil.Value;
            if (instruction.Definition.Name == BeamLinkNames.Line && code.SourceLocations.Count != 0)
            {
                int location = (int)instruction.Operands[0].Value;
                if (location < 0 || location >= code.SourceLocations.Count)
                    throw new InvalidDataException(BeamLineNames.Invalid);
                if (!SuppressedTailLine(code, index))
                    current = code.SourceLocations[location];
            }
            result[index] = current;
        }

        return result;
    }

    private static bool SuppressedTailLine(BeamLinkedCode code, int index)
    {
        if (index == 0 || index + 1 >= code.Instructions.Count)
            return false;
        BeamInstruction move = code.Instructions[index - 1];
        if (move.Definition.Name != BeamLinkNames.Move || move.Operands[1].Kind != BeamOperandParameters.X
            || move.Operands[1].Value != 0)
            return false;
        BeamInstruction call = code.Instructions[index + 1];
        if (call.Definition.Name is BeamLinkNames.LocalCall or BeamGeneralOpcodeNames.LocalLast)
            return true;
        if (call.Definition.Name is not (BeamLinkNames.ExternalTailCall or BeamOnLoadNames.ExternalLast))
            return false;
        BeamImport import = code.Structure.Imports[(int)call.Operands[1].Value];
        string module = code.Structure.Atoms[import.ModuleAtomIndex]!;
        string function = code.Structure.Atoms[import.FunctionAtomIndex]!;
        bool builtin = NativeBuiltinCatalog.Contains(module, function, import.Arity)
            || module == ErlangModuleNames.Module && function == ErlangBasicBifNames.Apply && import.Arity is 2 or 3;

        return !builtin;
    }
}
