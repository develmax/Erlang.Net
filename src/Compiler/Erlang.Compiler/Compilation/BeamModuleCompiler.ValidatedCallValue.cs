using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? KeepValidatedCallValueInRegisters(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body,
        string[] atomNames,
        (int Module, int Function, int Arity)[] importsByIndex
    )
    {
        using var output = new MemoryStream();
        var value = new BeamOperand(BeamOperandParameters.X, 0);
        var length = new BeamOperand(BeamOperandParameters.X, 1);
        int copied = 0;
        int entry = 0;
        Dictionary<(string Name, int Arity), FunctionDefinition>? definitions = null;
        Dictionary<int, (string Name, int Arity)>? entries = null;
        var binaryResults = new Dictionary<(string Name, int Arity), bool>();
        for (int end = 0; end < body.Count; end++)
        {
            if (body[end].Definition.Opcode is not (FunctionInfo or EndCode))
            {
                continue;
            }

            int start = entry;
            entry = end + 1;
            int allocation = start + 1;
            if (allocation + 4 >= end || body[start].Definition.Opcode != Label
                || body[allocation] is not
                {
                    Definition.Opcode: Allocate,
                    Operands: [var width, var live]
                } || width.Value.Sign <= 0)
            {
                continue;
            }

            int initialize = allocation + 1;
            int call = SkipLines(body[initialize].Definition.Opcode == InitYRegisters ? initialize + 1 : initialize);
            while (call < end && body[call] is
                {
                    Definition.Opcode: Move,
                    Operands: [
                    {
                        Kind: BeamOperandParameters.X or BeamOperandParameters.Atom or BeamOperandParameters.Integer
                        or BeamOperandParameters.LiteralValue or BeamOperandParameters.Nil
                    },
                    { Kind: BeamOperandParameters.X }]
                })
            {
                call = SkipLines(call + 1);
            }

            if (call + 3 >= end || body[call].Definition.Opcode is not (LocalCall or ExternalCall)
                || body[call + 1] is not
                {
                    Definition.Opcode: Move,
                    Operands: [var original, { Kind: BeamOperandParameters.Y, TypeIndex: null } saved]
                } || original != value || saved.Value >= width.Value)
            {
                continue;
            }

            int size = SkipLines(call + 2);
            if (size + 1 >= end || body[size] is not
                {
                    Definition.Opcode: GcBifOne,
                    Operands: [
                    { Kind: BeamOperandParameters.Failure }, { Value.IsZero: true },
                    var import, var measured, var sizeOutput]
                } || !SameProjectionRegister(saved, measured) || sizeOutput != value
                || !IsSizeImport(import, AutoImportNames.ByteSize)
                || body[size + 1] is not { Definition.Opcode: Move, Operands: [var input, var sizeSlot] }
                || input != value || sizeSlot.TypeIndex is not null || sizeSlot == saved
                || sizeSlot != length && (sizeSlot.Kind != BeamOperandParameters.Y || sizeSlot.Value >= width.Value))
            {
                continue;
            }

            int firstTest = SkipLines(size + 2);
            int success = firstTest;
            BeamOperand? rejected = null;
            while (success < end && body[success] is
                {
                    Definition.Opcode: ExactEqualTest or IsGreaterOrEqual or IsLess,
                    Operands: [{ Kind: BeamOperandParameters.Label } fail, var left, var right]
                } && (rejected is null || rejected == fail)
                && (SameProjectionRegister(left, sizeSlot) && right.Kind == BeamOperandParameters.Integer
                    || SameProjectionRegister(right, sizeSlot) && left.Kind == BeamOperandParameters.Integer))
            {
                rejected = fail;
                success = SkipLines(success + 1);
            }

            if (rejected is null || success >= end)
            {
                continue;
            }

            int heap = -1;
            int construction = -1;
            int restore = -1;
            if (body[success] is { Definition.Opcode: Move, Operands: [var stored, var target] }
                && stored == saved && target == value)
            {
                restore = success;
                success = SkipLines(success + 1);
            }
            else if (body[success] is { Definition.Opcode: TestHeap, Operands: [_, { Value.IsZero: true }] })
            {
                heap = success;
                construction = SkipLines(success + 1);
                if (construction >= end || body[construction] is not
                    {
                        Definition.Opcode: PutList,
                        Operands: [{ Kind: BeamOperandParameters.Atom }, var tail, var destination]
                    } || tail != saved || destination != value)
                {
                    continue;
                }

                success = SkipLines(construction + 1);
            }
            else
            {
                continue;
            }

            if (success >= end || !IsExit(
                success,
                width,
                allowReturn: true,
                out int afterSuccess
            ))
            {
                continue;
            }

            int failure = SkipLines(afterSuccess);
            if (failure >= end || body[failure] is not { Definition.Opcode: Label, Operands: [var label] }
                || label.Value != rejected.Value)
            {
                continue;
            }

            int failureExit = SkipLines(failure + 1);
            bool failureHasArgument = failureExit < end && body[failureExit] is
            {
                Definition.Opcode: Move,
                Operands: [
                {
                    Kind: BeamOperandParameters.Atom or BeamOperandParameters.Integer
                        or BeamOperandParameters.LiteralValue or BeamOperandParameters.Nil
                },
                    var failureArgument]
            } && failureArgument == value;
            if (failureHasArgument)
            {
                failureExit = SkipLines(failureExit + 1);
            }

            if (failureExit >= end || !failureHasArgument && body[failureExit].Definition.Opcode != IfEnd
                || !IsExit(
                    failureExit,
                    width,
                    allowReturn: false,
                    out int afterFailure
                )
                || !EndsFunction(afterFailure) || UnprotectedReturnLabels(body, start, end) is not null)
            {
                continue;
            }

            AdvanceTo(allocation);
            output.WriteByte(Allocate);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, 0);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, live.Value);
            copied = body[initialize].Offset;
            if (body[initialize].Definition.Opcode == InitYRegisters)
            {
                copied = body[initialize + 1].Offset;
            }

            AdvanceTo(call + 1);
            copied = body[call + 2].Offset;
            AdvanceTo(size);
            output.WriteByte(GcBifOne);
            BeamCompactWriter.Write(output, LabelTag, 0);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, 1);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, import.Value);
            int? measuredType = measured.TypeIndex;
            if (measuredType is null && SsaTypeInferenceEnabled && body[call].Definition.Opcode == ExternalCall
                && (IsSizeImport(body[call].Operands[1], AutoImportNames.IolistToBinary)
                    || IsSizeImport(body[call].Operands[1], AutoImportNames.ListToBinary)))
            {
                measuredType = GetOperandTypeIndex((MatchableTypeBits, null, null, null));
            }

            if (measuredType is null && SsaTypeInferenceEnabled && !ModuleOptimizationDisabled
                && OnLoadFunction is null && sourceModule is { } module
                && body[call] is
                {
                    Definition.Opcode: LocalCall,
                    Operands: [var callArity, { Kind: BeamOperandParameters.Label } entryOperand]
                })
            {
                definitions ??= module.Functions.ToDictionary(definition => (definition.Name, definition.Arity));
                entries ??= ReadLocalEntries();
                if (entries.TryGetValue((int)entryOperand.Value, out var signature) && callArity.Value == signature.Arity)
                {
                    if (!binaryResults.TryGetValue(signature, out bool proven))
                    {
                        proven = ProvesMeasurementReturn(signature, binary: true, definitions);
                        binaryResults.Add(signature, proven);
                    }

                    if (proven)
                    {
                        // binary_to_list on the original call result keeps the byte-aligned proof at the size read.
                        int unit = restore >= 0 && body[success].Definition.Opcode == TailExternalCallWithFrame
                            && IsSizeImport(body[success].Operands[1], AutoImportNames.BinaryToList)
                            ? BitSyntaxDefaults.BinaryUnit : BitSyntaxDefaults.BitStringUnit;
                        measuredType = GetOperandTypeIndex(
                            (MatchableTypeBits, null, null, unit)
                        );
                    }
                }
            }

            WriteTypedRegister(value, measuredType);
            WriteRegister(output, length);
            copied = body[size + 2].Offset;

            for (int index = firstTest; index < (restore >= 0 ? restore : heap); index++)
            {
                if (body[index].Definition.Opcode == Line)
                {
                    continue;
                }

                AdvanceTo(index);
                var instruction = body[index];
                output.WriteByte(instruction.Definition.Opcode);
                BeamCompactWriter.Write(output, LabelTag, instruction.Operands[0].Value);
                foreach (BeamOperand operand in instruction.Operands.Skip(1))
                {
                    if (operand.Kind == BeamOperandParameters.Integer)
                    {
                        BeamCompactWriter.Write(output, BeamWire.IntegerTag, operand.Value);
                    }
                    else
                    {
                        WriteTypedRegister(length, operand.TypeIndex);
                    }
                }

                copied = body[index + 1].Offset;
            }

            if (restore >= 0)
            {
                AdvanceTo(restore);
                copied = body[restore + 1].Offset;
            }
            else
            {
                AdvanceTo(heap);
                output.WriteByte(TestHeap);
                BeamCompactWriter.Write(output, BeamWire.UnsignedTag, body[heap].Operands[0].Value);
                BeamCompactWriter.Write(output, BeamWire.UnsignedTag, 1);
                copied = body[heap + 1].Offset;
                AdvanceTo(construction);
                output.WriteByte(PutList);
                BeamCompactWriter.Write(output, AtomTag, body[construction].Operands[0].Value);
                WriteRegister(output, value);
                WriteRegister(output, value);
                copied = body[construction + 1].Offset;
            }

            ReleaseFrame(success);
            ReleaseFrame(failureExit);

            int SkipLines(int index)
            {
                while (index < end && body[index].Definition.Opcode == Line)
                {
                    index++;
                }

                return index;
            }

            bool EndsFunction(int index)
            {
                int next = SkipLines(index);

                return next == end || body[end].Definition.Opcode == FunctionInfo
                    && body[next].Definition.Opcode == Label && SkipLines(next + 1) == end;
            }

            bool IsExit(
                int index,
                BeamOperand expectedWidth,
                bool allowReturn,
                out int next
            )
            {
                next = index + 1;
                BeamInstruction instruction = body[index];
                if (instruction.Definition.Opcode == IfEnd)
                {
                    return !allowReturn;
                }

                if (instruction.Definition.Opcode is TailLocalCallWithFrame or TailExternalCallWithFrame)
                {
                    return instruction.Operands[0].Value.IsOne && instruction.Operands[^1] == expectedWidth;
                }

                if (allowReturn && instruction.Definition.Opcode == Deallocate
                    && instruction.Operands[0] == expectedWidth)
                {
                    int ret = SkipLines(next);
                    next = ret + 1;

                    return ret < end && body[ret].Definition.Opcode == Return;
                }

                return false;
            }
        }

        Dictionary<int, (string Name, int Arity)> ReadLocalEntries()
        {
            var found = new Dictionary<int, (string Name, int Arity)>();
            for (int index = 0; index + 1 < body.Count; index++)
            {
                if (body[index] is
                    {
                        Definition.Opcode: FunctionInfo,
                        Operands: [_, { Kind: BeamOperandParameters.Atom } name, var arity]
                    }
                    && body[index + 1] is { Definition.Opcode: Label, Operands: [var label] })
                {
                    found.Add((int)label.Value, (atomNames[(int)name.Value - 1], (int)arity.Value));
                }
            }

            return found;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();

        bool IsSizeImport(BeamOperand import, string name)
        {
            var target = importsByIndex[(int)import.Value];

            return target.Arity == 1 && atomNames[target.Module - 1] == ErlangModuleNames.Module
                && atomNames[target.Function - 1] == name;
        }

        void AdvanceTo(int index)
        {
            output.Write(code.Span[copied..body[index].Offset]);
            copied = body[index].Offset;
        }

        void ReleaseFrame(int index)
        {
            int sizeOperand = LinearFrameSizeOperand(body[index].Definition.Opcode);
            if (sizeOperand >= 0)
            {
                copied = WritePostCallProjectionOperand(
                    output,
                    code,
                    body[index].Offset,
                    copied,
                    sizeOperand,
                    0
                );
            }
        }

        void WriteTypedRegister(BeamOperand register, int? type)
        {
            if (type is not null)
            {
                output.WriteByte(ExtendedTypedRegisterOperand);
            }

            WriteRegister(output, register);
            if (type is not null)
            {
                BeamCompactWriter.Write(output, BeamWire.UnsignedTag, type.Value);
            }
        }
    }
}
