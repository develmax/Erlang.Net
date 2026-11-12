using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private readonly record struct CodeEdit(
        int Start,
        int End,
        int Tag,
        int Value,
        bool IsJump = false
    );

    private int[]? emittedLabels;
    private int sharedReturnLabelCount;

    private int MapEmittedLabel(int label) => emittedLabels?[label] ?? label;

    private byte[]? ShareReturnBlocks(ReadOnlyMemory<byte> code)
    {
        var labelOperands = new List<(int Start, int End, int Label)>();
        IReadOnlyList<BeamInstruction> body;
        try
        {
            var header = new BeamCodeHeader(
                BeamWire.FormatVersion,
                highestOpcode,
                nextLabel,
                exports.Count + locals.Count,
                code
            );
            body = BeamInstructionReader.Read(
                header,
                atoms.Count + 1,
                literalBytes.Count,
                (start, end, label) => labelOperands.Add((start, end, label))
            );
        }
        catch (Exception error) when (error is InvalidDataException or NotSupportedException)
        {
            return null;
        }

        var redirects = new Dictionary<int, int>();
        var edits = new List<CodeEdit>();
        int functionEnd = body.Count;
        for (int index = body.Count - 1; index >= 0; index--)
        {
            if (body[index].Definition.Opcode != FunctionInfo)
            {
                continue;
            }

            CollectSharedReturns(
                body,
                index + 1,
                functionEnd,
                redirects,
                edits
            );
            functionEnd = index;
        }

        if (redirects.Count == 0)
        {
            return null;
        }

        sharedReturnLabelCount = redirects.Count;
        emittedLabels = new int[nextLabel];
        int next = 1;
        for (int label = 1; label < nextLabel; label++)
        {
            if (!redirects.ContainsKey(label))
            {
                emittedLabels[label] = next++;
            }
        }
        foreach (var (label, target) in redirects)
        {
            emittedLabels[label] = emittedLabels[target];
        }

        foreach (var (start, end, label) in labelOperands)
        {
            int target = MapEmittedLabel(label);
            if (target != label)
            {
                edits.Add(new(start, end, LabelTag, target));
            }
        }
        for (int index = 0; index < body.Count; index++)
        {
            if (body[index] is not { Definition.Opcode: Label, Operands: [var operand] })
            {
                continue;
            }

            int label = (int)operand.Value;
            int mapped = MapEmittedLabel(label);
            if (mapped != label && !redirects.ContainsKey(label))
            {
                edits.Add(new(body[index].Offset + 1, body[index + 1].Offset, BeamWire.UnsignedTag, mapped));
            }
        }

        using var output = new MemoryStream(code.Length);
        int copied = 0;
        foreach (var edit in edits.OrderBy(edit => edit.Start))
        {
            output.Write(code.Span[copied..edit.Start]);
            if (edit.IsJump)
            {
                output.WriteByte(Jump);
                BeamCompactWriter.Write(output, LabelTag, MapEmittedLabel(edit.Value));
            }
            else if (edit.Value != 0)
            {
                BeamCompactWriter.Write(output, edit.Tag, edit.Value);
            }
            copied = edit.End;
        }
        output.Write(code.Span[copied..]);

        return output.ToArray();
    }

    private static void CollectSharedReturns(
        IReadOnlyList<BeamInstruction> body,
        int start,
        int end,
        Dictionary<int, int> redirects,
        List<CodeEdit> edits
    )
    {
        var unprotected = UnprotectedReturnLabels(body, start, end);

        var returns = new Dictionary<(BeamOperand? Source, BeamOperand Slots), int>();
        // Prefer the later block, as OTP's reverse traversal in beam_jump:share_1 does.
        for (int index = end - 3; index > start; index--)
        {
            if (body[index].Definition.Opcode != Label || unprotected is not null && !unprotected.Contains(index))
            {
                continue;
            }

            int first = index + 1;
            BeamOperand? source = null;
            if (body[first] is { Definition.Opcode: Move, Operands: [var value, var destination] }
                && destination is { Kind: BeamOperandParameters.X, TypeIndex: null } && destination.Value.IsZero)
            {
                source = value;
                first++;
            }

            if (first + 1 >= end
                || body[first] is not { Definition.Opcode: Deallocate, Operands: [var slots] }
                || body[first + 1].Definition.Opcode != Return)
            {
                continue;
            }

            int label = (int)body[index].Operands[0].Value;
            if (returns.TryGetValue((source, slots), out int target))
            {
                redirects.Add(label, target);
                // Existing branches are redirected by the label map. A sequential predecessor needs a jump.
                bool fallsThrough = !EndsBeamBlock(body[index - 1].Definition.Opcode);
                edits.Add(new(
                    body[index].Offset,
                    body[first + 2].Offset,
                    LabelTag,
                    fallsThrough ? target : 0,
                    fallsThrough
                ));
            }
            else
            {
                returns.Add((source, slots), label);
            }
        }
    }

    private static bool EndsBeamBlock(int opcode) =>
        opcode is Return or Jump or SelectValue or SelectTupleArity or TailLocalCall or TailLocalCallWithFrame
            or TailExternalCall or TailExternalCallWithFrame or TailDynamicApply or CaseEnd or BadMatch
            or BadRecord or TryCaseEnd or Raise or LoopReceiveEnd or Wait;
}
