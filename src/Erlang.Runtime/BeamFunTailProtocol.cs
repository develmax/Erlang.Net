namespace Erlang;

internal static class BeamFunTailProtocol
{
    internal static bool TryDeallocation(
        BeamLinkedCode code,
        BeamLinkedFunction function,
        int next,
        out int slots
    )
    {
        slots = 0;
        if (next + 1 >= function.EndInstructionIndex
            || code.Instructions[next].Definition.Name != BeamLinkNames.Deallocate
            || code.Instructions[next + 1].Definition.Name != BeamLinkNames.Return)
            return false;
        slots = (int)code.Instructions[next].Operands[0].Value;

        return true;
    }
}
