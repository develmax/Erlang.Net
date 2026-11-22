using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private (ReadOnlyMemory<byte> Code, int LabelCount)? PartitionKnownKeyFindReturns(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(IsTuple)
            || !atoms.TryGetValue(LookupListModule, out int lists)
            || !atoms.TryGetValue(KeyFindFunction, out int keyFind)
            || !atoms.TryGetValue(ErlangBooleanAtoms.False, out int falseAtom)
            || !imports.TryGetValue((lists, keyFind, KeyFindArity), out int import)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body }
            || RewriteKnownKeyFindReturns(
                code,
                body,
                emittedImports?[import] ?? import,
                MapEmittedAtom(falseAtom),
                () => GetOperandTypeIndex((AtomTypeBits | TupleTypeBits, null, null, null))
            ) is not { } rewritten)
        {
            return null;
        }

        var ordered = OrderFinalPhysicalLabelsCore(
            rewritten,
            labelCount,
            formMultiKeySelections: false,
            relocateOnly: true
        ) ?? (Code: (ReadOnlyMemory<byte>)rewritten, LabelCount: labelCount);

        return (RebuildLineTable(ordered.Code.ToArray(), ordered.LabelCount), ordered.LabelCount);
    }

    private static byte[]? RewriteKnownKeyFindReturns(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body,
        int keyFind,
        int falseAtom,
        Func<int> resultType
    )
    {
        if (body.Any(instruction => instruction.Definition.Opcode is DebugLine or ExecutableLine))
        {
            return null;
        }

        using var output = new MemoryStream();
        BeamProjectionFlow? flow = null;
        var result = new BeamOperand(BeamOperandParameters.X, 0);
        int copied = 0;
        int entry = 0;
        for (int end = 0; end < body.Count; end++)
        {
            if (body[end].Definition.Opcode is not (FunctionInfo or EndCode))
            {
                continue;
            }

            int start = entry;
            entry = end + 1;
            (int Check, int Tuple, int Failure, int NonTupleLabel)? plan = null;
            for (int call = start + 1; call + 8 < end; call++)
            {
                int check = call + 1;
                if (body[call] is not { Definition.Opcode: ExternalCall, Operands: [var arity, var import] }
                    || arity.Value != KeyFindArity || import.Value != keyFind
                    || body[check] is not
                    {
                        Definition.Opcode: ExactEqualTest,
                        Operands: [var nonFalse, var selector, { Kind: BeamOperandParameters.Atom } literal]
                    } || selector != result || literal.Value != falseAtom
                    || body[check + 1] is not { Definition.Opcode: Move, Operands: [_, var atomResult] }
                    || atomResult != result || body[check + 2].Definition.Opcode != Deallocate
                    || body[check + 3].Definition.Opcode != Return
                    || body[check + 4] is not { Definition.Opcode: Label, Operands: [var tupleLabel] }
                    || tupleLabel.Value != nonFalse.Value || body[check + 5] is not
                    {
                        Definition.Opcode: IsTuple,
                        Operands: [var nonTuple, var tested]
                    } || tested != result || body[check + 6] is not
                    {
                        Definition.Opcode: TestArity,
                        Operands: [var wrongArity, var tuple, _]
                    } || tuple != result || nonTuple != wrongArity || !HasKnownKeyFindArguments(body, call))
                {
                    continue;
                }

                flow ??= new BeamProjectionFlow(body);
                if (!flow.TryGetSingleForwardTarget(check, (int)tupleLabel.Value, out int tupleEntry)
                    || tupleEntry != check + 4
                    || !flow.TryGetForwardLabel(check + 6, (int)wrongArity.Value, out int failure)
                    || failure + 2 >= end || !EndsBeamBlock(body[failure - 1].Definition.Opcode)
                    || body[failure + 1] is not { Definition.Opcode: Line, Operands: [{ Value.Sign: > 0 }] }
                    || body[failure + 2] is not { Definition.Opcode: CaseEnd, Operands: [var failed] }
                    || failed != result || !flow.HasUnprotectedIncomingPath(check, start))
                {
                    continue;
                }

                if (plan is not null)
                {
                    plan = null;
                    break;
                }

                plan = (check, check + 6, failure, (int)tupleLabel.Value);
            }

            if (plan is not { } partition)
            {
                continue;
            }

            // A successful keyfind returns a tuple or false. The arity and
            // bound fields still fail with the original returned tuple.
            output.Write(code.Span[copied..body[partition.Check].Offset]);
            output.WriteByte(IsTuple);
            BeamCompactWriter.Write(output, LabelTag, partition.NonTupleLabel);
            output.WriteByte(ExtendedTypedRegisterOperand);
            WriteRegister(output, result);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, resultType());
            output.Write(code.Span[body[partition.Tuple].Offset..body[partition.Failure].Offset]);
            output.Write(code.Span[body[partition.Check + 4].Offset..body[partition.Check + 5].Offset]);
            output.Write(code.Span[body[partition.Check + 1].Offset..body[partition.Check + 4].Offset]);
            copied = body[partition.Failure].Offset;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }
}
