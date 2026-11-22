using static Erlang.Compiler.BeamEmissionFormat;
using SourcePattern = Erlang.Pattern;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitTaggedStateRetainedRecordMapResult(TaggedStateEntryArm arm)
        {
            if (arm.Clause.Body is not Expr.Sequence
                {
                    Items:
                    [
                        Expr.Call { Arguments: [var firstArgument] } first,
                        Expr.Call { Arguments: [var secondArgument, Expr.Variable carrier] } second,
                        Expr.Match
                    {
                        Pattern: SourcePattern.Variable updated,
                        Value: Expr.RecordUpdate
                        {
                            Value: Expr.Variable originalCarrier,
                            Fields:
                                [
                                {
                                    Value: Expr.RecordUpdate
                                    {
                                        Value: Expr.Variable inner,
                                        Fields: [var innerChange]
                                    } innerUpdate
                                } outerChange
                                ]
                        } outerUpdate
                    },
                        Expr.Match
                    {
                        Pattern: SourcePattern.Tuple
                        {
                            Items: [SourcePattern.Any or SourcePattern.Variable, SourcePattern.Variable interval]
                        } replyBindings
                    } reply,
                        Expr.Tuple
                    {
                        Items:
                            [
                                Expr.Literal { Value: Atom resultTag },
                                var resultState,
                                Expr.Variable returnedCarrier,
                                Expr.List
                            {
                                Items:
                                    [
                                        Expr.Tuple
                                    {
                                        Items: [var timeoutTag, Expr.Variable returnedInterval, var timeoutEnd]
                                    },
                                        var followup
                                    ],
                                Tail: null
                            }
                            ]
                    }
                    ]
                }
                || carrier.Name != originalCarrier.Name || returnedCarrier.Name != updated.Name
                || returnedInterval.Name != interval.Name
                || !arm.Bindings.TryGetValue(carrier.Name, out var root) || root.Field is not null
                || !arm.Bindings.TryGetValue(inner.Name, out var nested) || nested.Input != root.Input
                || nested.Field != outerChange.Index
                || !arm.Checks.Contains((root.Input, outerUpdate.Arity, Term.A(outerUpdate.Name)))
                || innerUpdate.FailureSourceLine <= 0 || innerUpdate.Arity >= RecordUpdateArityLimit
                || !AttributeLiteral.TryNormalizeConstant(innerChange.Value, out Term replacement)
                || !AttributeLiteral.TryNormalizeConstant(firstArgument, out Term firstValue)
                || !AttributeLiteral.TryNormalizeConstant(secondArgument, out Term secondValue)
                || !AttributeLiteral.TryNormalizeConstant(resultState, out Term state)
                || !AttributeLiteral.TryNormalizeConstant(timeoutTag, out Term timer)
                || !AttributeLiteral.TryNormalizeConstant(timeoutEnd, out Term timerEnd)
                || !AttributeLiteral.TryNormalizeConstant(followup, out Term next)
                || !TryResolveOrdinaryCallTarget(first, out string? firstModule)
                || !TryResolveOrdinaryCallTarget(second, out string? secondModule))
            {
                return false;
            }

            Expr replyValue = reply.Value is Expr.Block block ? block.Body : reply.Value;
            if (replyValue is not Expr.Sequence
                {
                    Items:
                    [
                        Expr.Match
                    {
                        Pattern: MapPattern
                        {
                            Fields:
                                [
                                { Key: Expr.Literal { Value: Atom required }, Value: SourcePattern.Variable firstKey },
                                { Key: Expr.Literal { Value: Atom fetched }, Value: SourcePattern.Variable secondKey }
                                ]
                        },
                        Value: Expr.Call
                        {
                            Arguments:
                                [
                                    var firstOption,
                                    var secondOption,
                                    Expr.RecordAccess { Value: Expr.Variable readInner } options,
                                    var fourthOption,
                                    var fifthOption
                                ]
                        } lookup
                    } mapMatch,
                        Expr.Tuple { Items: [Expr.Variable left, Expr.Variable right] }
                    ]
                }
                || TermOrder.Compare(required, fetched, exact: true) >= 0
                || left.Name != firstKey.Name || right.Name != secondKey.Name || readInner.Name != inner.Name
                || options.Name != innerUpdate.Name || options.Arity != innerUpdate.Arity
                || options.Index <= 0 || options.Index >= options.Arity
                || mapMatch.SourceLine <= 0
                || !AttributeLiteral.TryNormalizeConstant(firstOption, out Term option0)
                || !AttributeLiteral.TryNormalizeConstant(secondOption, out Term option1)
                || !AttributeLiteral.TryNormalizeConstant(fourthOption, out Term option3)
                || !AttributeLiteral.TryNormalizeConstant(fifthOption, out Term option4)
                || !TryResolveOrdinaryCallTarget(lookup, out string? lookupModule))
            {
                return false;
            }

            var introduced = new[] { updated.Name, firstKey.Name, secondKey.Name, interval.Name }
                .Concat(replyBindings.Items[0] is SourcePattern.Variable { Name: not VariableScopeNames.Wildcard } discard
                    ? [discard.Name] : Array.Empty<string>()).ToArray();
            if (introduced.Distinct(StringComparer.Ordinal).Count() != introduced.Length
                || introduced.Contains(VariableScopeNames.Wildcard, StringComparer.Ordinal)
                || arm.Clause.Patterns.SelectMany(Semantics.Variables).Any(name => introduced.Contains(name, StringComparer.Ordinal)))
            {
                return false;
            }

            int badRecord = Label();
            int badMap = Label();
            // Only the original outer carrier survives the two effects. The nested
            // field is checked after both calls, at its first raising source use.
            EmitFrameAllocation(root.Input + 1, 1);
            owner.MoveRegister(
                XRegisterTag,
                root.Input,
                YRegisterTag,
                0
            );
            owner.EmitConstant(firstValue, 0);
            owner.EmitCallLine(first, location);
            Call(firstModule, first.Function, 1);
            Load(0, 1);
            owner.EmitConstant(secondValue, 0);
            owner.EmitCallLine(second, CallLocation(first, location));
            Call(secondModule, second.Function, 2);

            EmitSavedRecordField(0, outerChange.Index, 0);
            EmitDetailRecordTest(
                0,
                innerUpdate.Arity,
                Term.A(innerUpdate.Name),
                badRecord
            );
            EmitOwnedRecordHeap(innerUpdate.Arity + outerUpdate.Arity + 2, 1);
            EmitOwnedRecordUpdate(
                innerUpdate.Arity,
                XRegisterTag,
                0,
                1,
                [(innerChange.Index, 0, 0, replacement)]
            );
            owner.Op(UpdateRecord, AtomTag, owner.Intern(outerUpdate.Arity < RecordUpdateArityLimit ? "reuse" : "copy"));
            owner.Operand(BeamWire.UnsignedTag, outerUpdate.Arity);
            owner.EmitTypedRegister(0, TupleTypeBits, registerTag: YRegisterTag);
            owner.Operand(YRegisterTag, 0);
            owner.instructions.WriteByte(ExtendedListOperand);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.Operand(BeamWire.UnsignedTag, outerChange.Index + 1);
            owner.EmitTypedRegister(1, TupleTypeBits);

            owner.EmitTuplePart(0, options.Index, 2);
            owner.EmitOrderedTailMoves([], [(0, option0), (1, option1), (3, option3), (4, option4)]);
            owner.EmitCallLine(lookup, CallLocation(second, location));
            Call(lookupModule, lookup.Function, 5);
            EmitMapTest(0, badMap);
            owner.Op(HasMapFields, LabelTag, badMap);
            owner.Operand(XRegisterTag, 0);
            owner.instructions.WriteByte(ExtendedListOperand);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.LiteralOperand(required);
            owner.Op(GetMapElements, LabelTag, badMap);
            owner.EmitTypedRegister(0, MapTypeBits);
            owner.instructions.WriteByte(ExtendedListOperand);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.LiteralOperand(fetched);
            owner.Operand(XRegisterTag, 1);

            EmitOwnedRecordHeap(11, 2);
            owner.EmitTupleHeader(XRegisterTag, 3);
            owner.LiteralOperand(timer);
            owner.Operand(XRegisterTag, 1);
            owner.LiteralOperand(timerEnd);
            owner.Op(PutList, XRegisterTag, 0);
            owner.LiteralOperand(Term.List(next));
            owner.Operand(XRegisterTag, 0);
            owner.EmitTupleHeader(XRegisterTag, 4);
            owner.LiteralOperand(resultTag);
            owner.LiteralOperand(state);
            owner.Operand(YRegisterTag, 0);
            owner.Operand(XRegisterTag, 0);
            owner.EmitFrameReturn(1);

            Mark(badMap);
            owner.EmitLine(new(mapMatch.PreprocessorFile ?? location.File, mapMatch.SourceLine));
            owner.Op(BadMatch, XRegisterTag, 0);
            EmitCheckedResultRecordFailure(
                badRecord,
                new(innerUpdate.FailureSourceFile ?? location.File, innerUpdate.FailureSourceLine)
            );

            return true;
        }
    }
}
