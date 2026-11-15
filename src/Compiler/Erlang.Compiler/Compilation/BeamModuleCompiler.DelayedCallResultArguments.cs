using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private static byte[]? LoadCallArgumentsBeforeReusedResultStore(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body
    )
    {
        using var output = new MemoryStream();
        int copied = 0;
        for (int call = 5; call < body.Count; call++)
        {
            if (body[call].Definition.Opcode is not (LocalCall or ExternalCall or TailLocalCallWithFrame or TailExternalCallWithFrame)
                || body[call].Operands[0].Value < 3)
            {
                continue;
            }

            int firstArgument = call - 1;
            while (firstArgument > 0 && body[firstArgument].Definition.Opcode == Line)
            {
                firstArgument--;
            }

            if (body[firstArgument] is not { Definition.Opcode: Move, Operands: [
                { Kind: BeamOperandParameters.Y, TypeIndex: null } argument,
                { Kind: BeamOperandParameters.X, Value.IsZero: true }] })
            {
                continue;
            }

            int arity = (int)body[call].Operands[0].Value;
            var written = new HashSet<int> { 0 };
            int store = firstArgument - 1;
            while (store > 0 && body[store] is { Definition.Opcode: Move, Operands: [
                { Kind: BeamOperandParameters.LiteralValue or BeamOperandParameters.Integer
                    or BeamOperandParameters.Atom or BeamOperandParameters.Nil },
                { Kind: BeamOperandParameters.X } destination] }
                && destination.Value < arity && written.Add((int)destination.Value))
            {
                store--;
            }

            if (store < 2 || written.Count != arity - 1 || body[store].Offset < copied
                || body[store] is not { Definition.Opcode: Move, Operands: [
                    { Kind: BeamOperandParameters.X, Value.IsZero: true }, { Kind: BeamOperandParameters.Y } saved] }
                || argument.Value == saved.Value
                || body[store - 1] is not { Definition.Opcode: Move, Operands: [
                    { Kind: BeamOperandParameters.Y } previous, { Kind: BeamOperandParameters.X } next] }
                || previous.Value != saved.Value || next.Value >= arity || written.Contains((int)next.Value)
                || body[store - 2].Definition.Opcode is not (LocalCall or ExternalCall or DynamicApply or CallFun or CallFunTyped))
            {
                continue;
            }

            // The first argument cannot occupy X0 before the spill. Use the register
            // of a literal argument as the temporary and load the literal afterwards.
            BeamOperand temporary = body[store + 1].Operands[1];
            output.Write(code.Span[copied..body[store].Offset]);
            output.WriteByte(Move);
            WriteRegister(output, argument);
            WriteRegister(output, temporary);
            output.Write(code.Span[body[store].Offset..body[store + 1].Offset]);
            output.WriteByte(Move);
            WriteRegister(output, temporary);
            WriteRegister(output, body[firstArgument].Operands[1]);
            output.Write(code.Span[body[store + 1].Offset..body[firstArgument].Offset]);
            copied = body[firstArgument + 1].Offset;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);
        return output.ToArray();
    }
}
