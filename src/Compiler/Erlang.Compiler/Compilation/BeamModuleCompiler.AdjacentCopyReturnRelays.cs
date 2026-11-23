using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private static void CollectAdjacentCopyReturnRelays(
        IReadOnlyList<BeamInstruction> body,
        Dictionary<int, (int Target, int End)> relays
    )
    {
        int start = 0;
        for (int end = 0; end < body.Count; end++)
        {
            if (body[end].Definition.Opcode is not (FunctionInfo or EndCode))
            {
                continue;
            }

            int entry = start;
            start = end + 1;
            if (!HasUnprotectedCallKillSchedule(body, entry))
            {
                continue;
            }

            (int[] Owners, Dictionary<int, bool[]> Uses)? frame = null;
            for (int first = entry + 1; first + 5 < end; first++)
            {
                if (!EndsBeamBlock(body[first - 1].Definition.Opcode)
                    || ReadCopy(first, end) is not { } initial
                    || ReadCopy(first + 3, end) is not { } duplicate
                    || initial.Source != duplicate.Source
                    || (frame ??= ReadDisjointFrameOwnership(
                        body,
                        entry,
                        end,
                        allowCompleteTrims: true,
                        trackSiblingTrimEpochs: true,
                        allowFrameless: true
                    )) is not { } ownership
                    || ownership.Owners[first - entry] != UnframedOwner
                    || ownership.Owners[first + 3 - entry] != UnframedOwner)
                {
                    continue;
                }

                int survivor = first + 3;
                int target = duplicate.Label;
                while (ReadCopy(survivor + 3, end) is { } next && next.Source == initial.Source
                    && ownership.Owners[survivor + 3 - entry] == UnframedOwner)
                {
                    survivor += 3;
                    target = next.Label;
                }

                // The final occurrence is the common return. Both predecessors
                // already supply the same physical return operand without a frame.
                for (int previous = first; previous < survivor; previous += 3)
                {
                    relays.Add(previous, (target, previous + 3));
                }

                first = survivor;
            }
        }

        (BeamOperand Source, int Label)? ReadCopy(int position, int end) =>
            position + 2 < end && body[position] is { Definition.Opcode: Label, Operands: [var label] }
            && body[position + 1] is
            {
                Definition.Opcode: Move,
                Operands:
                [
                { Kind: BeamOperandParameters.X, TypeIndex: null } source,
                { Kind: BeamOperandParameters.X, TypeIndex: null, Value.IsZero: true }
                ]
            } && !source.Value.IsZero && body[position + 2].Definition.Opcode == Return
                ? (source, (int)label.Value) : null;
    }
}
