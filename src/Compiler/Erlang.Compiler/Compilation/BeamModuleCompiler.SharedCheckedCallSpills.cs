using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? ShareCheckedCallResultSpills(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(IsTaggedTuple)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body })
        {
            return null;
        }

        var sites = new SortedDictionary<int, (int End, int? MoveFrom)>();
        BeamProjectionFlow? flow = null;
        for (int info = 0; info + 4 < body.Count; info++)
        {
            if (body[info].Definition.Opcode != FunctionInfo)
            {
                continue;
            }

            int entry = info + 1;
            int end = entry;
            while (body[end].Definition.Opcode is not (FunctionInfo or EndCode))
            {
                end++;
            }

            info = end - 1;
            if (!Enumerable.Range(entry, end - entry).Any(index => body[index].Definition.Opcode == IsTaggedTuple)
                || Enumerable.Range(entry, end - entry).Any(index => body[index].Definition.Opcode
                    is CatchEnter or TryEnter or LoopReceive or LoopReceiveEnd or Wait or WaitTimeout
                    or DebugLine or ExecutableLine))
            {
                continue;
            }

            (int[] Owners, Dictionary<int, bool[]> Uses)? frames = null;
            for (int test = entry + 1; test + 3 < end; test++)
            {
                if (body[test - 1].Definition.Opcode is not (LocalCall or ExternalCall)
                    || body[test] is not
                    {
                        Definition.Opcode: IsTaggedTuple,
                        Operands: [{ Kind: BeamOperandParameters.Label } failure, var result, _, _]
                    } || !IsBareX0(result)
                    || body[test + 1] is not
                    {
                        Definition.Opcode: Move,
                        Operands: [var source, { Kind: BeamOperandParameters.Y, TypeIndex: null } saved]
                    } || source != result
                    || body[test + 2] is not { Definition.Opcode: GetTupleElement, Operands: [var tuple, _, _] }
                    || tuple != result)
                {
                    continue;
                }

                int failedLabel = (int)failure.Value;
                flow ??= new BeamProjectionFlow(body);
                if (!flow.TryGetSingleForwardTarget(test, failedLabel, out int failed)
                    || failed <= test + 2 || failed + 2 >= end
                    || body[failed + 1] is not { Definition.Opcode: Move, Operands: [var coldSource, var coldSaved] }
                    || coldSource != source || coldSaved != saved
                    || exports.Any(item => MapEmittedLabel(item.Entry) == failedLabel)
                    || locals.Any(item => MapEmittedLabel(item.Entry) == failedLabel)
                    || closures.Any(item => MapClosureEntryLabel(item.Entry) == failedLabel)
                    || (frames ??= ReadDisjointFrameOwnership(body, entry, end)) is not { } frame
                    || frame.Owners[test - entry] < 0
                    || frame.Owners[test - entry] != frame.Owners[failed + 1 - entry])
                {
                    continue;
                }

                // Both paths save X0 before using the saved slot. The check reads
                // only X0, and the sole failure edge can reuse the common definition.
                sites.Add(test, (test + 2, test + 1));
                sites.Add(failed + 1, (failed + 2, null));
            }
        }

        if (sites.Count == 0)
        {
            return null;
        }

        using var output = new MemoryStream(code.Length);
        int copied = 0;
        foreach (var (start, (end, moveFrom)) in sites)
        {
            output.Write(code.Span[copied..body[start].Offset]);
            if (moveFrom is int from)
            {
                output.Write(code.Span[body[from].Offset..body[end].Offset]);
                output.Write(code.Span[body[start].Offset..body[from].Offset]);
            }

            copied = body[end].Offset;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }
}
