using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private static int WriteGuardedProjectionTailCopies(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body,
        Stream output,
        int copied,
        int entry,
        int parameterCount
    )
    {
        if (entry + 9 >= body.Count || body[entry].Definition.Opcode != Label
            || body[entry + 1] is not { Definition.Opcode: Move, Operands: [var source, var scratch] }
            || !IsBareX(source) || source.Value >= parameterCount
            || !IsBareX(scratch) || scratch.Value < parameterCount)
        {
            return copied;
        }

        BeamInstruction tupleTest = body[entry + 2];
        BeamInstruction arityTest = body[entry + 3];
        BeamInstruction projection = body[entry + 4];
        BeamInstruction predicate = body[entry + 5];
        BeamInstruction reload = body[entry + 6];
        BeamInstruction success = body[entry + 7];
        BeamInstruction failure = body[entry + 8];
        BeamInstruction fallback = body[entry + 9];
        if (tupleTest is not { Definition.Opcode: IsTuple, Operands: [var fail, var tuple] }
            || !IsBareX(tuple) || tuple.Value >= parameterCount || tuple == source
            || arityTest is not { Definition.Opcode: TestArity, Operands: [var wrongArity, var arityInput, _] }
            || arityInput != tuple || wrongArity.Value != fail.Value
            || projection is not { Definition.Opcode: GetTupleElement, Operands: [var projectionInput, _, var projected] }
            || projectionInput != tuple || projected != source
            || predicate is not { Definition.Opcode: IsPid, Operands: [var wrongPid, var predicateInput] }
            || predicateInput != source || wrongPid.Value != fail.Value
            || reload is not { Definition.Opcode: Move, Operands: [var saved, var target] }
            || saved != scratch || target != tuple
            || success is not { Definition.Opcode: TailLocalCall, Operands: [var arity, _] }
            || source.Value >= arity.Value || tuple.Value >= arity.Value || scratch.Value < arity.Value
            || failure is not { Definition.Opcode: Label, Operands: [var fallbackEntry] }
            || fallbackEntry.Value != fail.Value
            || fallback is not { Definition.Opcode: TailLocalCall, Operands: [var fallbackArity, _] }
            || fallbackArity.Value > source.Value || fallbackArity.Value > scratch.Value)
        {
            return copied;
        }

        // The failure path can observe neither changed register. The tuple
        // register is left unchanged until all checks on the success path pass.
        output.Write(code.Span[copied..body[entry + 1].Offset]);
        int operandWidth = CompactRegisterCountWidth((int)source.Value);
        output.Write(code.Span[tupleTest.Offset..(predicate.Offset - operandWidth)]);
        BeamCompactWriter.Write(output, XRegisterTag, scratch.Value);
        output.Write(code.Span[predicate.Offset..(reload.Offset - operandWidth)]);
        BeamCompactWriter.Write(output, XRegisterTag, scratch.Value);
        WriteMove(source, tuple);
        WriteMove(scratch, source);

        return success.Offset;

        void WriteMove(BeamOperand from, BeamOperand to)
        {
            output.WriteByte(Move);
            BeamCompactWriter.Write(output, XRegisterTag, from.Value);
            BeamCompactWriter.Write(output, XRegisterTag, to.Value);
        }

        static bool IsBareX(BeamOperand operand) =>
            operand is { Kind: BeamOperandParameters.X, TypeIndex: null };
    }
}
