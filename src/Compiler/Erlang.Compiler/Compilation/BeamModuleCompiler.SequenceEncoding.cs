using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitSequenceEncodingWrapper(FunctionDefinition function)
        {
            if (function.Clauses is not [
                { Guard: null, Patterns: [Pattern.Variable source, Pattern.Variable tags],
                  Body: Expr.Sequence { Items: [
                      Expr.Match { Pattern: Pattern.Tuple { Items: [Pattern.Variable payload, Pattern.Variable length] },
                          Value: Expr.Call { Module: null, DynamicRemote: false,
                              Arguments: [Expr.Variable input, var empty, Expr.Literal { Value: Integer seed }] } encode },
                      Expr.Call { Module: null, DynamicRemote: false,
                          Arguments: [Expr.Variable tag, Expr.Variable bytes, Expr.Variable count] } finish] } }]
                || !DistinctBindings(source, tags, payload, length)
                || input.Name != source.Name || tag.Name != tags.Name || bytes.Name != payload.Name || count.Name != length.Name
                || !AttributeLiteral.TryNormalizeConstant(empty, out Term accumulator) || accumulator is not Nil
                || !seed.Value.IsZero || !entries.TryGetValue((encode.Function, 3), out int encoder)
                || !entries.TryGetValue((finish.Function, 3), out int finisher)
                || (InferPairFacts(encode) & PairFacts.Length) == 0)
            {
                return false;
            }

            owner.Op(Allocate, BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.MoveRegister(XRegisterTag, 1, YRegisterTag, 0);
            owner.EmitConstant(seed, 2);
            owner.EmitConstant(accumulator, 1);
            owner.EmitCallLine(encode, location);
            owner.EmitFieldCall(3, encoder);
            owner.EmitTuplePart(0, 0, 1);
            owner.EmitTuplePart(0, 1, 2);
            Load(0, 0);
            owner.Op(TailLocalCallWithFrame, BeamWire.UnsignedTag, 3);
            owner.Operand(LabelTag, finisher);
            owner.Operand(BeamWire.UnsignedTag, 1);

            return true;
        }

        private bool TryEmitPairForwarding(FunctionDefinition function)
        {
            if (function.Clauses is not [
                { Guard: null, Patterns: [Pattern.Variable tag,
                      Pattern.Tuple { Items: [Pattern.Variable payload, Pattern.Variable length] }],
                  Body: Expr.Call { Module: null, DynamicRemote: false,
                      Arguments: [Expr.Variable first, Expr.Variable second, Expr.Variable third] } finish }]
                || !DistinctBindings(tag, payload, length)
                || first.Name != tag.Name || second.Name != payload.Name || third.Name != length.Name
                || !entries.TryGetValue((finish.Function, 3), out int finisher)
                || !HasOnlyLocalCalls(function, 2, (_, call) =>
                    call.Arguments[1] is Expr.Tuple { Items.Count: 2 }
                    || (InferPairFacts(call.Arguments[1]) & PairFacts.Length) != 0))
            {
                return false;
            }

            owner.EmitTuplePart(1, 0, 2);
            owner.EmitTuplePart(1, 1, 1);
            owner.Op(Swap, XRegisterTag, 1);
            owner.Operand(XRegisterTag, 2);
            owner.Op(TailLocalCall, BeamWire.UnsignedTag, 3);
            owner.Operand(LabelTag, finisher);

            return true;
        }

        internal static (Expr.Call Read, Expr.Call Finish, int Reader, int? LocalFinisher)? ReadForwardedCallResult(
            FunctionDefinition function, IReadOnlyDictionary<(string Name, int Arity), int> entries)
        {
            if (function.Clauses is not [
                { Guard: null, Patterns: [Pattern.Variable first, Pattern.Variable second], Body: var body }]
                || !DistinctBindings(first, second))
            {
                return null;
            }

            if (body is Expr.Block block)
            {
                body = block.Body;
            }

            Expr.Call read;
            Expr.Call finish;
            if (body is Expr.Sequence { Items: [
                Expr.Match { Pattern: Pattern.Variable result, Value: Expr.Call reader },
                Expr.Call { DynamicRemote: false, Arguments: [Expr.Variable argument] } consumer] }
                && DistinctBindings(first, second, result) && argument.Name == result.Name)
            {
                read = reader;
                finish = consumer;
            }
            else if (body is Expr.Call { DynamicRemote: false, Arguments: [Expr.Call inner] } outer)
            {
                read = inner;
                finish = outer;
            }
            else
            {
                return null;
            }

            if (read is not { Module: null, DynamicRemote: false, Arguments: [Expr.Variable left, Expr.Variable right] }
                || left.Name != first.Name || right.Name != second.Name
                || !entries.TryGetValue((read.Function, 2), out int readerEntry))
            {
                return null;
            }

            if (finish.Module is null && entries.TryGetValue((finish.Function, 1), out int finishEntry))
            {
                return (read, finish, readerEntry, finishEntry);
            }

            return finish is { Module: null or ErlangModuleNames.Module, Function: "binary_to_list" }
                ? (read, finish, readerEntry, null)
                : null;
        }

        private bool TryEmitForwardedCallResult(FunctionDefinition function)
        {
            if (ReadForwardedCallResult(function, entries) is not { } flow)
            {
                return false;
            }
            owner.Op(Allocate, BeamWire.UnsignedTag, 0);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.EmitCallLine(flow.Read, location);
            owner.EmitFieldCall(2, flow.Reader);
            if (flow.LocalFinisher is int finishEntry)
            {
                owner.Op(TailLocalCallWithFrame, BeamWire.UnsignedTag, 1);
                owner.Operand(LabelTag, finishEntry);
                owner.Operand(BeamWire.UnsignedTag, 0);
            }
            else
            {
                owner.EmitCallLine(flow.Finish, CallLocation(flow.Read, location));
                owner.EmitTailRemote(ErlangModuleNames.Module, flow.Finish.Function, framed: true);
            }

            return true;
        }

        private bool TryEmitSequenceEncoding(FunctionDefinition function)
        {
            if (function.Clauses is not [
                { Guard: null, Patterns: [var nil, Pattern.Variable bytes, Pattern.Variable count],
                  Body: Expr.Tuple { Items: [Expr.Call { Module: "lists", Function: "reverse", DynamicRemote: false,
                      Arguments: [Expr.Variable reversed] } reverse, Expr.Variable length] } },
                { Guard: null, Patterns: [
                      Pattern.List { Items: [Pattern.Variable head], Tail: Pattern.Variable tail },
                      Pattern.Variable acc, Pattern.Variable total],
                  Body: Expr.Sequence { Items: [
                      Expr.Match { Pattern: Pattern.Tuple { Items: [Pattern.Variable part, Pattern.Variable size] },
                          Value: Expr.Call { Module: null, DynamicRemote: false,
                              Arguments: [Expr.Variable item, var tag] } encode },
                      Expr.Call { Module: null, DynamicRemote: false, Arguments: [
                          Expr.Variable next, Expr.List { Items: [Expr.Variable payload], Tail: Expr.Variable previous },
                          Expr.Binary { Operator: ErlangOperators.Plus, Left: Expr.Variable left,
                              Right: Expr.Variable right } sum] } recur] } }
                ]
                || !IsEmptyListPattern(nil) || !DistinctBindings(bytes, count)
                || reversed.Name != bytes.Name || length.Name != count.Name
                || !DistinctBindings(head, tail, acc, total, part, size)
                || item.Name != head.Name || next.Name != tail.Name || payload.Name != part.Name
                || previous.Name != acc.Name || left.Name != total.Name || right.Name != size.Name
                || recur.Function != function.Name || !entries.TryGetValue((function.Name, 3), out int entry)
                || !entries.TryGetValue((encode.Function, 2), out int encoder)
                || !AttributeLiteral.TryNormalizeConstant(tag, out Term constant)
                || (InferPairFacts(encode) & PairFacts.Length) == 0)
            {
                return false;
            }

            bool naturalTotal = HasOnlyLocalCalls(function, 3, (_, call) =>
                ReferenceEquals(call, recur) || call.Arguments[2] is Expr.Literal { Value: Integer seed } && seed.Value >= 0);
            int functionInfo = owner.nextLabel - 2;
            int empty = Label();
            owner.EmitNonemptyList(0, empty);
            owner.Op(Allocate, BeamWire.UnsignedTag, 3);
            owner.Operand(BeamWire.UnsignedTag, 3);
            owner.MoveRegister(XRegisterTag, 2, YRegisterTag, 1);
            owner.MoveRegister(XRegisterTag, 1, YRegisterTag, 2);
            owner.EmitListParts(0, XRegisterTag, 0, YRegisterTag, 0);
            owner.EmitConstant(constant, 1);
            owner.EmitCallLine(encode, location);
            owner.EmitFieldCall(2, encoder);
            owner.EmitTuplePart(0, 1, 1);

            var callLocation = CallLocation(encode, location);
            owner.EmitArithmeticLine(sum.SourceLine, ref callLocation);
            owner.Op(GcBifTwo, LabelTag, 0);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, ErlangOperators.Plus, 2));
            if (naturalTotal)
            {
                owner.EmitTypedRegister(1, IntegerTypeBits, lower: 0, registerTag: YRegisterTag);
            }
            else
            {
                owner.Operand(YRegisterTag, 1);
            }
            owner.EmitTypedRegister(1, IntegerTypeBits, lower: 0);
            owner.Operand(XRegisterTag, 1);

            owner.Op(TestHeap, BeamWire.UnsignedTag, 2);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.EmitTuplePart(0, 0, 0);
            owner.Op(PutList, XRegisterTag, 0);
            owner.Operand(YRegisterTag, 2);
            owner.Operand(XRegisterTag, 0);
            owner.MoveRegister(XRegisterTag, 1, XRegisterTag, 2);
            owner.MoveRegister(XRegisterTag, 0, XRegisterTag, 1);
            Load(0, 0);
            owner.Op(TailLocalCallWithFrame, BeamWire.UnsignedTag, 3);
            owner.Operand(LabelTag, entry);
            owner.Operand(BeamWire.UnsignedTag, 3);

            Mark(empty);
            owner.EmitNilTest(XRegisterTag, 0, functionInfo);
            owner.Op(Allocate, BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, 3);
            owner.MoveRegister(XRegisterTag, 2, YRegisterTag, 0);
            owner.MoveRegister(XRegisterTag, 1, XRegisterTag, 0);
            owner.EmitCallLine(reverse, location);
            Call(reverse.Module, reverse.Function, 1);
            owner.Op(TestHeap, BeamWire.UnsignedTag, 3);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.Op(PutTuple, XRegisterTag, 0);
            owner.instructions.WriteByte(ExtendedListOperand);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.Operand(XRegisterTag, 0);
            owner.Operand(YRegisterTag, 0);
            owner.EmitFrameReturn(1);

            return true;
        }
    }
}
