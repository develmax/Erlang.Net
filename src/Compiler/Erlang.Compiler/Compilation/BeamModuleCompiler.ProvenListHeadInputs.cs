using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? AnnotateProvenListOperands(
        ReadOnlyMemory<byte> code,
        int labelCount,
        IReadOnlyDictionary<int, int> localResultEntries
    )
    {
        if (properListSourceFunctions is null || !SsaTypeInferenceEnabled || ModuleOptimizationDisabled
            || OnLoadFunction is not null || sourceModule?.DeclaredNifs is { Count: > 0 })
        {
            return null;
        }

        int headImport = atoms.TryGetValue(AutoImportNames.Hd, out int headAtom)
            && atoms.TryGetValue(ErlangModuleNames.Module, out int erlangAtom)
            ? imports.GetValueOrDefault((erlangAtom, headAtom, 1), -1) : -1;
        if (headImport < 0 && !code.Span.Contains(IsNil))
        {
            return null;
        }

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

        string[] atomNames = atoms.OrderBy(pair => pair.Value).Select(pair => pair.Key).ToArray();
        using var output = new MemoryStream();
        BeamProjectionFlow? flow = null;
        Dictionary<int, (string Name, int Arity)>? functionsByEntry = null;
        (string Name, int Arity) signature = default;
        int entry = -1;
        int copied = 0;
        for (int index = 0; index + 1 < body.Count; index++)
        {
            BeamInstruction instruction = body[index];
            if (instruction is { Definition.Opcode: FunctionInfo, Operands: [_, var function, var arity] })
            {
                signature = (atomNames[(int)function.Value - 1], (int)arity.Value);
                entry = body[index + 1].Definition.Opcode == Label && !exportedFunctions.Contains(signature) ? index + 1 : -1;
                continue;
            }

            if (instruction is
                {
                    Definition.Opcode: IsNil,
                    Operands: [_, { Kind: BeamOperandParameters.X, TypeIndex: null } list]
                } && HasProvenNilInput(index, (int)list.Value))
            {
                const int nilInput = 1;
                copied = AnnotateRegisterInput(
                    code,
                    output,
                    copied,
                    instruction,
                    nilInput,
                    ProperListTypeBits
                );
                continue;
            }

            if (headImport < 0 || entry < 0 || instruction is not
                {
                    Definition.Opcode: BifOne,
                    Operands: [
                    { Kind: BeamOperandParameters.Failure }, var import,
                    { Kind: BeamOperandParameters.X or BeamOperandParameters.Y, TypeIndex: null } source,
                    { Kind: BeamOperandParameters.X or BeamOperandParameters.Y, TypeIndex: null } destination]
                }
                || import.Value != headImport)
            {
                continue;
            }

            flow ??= new BeamProjectionFlow(body);
            if (flow.ReadProperListEntryOrigins(
                index,
                entry,
                source,
                argument => HasNonemptyListEntryFact(signature, argument)
            ))
            {
                output.Write(code.Span[copied..instruction.Offset]);
                output.WriteByte(GetHead);
                WriteRegister(source);
                WriteRegister(destination);
                copied = body[index + 1].Offset;
                continue;
            }

            if (!flow.ReadProperListEntryOrigins(
                index,
                entry,
                source,
                argument => HasProperListEntryFact(signature, argument)
            ))
            {
                continue;
            }

            // The entry proof includes nil. Keep the fallible BIF and record its input type.
            const int headInput = 2;
            copied = AnnotateRegisterInput(
                code,
                output,
                copied,
                instruction,
                headInput,
                ProperListTypeBits
            );
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();

        bool HasProvenNilInput(int index, int tested)
        {
            int prior = index - 1;
            while (prior >= 0 && body[prior].Definition.Opcode == Line)
            {
                prior--;
            }

            if (entry >= 0 && prior == entry)
            {
                return tested < signature.Arity && HasProperListEntryFact(signature, tested);
            }

            if (tested == 0 && HasProvenLocalListResult(prior))
            {
                return true;
            }

            // A checked tail of a proper list is also proper. The head
            // projection carries no such guarantee.
            if (prior <= 0 || body[prior] is not
                {
                    Definition.Opcode: GetList or GetTail,
                    Operands: [{ Kind: BeamOperandParameters.X or BeamOperandParameters.Y, TypeIndex: null } original,
                        .., { Kind: BeamOperandParameters.X, TypeIndex: null } tail]
                } || tail.Value != tested)
            {
                return false;
            }

            if (entry >= 0 && (flow ??= new BeamProjectionFlow(body)).ReadProperListEntryOrigins(
                prior,
                entry,
                original,
                argument => HasProperListEntryFact(signature, argument)
            ))
            {
                return true;
            }

            return original.Kind == BeamOperandParameters.X && original.Value.IsZero
                && body[prior - 1] is
                {
                    Definition.Opcode: IsNonemptyList, Operands: [_, var checkedSource]
                } && SameProjectionRegister(original, checkedSource) && HasProvenLocalListResult(prior - 2);
        }

        bool HasProvenLocalListResult(int call)
        {
            if (call < 0 || body[call] is not { Definition.Opcode: LocalCall, Operands: [_, var callee] })
            {
                return false;
            }

            if (localResultEntries.GetValueOrDefault((int)callee.Value) == ProperListTypeBits)
            {
                return true;
            }

            functionsByEntry ??= IndexFunctionEntries();

            return functionsByEntry.TryGetValue((int)callee.Value, out var called)
                && ReadPrivateEntryFacts()?.ProvesProperListResult(called) == true;
        }

        Dictionary<int, (string Name, int Arity)> IndexFunctionEntries()
        {
            var entries = new Dictionary<int, (string Name, int Arity)>();
            for (int index = 0; index + 1 < body.Count; index++)
            {
                if (body[index] is { Definition.Opcode: FunctionInfo, Operands: [_, var function, var arity] }
                    && body[index + 1] is { Definition.Opcode: Label, Operands: [var label] })
                {
                    entries.Add((int)label.Value, (atomNames[(int)function.Value - 1], (int)arity.Value));
                }
            }

            return entries;
        }

        void WriteRegister(BeamOperand register)
        {
            int tag = register.Kind == BeamOperandParameters.X ? XRegisterTag : YRegisterTag;
            BeamCompactWriter.Write(output, tag, (int)register.Value);
        }
    }
}
