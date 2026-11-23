using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private HashSet<int> ReadNilTerminatedMapperEntries(
        IReadOnlyList<FunctionDefinition> functions,
        IReadOnlyDictionary<(string Name, int Arity), int> localEntries
    )
    {
        var result = new HashSet<int>();
        foreach (FunctionDefinition function in functions)
        {
            if (function.HasNilTerminatedMapperResult
                && localEntries.TryGetValue((function.Name, function.Arity), out int label)
                && (label > 0 || deferredEntries.TryGetValue(label, out label)))
            {
                result.Add(MapEmittedLabel(label));
            }
        }

        return result;
    }

    private byte[]? AnnotateFinalListRecordResults(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (!SsaTypeInferenceEnabled || ModuleOptimizationDisabled || !code.Span.Contains(UpdateRecord)
            || properListSourceFunctions is not { } definitions
            || !definitions.Values.Any(function => function.HasNilTerminatedMapperResult && function.IsRecursiveListMapper)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body })
        {
            return null;
        }

        // Label relocation follows the source passes. Match the proven mapper MFA
        // against its final function entry before using the call target.
        var names = atoms.Where(pair => MapEmittedAtom(pair.Value) > 0)
            .ToDictionary(pair => MapEmittedAtom(pair.Value), pair => pair.Key);
        var mappers = new HashSet<int>();
        for (int index = 0; index + 1 < body.Count; index++)
        {
            if (body[index] is { Definition.Opcode: FunctionInfo, Operands: [_, var name, var arity] }
                && body[index + 1] is { Definition.Opcode: Label, Operands: [var label] }
                && names.TryGetValue((int)name.Value, out string? function)
                && definitions.TryGetValue((function, (int)arity.Value), out var definition)
                && definition.HasNilTerminatedMapperResult && definition.IsRecursiveListMapper)
            {
                mappers.Add((int)label.Value);
            }
        }

        return mappers.Count == 0 ? null : AnnotateListProducerResults(
            code,
            labelCount,
            [],
            recordListMappers: mappers,
            recordUpdatesOnly: true
        );
    }

    private byte[]? AnnotateListProducerResults(
        ReadOnlyMemory<byte> code,
        int labelCount,
        Dictionary<int, int> localResultEntries,
        IReadOnlySet<int>? compactedEntries = null,
        IReadOnlySet<int>? recordListMappers = null,
        bool recordUpdatesOnly = false
    )
    {
        if (!SsaTypeInferenceEnabled || ModuleOptimizationDisabled)
        {
            return null;
        }

        bool hasLocalLists = localResultEntries.ContainsValue(ProperListTypeBits);
        int erlang = atoms.GetValueOrDefault(ErlangModuleNames.Module);
        int tupleToList = FindImport(AutoImportNames.TupleToList);
        int atomToList = FindImport(AutoImportNames.AtomToList);
        int binaryToList = FindImport(AutoImportNames.BinaryToList);
        int head = FindImport(AutoImportNames.Hd);
        int tail = FindImport(AutoImportNames.Tl);
        int length = FindImport(AutoImportNames.Length);
        if (!hasLocalLists && recordListMappers is not { Count: > 0 } && ((tupleToList < 0 && atomToList < 0 && binaryToList < 0)
            || (head < 0 && tail < 0 && length < 0)))
        {
            return null;
        }

        IReadOnlyList<BeamInstruction> instructions;
        try
        {
            var header = new BeamCodeHeader(
                BeamWire.FormatVersion,
                highestOpcode,
                labelCount,
                exports.Count + locals.Count,
                code
            );
            instructions = BeamInstructionReader.Read(header, atoms.Count + 1, literalBytes.Count);
        }
        catch (Exception error) when (error is InvalidDataException or NotSupportedException)
        {
            return null;
        }

        using var output = new MemoryStream();
        int copied = 0;
        var callResult = new BeamOperand(BeamOperandParameters.X, 0);
        BeamOperand? listResult = null;
        bool localList = false;
        bool keptForRecord = false;
        bool recordMapperOnly = false;
        bool inScope = compactedEntries is null;
        bool nextIsEntry = false;
        foreach (var instruction in instructions)
        {
            if (compactedEntries is not null && instruction.Definition.Opcode == FunctionInfo)
            {
                inScope = false;
                nextIsEntry = true;
                listResult = null;
                keptForRecord = false;
                continue;
            }

            if (nextIsEntry)
            {
                inScope = instruction is { Definition.Opcode: Label, Operands: [var entry] }
                    && compactedEntries!.Contains((int)entry.Value);
                nextIsEntry = false;
            }

            if (!inScope || instruction.Definition.Opcode == Line)
            {
                continue;
            }

            if (instruction is { Definition.Opcode: LocalCall, Operands: [_, var callee] })
            {
                int label = (int)callee.Value;
                localList = localResultEntries.GetValueOrDefault(label) == ProperListTypeBits;
                recordMapperOnly = !localList && recordListMappers?.Contains(label) == true;
                listResult = localList || recordMapperOnly ? callResult : null;
                keptForRecord = false;
                continue;
            }

            // The resolved conversion produces a list only on its successful continuation.
            if (!recordUpdatesOnly && compactedEntries is null
                && instruction is { Definition.Opcode: ExternalCall, Operands: [_, var import] }
                && ((int)import.Value == tupleToList || (int)import.Value == atomToList
                    || (int)import.Value == binaryToList))
            {
                listResult = callResult;
                localList = false;
                keptForRecord = false;
                recordMapperOnly = false;
                continue;
            }

            if (listResult is null)
            {
                continue;
            }

            if (keptForRecord)
            {
                if (instruction.Definition.Opcode == UpdateRecord)
                {
                    for (int field = FirstRecordUpdateValueOperand; field < instruction.Operands.Count;
                        field += RecordUpdateFieldPairWidth)
                    {
                        BeamOperand value = instruction.Operands[field];
                        if (value.TypeIndex is null && SameProjectionRegister(listResult, value))
                        {
                            copied = AnnotateRegisterInput(
                                code,
                                output,
                                copied,
                                instruction,
                                field,
                                ProperListTypeBits
                            );
                        }
                    }
                }

                listResult = null;
                keptForRecord = false;
                continue;
            }

            if (compactedEntries is null && (localList || recordMapperOnly) && listResult == callResult
                && instruction is { Definition.Opcode: TestHeap, Operands: [_, { Value.IsOne: true }] })
            {
                // The GC root keeps the successful call result alive for the
                // immediately following record update.
                keptForRecord = true;
                continue;
            }

            if (instruction is { Definition.Opcode: Move, Operands: [var from, var to] }
                && SameProjectionRegister(listResult, from))
            {
                listResult = to;
                continue;
            }

            int input = recordMapperOnly ? -1 : instruction switch
            {
                { Definition.Opcode: IsNil } when localList => 1,
                { Definition.Opcode: BifOne, Operands: [_, var operation, _, _] }
                    when !localList && ((int)operation.Value == head || (int)operation.Value == tail) => 2,
                { Definition.Opcode: GcBifOne, Operands: [_, _, var operation, _, _] }
                    when (int)operation.Value == length => 3,
                _ => -1
            };
            if (input >= 0 && instruction.Operands[input] is { TypeIndex: null } operand
                && SameProjectionRegister(listResult, operand)
                && (compactedEntries is null || localList && instruction.Definition.Opcode == IsNil && IsBareX0(operand)))
            {
                copied = AnnotateRegisterInput(
                    code,
                    output,
                    copied,
                    instruction,
                    input,
                    ProperListTypeBits
                );
            }

            // A different instruction, including a label with other predecessors, ends this proof.
            listResult = null;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();

        int FindImport(string function) => atoms.TryGetValue(function, out int atom)
            && imports.TryGetValue((erlang, atom, 1), out int index) ? index : -1;
    }
}
