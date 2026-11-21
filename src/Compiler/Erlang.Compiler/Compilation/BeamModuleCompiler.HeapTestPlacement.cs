using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? ReserveHeapBeforeStackTupleProjection(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !code.Span.Contains(TestHeap)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { } decoded)
        {
            return null;
        }

        var body = decoded.Body;
        using var output = new MemoryStream();
        int copied = 0;
        for (int index = 1; index + 1 < body.Count; index++)
        {
            if (body[index] is not { Definition.Opcode: TestHeap, Operands: [_, { Value.IsOne: true }] }
                || body[index - 1] is not
                {
                    Definition.Opcode: GetTupleElement,
                    Operands: [
                    { Kind: BeamOperandParameters.Y, TypeIndex: null },
                        _,
                    { Kind: BeamOperandParameters.X, Value.IsZero: true, TypeIndex: null }
                    ]
                })
            {
                continue;
            }

            int projection = body[index - 1].Offset;
            int heap = body[index].Offset;
            int end = body[index + 1].Offset;
            output.Write(code.Span[copied..projection]);
            // The Y slot roots the tuple. The projected value can be defined after the heap check.
            WriteHeapRoots(
                output,
                code,
                heap,
                end,
                1,
                0
            );
            output.Write(code.Span[projection..heap]);
            copied = end;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }

    private static byte[]? MoveHeapTestsBeforeNeutralInstructions(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body,
        string[] atomNames,
        (int Module, int Function, int Arity)[] importsByIndex
    )
    {
        using var output = new MemoryStream();
        BeamProjectionFlow? flow = null;
        int copied = 0;
        for (int index = 1; index + 1 < body.Count; index++)
        {
            BeamInstruction heap = body[index];
            if (heap.Definition.Opcode != TestHeap || heap.Operands[^1].Value > BeamOperandParameters.MaximumRegister)
            {
                continue;
            }

            int original = (int)heap.Operands[^1].Value;
            var checkedProjection = ReadCheckedTupleHeap(
                body,
                index,
                original,
                ref flow
            ) ?? ReadRecordUpdateProjection(
                body,
                index,
                original,
                ref flow
            );
            if (checkedProjection is { } projection)
            {
                int projections = body[projection.First].Offset;
                int guard = body[projection.End].Offset;
                int endOfHeap = body[index + 1].Offset;
                output.Write(code.Span[copied..projections]);
                output.Write(code.Span[guard..heap.Offset]);
                WriteHeapRoots(
                    output,
                    code,
                    heap.Offset,
                    endOfHeap,
                    original,
                    projection.Live
                );
                output.Write(code.Span[projections..guard]);
                copied = endOfHeap;
                continue;
            }

            int live = original;
            int first = index;
            bool passedReceiveRemoval = false;
            bool passedListProjection = false;
            bool passedInPlaceProjection = false;
            while (first > 0)
            {
                int? source = null;
                int destination;
                switch (body[first - 1])
                {
                    case { Definition.Opcode: RemoveMessage }:
                        // Removing the message keeps the existing X register roots.
                        passedReceiveRemoval = true;
                        first--;
                        continue;
                    case
                    {
                        Definition.Opcode: GetTupleElement,
                        Operands: [
                        { Kind: BeamOperandParameters.X, TypeIndex: null } tuple,
                            _,
                        { Kind: BeamOperandParameters.X, TypeIndex: null } field
                        ]
                    } when tuple.Value < live:
                        source = (int)tuple.Value;
                        destination = (int)field.Value;
                        // The live register can root the tuple in place of its field.
                        passedInPlaceProjection |= first == index && source == destination;
                        break;
                    case
                    {
                        Definition.Opcode: GetHead or GetTail,
                        Operands: [{ Kind: BeamOperandParameters.X, TypeIndex: null } list, var projected]
                    } when list == projected && list.Value < live && first >= 2
                        && body[first - 2] is
                        {
                            Definition.Opcode: IsNonemptyList,
                            Operands: [_, var tested]
                        } && tested == list:
                        passedListProjection = true;
                        first--;
                        continue;
                    case
                    {
                        Definition.Opcode: BifZero,
                        Operands: [var import, { Kind: BeamOperandParameters.X, TypeIndex: null } result]
                    } when IsHeapNeutralNullaryBif(importsByIndex[(int)import.Value], atomNames):
                        destination = (int)result.Value;
                        break;
                    default:
                        destination = -1;
                        break;
                }

                if (destination < 0)
                {
                    break;
                }

                if (destination == live - 1 && source != destination)
                {
                    live--;
                }
                else if (destination < live && source != destination)
                {
                    // Dropping an interior register would leave an unproven slot in the GC root prefix.
                    break;
                }

                first--;
            }

            if (live == original && !passedReceiveRemoval && !passedListProjection && !passedInPlaceProjection)
            {
                continue;
            }

            int start = body[first].Offset;
            int end = body[index + 1].Offset;
            output.Write(code.Span[copied..start]);
            WriteHeapRoots(
                output,
                code,
                heap.Offset,
                end,
                original,
                live
            );
            output.Write(code.Span[start..heap.Offset]);
            copied = end;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }

    private static void WriteHeapRoots(
        Stream output,
        ReadOnlyMemory<byte> code,
        int start,
        int end,
        int previous,
        int live
    )
    {
        int width = CompactRegisterCountWidth(previous);
        output.Write(code.Span[start..(end - width)]);
        BeamCompactWriter.Write(output, BeamWire.UnsignedTag, live);
    }

    private static bool IsHeapNeutralNullaryBif(
        (int Module, int Function, int Arity) import,
        string[] atomNames
    ) => import.Arity == 0 && atomNames[import.Module - 1] == ErlangModuleNames.Module
        && atomNames[import.Function - 1] is AutoImportNames.Self or AutoImportNames.Node;
}
