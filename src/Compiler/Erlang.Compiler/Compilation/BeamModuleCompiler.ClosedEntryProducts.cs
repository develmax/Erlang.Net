using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? AnnotateClosedEntryProducts(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || sourceModule is not { DeclaredFunctions: var functions }
            || !code.Span.Contains(GcBifTwo)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body }
            || !atoms.TryGetValue(ErlangModuleNames.Module, out int erlang)
            || !atoms.TryGetValue(ErlangOperators.Multiply, out int multiply)
            || !imports.TryGetValue((erlang, multiply, 2), out int import))
        {
            return null;
        }

        using var output = new MemoryStream();
        IReadOnlyDictionary<(string Name, int Arity), FunctionDefinition>? declarations = null;
        GeneralEmitter? analyzer = null;
        var names = atoms.OrderBy(atom => atom.Value).Select(atom => atom.Key).ToArray();
        int copied = 0;
        for (int info = 0; info + 4 < body.Count; info++)
        {
            if (body[info] is not { Definition.Opcode: FunctionInfo, Operands: [_, var function, { Value.IsOne: true }] }
                || body[info + 1].Definition.Opcode != Label)
            {
                continue;
            }

            int index = info + 2;
            while (body[index].Definition.Opcode == Line)
            {
                index++;
            }

            if (index + 1 >= body.Count || body[index] is not
                {
                    Definition.Opcode: GcBifTwo,
                    Operands: [
                    { Kind: BeamOperandParameters.Failure, Value.IsZero: true },
                    { Value.IsOne: true },
                        var called, var left, var right,
                    { Kind: BeamOperandParameters.X, TypeIndex: null } destination
                    ]
                } || called.Value != import || destination.Value > 1
                || body[index + 1] is not { Definition.Opcode: Allocate, Operands: [{ Value.IsZero: true }, _] })
            {
                continue;
            }

            BeamOperand? multiplier = IsBareX0(left) && right.Kind == BeamOperandParameters.Integer ? right
                : IsBareX0(right) && left.Kind == BeamOperandParameters.Integer ? left
                : null;
            if (multiplier is null)
            {
                continue;
            }

            declarations ??= functions.ToDictionary(item => (item.Name, item.Arity));
            var signature = (names[(int)function.Value - 1], 1);
            if (!declarations.TryGetValue(signature, out FunctionDefinition? declaration))
            {
                continue;
            }

            analyzer ??= new GeneralEmitter(
                this,
                new Dictionary<(string Name, int Arity), int>(),
                declarations,
                default,
                frameSize: 0
            );
            if (analyzer.ReadCapturedEntryIntegerLowerBound(declaration) is not long lower)
            {
                continue;
            }

            // The only register input is the unchanged incoming formal. Its
            // private call contexts prove integer multiplication and allow
            // OTP's variable-first order without changing badarith operands.
            output.Write(code.Span[copied..body[index].Offset]);
            output.WriteByte(GcBifTwo);
            BeamCompactWriter.Write(output, LabelTag, 0);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, 1);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, import);
            output.WriteByte(ExtendedTypedRegisterOperand);
            BeamCompactWriter.Write(output, XRegisterTag, 0);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, GetOperandTypeIndex((IntegerTypeBits, lower, null, null)));
            BeamCompactWriter.Write(output, BeamWire.IntegerTag, multiplier.Value);
            WriteRegister(output, destination);
            copied = body[index + 1].Offset;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }
}
