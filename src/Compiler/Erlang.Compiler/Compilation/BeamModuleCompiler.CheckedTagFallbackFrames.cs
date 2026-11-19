using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private static EntryFramePlacement? PlanCheckedTagFallbackFrame(
        IReadOnlyList<BeamInstruction> body,
        int start,
        int allocation,
        int end
    )
    {
        int first = allocation + 1;
        if (start == 0 || allocation != start + 1 || first + 3 >= end
            || body[allocation] is not
            {
                Definition.Opcode: Allocate,
                Operands: [{ Value.IsZero: true }, var live]
            } || live.Value != body[start - 1].Operands[^1].Value || live.Value.Sign <= 0)
        {
            return null;
        }

        var prefixLabels = new HashSet<int> { (int)body[start].Operands[0].Value };
        int tag = first;
        if (body[first] is
            {
                Definition.Opcode: IsPid,
                Operands: [var next, { Kind: BeamOperandParameters.X, Value.IsZero: true, TypeIndex: null }]
            })
        {
            // The PID and record guards share the original arguments and the same tail call.
            if (first + 4 >= end
                || body[first + 1] is not { Definition.Opcode: Jump, Operands: [var hit] }
                || !IsLabel(first + 2, next)
                || !IsLabel(first + 4, hit)
                || !prefixLabels.Add((int)next.Value)
                || !prefixLabels.Add((int)hit.Value))
            {
                return null;
            }

            tag = first + 3;
        }

        if (body[tag] is not
            {
                Definition.Opcode: IsTaggedTuple,
                Operands: [
                { Kind: BeamOperandParameters.Label } rejected,
                { Kind: BeamOperandParameters.X, Value.IsZero: true, TypeIndex: null },
                { Kind: BeamOperandParameters.Unsigned, Value.Sign: > 0 },
                { Kind: BeamOperandParameters.Atom }
                ]
            })
        {
            return null;
        }

        int arity = (int)live.Value;
        int terminal = tag == first ? tag + 1 : tag + 2;
        for (; terminal < end; terminal++)
        {
            BeamInstruction instruction = body[terminal];
            if (instruction.Definition.Opcode == Line)
            {
                continue;
            }

            if (instruction is not
                {
                    Definition.Opcode: Move,
                    Operands: [
                    {
                        Kind: BeamOperandParameters.Atom or BeamOperandParameters.Integer
                            or BeamOperandParameters.LiteralValue or BeamOperandParameters.Nil
                    },
                    { Kind: BeamOperandParameters.X, TypeIndex: null } destination
                    ]
                } || destination.Value != arity)
            {
                break;
            }

            arity++;
        }

        int fallback = terminal + 2;
        if (fallback >= end || body[terminal] is not
            {
                Definition.Opcode: TailLocalCallWithFrame,
                Operands: [var outgoing, _, { Value.IsZero: true }]
            } || outgoing.Value != arity
            || !IsLabel(terminal + 1, rejected)
            || !prefixLabels.Add((int)rejected.Value))
        {
            return null;
        }

        int call = fallback;
        while (call < end && body[call].Definition.Opcode == Line)
        {
            call++;
        }

        if (call >= end || body[call].Definition.Opcode is not (LocalCall or ExternalCall)
            || body[call].Operands[0].Value > live.Value)
        {
            return null;
        }

        // A framed fallback must not re-enter the prefix or allocate again at its entry.
        for (int index = fallback; index < end; index++)
        {
            BeamInstruction instruction = body[index];
            if (instruction.Definition.Opcode is Allocate or AllocateHeap or TrimFrame or CatchEnter or TryEnter
                    or LoopReceive or LoopReceiveEnd or Wait or WaitTimeout or ExecutableLine or DebugLine
                || instruction.Operands.Any(operand => operand.Kind == BeamOperandParameters.Label
                    && prefixLabels.Contains((int)operand.Value)))
            {
                return null;
            }
        }

        if (ReadDisjointFrameOwnership(body, start, end) is null)
        {
            return null;
        }

        return new(
            first,
            [],
            [terminal],
            new Dictionary<int, int> { [fallback] = (int)live.Value },
            new Dictionary<int, int>(),
            new Dictionary<int, (int Slot, int Register)[]>()
        );

        bool IsLabel(int index, BeamOperand reference) => body[index] is
        {
            Definition.Opcode: Label,
            Operands: [var actual]
        } && reference.Kind == BeamOperandParameters.Label
            && reference.Value.Sign > 0 && actual.Value == reference.Value;
    }
}
