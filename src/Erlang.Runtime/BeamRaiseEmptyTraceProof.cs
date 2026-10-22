namespace Erlang;

internal static class BeamRaiseEmptyTraceProof
{
    internal static bool Prove(BeamLinkedCode code, BeamLinkedFunction function, int callPc)
    {
        if (callPc < function.FirstInstructionIndex || callPc >= function.EndInstructionIndex)
            return false;
        BeamInstruction call = code.Instructions[callPc];
        if (call.Definition.Name != BeamLinkNames.ExternalCall)
            return false;
        BeamImport import = code.Structure.Imports[(int)call.Operands[1].Value];
        if (import.Arity != BeamRaiseEmptyTraceNames.Arity
            || code.Structure.Atoms[import.ModuleAtomIndex] != ErlangModuleNames.Module
            || code.Structure.Atoms[import.FunctionAtomIndex] != ErlangRaiseBifNames.Raise)
            return false;
        // Only a straight-line basic-block suffix is authenticated. Unknown
        // operations, labels, calls and branches stop this bounded provenance.
        for (int pc = callPc - 1; pc >= function.FirstInstructionIndex; pc--)
        {
            BeamInstruction instruction = code.Instructions[pc];
            if (instruction.Definition.Name is BeamLinkNames.Line or BeamOnLoadNames.InitY)
                continue;
            if (instruction.Definition.Name != BeamLinkNames.Move)
                return false;
            BeamOperand target = instruction.Operands[1];
            if (target.Kind == BeamOperandParameters.X && target.Value == BeamTryParameters.RawRaiseTraceRegister)
                return instruction.Operands[0].Kind == BeamOperandParameters.Nil;
        }

        return false;
    }
}
