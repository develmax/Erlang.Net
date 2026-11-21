using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitNilEntryForwardedResult(FunctionDefinition function)
        {
            if (owner.ModuleOptimizationDisabled || !owner.SsaTypeInferenceEnabled
                || function.Clauses is not
                [
                { Guard: null, Patterns: [var empty], Body: var emptyResult },
                {
                    Guard: null,
                    Patterns: [Pattern.Variable argument],
                    Body: Expr.Sequence
                    {
                        Items:
                            [
                                Expr.Match { Pattern: Pattern.Variable bound, Value: Expr.Call call },
                                Expr.Call { Module: null, DynamicRemote: false, Arguments: var arguments } next
                            ]
                    }
                } other
                ]
                || !IsEmptyListPattern(empty) || !AttributeLiteral.TryNormalizeConstant(emptyResult, out var constant)
                || !DistinctBindings(argument, bound) || !HasUnconditionalParameters(other)
                || arguments.Count < 2 || arguments[0] is not Expr.Variable result || result.Name != bound.Name
                || ReadEntryCall(call, [argument.Name]) is not { Module: not null } input
                || !entries.TryGetValue((next.Function, arguments.Count), out int entry))
            {
                return false;
            }

            var finals = new List<(int Destination, Term Value)>();
            for (int index = 1; index < arguments.Count; index++)
            {
                if (!AttributeLiteral.TryNormalizeConstant(arguments[index], out var term))
                {
                    return false;
                }

                finals.Add((index, term));
            }

            int fallback = Label();
            owner.EmitNilTest(XRegisterTag, 0, fallback);
            owner.EmitConstant(constant, 0);
            owner.instructions.WriteByte(Return);
            Mark(fallback);
            EmitFrameAllocation(EntryCallLiveCount(input, [argument.Name]), 0);
            EmitEntryCall(input);
            owner.EmitOrderedTailMoves([], finals);
            owner.EmitCallLine(next, CallLocation(call, location));
            owner.Op(TailLocalCallWithFrame, BeamWire.UnsignedTag, arguments.Count);
            owner.Operand(LabelTag, entry);
            owner.Operand(BeamWire.UnsignedTag, 0);

            return true;
        }

        private bool TryEmitSharedLiteralConsContinuation(FunctionDefinition function)
        {
            if (owner.ModuleOptimizationDisabled || !owner.SsaTypeInferenceEnabled
                || function.Clauses is not
                [
                { Guard: null, Patterns: [var empty, Pattern.Variable emptyAcc], Body: Expr.Tuple { Items: [var emptyHead, Expr.Call emptyCall] } },
                {
                    Guard: null,
                    Patterns: [Pattern.List { Tail: null, Items: var key }, Pattern.Variable exactAcc],
                    Body: Expr.Tuple { Items: [Expr.Binary exactBody, var exactTail] }
                },
                {
                    Guard: null,
                    Patterns: [Pattern.List { Tail: Pattern.Variable kept, Items: var sameKey }, Pattern.Variable openAcc],
                    Body: Expr.Tuple { Items: [Expr.Binary openBody, Expr.Variable returned] }
                },
                {
                    Guard: null,
                    Patterns: [Pattern.List { Items: [Pattern.Variable first], Tail: Pattern.Variable tail }, Pattern.Variable accum],
                    Body: Expr.Call
                    {
                        Module: null,
                        DynamicRemote: false,
                        Arguments: [Expr.Variable forwarded, Expr.List { Items: [Expr.Variable consHead], Tail: Expr.Variable consTail }]
                    } recur
                }
                ]
                || !IsEmptyListPattern(empty)
                || !AttributeLiteral.TryNormalizeConstant(emptyHead, out var nilHead) || nilHead is not Nil
                || !AttributeLiteral.TryNormalizeConstant(exactTail, out var nilTail) || nilTail is not Nil
                || key.Count < 2 || !key.All(pattern => pattern is Pattern.Literal { Value: Integer or Atom })
                || !key.SequenceEqual(sameKey) || !DistinctBindings(kept, openAcc) || !DistinctBindings(first, tail, accum)
                || emptyAcc.Name == VariableScopeNames.Wildcard || exactAcc.Name == VariableScopeNames.Wildcard
                || returned.Name != kept.Name || forwarded.Name != tail.Name || consHead.Name != first.Name || consTail.Name != accum.Name
                || recur.Function != function.Name || !entries.TryGetValue((function.Name, 2), out int entry)
                || !ReadConsContinuationCall(emptyCall, emptyAcc)
                || ReadConsContinuationAppend(exactBody, exactAcc) is not { } exact
                || ReadConsContinuationAppend(openBody, openAcc) is not { } open)
            {
                return false;
            }

            int functionInfo = owner.nextLabel - 2;
            int suffix = Label();
            int generic = Label();
            int nonList = Label();

            owner.EmitNonemptyList(0, nonList);
            owner.EmitListParts(
                0,
                XRegisterTag,
                2,
                XRegisterTag,
                0
            );
            EmitKey(2, ((Pattern.Literal)key[0]).Value);
            for (int i = 1; i < key.Count; i++)
            {
                int input = i == 1 ? 0 : 4;
                owner.EmitNonemptyList(input, generic);
                owner.EmitListParts(
                    input,
                    XRegisterTag,
                    3,
                    XRegisterTag,
                    4
                );
                EmitKey(3, ((Pattern.Literal)key[i]).Value);
            }

            owner.EmitNilTest(XRegisterTag, 4, suffix);
            EmitAppend(exact.Call, exact.Suffix, keepTail: false);
            Mark(suffix);
            EmitAppend(open.Call, open.Suffix, keepTail: true);
            Mark(generic);
            owner.Op(TestHeap, BeamWire.UnsignedTag, ConsHeapWords);
            owner.Operand(BeamWire.UnsignedTag, 3);
            owner.Op(PutList, XRegisterTag, 2);
            owner.Operand(XRegisterTag, 1);
            owner.Operand(XRegisterTag, 1);
            owner.Op(TailLocalCall, BeamWire.UnsignedTag, 2);
            owner.Operand(LabelTag, entry);

            Mark(nonList);
            owner.EmitNilTest(XRegisterTag, 0, functionInfo);
            EmitFrameAllocation(2, 0);
            EmitConsContinuationCall(emptyCall);
            EmitResult((AtomTag, NilAtomIndex), (XRegisterTag, 0));

            return true;

            void EmitKey(int register, Term value)
            {
                owner.Op(ExactEqualTest, LabelTag, generic);
                owner.Operand(XRegisterTag, register);
                owner.LiteralOperand(value);
            }

            void EmitAppend(Expr.Call call, Term value, bool keepTail)
            {
                EmitFrameAllocation(keepTail ? 5 : 2, keepTail ? 1 : 0);
                if (keepTail)
                {
                    owner.MoveRegister(
                        XRegisterTag,
                        4,
                        YRegisterTag,
                        0
                    );
                }

                EmitConsContinuationCall(call);
                owner.EmitConstant(value, 1);
                Call(ErlangModuleNames.Module, ErlangOperators.Append, 2);
                EmitResult((XRegisterTag, 0), keepTail ? (YRegisterTag, 0) : (AtomTag, NilAtomIndex));
            }

            void EmitResult((int Tag, int Value) first, (int Tag, int Value) second)
            {
                owner.Op(TestHeap, BeamWire.UnsignedTag, 3);
                owner.Operand(BeamWire.UnsignedTag, 1);
                owner.EmitTupleHeader(XRegisterTag, 2);
                owner.Operand(first.Tag, first.Value);
                owner.Operand(second.Tag, second.Value);
                owner.EmitFrameReturn(second.Tag == YRegisterTag ? 1 : 0);
            }

            void EmitConsContinuationCall(Expr.Call call)
            {
                owner.MoveRegister(
                    XRegisterTag,
                    1,
                    XRegisterTag,
                    0
                );
                owner.EmitCallLine(call, location);
                Call(call.Module, call.Function, 1);
            }
        }

        private (Expr.Call Call, Term Suffix)? ReadConsContinuationAppend(Expr.Binary expression, Pattern.Variable accumulator) =>
            expression is
            {
                Operator: ErlangOperators.Append,
                Left: Expr.Call call,
                Right: var suffix
            } && ReadConsContinuationCall(call, accumulator)
                && AttributeLiteral.TryNormalizeConstant(suffix, out var value)
                ? (call, value) : null;

        private bool ReadConsContinuationCall(Expr.Call call, Pattern.Variable accumulator) =>
            call is { Module: not null, DynamicRemote: false, Arguments: [Expr.Variable input] }
                && input.Name == accumulator.Name && TryResolveOrdinaryCall(call, out _);
    }
}
