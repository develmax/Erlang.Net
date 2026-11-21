using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? ScheduleEntryTupleFunctionCall(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !code.Span.Contains(CallFun)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body })
        {
            return null;
        }

        using var output = new MemoryStream(code.Length);
        int copied = 0;
        for (int info = 0; info + 8 < body.Count; info++)
        {
            int firstProjection = info + 4;
            int allocation = firstProjection + 2;
            if (body[info] is not { Definition.Opcode: FunctionInfo, Operands: [_, _, var inputArity] }
                || inputArity.Value < 2
                || body[info + 1].Definition.Opcode != Label
                || body[info + 2] is not { Definition.Opcode: IsTuple, Operands: [var failure, var tuple] }
                || !IsBareX0(tuple)
                || body[info + 3] is not
                {
                    Definition.Opcode: TestArity,
                    Operands: [var arityFailure, var checkedTuple, { Value: var tupleArity }]
                }
                || arityFailure != failure || checkedTuple != tuple || tupleArity != 2
                || body[firstProjection] is not
                {
                    Definition.Opcode: GetTupleElement,
                    Operands: [var firstSource, { Value.IsZero: true },
                    { Kind: BeamOperandParameters.X, TypeIndex: null } first]
                }
                || firstSource != tuple || first.Value != inputArity.Value
                || body[firstProjection + 1] is not
                {
                    Definition.Opcode: GetTupleElement,
                    Operands: [var secondSource, { Value.IsOne: true },
                    { Kind: BeamOperandParameters.X, TypeIndex: null } second]
                }
                || secondSource != tuple || second.Value != first.Value + 1
                || body[allocation] is not { Definition.Opcode: Allocate, Operands: [var width, var live] }
                || live.Value != second.Value + 1)
            {
                continue;
            }

            int move = allocation + 1;
            while (body[move] is
                {
                    Definition.Opcode: Move,
                    Operands: [{ Kind: BeamOperandParameters.X, TypeIndex: null } savedArgument,
                    { Kind: BeamOperandParameters.Y, TypeIndex: null }]
                } && savedArgument.Value < inputArity.Value)
            {
                move++;
            }

            if (move + 2 >= body.Count
                || body[move] is not { Definition.Opcode: Move, Operands: [var high, var argument] }
                || high != second || !IsBareX0(argument)
                || body[move + 1] is not
                {
                    Definition.Opcode: Move,
                    Operands: [var low, { Kind: BeamOperandParameters.X, TypeIndex: null, Value.IsOne: true }]
                }
                || low != first)
            {
                continue;
            }

            int call = move + 2;
            if (body[call].Definition.Opcode == Line)
            {
                call++;
            }

            if (call >= body.Count
                || body[call] is not { Definition.Opcode: CallFun, Operands: [{ Value.IsOne: true }] })
            {
                continue;
            }

            // The checked tuple is an incoming root. Field values need no
            // additional roots when projected after the argument saves.
            output.Write(code.Span[copied..body[firstProjection].Offset]);
            output.WriteByte(Allocate);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, width.Value);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, inputArity.Value);
            output.Write(code.Span[body[allocation + 1].Offset..body[move].Offset]);
            WriteField(0, 1);
            WriteField(1, 0);
            copied = body[move + 2].Offset;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();

        void WriteField(int index, int destination)
        {
            output.WriteByte(GetTupleElement);
            BeamCompactWriter.Write(output, XRegisterTag, 0);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, index);
            BeamCompactWriter.Write(output, XRegisterTag, destination);
        }
    }
}
