namespace Erlang;

internal static class BeamNativePrimitives
{
    internal static bool IsStub(BeamLinkedCode code, BeamLinkedFunction function)
    {
        if (!NativeBuiltinCatalog.Contains(function.Module, function.Name, function.Arity))
            return false;
        BeamInstruction[] body = code.Instructions.Skip(function.FirstInstructionIndex)
            .Take(function.EndInstructionIndex - function.FirstInstructionIndex)
            .Where(instruction => instruction.Definition.Name is not (BeamLinkNames.Label or BeamLinkNames.Line)).ToArray();
        if (body.Length != 2 || body[0].Definition.Name != BeamLinkNames.Move || body[1].Definition.Name != BeamLinkNames.ExternalTailCall)
            return false;
        var move = body[0].Operands;
        var call = body[1].Operands;
        if (move[0].Kind != BeamOperandParameters.Atom || code.Structure.Atoms[(int)move[0].Value] is not
            (ErlangErrorReasons.UndefinedFunction or BeamNativePrimitiveNames.UndefinedStubReason)
            || move[1].Kind != BeamOperandParameters.X || move[1].Value != 0 || call[0].Value != 1)
            return false;
        BeamImport import = code.Structure.Imports[(int)call[1].Value];

        return import.Arity == 1 && code.Structure.Atoms[import.ModuleAtomIndex] == ErlangModuleNames.Module
            && code.Structure.Atoms[import.FunctionAtomIndex] == ErlangErrorMetadataNames.NifError;
    }
}
