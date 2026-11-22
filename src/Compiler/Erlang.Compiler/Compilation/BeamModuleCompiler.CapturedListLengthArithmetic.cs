using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private int? ReadCapturedListLength(
        Clause clause,
        Expr.ListComprehension comprehension,
        string[] captures
    )
    {
        foreach (Expr.Sequence sequence in LocalFunctionEntries.Walk(clause.Body).OfType<Expr.Sequence>())
        {
            for (int index = 1; index < sequence.Items.Count; index++)
            {
                if (ReadDirectHelperExpression(sequence.Items[index]) is not Expr.Binary
                    {
                        Operator: ErlangOperators.Append,
                        Left: var mapped
                    } || !ReferenceEquals(mapped, comprehension)
                    || sequence.Items[index - 1] is not Expr.Match
                    {
                        Pattern: Pattern.Variable { Name: not VariableScopeNames.Wildcard } binding,
                        Value: Expr.Call
                        {
                            Module: null or ErlangModuleNames.Module, DynamicRemote: false,
                            Function: AutoImportNames.Length, Arguments: [Expr.Variable]
                        } length
                    } || !ResolvesErlangBif(length))
                {
                    continue;
                }

                int capture = Array.IndexOf(captures, binding.Name);

                // The immediately preceding successful match proves the value
                // passed to the new private helper. Each recursive edge keeps
                // the exact same capture in the same formal position.
                return capture < 0 ? null : capture + 1;
            }
        }

        return null;
    }

    private byte[]? UseIncomingListLengthForArithmetic(
        ReadOnlyMemory<byte> code,
        int labelCount,
        IReadOnlyList<FunctionDefinition> functions
    )
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(GcBifTwo)
            || !atoms.TryGetValue(ErlangModuleNames.Module, out int erlang)
            || !atoms.TryGetValue(ErlangOperators.Plus, out int plus)
            || !imports.TryGetValue((erlang, plus, 2), out int addition))
        {
            return null;
        }

        var captures = functions.Where(function => function.CapturedListLengthParameter.HasValue
                && atoms.ContainsKey(function.Name))
            .ToDictionary(
                function => (MapEmittedAtom(atoms[function.Name]), function.Arity),
                function => function.CapturedListLengthParameter!.Value
            );
        if (captures.Count == 0 || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body })
        {
            return null;
        }

        int addImport = emittedImports?[addition] ?? addition;
        var source = new BeamOperand(BeamOperandParameters.X, 0);
        using var output = new MemoryStream();
        int copied = 0;
        for (int info = 0; info + 6 < body.Count; info++)
        {
            if (body[info] is not { Definition.Opcode: FunctionInfo, Operands: [_, var name, var arguments] }
                || !captures.TryGetValue(((int)name.Value, (int)arguments.Value), out int capture)
                || body[info + 1].Definition.Opcode != Label
                || body[info + 2] is not { Definition.Opcode: IsNonemptyList, Operands: [_, var checkedList] }
                || checkedList != source || body[info + 3] is not
                {
                    Definition.Opcode: Allocate,
                    Operands: [_, var live]
                } || live.Value != arguments.Value || capture >= live.Value)
            {
                continue;
            }

            int index = info + 4;
            BeamOperand? saved = null;
            while (body[index] is
                {
                    Definition.Opcode: Move,
                    Operands:
                    [
                    { Kind: BeamOperandParameters.X, TypeIndex: null } incoming,
                    { Kind: BeamOperandParameters.Y, TypeIndex: null } slot
                    ]
                })
            {
                if (incoming.Value == capture)
                {
                    saved = slot;
                }
                else if (slot == saved)
                {
                    saved = null;
                }

                index++;
            }

            if (saved is null || body[index] is not
                {
                    Definition.Opcode: GetList, Operands: [var list, var head, var tail]
                } || list != source || head.Kind != BeamOperandParameters.Y
                || tail.Kind != BeamOperandParameters.Y || head == saved || tail == saved)
            {
                continue;
            }

            index++;
            while (body[index].Definition.Opcode == Line)
            {
                index++;
            }

            if (index + 1 >= body.Count || body[index] is not
                {
                    Definition.Opcode: GcBifTwo,
                    Operands:
                    [
                    { Kind: BeamOperandParameters.Failure, Value.IsZero: true },
                    { Value.IsZero: true },
                        var called, var operand, { Kind: BeamOperandParameters.Integer } constant, var result
                    ]
                } || called.Value != addImport || operand != saved || result != source)
            {
                continue;
            }

            // Since allocation, the successful prefix has only copied to Y.
            // The saved length is still in its incoming X register, and every
            // X register covered by the allocation's live count remains valid.
            output.Write(code.Span[copied..body[index].Offset]);
            output.WriteByte(GcBifTwo);
            BeamCompactWriter.Write(output, LabelTag, 0);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, live.Value);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, called.Value);
            output.WriteByte(ExtendedTypedRegisterOperand);
            BeamCompactWriter.Write(output, XRegisterTag, capture);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, GetOperandTypeIndex((
                IntegerTypeBits,
                0,
                OtpSizeUpperBound,
                null
            )));
            BeamCompactWriter.Write(output, BeamWire.IntegerTag, constant.Value);
            WriteRegister(output, result);
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
