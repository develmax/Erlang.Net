using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace Erlang;

internal static class BeamGuardedTupleBifProof
{
    private static readonly ConditionalWeakTable<BeamLinkedCode, ConcurrentDictionary<BeamInstruction, bool>> Proofs = new();

    internal static bool HasTupleGuard(BeamLinkedCode code, BeamInstruction instruction)
        => Proofs.GetValue(code, _ => new(ReferenceEqualityComparer.Instance)).GetOrAdd(instruction, value => Prove(code, value));

    private static bool Prove(BeamLinkedCode code, BeamInstruction instruction)
    {
        int current = -1;
        for (int index = 0; index < code.Instructions.Count; index++)
        {
            if (ReferenceEquals(code.Instructions[index], instruction))
            {
                current = index;
                break;
            }
        }
        if (current < 0)
            return false;
        BeamOperand source = instruction.Operands[2];
        if (source.Kind != BeamOperandParameters.X)
            return false;
        for (int index = current - 1; index >= 0; index--)
        {
            BeamInstruction previous = code.Instructions[index];
            switch (previous.Definition.Name)
            {
                case BeamGeneralOpcodeNames.TupleTest:
                    return SameRegister(previous.Operands[1], source);

                case BeamLinkNames.Line:
                case BeamOnLoadNames.InitY:
                    continue;

                case BeamLinkNames.Allocate:
                case BeamLinkNames.AllocateHeap:
                    int liveIndex = previous.Definition.Name == BeamLinkNames.AllocateHeap ? 2 : 1;
                    if (previous.Operands[liveIndex].Value <= source.Value)
                        return false;
                    continue;

                case BeamLinkNames.Move:
                    if (SameRegister(previous.Operands[1], source))
                        return false;
                    continue;

                default:
                    // Labels, branches, calls and all unmodelled writes end the proof.
                    return false;
            }
        }

        return false;
    }

    private static bool SameRegister(BeamOperand first, BeamOperand second)
        => first.Kind == second.Kind && first.Value == second.Value;
}
