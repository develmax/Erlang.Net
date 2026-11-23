using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private const string EnumeratePortsFunction = "ports";

    private byte[]? OrderSafeEnumerationTailArguments(ReadOnlyMemory<byte> code, int labelCount)
    {
        IReadOnlyList<BeamInstruction> body;
        try
        {
            var header = new BeamCodeHeader(
                BeamWire.FormatVersion,
                highestOpcode,
                labelCount,
                exports.Count + locals.Count,
                code
            );
            body = BeamInstructionReader.Read(header, atoms.Count + 1, literalBytes.Count);
        }
        catch (Exception error) when (error is InvalidDataException or NotSupportedException)
        {
            return null;
        }

        var importsByIndex = imports.OrderBy(item => item.Value).Select(item => item.Key).ToArray();
        var names = atoms.OrderBy(item => item.Value).Select(item => item.Key).ToArray();
        using var output = new MemoryStream();
        int copied = 0;
        int entry = 0;
        for (int index = 0; index + 10 < body.Count; index++)
        {
            if (body[index].Definition.Opcode == FunctionInfo)
            {
                entry = (int)body[index + 1].Operands[0].Value;
            }

            if (body[index].Definition.Opcode != Line
                || body[index + 1] is not { Definition.Opcode: Allocate, Operands: [var frame, var live] }
                || frame.Value != 1 || live.Value != 2
                || !Moves(index + 2, X(1), Y(0))
                || body[index + 3] is not { Definition.Opcode: ExternalCall, Operands: [var arity, var callee] }
                || !arity.Value.IsZero || !IsSafeEnumeration(callee)
                || !Moves(index + 4, X(0), X(3))
                || body[index + 5] is not { Definition.Opcode: Move, Operands: [{ Kind: BeamOperandParameters.Atom }, var destination] }
                || destination != X(0)
                || !Moves(index + 6, Y(0), X(1))
                || !Moves(index + 7, new BeamOperand(BeamOperandParameters.Integer, 0), X(2))
                || body[index + 8] is not
                {
                    Definition.Opcode: TailLocalCallWithFrame,
                    Operands: [var recurArity, var recur, var deallocate]
                } || recurArity.Value != 4 || recur.Value != entry || deallocate.Value != 1)
            {
                continue;
            }

            // The pinned safe BIF table removes the call's source location.
            // Reserve the direct result copy until after independent arguments.
            output.Write(code.Span[copied..body[index].Offset]);
            output.Write(code.Span[body[index + 1].Offset..body[index + 4].Offset]);
            Copy(index + 7);
            Copy(index + 6);
            Copy(index + 4);
            Copy(index + 5);
            copied = body[index + 8].Offset;
            index += 8;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();

        bool IsSafeEnumeration(BeamOperand callee)
        {
            var (module, function, arity) = importsByIndex[(int)callee.Value];

            return arity == 0 && names[module - 1] == ErlangModuleNames.Module
                && names[function - 1] is EnumeratePortsFunction or AutoImportNames.Processes;
        }

        bool Moves(int index, BeamOperand from, BeamOperand to) =>
            body[index].Definition.Opcode == Move && body[index].Operands.SequenceEqual([from, to]);

        void Copy(int index) => output.Write(code.Span[body[index].Offset..body[index + 1].Offset]);

        static BeamOperand X(int register) => new(BeamOperandParameters.X, register);

        static BeamOperand Y(int register) => new(BeamOperandParameters.Y, register);
    }
}
