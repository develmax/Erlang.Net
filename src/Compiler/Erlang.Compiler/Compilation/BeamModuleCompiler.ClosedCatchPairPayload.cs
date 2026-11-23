using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? ShareClosedCatchPairPayload(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(CatchEnd)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { } decoded)
        {
            return null;
        }

        var body = decoded.Body;
        var x0 = new BeamOperand(BeamOperandParameters.X, 0);
        var x1 = new BeamOperand(BeamOperandParameters.X, 1);
        var labels = body.Select((instruction, index) => (instruction, index))
            .Where(item => item.instruction.Definition.Opcode == Label)
            .ToDictionary(item => item.instruction.Operands[0].Value, item => item.index);
        var references = body.Where(instruction => instruction.Definition.Opcode != Label)
            .SelectMany(instruction => instruction.Operands)
            .Where(operand => operand.Kind == BeamOperandParameters.Label)
            .GroupBy(operand => operand.Value).ToDictionary(group => group.Key, group => group.Count());
        using var output = new MemoryStream();
        int copied = 0;
        int start = 0;
        int end = 0;
        bool closed = false;
        for (int index = 0; index + 7 < body.Count; index++)
        {
            if (body[index].Definition.Opcode == FunctionInfo)
            {
                start = index + 1;
                end = start;
                int handlers = 0;
                bool otherScopes = false;
                while (body[end].Definition.Opcode is not (FunctionInfo or EndCode))
                {
                    byte opcode = body[end].Definition.Opcode;
                    handlers += opcode == CatchEnter ? 1 : 0;
                    otherScopes |= opcode is TryEnter or DebugLine or ExecutableLine;
                    end++;
                }

                closed = handlers == 1 && !otherScopes;
            }

            if (!closed || index + 7 >= end
                || body[index] is not
                {
                    Definition.Opcode: CatchEnd,
                    Operands: [{ Kind: BeamOperandParameters.Y, TypeIndex: null } released]
                }
                || body[index + 1] is not { Definition.Opcode: IsTuple, Operands: [var invalid, var tuple] }
                || tuple != x0
                || body[index + 2] is not { Definition.Opcode: TestArity, Operands: [var wrong, var sized, var arity] }
                || wrong != invalid || sized != x0 || arity.Value != 2
                || !ReadsField(index + 3, 0, x1)
                || body[index + 4] is not
                {
                    Definition.Opcode: SelectValue,
                    Operands: [var tag, var fallback, _, { Kind: BeamOperandParameters.Atom }, var firstLabel,
                    { Kind: BeamOperandParameters.Atom }, var secondLabel]
                } || tag != x1 || fallback != invalid || firstLabel == secondLabel
                || !labels.TryGetValue(firstLabel.Value, out int left)
                || !labels.TryGetValue(secondLabel.Value, out int right)
                || references.GetValueOrDefault(firstLabel.Value) != 1
                || references.GetValueOrDefault(secondLabel.Value) != 1
                || !labels.TryGetValue(invalid.Value, out int rejected)
                || rejected < start || rejected + 2 >= end || !Exits(rejected))
            {
                continue;
            }

            int first = Math.Min(left, right);
            int second = Math.Max(left, right);
            if (first != index + 5 || second + 2 >= end || second <= first + 1
                || !EndsBeamBlock(body[second - 1].Definition.Opcode)
                || !ReadsField(first + 1, 1, released) || !ReadsField(second + 1, 1, released))
            {
                continue;
            }

            // After CatchEnd the reused handler slot has the one common
            // payload on both selected edges. The fallback reads only X0.
            output.Write(code.Span[copied..body[index + 4].Offset]);
            Copy(first + 1, first + 2);
            Copy(index + 4, first + 1);
            Copy(first + 2, second + 1);
            copied = body[second + 2].Offset;
            index = second + 1;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();

        bool ReadsField(int index, int offset, BeamOperand destination) =>
            body[index] is
            {
                Definition.Opcode: GetTupleElement,
                Operands: [var source, var field, var target]
            } && source == x0 && field.Value == offset && target == destination;

        bool Exits(int label)
        {
            int next = body[label + 1].Definition.Opcode == Line ? label + 2 : label + 1;

            return body[next] is { Definition.Opcode: CaseEnd, Operands: [var failed] } && failed == x0;
        }

        void Copy(int start, int end) => output.Write(code.Span[body[start].Offset..body[end].Offset]);
    }
}
