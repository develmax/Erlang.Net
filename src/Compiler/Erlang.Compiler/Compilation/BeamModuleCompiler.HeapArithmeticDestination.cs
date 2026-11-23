using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? ForwardHeapArithmeticDestination(ReadOnlyMemory<byte> code, int labelCount) =>
        RewriteHeapArithmeticDestination(code, labelCount, wrappedCons: false);

    private byte[]? ForwardWrappedHeapArithmeticDestination(ReadOnlyMemory<byte> code, int labelCount) =>
        RewriteHeapArithmeticDestination(code, labelCount, wrappedCons: true);

    private byte[]? RewriteHeapArithmeticDestination(ReadOnlyMemory<byte> code, int labelCount, bool wrappedCons)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(GcBifTwo)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body })
        {
            return null;
        }

        var names = atoms.OrderBy(entry => entry.Value).Select(entry => entry.Key).ToArray();
        var imported = imports.OrderBy(entry => entry.Value).Select(entry => entry.Key).ToArray();
        using var output = new MemoryStream();
        int copied = 0;
        for (int index = 0; index + 3 < body.Count; index++)
        {
            BeamInstruction arithmetic = body[index];
            if (arithmetic is not
                {
                    Definition.Opcode: GcBifTwo,
                    Operands: [
                    { Kind: BeamOperandParameters.Failure, Value.IsZero: true },
                    { Kind: BeamOperandParameters.Unsigned, Value.Sign: > 0 },
                        var target,
                        _,
                        _,
                        var result
                    ]
                } || !IsBareX0(result) || target.Value < 0 || target.Value >= imported.Length
                || !IsLeafArithmeticGcBif(arithmetic, names, imported)
                || body[index + 1] is not
                {
                    Definition.Opcode: Move,
                    Operands: [var source, { Kind: BeamOperandParameters.X, Value.Sign: > 0, TypeIndex: null } destination]
                } || source != result
                || body[index + 2] is not { Definition.Opcode: TestHeap, Operands: [_, var live] }
                || live.Value <= destination.Value
                || body[index + 3] is not { Definition.Opcode: PutList, Operands: [var head, var tail, var list] }
                || SameProjectionRegister(head, result) || SameProjectionRegister(tail, result)
                || (!SameProjectionRegister(head, destination) && !SameProjectionRegister(tail, destination))
                || (wrappedCons
                    ? list != destination || index + 4 >= body.Count
                        || !OverwritesDeadArithmeticResult(body[index + 4], result)
                    : !IsBareX0(list)))
            {
                continue;
            }

            int destinationOffset = body[index + 1].Offset - 1;
            if (code.Span[destinationOffset] != XRegisterTag)
            {
                continue;
            }

            // The arithmetic retains the incoming, initialized X0. The heap
            // check roots the result in its new register. The constructor
            // consumes the result. Either the cons or the immediately
            // following tuple replaces X0 before a value read.
            output.Write(code.Span[copied..destinationOffset]);
            BeamCompactWriter.Write(output, XRegisterTag, destination.Value);
            copied = body[index + 2].Offset;
            index += 2;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }

    private static bool OverwritesDeadArithmeticResult(BeamInstruction instruction, BeamOperand result) =>
        instruction is { Definition.Opcode: PutTuple, Operands: [var destination, _, ..] }
        && destination == result
        && !instruction.Operands.Skip(2).Any(field => SameProjectionRegister(field, result));
}
