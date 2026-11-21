using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private readonly record struct PostCallResultListBranch(
        int Spill,
        int Heap,
        int FirstCons,
        int SaveList,
        int Compare,
        int Reload,
        int Tail,
        int ReturnMove
    );

    private static PostCallResultListBranch? ReadPostCallResultListBranch(
        IReadOnlyList<BeamInstruction> body,
        int start,
        int end
    )
    {
        if (end - start < 16 || body[end - 1].Definition.Opcode != Return
            || body[end - 2] is not { Definition.Opcode: Deallocate, Operands: [var width] }
            || body[end - 3] is not
            {
                Definition.Opcode: Move,
                Operands: [{ Kind: BeamOperandParameters.Y, TypeIndex: null } saved, var returned]
            }
            || !IsBareX0(returned)
            || body[end - 4] is not { Definition.Opcode: Label, Operands: [var next] }
            || body[end - 5] is not
            {
                Definition.Opcode: TailExternalCallWithFrame,
                Operands: [var arity, _, var callWidth]
            }
            || arity.Value != 2 || callWidth.Value != width.Value)
        {
            return null;
        }

        int tail = end - 5;
        int setup = tail;
        while (setup > start && body[setup - 1].Definition.Opcode == Line)
        {
            setup--;
        }

        int compare = setup - 3;
        if (compare < start + 8
            || body[compare] is not
            {
                Definition.Opcode: ExactEqualTest,
                Operands: [var rejected, var result, { Kind: BeamOperandParameters.Atom } literal]
            }
            || rejected.Value != next.Value || result != saved
            || body[compare + 1] is not
            {
                Definition.Opcode: Move,
                Operands: [{ Kind: BeamOperandParameters.Y, TypeIndex: null } arguments,
                { Kind: BeamOperandParameters.X, TypeIndex: null, Value.IsOne: true }]
            }
            || arguments == saved
            || body[compare + 2] is not { Definition.Opcode: Move, Operands: [var moved, var errorArgument] }
            || moved != literal || !IsBareX0(errorArgument)
            || body[compare - 1] is not { Definition.Opcode: Move, Operands: [var built, var stored] }
            || !IsBareX0(built) || stored != arguments)
        {
            return null;
        }

        int saveList = compare - 1;
        int firstCons = saveList;
        bool nilTail = false;
        while (firstCons > start && body[firstCons - 1] is
            {
                Definition.Opcode: PutList,
                Operands: [{ Kind: BeamOperandParameters.Y, TypeIndex: null } head, var consTail, var destination]
            }
        && head != saved && IsBareX0(destination))
        {
            if (consTail.Kind == BeamOperandParameters.Nil)
            {
                firstCons--;
                nilTail = true;
                break;
            }

            if (!IsBareX0(consTail))
            {
                return null;
            }

            firstCons--;
        }

        int heap = firstCons - 1;
        int spill = heap - 1;
        if (!nilTail || spill <= start
            || body[heap] is not
            {
                Definition.Opcode: TestHeap,
                Operands: [{ Value: var words }, { Value.IsZero: true }]
            }
            || words != (saveList - firstCons) * ConsHeapWords
            || body[spill] is not { Definition.Opcode: Move, Operands: [var incoming, var alias] }
            || !IsBareX0(incoming) || alias != saved
            || body[spill - 1].Definition.Opcode is not (LocalCall or ExternalCall))
        {
            return null;
        }

        for (int index = start; index < spill; index++)
        {
            if (body[index].Definition.Opcode is TryEnter or CatchEnter or LoopReceive or LoopReceiveEnd
                or Wait or WaitTimeout or DebugLine or ExecutableLine or TrimFrame)
            {
                return null;
            }
        }

        return new(
            spill,
            heap,
            firstCons,
            saveList,
            compare,
            compare + 1,
            tail,
            end - 3
        );
    }

    private static void AddPostCallResultListEdits(
        IReadOnlyList<BeamInstruction> body,
        IReadOnlyList<(int Start, int End, int Slot)> operands,
        PostCallResultListBranch branch,
        List<CodeEdit> edits
    )
    {
        Remove(branch.Spill);
        ReplaceTrailingOperand(branch.Heap, BeamWire.UnsignedTag, 1);
        for (int index = branch.FirstCons; index < branch.SaveList; index++)
        {
            int next = body[index + 1].Offset;
            if (index != branch.FirstCons)
            {
                edits.Add(new(
                    next - 2,
                    next - 1,
                    XRegisterTag,
                    1
                ));
            }

            ReplaceTrailingOperand(index, XRegisterTag, 1);
        }

        Remove(branch.SaveList);
        int stack = FirstStackOperandAtOrAfter(operands, body[branch.Compare].Offset);
        var (from, to, _) = operands[stack];
        edits.Add(new(
            from,
            to,
            XRegisterTag,
            0
        ));
        Remove(branch.Reload);
        Remove(branch.Reload + 1);
        Remove(branch.ReturnMove);

        void Remove(int index) => edits.Add(new(
            body[index].Offset,
            body[index + 1].Offset,
            0,
            0,
            IsRemoval: true
        ));

        void ReplaceTrailingOperand(int index, int tag, int value)
        {
            int next = body[index + 1].Offset;
            edits.Add(new(
                next - 1,
                next,
                tag,
                value
            ));
        }
    }
}
