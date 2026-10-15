namespace Erlang;

internal static class BeamGeneratedMetadata
{
    internal static bool IsGenerated(BeamLinkedCode code, BeamLinkedFunction function)
    {
        if (function.Name != BeamPreparationNames.ModuleInfo || function.Arity is not (0 or 1))
            return false;
        BeamInstruction[] body = code.Instructions.Skip(function.FirstInstructionIndex)
            .Take(function.EndInstructionIndex - function.FirstInstructionIndex)
            .Where(value => value.Definition.Name is not (BeamLinkNames.Label or BeamLinkNames.Line)).ToArray();
        int length = function.Arity == 0 ? BeamPreparationNames.MetadataZeroBodyInstructions : BeamPreparationNames.MetadataKeyBodyInstructions;
        int start = 0;
        if (body.Length != 0 && body[0].Definition.Name == BeamNifNames.Start)
        {
            if (body[0].Operands.Count != 0)
                return false;
            start++;
        }
        if (body.Length != length + start)
            return false;
        int index = start;
        if (function.Arity == 1)
        {
            if (!Move(
                body[index++],
                BeamOperandParameters.X,
                BeamPreparationNames.ModuleRegister,
                BeamPreparationNames.KeyRegister
            ))
                return false;
        }
        BeamInstruction module = body[index++];
        if (module.Definition.Name != BeamLinkNames.Move || module.Operands[0].Kind != BeamOperandParameters.Atom
            || code.Structure.Atoms[(int)module.Operands[0].Value] != function.Module
            || module.Operands[1].Kind != BeamOperandParameters.X || module.Operands[1].Value != BeamPreparationNames.ModuleRegister)
            return false;
        BeamInstruction call = body[index];
        if (call.Definition.Name != BeamLinkNames.ExternalTailCall || call.Operands[0].Value != function.Arity + BeamPreparationNames.MetadataImportExtraArguments)
            return false;
        BeamImport import = code.Structure.Imports[(int)call.Operands[1].Value];

        return code.Structure.Atoms[import.ModuleAtomIndex] == ErlangModuleNames.Module
            && code.Structure.Atoms[import.FunctionAtomIndex] == BeamPreparationNames.GetModuleInfo
            && import.Arity == function.Arity + BeamPreparationNames.MetadataImportExtraArguments;
    }

    internal static ValueTask<Term> Invoke(ProcessContext context, string module, IReadOnlyList<Term> arguments)
    {
        Term[] values = new Term[arguments.Count + BeamPreparationNames.MetadataImportExtraArguments];
        values[0] = Term.A(module);
        for (int index = 0; index < arguments.Count; index++)
            values[index + BeamPreparationNames.MetadataImportExtraArguments] = arguments[index];

        return context.Runtime.Modules.Call(
            context,
            ErlangModuleNames.Module,
            BeamPreparationNames.GetModuleInfo,
            values
        );
    }

    private static bool Move(
        BeamInstruction instruction,
        string kind,
        int source,
        int destination
    ) =>
        instruction.Definition.Name == BeamLinkNames.Move && instruction.Operands[0].Kind == kind
        && instruction.Operands[0].Value == source && instruction.Operands[1].Kind == BeamOperandParameters.X
        && instruction.Operands[1].Value == destination;
}
