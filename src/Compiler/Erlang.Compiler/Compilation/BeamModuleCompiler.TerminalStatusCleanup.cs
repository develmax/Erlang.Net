using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? TrimTerminalStatusCleanup(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(ExactEqualTest)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body })
        {
            return null;
        }

        string[] atomNames = atoms.OrderBy(entry => entry.Value).Select(entry => entry.Key).ToArray();
        var pureCleanupImports = imports.Where(entry => entry.Key.Arity == 1
                && BeamTrimBifs.IsPure(atomNames[entry.Key.Module - 1], atomNames[entry.Key.Function - 1], entry.Key.Arity))
            .Select(entry => emittedImports?[entry.Value] ?? entry.Value).ToHashSet();
        byte[]? result = RewriteTerminalStatusCleanup(code, body, pureCleanupImports);
        if (result is not null)
        {
            highestOpcode = Math.Max(highestOpcode, TrimFrame);
        }

        return result;
    }

    private static byte[]? RewriteTerminalStatusCleanup(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body,
        IReadOnlySet<int> pureCleanupImports
    )
    {
        var x0 = new BeamOperand(BeamOperandParameters.X, 0);
        using var output = new MemoryStream();
        int copied = 0;
        int entry = 0;
        BeamProjectionFlow? flow = null;
        for (int end = 0; end < body.Count; end++)
        {
            if (body[end].Definition.Opcode is not (FunctionInfo or EndCode))
            {
                continue;
            }

            int start = entry;
            entry = end + 1;
            if (start + 9 >= end || body[start].Definition.Opcode != Label)
            {
                continue;
            }

            (int[] Owners, Dictionary<int, bool[]> Uses)? ownership = null;
            bool? unprotected = null;
            for (int call = start + 9; call + 3 < end; call++)
            {
                if (body[call] is not
                    {
                        Definition.Opcode: ExternalCall,
                        Operands: [{ Value.IsOne: true }, var function]
                    } || pureCleanupImports.Contains((int)function.Value)
                    || body[call + 1] is not
                    {
                        Definition.Opcode: Move,
                        Operands: [{ Kind: BeamOperandParameters.Y, TypeIndex: null } saved, var returned]
                    } || returned != x0 || body[call + 2] is not
                    {
                        Definition.Opcode: Deallocate,
                        Operands: [var frameWidth]
                    } || frameWidth.Value < 2 || frameWidth.Value > BeamOperandParameters.MaximumRegister
                    || saved.Value >= frameWidth.Value || body[call + 3].Definition.Opcode != Return)
                {
                    continue;
                }

                int firstLine = call;
                while (firstLine > start && body[firstLine - 1].Definition.Opcode == Line)
                {
                    firstLine--;
                }

                int landing = firstLine - 3;
                if (firstLine == call || landing < start + 6
                    || body[landing] is not { Definition.Opcode: Label, Operands: [var target] }
                    || !IsTrimRegisterMove(
                        body[landing + 1],
                        BeamOperandParameters.X,
                        0,
                        BeamOperandParameters.Y,
                        (int)saved.Value
                    )
                    || body[landing + 2] is not
                    {
                        Definition.Opcode: Move,
                        Operands: [{ Kind: BeamOperandParameters.Y, TypeIndex: null } resource, var outgoing]
                    } || outgoing != x0 || resource == saved || resource.Value >= frameWidth.Value
                    || body[landing - 1].Definition.Opcode != Return
                    || body[landing - 2] is not { Definition.Opcode: Deallocate, Operands: [var successWidth] }
                    || successWidth != frameWidth)
                {
                    continue;
                }

                int test = landing - 4;
                if (body[landing - 3] is
                    {
                        Definition.Opcode: PutTuple,
                        Operands: [var tuple, _, { Kind: BeamOperandParameters.Atom }, var payload]
                    } && tuple == x0 && payload == resource && body[test].Definition.Opcode == TestHeap)
                {
                    test--;
                }
                else if (body[landing - 3] is not
                {
                    Definition.Opcode: Move,
                    Operands: [{ Kind: BeamOperandParameters.Y, TypeIndex: null } value, var destination]
                } || destination != x0 || value == saved)
                {
                    continue;
                }

                int argument = test - 2;
                while (argument > start && body[argument].Definition.Opcode == Line)
                {
                    argument--;
                }

                if (argument <= start || body[test - 1] is not
                    {
                        Definition.Opcode: ExternalCall or LocalCall,
                        Operands: [{ Value.Sign: > 0 }, _]
                    } || !IsTrimRegisterMove(
                        body[argument],
                        BeamOperandParameters.Y,
                        (int)resource.Value,
                        BeamOperandParameters.X,
                        0
                    ) || body[test] is not
                    {
                        Definition.Opcode: ExactEqualTest,
                        Operands: [var rejected, var result, { Kind: BeamOperandParameters.Atom }]
                    } || result != x0 || rejected.Value != target.Value
                    || !(unprotected ??= HasUnprotectedCallKillSchedule(body, start)))
                {
                    continue;
                }

                flow ??= new BeamProjectionFlow(body);
                if (!flow.TryGetSingleForwardTarget(test, (int)target.Value, out int owned) || owned != landing
                    || (ownership ??= ReadDisjointFrameOwnership(
                        body,
                        start,
                        end,
                        trackSiblingTrimEpochs: true
                    )) is not { } frames)
                {
                    continue;
                }

                int owner = frames.Owners[landing - start];
                if (owner < 0 || frames.Owners[test - start] != owner || frames.Owners[call - start] != owner
                    || frames.Owners[call + 2 - start] != owner || frames.Uses[owner].Length != frameWidth.Value)
                {
                    continue;
                }

                int top = (int)frameWidth.Value - 1;
                output.Write(code.Span[copied..body[landing + 1].Offset]);
                // The close argument must leave its old slot before the
                // trim. Reuse the highest slot for the returned status.
                if (resource.Value == top)
                {
                    Copy(landing + 1);
                    Copy(landing + 2);
                    WriteMove(
                        YRegisterTag,
                        (int)saved.Value,
                        YRegisterTag,
                        top
                    );
                }
                else
                {
                    WriteMove(
                        XRegisterTag,
                        0,
                        YRegisterTag,
                        top
                    );
                    Copy(landing + 2);
                }

                output.WriteByte(TrimFrame);
                BeamCompactWriter.Write(output, BeamWire.UnsignedTag, top);
                BeamCompactWriter.Write(output, BeamWire.UnsignedTag, 1);
                output.Write(code.Span[body[firstLine].Offset..body[call + 1].Offset]);
                WriteMove(
                    YRegisterTag,
                    0,
                    XRegisterTag,
                    0
                );
                output.WriteByte(Deallocate);
                BeamCompactWriter.Write(output, BeamWire.UnsignedTag, 1);
                copied = body[call + 3].Offset;
                call += 3;
            }
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();

        void Copy(int index) => output.Write(code.Span[body[index].Offset..body[index + 1].Offset]);

        void WriteMove(
            int inputTag,
            int input,
            int outputTag,
            int result
        )
        {
            output.WriteByte(Move);
            BeamCompactWriter.Write(output, inputTag, input);
            BeamCompactWriter.Write(output, outputTag, result);
        }
    }
}
