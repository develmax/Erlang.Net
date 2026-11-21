using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitTaggedListPayloadRecursion(FunctionDefinition function, int failure)
        {
            if (owner.ModuleOptimizationDisabled || !owner.SsaTypeInferenceEnabled
                || function.Arity != 2 || !entries.TryGetValue((function.Name, function.Arity), out int entry)
                || function.Clauses is not
                [
                { Guard: null, Patterns: [var nil, Pattern.Variable empty], Body: Expr.Variable emptyResult },
                {
                    Guard: null,
                    Patterns: [Pattern.List
                    {
                        Items: [Pattern.Tuple
                        {
                            Items: [Pattern.Literal { Value: Atom tag },
                                Pattern.Tuple { Items: [Pattern.Variable firstField, Pattern.Variable secondField] }]
                        }],
                        Tail: Pattern.Variable pairRest
                    }, Pattern.Variable pairAccumulator],
                    Body: Expr.Sequence
                    {
                        Items:
                        [
                            Expr.Match { Pattern: Pattern.Variable pairResult, Value: Expr.Call pairBuild },
                            Expr.Call pairRecur
                        ]
                    }
                },
                {
                    Patterns: [Pattern.List
                    {
                        Items: [Pattern.Tuple { Items: [Pattern.Literal { Value: Atom listTag }, Pattern.Variable list] }],
                        Tail: Pattern.Variable listRest
                    }, Pattern.Variable listAccumulator],
                    Body: Expr.Sequence
                    {
                        Items:
                        [
                            Expr.Match { Pattern: Pattern.Variable firstValue, Value: Expr.Call firstLookup },
                            Expr.Match { Pattern: Pattern.Variable secondValue, Value: Expr.Call secondLookup },
                            Expr.Match { Pattern: Pattern.Variable listResult, Value: Expr.Call listBuild },
                            Expr.Call listRecur
                        ]
                    }
                } listClause
                ]
                || ListEntryTest(nil) != IsNil || empty.Name != emptyResult.Name
                || !tag.Equals(listTag)
                || !Distinct(empty)
                || !Distinct(
                    firstField,
                    secondField,
                    pairRest,
                    pairAccumulator,
                    pairResult
                )
                || !Distinct(
                    list,
                    listRest,
                    listAccumulator,
                    firstValue,
                    secondValue,
                    listResult
                )
                || ReadEntryTypeGuard(listClause with { Patterns = [list] }, listClause.Guard) is not (IsList, 0)
                || !IsRecurrence(
                    pairRecur,
                    pairRest,
                    pairAccumulator,
                    pairResult
                )
                || !IsRecurrence(
                    listRecur,
                    listRest,
                    listAccumulator,
                    listResult
                )
                || !IsPair(pairBuild, firstField, secondField)
                || !IsPair(listBuild, firstValue, secondValue)
                || ReadEntryCall(pairBuild, [firstField.Name, secondField.Name]) is not { } pair
                || ReadEntryCall(listBuild, [firstValue.Name, secondValue.Name]) is not { } constructed
                || ReadLookup(firstLookup, list) is not { } lookup
                || ReadLookup(secondLookup, list) is not { } update)
            {
                return false;
            }

            const int accumulator = 1;
            const int payload = 2;
            const int rest = 3;
            const int live = rest + 1;
            const int pairFrame = 2;
            const int listFrame = 3;
            int listBranch = Label();
            int emptyBranch = Label();
            owner.EmitNonemptyList(0, emptyBranch);
            owner.EmitListParts(
                0,
                XRegisterTag,
                payload,
                XRegisterTag,
                rest
            );
            EmitDetailRecordTest(
                payload,
                2,
                tag,
                failure
            );
            Project(1, payload);
            EmitTupleShapeTest(
                XRegisterTag,
                payload,
                2,
                listBranch
            );
            EmitFrameAllocation(live, pairFrame);
            SaveIncoming(rest, 0);
            SaveIncoming(accumulator, 1);
            Project(0, 0);
            Project(1, 1);
            owner.EmitCallLine(pairBuild, location);
            Call(pair.Module, pairBuild.Function, pairBuild.Arguments.Count);
            EmitRecurrence(pairFrame);

            Mark(listBranch);
            owner.Op(IsList, LabelTag, failure);
            owner.Operand(XRegisterTag, payload);
            EmitFrameAllocation(live, listFrame);
            SaveIncoming(payload, 0);
            SaveIncoming(rest, 1);
            SaveIncoming(accumulator, 2);
            owner.MoveRegister(
                XRegisterTag,
                payload,
                XRegisterTag,
                1
            );
            owner.EmitConstant(lookup.Constants[0].Value, 0);
            owner.EmitCallLine(firstLookup, location);
            Call(lookup.Module, firstLookup.Function, firstLookup.Arguments.Count);
            owner.MoveRegister(
                YRegisterTag,
                0,
                XRegisterTag,
                1
            );
            SaveIncoming(0, 0);
            owner.EmitConstant(update.Constants[0].Value, 0);
            var source = CallLocation(firstLookup, location);
            owner.EmitCallLine(secondLookup, source);
            Call(update.Module, secondLookup.Function, secondLookup.Arguments.Count);
            owner.EmitOrderedTailMoves([(1, 0)], [], [(0, 0)]);
            owner.Op(TrimFrame, BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, pairFrame);
            owner.EmitCallLine(listBuild, CallLocation(secondLookup, source));
            Call(constructed.Module, listBuild.Function, listBuild.Arguments.Count);
            EmitRecurrence(pairFrame);

            Mark(emptyBranch);
            owner.Op(IsNil, LabelTag, failure);
            owner.Operand(XRegisterTag, 0);
            owner.MoveRegister(
                XRegisterTag,
                1,
                XRegisterTag,
                0
            );
            owner.instructions.WriteByte(Return);

            return true;

            bool IsRecurrence(
                Expr.Call call,
                Pattern.Variable tail,
                Pattern.Variable acc,
                Pattern.Variable result
            ) =>
                call is
                {
                    Module: null,
                    DynamicRemote: false,
                    Arguments: [Expr.Variable argument, Expr.List
                    {
                        Items: [Expr.Variable head],
                        Tail: Expr.Variable previous
                    }]
                }
                && call.Function == function.Name && argument.Name == tail.Name
                && head.Name == result.Name && previous.Name == acc.Name;

            static bool IsPair(Expr.Call call, Pattern.Variable first, Pattern.Variable second) =>
                call.Arguments is [Expr.Variable a, Expr.Variable b] && a.Name == first.Name && b.Name == second.Name;

            static bool Distinct(params Pattern.Variable[] bindings) =>
                bindings.All(binding => binding.Name != VariableScopeNames.Wildcard)
                && bindings.Select(binding => binding.Name).Distinct(StringComparer.Ordinal).Count() == bindings.Length;

            EntryCall? ReadLookup(Expr.Call call, Pattern.Variable from) =>
                call.Arguments is [_, Expr.Variable data] && data.Name == from.Name
                && ReadEntryCall(call, [VariableScopeNames.Wildcard, from.Name]) is { Constants: [{ Destination: 0 }] } lookupCall
                    ? lookupCall
                    : null;

            void Project(int field, int destination)
            {
                owner.Op(GetTupleElement, XRegisterTag, payload);
                owner.Operand(BeamWire.UnsignedTag, field);
                owner.Operand(XRegisterTag, destination);
            }

            void SaveIncoming(int sourceRegister, int stackSlot) =>
                owner.MoveRegister(
                    XRegisterTag,
                    sourceRegister,
                    YRegisterTag,
                    stackSlot
                );

            void EmitRecurrence(int frame)
            {
                owner.Op(TestHeap, BeamWire.UnsignedTag, ConsHeapWords);
                owner.Operand(BeamWire.UnsignedTag, 1);
                owner.Op(PutList, XRegisterTag, 0);
                owner.Operand(YRegisterTag, 1);
                owner.Operand(XRegisterTag, 1);
                owner.MoveRegister(
                    YRegisterTag,
                    0,
                    XRegisterTag,
                    0
                );
                owner.Op(TailLocalCallWithFrame, BeamWire.UnsignedTag, function.Arity);
                owner.Operand(LabelTag, entry);
                owner.Operand(BeamWire.UnsignedTag, frame);
            }
        }
    }
}
