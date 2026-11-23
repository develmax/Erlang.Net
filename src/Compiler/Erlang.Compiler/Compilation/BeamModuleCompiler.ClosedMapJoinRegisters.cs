using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    // These block roles describe a two-field map decision with a common
    // constructor join, an optional output field, and an iterator fallback.
    private enum MapJoinStep
    {
        EntryCopy,
        CheckMap,
        ReadPair,
        FirstTest,
        FirstHeap,
        FirstTuple,
        FirstJump,
        SecondArm,
        SecondTest,
        SecondHeap,
        SecondTuple,
        SecondJump,
        ThirdArm,
        ThirdMap,
        ThirdHeap,
        ThirdTuple,
        Joined,
        JoinCopy,
        ReadEncoding,
        CheckEncoding,
        ProjectEncoding,
        ListGuard,
        ListJump,
        BinaryArm,
        BinaryGuard,
        Encoded,
        BaseOutput,
        OptionalField,
        IsAbsent,
        BaseReturnCopy,
        BaseReturn,
        AddedArm,
        AddedOutput,
        AddedReturn,
        Fallback,
        IteratorArgument
    }

    private static readonly byte[] MapJoinOpcodes =
    [
        Move, IsMap, GetMapElements, ExactEqualTest, TestHeap, PutTuple, Jump,
        Label, ExactEqualTest, TestHeap, PutTuple, Jump, Label, PutMapAssoc, TestHeap, PutTuple,
        Label, Move, GetMapElements, IsTaggedTuple, GetTupleElement, IsList, Jump, Label, IsBinary,
        Label, PutMapAssoc, GetMapElements, ExactEqualTest, Move, Return, Label, PutMapAssoc, Return, Label, Move
    ];

    private byte[]? AllocateClosedMapJoinRegisters(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || mapIteratorHelpers.Count == 0
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body })
        {
            return null;
        }

        using var output = new MemoryStream();
        int copied = 0;
        for (int entry = 1; entry + MapJoinOpcodes.Length < body.Count; entry++)
        {
            if (body[entry - 1].Definition.Opcode != Label || entry < 2
                || body[entry - 2] is not { Definition.Opcode: FunctionInfo, Operands: [_, var name, { Value.IsOne: true }] }
                || !atoms.Any(pair => MapEmittedAtom(pair.Value) == (int)name.Value
                    && mapIteratorHelpers.ContainsKey((MappingName(pair.Key, 1), MapIteratorHelperArity)))
                || !ReadClosedMapJoin(body, entry, out var erased))
            {
                continue;
            }

            int end = erased[^1] + 1;
            output.Write(code.Span[copied..body[entry].Offset]);
            for (int index = entry; index < end; index++)
            {
                if (erased.Contains(index))
                {
                    continue;
                }

                if (index > entry + (int)MapJoinStep.IteratorArgument)
                {
                    output.Write(code.Span[body[index].Offset..body[index + 1].Offset]);
                    continue;
                }

                MapJoinStep step = (MapJoinStep)(index - entry);
                BeamInstruction instruction = body[index];
                output.WriteByte(instruction.Definition.Opcode);
                for (int position = 0; position < instruction.Operands.Count; position++)
                {
                    BeamOperand operand = instruction.Operands[position];
                    if (operand.Kind == BeamOperandParameters.X)
                    {
                        operand = operand with { Value = RegisterAfterJoinReallocation(step, position, (int)operand.Value) };
                    }
                    else if ((step is MapJoinStep.FirstHeap or MapJoinStep.SecondHeap) && position == 1)
                    {
                        MapJoinStep tuple = step == MapJoinStep.FirstHeap ? MapJoinStep.FirstTuple : MapJoinStep.SecondTuple;
                        BeamOperand value = body[entry + (int)tuple].Operands[^1];
                        int field = RegisterAfterJoinReallocation(tuple, 3, (int)value.Value);
                        operand = operand with { Value = field + 1 };
                    }

                    WriteMapJoinOperand(output, operand);
                }
            }

            copied = body[end].Offset;
            entry = end - 1;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }

    private static int RegisterAfterJoinReallocation(MapJoinStep step, int position, int register)
    {
        // Keep the formal argument in ABI X0. X1 owns the merged value and X2
        // is available for the second input and later checked data projection.
        if ((step is MapJoinStep.FirstTuple or MapJoinStep.SecondTuple or MapJoinStep.ThirdTuple) && position == 0
            || step == MapJoinStep.ThirdMap && position == 2
            || step == MapJoinStep.ThirdTuple && position == 3)
        {
            return 1;
        }

        if (step == MapJoinStep.BaseReturnCopy && position == 1 || step == MapJoinStep.AddedOutput && position == 2)
        {
            return 0;
        }

        return register switch
        {
            0 => 2,
            1 => 0,
            2 => 1,
            _ => register
        };
    }

    private static bool ReadClosedMapJoin(IReadOnlyList<BeamInstruction> body, int entry, out int[] erased)
    {
        erased = [];
        for (int i = 0; i < MapJoinOpcodes.Length; i++)
        {
            if (body[entry + i].Definition.Opcode != MapJoinOpcodes[i]
                || body[entry + i].Operands.Any(operand => operand.Kind is not (
                    BeamOperandParameters.X or BeamOperandParameters.Label or BeamOperandParameters.Failure
                    or BeamOperandParameters.Unsigned or BeamOperandParameters.Count
                    or BeamOperandParameters.Atom or BeamOperandParameters.Integer or BeamOperandParameters.LiteralValue
                )))
            {
                return false;
            }
        }

        var map = new BeamOperand(BeamOperandParameters.X, 1);
        var scratch = new BeamOperand(BeamOperandParameters.X, 0);
        var shared = new BeamOperand(BeamOperandParameters.X, 2);
        if (At(MapJoinStep.EntryCopy) is not [var input, var carrier] || input != scratch || carrier != map
            || At(MapJoinStep.CheckMap) is not [var outer, var checkedMap] || checkedMap != map
            || At(MapJoinStep.ReadPair) is not [var missing, var mapRead, { Value: var pairSize }, var firstKey, var first,
                var secondKey, var second] || pairSize != 4 || firstKey == secondKey || first != scratch || second != shared
            || missing != outer || !SameProjectionRegister(mapRead, map)
            || !ReadDecisionArm(
                MapJoinStep.FirstTest,
                MapJoinStep.FirstHeap,
                MapJoinStep.FirstTuple,
                MapJoinStep.FirstJump,
                MapJoinStep.SecondArm,
                scratch,
                shared
            )
            || !ReadDecisionArm(
                MapJoinStep.SecondTest,
                MapJoinStep.SecondHeap,
                MapJoinStep.SecondTuple,
                MapJoinStep.SecondJump,
                MapJoinStep.ThirdArm,
                shared,
                scratch
            )
            || At(MapJoinStep.ThirdMap) is not [{ Kind: BeamOperandParameters.Failure }, { Kind: BeamOperandParameters.LiteralValue }, var builtPair,
            { Value: var pairRoots }, { Value: var fieldCount }, _, var pairFirst, _, var pairSecond]
            || builtPair != scratch || pairRoots != 3 || fieldCount != 4
            || !(pairFirst == scratch && pairSecond == shared || pairFirst == shared && pairSecond == scratch)
            || At(MapJoinStep.ThirdHeap) is not [{ Value: var thirdWords }, { Value: var thirdRoots }]
            || thirdWords != 3 || thirdRoots != 2 || !Tuple(MapJoinStep.ThirdTuple, scratch)
            || At(MapJoinStep.JoinCopy) is not [var tupleResult, var newOwner] || tupleResult != scratch || newOwner != shared
            || !Projection(
                MapJoinStep.ReadEncoding,
                Target(MapJoinStep.Fallback),
                scratch,
                out _
            )
            || At(MapJoinStep.CheckEncoding) is not [var failedTag, var encoding, { Value: var arity },
            { Kind: BeamOperandParameters.Atom }] || failedTag.Value != Target(MapJoinStep.Fallback)
            || encoding != scratch || arity != 2
            || At(MapJoinStep.ProjectEncoding) is not [var encoded, { Value.IsOne: true }, var projected]
            || encoded != scratch || projected != scratch
            || At(MapJoinStep.ListGuard) is not [var listFailure, var list] || listFailure.Value != Target(MapJoinStep.BinaryArm) || list != scratch
            || At(MapJoinStep.ListJump)[0].Value != Target(MapJoinStep.Encoded)
            || At(MapJoinStep.BinaryGuard) is not [var binaryFailure, var binary]
            || binaryFailure.Value != Target(MapJoinStep.Fallback) || binary != scratch
            || !Projection(
                MapJoinStep.OptionalField,
                At(MapJoinStep.OptionalField)[0].Value,
                new(BeamOperandParameters.X, 4),
                out var optionKey
            )
            || At(MapJoinStep.BaseOutput) is not [var createFailure, var literal, { Kind: BeamOperandParameters.X, Value: var temporary },
            { Value: var baseRoots }, { Value: var baseSize }, var keyA, var valueA, var keyB, var valueB]
            || createFailure.Kind != BeamOperandParameters.Failure || literal.Kind != BeamOperandParameters.LiteralValue
            || temporary != 3 || baseRoots != 3 || baseSize != 4 || valueA != scratch || valueB != shared
            || At(MapJoinStep.IsAbsent) is not [var update, { Kind: BeamOperandParameters.X, Value: var option },
            { Kind: BeamOperandParameters.Atom }] || option != 4 || update.Value != Target(MapJoinStep.AddedArm)
            || At(MapJoinStep.BaseReturnCopy) is not [{ Kind: BeamOperandParameters.X, Value: var baseSource }, var returnDestination]
            || baseSource != 3 || returnDestination != scratch
            || At(MapJoinStep.AddedOutput) is not [var updateFailure, var newLiteral, var result,
            { Value: var updateRoots }, { Value: var addedSize }, var newKeyA, var newValueA, var newKeyB, var newValueB,
                var extraKey, { Kind: BeamOperandParameters.X, Value: var extraValue }]
            || updateFailure != createFailure || newLiteral != literal || result != scratch || updateRoots != 5 || addedSize != 6
            || newKeyA != keyA || newValueA != valueA || newKeyB != keyB || newValueB != valueB
            || extraKey != optionKey || extraValue != 4
            || At(MapJoinStep.IteratorArgument) is not [var savedMap, var iteratorInput] || savedMap != map || iteratorInput != scratch)
        {
            return false;
        }

        var flow = new BeamProjectionFlow(body);
        int argument = entry + (int)MapJoinStep.IteratorArgument;
        int coldLimit = body.Select((instruction, index) => (instruction, index))
            .Skip(argument + 1)
            .FirstOrDefault(row => row.instruction.Definition.Opcode is FunctionInfo or EndCode).index;
        var remove = new List<int> { entry, entry + (int)MapJoinStep.JoinCopy, argument };
        if (!ReadMapError(
            outer,
            entry + (int)MapJoinStep.ReadPair,
            coldLimit,
            out int outerCopy
        )
            || !ReadMapError(
                At(MapJoinStep.OptionalField)[0],
                entry + (int)MapJoinStep.OptionalField,
                coldLimit,
                out int innerCopy
            )
            || innerCopy >= outerCopy)
        {
            return false;
        }

        var transfers = body.SelectMany((instruction, position) => instruction.Operands
                .Where(operand => operand.Kind == BeamOperandParameters.Label)
                .Select(operand => (Label: (int)operand.Value, Position: position)))
            .ToLookup(pair => pair.Label, pair => pair.Position);
        if (!References(Target(MapJoinStep.SecondArm), MapJoinStep.FirstTest)
            || !References(Target(MapJoinStep.ThirdArm), MapJoinStep.SecondTest)
            || !References(Target(MapJoinStep.Joined), MapJoinStep.FirstJump, MapJoinStep.SecondJump)
            || !References(Target(MapJoinStep.BinaryArm), MapJoinStep.ListGuard)
            || !References(Target(MapJoinStep.Encoded), MapJoinStep.ListJump)
            || !References(Target(MapJoinStep.AddedArm), MapJoinStep.IsAbsent)
            || !References(
                Target(MapJoinStep.Fallback),
                MapJoinStep.ReadEncoding,
                MapJoinStep.CheckEncoding,
                MapJoinStep.BinaryGuard
            )
            || !References(outer.Value, MapJoinStep.CheckMap, MapJoinStep.ReadPair)
            || !References(At(MapJoinStep.OptionalField)[0].Value, MapJoinStep.OptionalField))
        {
            return false;
        }

        for (int index = argument + 1; index <= outerCopy + 2; index++)
        {
            if (index == innerCopy || index == outerCopy)
            {
                continue;
            }

            BeamInstruction instruction = body[index];
            if (instruction.Operands.Any(operand => operand.Kind == BeamOperandParameters.X && !operand.Value.IsZero)
                || instruction.Definition.Opcode is (LocalCall or ExternalCall or TailExternalCallWithFrame)
                    && !instruction.Operands[0].Value.IsOne
                || instruction.Definition.Opcode is not (IsNil or Label or Line or Move or Allocate
                    or TestHeap or PutTuple or TrimFrame or LocalCall or ExternalCall or TailExternalCallWithFrame or CaseEnd))
            {
                return false;
            }
        }

        remove.Add(innerCopy);
        remove.Add(outerCopy);
        erased = [.. remove];

        return true;

        IReadOnlyList<BeamOperand> At(MapJoinStep step) => body[entry + (int)step].Operands;

        System.Numerics.BigInteger Target(MapJoinStep step) => At(step)[0].Value;

        bool References(System.Numerics.BigInteger label, params MapJoinStep[] steps) =>
            transfers[(int)label].SequenceEqual(steps.Select(step => entry + (int)step));

        bool Tuple(MapJoinStep step, BeamOperand term) =>
            At(step) is [var destination, { Value: var size }, { Kind: BeamOperandParameters.Atom }, var value]
            && destination == scratch && size == 2 && value == term;

        bool ReadDecisionArm(
            MapJoinStep test,
            MapJoinStep heap,
            MapJoinStep tuple,
            MapJoinStep jump,
            MapJoinStep next,
            BeamOperand tested,
            BeamOperand value
        ) =>
            At(test) is [var failed, var source, { Kind: BeamOperandParameters.Atom }]
            && failed.Value == Target(next) && source == tested
            && At(heap) is [{ Value: var words }, { Value: var live }] && words == 3 && live == System.Numerics.BigInteger.Max(value.Value, map.Value) + 1
            && Tuple(tuple, value) && At(jump)[0].Value == Target(MapJoinStep.Joined);

        bool Projection(
            MapJoinStep step,
            System.Numerics.BigInteger failure,
            BeamOperand destination,
            out BeamOperand key
        )
        {
            key = scratch;
            if (At(step) is not [var missing, var source, { Value: var pairCount },
                { Kind: BeamOperandParameters.Atom } found, var output] || missing.Value != failure
                || !SameProjectionRegister(source, map) || pairCount != 2 || output != destination)
            {
                return false;
            }

            key = found;

            return true;
        }

        bool ReadMapError(
            BeamOperand failure,
            int from,
            int limit,
            out int copy
        )
        {
            copy = -1;
            if (limit <= argument || !flow.TryGetForwardLabel(from, (int)failure.Value, out int label) || label <= argument
                || label + 3 >= limit || body[label + 1] is not { Definition.Opcode: Move, Operands: [var source, var destination] }
                || source != map || destination != scratch || body[label + 2].Definition.Opcode != Line
                || body[label + 3] is not { Definition.Opcode: CaseEnd, Operands: [var error] } || error != scratch)
            {
                return false;
            }

            copy = label + 1;

            return true;
        }
    }

    private static void WriteMapJoinOperand(Stream output, BeamOperand operand)
    {
        if (operand.Kind == BeamOperandParameters.X)
        {
            if (operand.TypeIndex is not null)
            {
                output.WriteByte(ExtendedTypedRegisterOperand);
            }

            WriteRegister(output, operand);
            if (operand.TypeIndex is int type)
            {
                BeamCompactWriter.Write(output, BeamWire.UnsignedTag, type);
            }

            return;
        }

        if (operand.Kind is BeamOperandParameters.Count or BeamOperandParameters.LiteralValue)
        {
            output.WriteByte(operand.Kind == BeamOperandParameters.Count ? ExtendedListOperand : ExtendedLiteralOperand);
        }

        int tag = operand.Kind switch
        {
            BeamOperandParameters.Atom => AtomTag,
            BeamOperandParameters.Label or BeamOperandParameters.Failure => LabelTag,
            BeamOperandParameters.Integer => BeamWire.IntegerTag,
            _ => BeamWire.UnsignedTag
        };
        BeamCompactWriter.Write(output, tag, operand.Value);
    }
}
