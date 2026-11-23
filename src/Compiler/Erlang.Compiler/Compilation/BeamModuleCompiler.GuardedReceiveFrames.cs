using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private static readonly byte[] GuardedReceiveFrameShape =
    [
        Allocate, InitYRegisters, Move, Move, Move, Move, IsNonemptyList, GetList, IsLess, Move, Line,
        LocalCall, Move, TrimFrame, Line, GcBifTwo, Move, Move, Move, Move, TailLocalCallWithFrame,
        Label, Label, LoopReceive, Move, ExactEqualTest, RemoveMessage, Move, Move, Move, Move, TailLocalCallWithFrame,
        Label, IsPid, IsLess, RemoveMessage, Move, Line, GcBifTwo, Move, Move, Move, Move, TailLocalCallWithFrame,
        Label, RemoveMessage, Move, TestHeap, PutTuple, Line, ExternalCall, Move, Move, Move, Move, TailLocalCallWithFrame,
        Label, Wait
    ];

    private byte[]? LocalizeGuardedReceiveFrames(
        ReadOnlyMemory<byte> code,
        int labelCount,
        IReadOnlyList<FunctionDefinition> functions,
        IReadOnlyDictionary<(string Name, int Arity), int> localEntries
    )
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(LoopReceive))
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

        var names = atoms.OrderBy(item => item.Value).Select(item => item.Key).ToArray();
        var importsByIndex = imports.OrderBy(item => item.Value).Select(item => item.Key).ToArray();
        var definitions = functions.ToDictionary(function => (function.Name, function.Arity));
        ILookup<int, int>? references = null;
        using var output = new MemoryStream();
        int copied = 0;
        int entry = 0;
        FunctionDefinition? function = null;
        for (int start = 0; start + GuardedReceiveFrameShape.Length < body.Count; start++)
        {
            if (body[start].Definition.Opcode == FunctionInfo)
            {
                var info = body[start].Operands;
                definitions.TryGetValue((names[(int)info[1].Value - 1], (int)info[2].Value), out function);
                entry = (int)body[start + 1].Operands[0].Value;
                continue;
            }

            if (function is not { Arity: 4 } || body[start].Definition.Opcode != Allocate
                || body[start].Operands[0].Value != 6)
            {
                continue;
            }

            BeamInstruction[] shape = body.Skip(start).Take(GuardedReceiveFrameShape.Length).ToArray();
            if (!shape.Select(instruction => instruction.Definition.Opcode).SequenceEqual(GuardedReceiveFrameShape))
            {
                continue;
            }

            IReadOnlyList<BeamOperand> Operands(int offset) => shape[offset].Operands;
            BeamOperand receive = L((int)Operands(21)[0].Value);
            BeamOperand scan = L((int)Operands(22)[0].Value);
            BeamOperand wait = L((int)Operands(56)[0].Value);
            BeamOperand notNotification = L((int)Operands(32)[0].Value);
            BeamOperand notAcknowledgement = L((int)Operands(44)[0].Value);
            if (!Operands(0).SequenceEqual([U(6), U(4)])
                || !Operands(1).SequenceEqual([new BeamOperand(BeamOperandParameters.Count, 2), Y(4), Y(5)])
                || !Enumerable.Range(0, 4).All(index => Pair(2 + index, X(index), Y(index)))
                || !Operands(6).SequenceEqual([receive, Y(3)])
                || !Operands(7).SequenceEqual([Y(3), Y(4), Y(5)])
                || !Operands(8).SequenceEqual([receive, Y(2), Y(1)])
                || !Pair(9, Y(4), X(0))
                || Operands(11) is not [var arity, var local] || !arity.Value.IsOne
                || !Pair(12, Y(0), Y(4))
                || !Pair(13, U(1), U(5))
                || !Bif(
                    15,
                    ErlangOperators.Plus,
                    1,
                    Y(1),
                    X(0),
                    X(0)
                )
                || !Pair(16, X(0), X(2)) || !Pair(17, Y(3), X(0)) || !Pair(18, Y(0), X(1)) || !Pair(19, Y(4), X(3))
                || !Tail(20, 5)
                || !Pair(23, wait, X(0)) || !Pair(24, X(0), X(1))
                || Operands(25) is not [var notifyFailure, var message, { Kind: BeamOperandParameters.Atom } notify]
                || notifyFailure != notNotification || message != X(1)
                || !Forwarded(27, [2, 1, 3, 0]) || !Tail(31, 6)
                || !Pair(33, notAcknowledgement, X(1))
                || !Operands(34).SequenceEqual([notAcknowledgement, I(0), Y(2)])
                || !Pair(36, X(1), Y(4))
                || !Bif(
                    38,
                    ErlangOperators.Minus,
                    0,
                    Y(2),
                    I(1),
                    X(0)
                )
                || !Pair(39, X(0), X(2)) || !Pair(40, Y(0), X(0)) || !Pair(41, Y(1), X(1)) || !Pair(42, Y(3), X(3))
                || !Tail(43, 6)
                || !Pair(46, X(1), Y(4)) || Operands(47)[1] != U(0)
                || Operands(48) is not [var tuple, { Kind: BeamOperandParameters.Count } width, .., var payload]
                || tuple != X(0) || payload != Y(4)
                || Operands(48).Skip(2).SkipLast(1).Any(operand => operand.Kind is BeamOperandParameters.X or BeamOperandParameters.Y)
                || !Operands(50)[0].Value.IsOne || !Forwarded(51, [2, 1, 3, 0]) || !Tail(55, 6)
                || Operands(57) is not [var back] || back != scan)
            {
                continue;
            }

            references ??= body.SelectMany((instruction, index) => instruction.Operands
                    .Where(operand => operand.Kind == BeamOperandParameters.Label)
                    .Select(operand => (label: (int)operand.Value, index)))
                .ToLookup(item => item.label, item => item.index);
            if (!OnlyTargets(receive, 6, 8) || !OnlyTargets(scan, 57) || !OnlyTargets(wait, 23)
                || !OnlyTargets(notNotification, 25) || !OnlyTargets(notAcknowledgement, 33, 34)
                || !ReadClosedIntegerRecurrence(function, 2, definitions)
                || ReadLocalRange((int)local.Value, definitions, localEntries) is not { } range)
            {
                continue;
            }

            int integer = GetOperandTypeIndex((IntegerTypeBits, null, null, null));
            int positive = GetOperandTypeIndex((IntegerTypeBits, 1, null, null));
            int increment = GetOperandTypeIndex((IntegerTypeBits, range.Lower, range.Upper, null));
            output.Write(code.Span[copied..shape[0].Offset]);
            Op(IsNonemptyList, receive, X(3));
            Op(
                GetList,
                X(3),
                X(4),
                X(5)
            );
            Op(
                IsLess,
                receive,
                X(2) with { TypeIndex = integer },
                X(1)
            );
            Op(Allocate, U(4), U(6));
            Op(Move, X(5), Y(0));
            Op(Move, X(2), Y(1));
            Op(Move, X(1), Y(2));
            Op(Move, X(0), Y(3));
            Op(Move, X(4), X(0));
            Copy(10);
            Copy(11);
            Copy(14);
            Op(
                GcBifTwo,
                Operands(15)[0],
                U(1),
                Operands(15)[2],
                Y(1) with { TypeIndex = integer },
                X(0) with { TypeIndex = increment },
                X(2)
            );
            EmitRetainedArguments(skipCounter: true);
            EmitTail();

            Copy(21);
            Op(Allocate, U(4), U(4));
            for (int slot = 0; slot < 4; slot++)
            {
                Op(Move, X(3 - slot), Y(slot));
            }
            Copy(22);
            Copy(23);
            Op(
                ExactEqualTest,
                notNotification,
                X(0),
                notify
            );
            Copy(26);
            EmitRetainedArguments(skipCounter: false);
            EmitTail();

            Copy(32);
            Op(IsPid, notAcknowledgement, X(0));
            Op(
                IsGreaterOrEqual,
                notAcknowledgement,
                Y(1) with { TypeIndex = integer },
                I(1)
            );
            Copy(35);
            Copy(37);
            Op(
                GcBifTwo,
                Operands(38)[0],
                U(0),
                Operands(38)[2],
                Y(1) with { TypeIndex = positive },
                I(1),
                X(2)
            );
            EmitRetainedArguments(skipCounter: true);
            EmitTail();

            Copy(44);
            Op(TestHeap, Operands(47)[0], U(1));
            Copy(45);
            output.WriteByte(PutTuple);
            Write(tuple);
            Write(width);
            foreach (BeamOperand operand in Operands(48).Skip(2).SkipLast(1))
            {
                Write(operand);
            }
            Write(X(0));
            Copy(49);
            Copy(50);
            EmitRetainedArguments(skipCounter: false);
            EmitTail();
            Copy(56);
            Copy(57);
            copied = body[start + shape.Length].Offset;
            start += shape.Length - 1;

            bool Pair(int offset, BeamOperand left, BeamOperand right) => Operands(offset).SequenceEqual([left, right]);

            bool Tail(int offset, int frame) => Operands(offset).SequenceEqual([U(4), L(entry), U(frame)]);

            bool Forwarded(int offset, int[] inputs) => inputs.Select((argument, index) =>
                Pair(offset + index, Y(argument), X(argument))).SequenceEqual([true, true, true, true]);

            bool OnlyTargets(BeamOperand target, params int[] offsets) =>
                references[(int)target.Value].Order().SequenceEqual(offsets.Select(offset => offset + start));

            bool Bif(
                int offset,
                string operation,
                int live,
                BeamOperand left,
                BeamOperand right,
                BeamOperand destination
            )
            {
                var operands = Operands(offset);
                // The recurrence proof below rederives the counter type for the moved operand.
                if (operands[0].Kind != BeamOperandParameters.Failure || operands[1] != U(live)
                    || operands[2].Value >= importsByIndex.Length
                    || (operands[3] with { TypeIndex = null }) != left
                    || operands[4] != right || operands[5] != destination)
                {
                    return false;
                }

                var (owner, name, arity) = importsByIndex[(int)operands[2].Value];

                return arity == 2 && names[owner - 1] == ErlangModuleNames.Module && names[name - 1] == operation;
            }

            void Copy(int offset) => output.Write(code.Span[shape[offset].Offset..body[start + offset + 1].Offset]);

            void EmitTail() => Op(
                TailLocalCallWithFrame,
                U(4),
                L(entry),
                U(4)
            );

            void EmitRetainedArguments(bool skipCounter)
            {
                Op(Move, Y(0), X(3));
                if (!skipCounter)
                {
                    Op(Move, Y(1), X(2));
                }
                Op(Move, Y(2), X(1));
                Op(Move, Y(3), X(0));
            }
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();

        void Op(byte opcode, params BeamOperand[] operands)
        {
            output.WriteByte(opcode);
            foreach (BeamOperand operand in operands)
            {
                Write(operand);
            }
        }

        void Write(BeamOperand operand)
        {
            if (operand.Kind == BeamOperandParameters.Count)
            {
                output.WriteByte(ExtendedListOperand);
            }
            else if (operand.Kind == BeamOperandParameters.LiteralValue)
            {
                output.WriteByte(ExtendedLiteralOperand);
            }
            else if (operand.TypeIndex.HasValue)
            {
                output.WriteByte(ExtendedTypedRegisterOperand);
            }

            int tag = operand.Kind switch
            {
                BeamOperandParameters.Atom or BeamOperandParameters.Nil => AtomTag,
                BeamOperandParameters.X => XRegisterTag,
                BeamOperandParameters.Y => YRegisterTag,
                BeamOperandParameters.Label or BeamOperandParameters.Failure => LabelTag,
                BeamOperandParameters.Integer => BeamWire.IntegerTag,
                _ => BeamWire.UnsignedTag
            };
            BeamCompactWriter.Write(output, tag, operand.Value);
            if (operand.TypeIndex is int type)
            {
                BeamCompactWriter.Write(output, BeamWire.UnsignedTag, type);
            }
        }

        static BeamOperand X(int register) => new(BeamOperandParameters.X, register);

        static BeamOperand Y(int register) => new(BeamOperandParameters.Y, register);

        static BeamOperand U(int value) => new(BeamOperandParameters.Unsigned, value);

        static BeamOperand I(int value) => new(BeamOperandParameters.Integer, value);

        static BeamOperand L(int value) => new(BeamOperandParameters.Label, value);
    }
}
