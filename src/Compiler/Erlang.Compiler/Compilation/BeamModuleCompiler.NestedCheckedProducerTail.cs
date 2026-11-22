using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitNestedCheckedProducerTail(FunctionDefinition function)
        {
            if (owner.ModuleOptimizationDisabled || !owner.SsaTypeInferenceEnabled || recursiveClosure is not null
                || function.Arity != 3 || function.Clauses is not
                [
                {
                    Guard: null,
                    Body: Expr.Sequence
                    {
                        Items:
                            [
                                Expr.Match
                            {
                                Pattern: Pattern.Tuple
                                {
                                    Items:
                                        [
                                            Pattern.Literal { Value: Atom firstTag },
                                            Pattern.Tuple { Items: [Pattern.Variable selector, Pattern.Variable payload] }
                                        ]
                                },
                                Value: Expr.Call firstCall
                            } firstMatch,
                                Expr.Case
                            {
                                Value: Expr.Variable tested,
                                Clauses:
                                    [
                                    {
                                        Guard: null,
                                        Patterns: [Pattern.Literal { Value: Atom selected }],
                                        Body: Expr.Sequence
                                        {
                                            Items:
                                                [
                                                    Expr.Match
                                                {
                                                    Pattern: Pattern.List
                                                    {
                                                        Items: [Pattern.Variable head, Pattern.Variable ignored], Tail: null
                                                    },
                                                    Value: Expr.Variable source
                                                } listMatch,
                                                    Expr.Call effect,
                                                    Expr.Match
                                                {
                                                    Pattern: Pattern.Tuple
                                                    {
                                                        Items: [Pattern.Literal { Value: Atom secondTag }, Pattern.Variable result]
                                                    },
                                                    Value: Expr.Call secondCall
                                                } secondMatch,
                                                    Expr.Call tail
                                                ]
                                        }
                                    },
                                    {
                                        Guard: null,
                                        Patterns: [Pattern.Variable { Name: VariableScopeNames.Wildcard }],
                                        Body: Expr.Call fallback
                                    }
                                    ]
                            }
                            ]
                    }
                } clause
                ]
                || !HasUnconditionalParameters(clause) || tested.Name != selector.Name || source.Name != payload.Name
                || !DistinctBindings([.. clause.Patterns.Cast<Pattern.Variable>(), selector, payload, head, result]))
            {
                return false;
            }

            string[] parameters = clause.Patterns.Cast<Pattern.Variable>().Select(parameter => parameter.Name).ToArray();
            string[] saved = parameters.Reverse().ToArray();
            if ((ignored.Name != VariableScopeNames.Wildcard
                    && saved.Contains(ignored.Name, StringComparer.Ordinal))
                || ignored.Name == selector.Name || ignored.Name == payload.Name
                || ignored.Name == head.Name || ignored.Name == result.Name
                || ReadEntryCall(firstCall, parameters) is not { } first
                || ReadEntryCall(effect, ["", head.Name], saved) is not { SavedInputs: { Count: 3 } } onSelected
                || ReadEntryCall(fallback, [selector.Name], saved) is not { Module: null, SavedInputs: { Count: 3 } } onOther
                || !onSelected.SavedInputs.Select(move => move.Source).Order().SequenceEqual([0, 1, 2])
                || !onSelected.SavedInputs.SequenceEqual(onOther.SavedInputs)
                || onSelected.Moves is not [var selectedMove] || onOther.Moves is not [var otherMove]
                || selectedMove.Source != 1 || otherMove.Source != 0 || selectedMove.Destination != otherMove.Destination
                || effect.Arguments.Count != fallback.Arguments.Count
                || onSelected.Module != onOther.Module || effect.Function != fallback.Function
                || ReadEntryCall(secondCall, [], saved) is not { SavedInputs: [{ Source: 0 }] } second
                || ReadEntryCall(tail, ["", "", result.Name], saved[1..]) is not
                { Module: null, SavedInputs: { Count: 2 }, Moves.Count: 0, Constants.Count: 0 } continuation
                || tail.Arguments.Count != 3
                || tail.Arguments[2] is not Expr.Variable projected || projected.Name != result.Name
                || !continuation.SavedInputs.Select(move => move.Source).Order().SequenceEqual([0, 1]))
            {
                return false;
            }

            const int inputFrameSize = 3;
            const int retainedFrameSize = 2;
            const int resultRegister = 2;
            EmitFrameAllocation(function.Arity, inputFrameSize);
            for (int slot = 0; slot < inputFrameSize; slot++)
            {
                owner.MoveRegister(
                    XRegisterTag,
                    inputFrameSize - 1 - slot,
                    YRegisterTag,
                    slot
                );
            }

            EmitEntryCall(first);
            var firstLocation = CallLocation(firstCall, location);
            int badNested = Label();
            int different = Label();
            int badList = Label();
            int badLast = Label();
            owner.EmitRecordTest(
                badNested,
                XRegisterTag,
                0,
                firstTag,
                2
            );
            owner.EmitTuplePart(0, 1, 1);
            owner.EmitTupleCheck(1, 2, badNested);
            owner.EmitTuplePart(1, 0, 0);
            owner.EmitAtomTest(0, selected.Name, different);

            // The literal branch owns the second projection. The opposite
            // branch can tail-call with the first member still in X0.
            owner.EmitTuplePart(1, 1, 0);
            owner.Op(IsNonemptyList, LabelTag, badList);
            owner.Operand(XRegisterTag, 0);
            owner.Op(GetList, XRegisterTag, 0);
            owner.Operand(XRegisterTag, 1);
            owner.Operand(XRegisterTag, resultRegister);
            owner.Op(IsNonemptyList, LabelTag, badList);
            owner.Operand(XRegisterTag, resultRegister);
            owner.Op(GetTail, XRegisterTag, resultRegister);
            owner.Operand(XRegisterTag, resultRegister);
            owner.Op(IsNil, LabelTag, badList);
            owner.Operand(XRegisterTag, resultRegister);
            EmitStackCallArguments(onSelected);
            owner.EmitCallLine(effect, firstLocation);
            Call(onSelected.Module, effect.Function, effect.Arguments.Count);
            var selectedLocation = CallLocation(effect, firstLocation);

            EmitStackCallArguments(second);
            // The lowest slot is an outgoing argument of this producer
            // only. Its X copy is prepared before the slot is retired.
            owner.Op(TrimFrame, BeamWire.UnsignedTag, inputFrameSize - retainedFrameSize);
            owner.Operand(BeamWire.UnsignedTag, retainedFrameSize);
            owner.EmitCallLine(secondCall, selectedLocation);
            Call(second.Module, secondCall.Function, secondCall.Arguments.Count);
            var secondLocation = CallLocation(secondCall, selectedLocation);
            owner.EmitRecordTest(
                badLast,
                XRegisterTag,
                0,
                secondTag,
                2
            );
            owner.EmitTuplePart(0, 1, resultRegister);
            EmitStackCallArguments(continuation);
            EmitPreparedFrameTail(continuation, secondLocation, retainedFrameSize);

            Mark(different);
            EmitStackCallArguments(onOther);
            EmitPreparedFrameTail(onOther, firstLocation, inputFrameSize);
            Mark(badLast);
            var lastMatchLocation = MatchLocation(secondMatch, secondLocation);
            if (lastMatchLocation != secondLocation)
            {
                owner.EmitLine(lastMatchLocation);
            }

            owner.Op(BadMatch, XRegisterTag, 0);
            Mark(badList);
            owner.EmitLine(MatchLocation(listMatch, firstLocation));
            owner.Op(BadMatch, XRegisterTag, 0);
            Mark(badNested);
            owner.EmitLine(MatchLocation(firstMatch, firstLocation));
            owner.Op(BadMatch, XRegisterTag, 0);

            return true;

            BeamSourceLocation MatchLocation(Expr.Match match, BeamSourceLocation previous) =>
                match.SourceLine == 0
                    ? previous
                    : new BeamSourceLocation(match.PreprocessorFile ?? location.File, match.SourceLine);

            void EmitStackCallArguments(EntryCall call) =>
                owner.EmitOrderedTailMoves(call.Moves, call.Constants, call.SavedInputs);
        }
    }
}
