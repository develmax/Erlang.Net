using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class BeamProjectionFlow
    {
        internal bool HasCheckedTupleField(int allocation, BeamOperand tuple, BeamOperand field)
        {
            int current = allocation;
            while (predecessors[current] is [var previous] && previous < current)
            {
                BeamInstruction check = body[previous];
                if (check is
                    {
                        Definition.Opcode: SelectTupleArity,
                        Operands: [var selected, _, _, ..]
                    }
                    && SameProjectionRegister(selected, tuple)
                    && body[current] is { Definition.Opcode: Label, Operands: [var entry] })
                {
                    for (int index = 3; index + 1 < check.Operands.Count; index += 2)
                    {
                        if (check.Operands[index + 1].Value == entry.Value)
                        {
                            return field.Value < check.Operands[index].Value;
                        }
                    }

                    return false;
                }

                if (previous + 1 != current)
                {
                    return false;
                }

                if (check is
                    {
                        Definition.Opcode: IsTaggedTuple or TestArity,
                        Operands: [_, var checkedTuple, var count, ..]
                    }
                    && SameProjectionRegister(checkedTuple, tuple))
                {
                    return field.Value < count.Value;
                }

                if (check.Definition.Opcode is not (Label or Line or ExactEqualTest or ExactNotEqualTest)
                    && (check is not { Definition.Opcode: GetTupleElement, Operands: [_, _, var destination] }
                        || SameProjectionRegister(destination, tuple)))
                {
                    return false;
                }

                current = previous;
            }

            return false;
        }
    }
}
