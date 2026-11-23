using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? TrimRecursiveAppendFrames(
        ReadOnlyMemory<byte> code,
        int labelCount,
        IReadOnlyList<FunctionDefinition> functions
    )
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(PutList)
            || !atoms.TryGetValue(ErlangModuleNames.Module, out int erlang)
            || !atoms.TryGetValue(ErlangOperators.Append, out int append)
            || !imports.TryGetValue((erlang, append, 2), out int import))
        {
            return null;
        }

        var helpers = functions.Where(function => function.IsRecursiveListMapper && atoms.ContainsKey(function.Name))
            .Select(function => (MapEmittedAtom(atoms[function.Name]), function.Arity))
            .ToHashSet();
        if (helpers.Count == 0 || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body })
        {
            return null;
        }

        int appendImport = emittedImports?[import] ?? import;
        int errorImport = atoms.TryGetValue(AutoImportNames.Error, out int errorAtom)
            && imports.TryGetValue((erlang, errorAtom, 1), out int exit)
            ? emittedImports?[exit] ?? exit : -1;
        int badGenerator = atoms.TryGetValue(ComprehensionErrorReasons.BadGenerator, out int reason)
            ? MapEmittedAtom(reason) : -1;
        var x0 = new BeamOperand(BeamOperandParameters.X, 0);
        var x1 = new BeamOperand(BeamOperandParameters.X, 1);
        using var output = new MemoryStream();
        var trimSites = new HashSet<int>();
        BeamProjectionFlow? flow = null;
        int copied = 0;
        int addedLines = 0;
        for (int info = 0; info + 10 < body.Count; info++)
        {
            if (body[info] is not { Definition.Opcode: FunctionInfo, Operands: [_, var name, var arity] }
                || !helpers.Contains(((int)name.Value, (int)arity.Value))
                || arity.Value < 2 || arity.Value >= FirstReservedXRegister
                || body[info + 1] is not { Definition.Opcode: Label, Operands: [var entry] }
                || !HasUnprotectedCallKillSchedule(body, info + 1))
            {
                continue;
            }

            int end = info + 2;
            while (body[end].Definition.Opcode is not (FunctionInfo or EndCode))
            {
                end++;
            }

            if (ReadDisjointFrameOwnership(body, info + 1, end) is not { } owner || owner.Uses.Count != 1)
            {
                continue;
            }

            int argumentCount = (int)arity.Value;
            for (int call = info + 4; call + argumentCount + 6 < end; call++)
            {
                if (body[call] is not
                    { Definition.Opcode: ExternalCall, Operands: [{ Value: var count }, var target] }
                    || count != 2 || target.Value != appendImport
                    || body[call - 2].Definition.Opcode is not (LocalCall or ExternalCall)
                    || !TryReadStackArgument(call - 1, out var head, out var appendArgument) || appendArgument != x1
                    || body[call + 1] is not { Definition.Opcode: Move, Operands: [var result, var spill] }
                    || result != x0 || spill != head)
                {
                    continue;
                }

                int receive = call + 2;
                int recurse = receive + argumentCount;
                var loads = new Dictionary<int, (BeamOperand Slot, BeamOperand Register)>();
                var usedSlots = new HashSet<BeamOperand>();
                for (int index = receive; index < recurse; index++)
                {
                    if (!TryReadStackArgument(index, out var slot, out var argument)
                        || slot == head || argument.Value >= argumentCount || !usedSlots.Add(slot)
                        || !loads.TryAdd((int)argument.Value, (slot, argument)))
                    {
                        break;
                    }
                }

                while (recurse < end && body[recurse].Definition.Opcode == Line)
                {
                    recurse++;
                }

                if (loads.Count != argumentCount || recurse + 4 >= end
                    || body[recurse] is not
                    { Definition.Opcode: LocalCall, Operands: [var recursiveArity, var recursiveEntry] }
                    || recursiveArity.Value != argumentCount || recursiveEntry.Value != entry.Value
                    || body[recurse + 1] is not { Definition.Opcode: TestHeap, Operands: [_, { Value.IsOne: true }] }
                    || body[recurse + 2] is not
                    { Definition.Opcode: PutList, Operands: [var saved, var tail, var outputList] }
                    || saved != head || tail != x0 || outputList != x0
                    || body[recurse + 3] is not { Definition.Opcode: Deallocate, Operands: [var width] }
                    || body[recurse + 4].Definition.Opcode != Return
                    || width.Value < 2 || !owner.Uses.TryGetValue(owner.Owners[call - info - 1], out var frame)
                    || frame.Length != width.Value || owner.Owners[recurse - info - 1] != owner.Owners[call - info - 1])
                {
                    continue;
                }

                int remaining = frame.Length - 1;
                var last = new BeamOperand(BeamOperandParameters.Y, remaining);
                var finalCapture = loads.SingleOrDefault(load => load.Value.Slot == last);
                if (finalCapture.Key == 0)
                {
                    continue;
                }

                flow ??= new BeamProjectionFlow(body);
                if (!flow.IsInitialized(call - 1, head, allowOwnedCollectingWrite: true)
                    || !flow.IsDead(call, head))
                {
                    continue;
                }

                // The append argument remains rooted in X1. Its old stack
                // value is killed when the call returns the new list head.
                output.Write(code.Span[copied..body[call].Offset]);
                WriteFrameInitialization(output, [(int)head.Value]);
                CopyInstruction(call);

                // The recursive call still needs the old highest capture.
                // Copy it first and put the new head in its freed slot.
                var temporary = new BeamOperand(BeamOperandParameters.X, argumentCount);
                WriteMove(loads[0].Slot, temporary);
                WriteMove(last, finalCapture.Value.Register);
                WriteMove(x0, last);
                foreach (var (argument, load) in loads)
                {
                    if (argument != 0 && argument != finalCapture.Key)
                    {
                        WriteMove(load.Slot, load.Register);
                    }
                }

                trimSites.Add(checked((int)output.Position));
                WriteFrameInitialization(output, Enumerable.Range(0, remaining).ToArray());
                WriteMove(temporary, x0);
                if (recurse == receive + argumentCount
                    && ReadRecursiveGeneratorLocation(info, recurse + 5, end) is { } recursiveLocation)
                {
                    output.WriteByte(Line);
                    BeamCompactWriter.Write(output, BeamWire.UnsignedTag, recursiveLocation);
                    addedLines++;
                }

                output.Write(code.Span[body[receive + argumentCount].Offset..body[recurse + 2].Offset]);
                output.WriteByte(PutList);
                WriteRegister(output, last);
                WriteRegister(output, x0);
                WriteRegister(output, x0);
                copied = body[recurse + 3].Offset;
                break;
            }

            info = end - 1;
        }

        if (copied == 0)
        {
            return null;
        }

        highestOpcode = Math.Max(highestOpcode, InitYRegisters);
        output.Write(code.Span[copied..]);

        if (TrimLinearStackFrames(output.ToArray(), labelCount, trimSites) is not { } trimmed)
        {
            return null;
        }

        lineInstructions += addedLines;

        return trimmed;

        int? ReadRecursiveGeneratorLocation(int info, int afterReturn, int end)
        {
            if (info == 0 || errorImport < 0 || badGenerator < 0 || body[info - 1] is not
                { Definition.Opcode: Line, Operands: [var generator] } || generator.Value <= 0)
            {
                return null;
            }

            for (int line = afterReturn + 1; line + 1 < end; line++)
            {
                if (body[line] is { Definition.Opcode: Line, Operands: [var failure] } && failure == generator
                    && body[line - 1] is
                    {
                        Definition.Opcode: PutTuple,
                        Operands: [var tuple, { Value: var arity }, { Kind: BeamOperandParameters.Atom } tag, var input]
                    } && arity == 2 && tuple == x0 && input == x0 && tag.Value == badGenerator
                    && body[line + 1] is
                    {
                        Definition.Opcode: TailExternalCall,
                        Operands: [{ Value.IsOne: true }, var errorTarget]
                    } && errorTarget.Value == errorImport)
                {
                    // v3_core retains the generator annotation on the recursive apply.
                    // The cold failure has the same location, so the existing physical
                    // line deduplicator can remove its later copy.
                    return (int)generator.Value;
                }
            }

            return null;
        }

        bool TryReadStackArgument(int index, out BeamOperand slot, out BeamOperand argument)
        {
            if (body[index] is
                {
                    Definition.Opcode: Move,
                    Operands:
                    [
                    { Kind: BeamOperandParameters.Y, TypeIndex: null } source,
                    { Kind: BeamOperandParameters.X, TypeIndex: null } destination
                    ]
                })
            {
                slot = source;
                argument = destination;

                return true;
            }

            slot = null!;
            argument = null!;

            return false;
        }

        void WriteMove(BeamOperand source, BeamOperand destination)
        {
            output.WriteByte(Move);
            WriteRegister(output, source);
            WriteRegister(output, destination);
        }

        void CopyInstruction(int index) => output.Write(code.Span[body[index].Offset..body[index + 1].Offset]);
    }
}
