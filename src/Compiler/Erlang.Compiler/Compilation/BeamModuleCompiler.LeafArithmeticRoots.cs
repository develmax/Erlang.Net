using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private static bool IsLeafArithmeticGcBif(
        BeamInstruction instruction,
        string[] atomNames,
        (int Module, int Function, int Arity)[] importsByIndex
    )
    {
        if (instruction.Definition.Opcode != GcBifTwo
            || instruction.Operands[0].Kind is not (BeamOperandParameters.Failure or BeamOperandParameters.Label))
        {
            return false;
        }

        var import = importsByIndex[(int)instruction.Operands[2].Value];

        return import.Arity == 2 && atomNames[import.Module - 1] == ErlangModuleNames.Module
            && GeneralEmitter.IsRegisterArithmeticOperator(atomNames[import.Function - 1]);
    }

    private static int WriteLeafArithmeticRoots(
        ReadOnlyMemory<byte> code,
        Stream output,
        int offset,
        int copied,
        int live
    )
    {
        int operands = offset + 1;
        var cursor = new BeamCursor(code[operands..]);
        _ = BeamCompactReader.Read(cursor);
        int liveOffset = operands + cursor.Position;
        _ = BeamCompactReader.Read(cursor);
        output.Write(code.Span[copied..liveOffset]);
        BeamCompactWriter.Write(output, BeamWire.UnsignedTag, live);

        return operands + cursor.Position;
    }
}
