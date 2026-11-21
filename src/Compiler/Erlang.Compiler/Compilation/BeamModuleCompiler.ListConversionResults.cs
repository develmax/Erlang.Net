using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? AnnotateListProducerResults(
        ReadOnlyMemory<byte> code,
        int labelCount,
        Dictionary<int, int> localResultEntries
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
        if (!hasLocalLists && ((tupleToList < 0 && atomToList < 0 && binaryToList < 0)
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
        foreach (var instruction in instructions)
        {
            if (instruction.Definition.Opcode == Line)
            {
                continue;
            }

            if (hasLocalLists && instruction is { Definition.Opcode: LocalCall, Operands: [_, var callee] }
                && localResultEntries.GetValueOrDefault((int)callee.Value) == ProperListTypeBits)
            {
                listResult = callResult;
                localList = true;
                continue;
            }

            // The resolved conversion produces a list only on its successful continuation.
            if (instruction is { Definition.Opcode: ExternalCall, Operands: [_, var import] }
                && ((int)import.Value == tupleToList || (int)import.Value == atomToList
                    || (int)import.Value == binaryToList))
            {
                listResult = callResult;
                localList = false;
                continue;
            }

            if (listResult is null)
            {
                continue;
            }

            if (instruction is { Definition.Opcode: Move, Operands: [var from, var to] }
                && SameProjectionRegister(listResult, from))
            {
                listResult = to;
                continue;
            }

            int input = instruction switch
            {
                { Definition.Opcode: IsNil } when localList => 1,
                { Definition.Opcode: BifOne, Operands: [_, var operation, _, _] }
                    when !localList && ((int)operation.Value == head || (int)operation.Value == tail) => 2,
                { Definition.Opcode: GcBifOne, Operands: [_, _, var operation, _, _] }
                    when (int)operation.Value == length => 3,
                _ => -1
            };
            if (input >= 0 && instruction.Operands[input] is { TypeIndex: null } operand
                && SameProjectionRegister(listResult, operand))
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
