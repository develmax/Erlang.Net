using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? AnnotateProvenListHeadInputs(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (properListSourceFunctions is null || !SsaTypeInferenceEnabled || ModuleOptimizationDisabled
            || OnLoadFunction is not null || sourceModule?.DeclaredNifs is { Count: > 0 }
            || !atoms.TryGetValue(AutoImportNames.Hd, out int headAtom)
            || !atoms.TryGetValue(ErlangModuleNames.Module, out int erlangAtom)
            || !imports.TryGetValue((erlangAtom, headAtom, 1), out int headImport))
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

            if (entry < 0 || instruction is not
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

        void WriteRegister(BeamOperand register)
        {
            int tag = register.Kind == BeamOperandParameters.X ? XRegisterTag : YRegisterTag;
            BeamCompactWriter.Write(output, tag, (int)register.Value);
        }
    }
}
