using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private const int ConsTypeBits = 1 << 2;

    private sealed partial class GeneralEmitter
    {
        private bool TryEmitSingletonListResultPrefix(Clause clause)
        {
            if (clause.Guard is not null || !HasUnconditionalParameters(clause)
                || clause.Body is not Expr.Case
                {
                    Value: Expr.Call read,
                    Clauses: [
                        {
                            Guard: null,
                            Patterns: [Pattern.List { Items: [Pattern.Variable head], Tail: null }],
                            Body: Expr.List { Items: [var prefix, Expr.Variable last], Tail: null }
                        },
                        { Guard: null, Patterns: [Pattern.Variable fallback], Body: Expr.Variable returned }
                    ]
                }
                || head.Name != last.Name || fallback.Name != returned.Name
                || head.Name == VariableScopeNames.Wildcard || fallback.Name == VariableScopeNames.Wildcard
                || !AttributeLiteral.TryNormalizeConstant(prefix, out Term constant))
            {
                return false;
            }

            var parameters = clause.Patterns.Cast<Pattern.Variable>().Select(parameter => parameter.Name).ToArray();
            if (parameters.Contains(head.Name) || parameters.Contains(fallback.Name)
                || ReadEntryCall(read, parameters) is not { } producer)
            {
                return false;
            }

            bool knownNonemptyList = producer.Module == ErlangModuleNames.Module
                && read is { Function: AutoImportNames.IntegerToList, Arguments.Count: 1 };
            EmitFrameAllocation(EntryCallLiveCount(producer, parameters), 0);
            EmitEntryCall(producer);
            int unchanged = Label();
            if (!knownNonemptyList)
            {
                owner.EmitNonemptyList(0, unchanged);
            }

            owner.Op(GetTail, XRegisterTag, 0);
            owner.Operand(XRegisterTag, 1);
            owner.Op(IsNil, LabelTag, unchanged);
            if (knownNonemptyList)
            {
                owner.EmitTypedRegister(1, ProperListTypeBits);
            }
            else
            {
                owner.Operand(XRegisterTag, 1);
            }

            // A singleton can reuse the original list as the tail of the result.
            owner.Op(TestHeap, BeamWire.UnsignedTag, ConsHeapWords);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.instructions.WriteByte(PutList);
            owner.EmitPatternOperand(constant);
            owner.Operand(XRegisterTag, 0);
            owner.Operand(XRegisterTag, 0);
            owner.EmitFrameReturn();
            Mark(unchanged);
            owner.EmitFrameReturn();

            return true;
        }

        private bool TryEmitEntryLookupProjection(Clause clause)
        {
            if (clause.Guard is not null || !HasUnconditionalParameters(clause)
                || clause.Body is not Expr.Case { Value: Expr.Call read, Clauses: [var first, var second] })
            {
                return false;
            }

            var (projection, fallback) = first.Patterns is [Pattern.Tuple { }] ? (first, second) : (second, first);
            if (projection is not { Patterns: [Pattern.Tuple tuple], Guard: null, Body: Expr.Variable returned }
                || ReadSingleTupleField(tuple) is not { Field: Pattern.Variable field } access
                || field.Name != returned.Name
                || fallback is not { Patterns: [Pattern.Literal { Value: Atom missing }], Guard: null, Body: var unmatched }
                || !AttributeLiteral.TryNormalizeConstant(unmatched, out Term otherwise))
            {
                return false;
            }

            var parameters = clause.Patterns.Cast<Pattern.Variable>().Select(parameter => parameter.Name).ToArray();
            if (parameters.Contains(field.Name) || ReadEntrySelector(read, parameters, []) is not { } selector)
            {
                return false;
            }

            if (!HasKnownNonTupleResult(selector.Call, missing))
            {
                return false;
            }

            int trimmed = EmitEntrySelector(selector, read, parameters);
            int frame = selector.Saved.Length - trimmed;
            int absent = Label();
            int wrongShape = Label();
            owner.Op(IsTuple, LabelTag, absent);
            owner.EmitTypedRegister(0, AtomTypeBits | TupleTypeBits);
            EmitDetailRecordTest(0, tuple.Items.Count, access.Tag, wrongShape, typedTuple: true);
            owner.EmitTuplePart(0, access.Index, 0);
            owner.EmitFrameReturn(frame);
            Mark(absent);
            owner.EmitConstant(otherwise, 0);
            owner.EmitFrameReturn(frame);
            Mark(wrongShape);
            owner.Op(CaseEnd, XRegisterTag, 0);
            return true;
        }

        private bool TryEmitListLookup(FunctionDefinition function)
        {
            if (function.Clauses is not [
                { Guard: null, Patterns: [], Body: Expr.Case
                    { Value: Expr.Call { Module: not null, DynamicRemote: false, Arguments: [var argument] } input,
                      Clauses: [
                        { Patterns: [Pattern.Variable list], Guard: Expr.Call listGuard,
                          Body: Expr.Case { Value: Expr.Call { Module: "lists", Function: "keyfind", DynamicRemote: false,
                              Arguments: [var key, Expr.Literal { Value: Integer position }, Expr.Variable source] } find,
                            Clauses: [
                                { Patterns: [Pattern.Tuple { Items: [
                                    Pattern.Variable { Name: VariableScopeNames.Wildcard }, Pattern.Variable value] }],
                                  Guard: Expr.Call valueGuard, Body: Expr.Variable result },
                                var missingValue] } },
                        var missingList] } }]
                || !DistinctBindings(list, value) || source.Name != list.Name || result.Name != value.Name
                || !IsListGuard(listGuard, list.Name) || !IsListGuard(valueGuard, value.Name)
                || !IsNilFallback(missingList) || !IsNilFallback(missingValue)
                || !AttributeLiteral.TryNormalizeConstant(argument, out Term constant)
                || !AttributeLiteral.TryNormalizeConstant(key, out Term literalKey))
            {
                return false;
            }

            int missing = Label();
            owner.EmitEmptyFrame();
            owner.EmitConstant(constant, 0);
            owner.EmitCallLine(input, location);
            Call(input.Module, input.Function, 1);
            owner.Op(IsList, LabelTag, missing);
            owner.Operand(XRegisterTag, 0);
            owner.EmitConstant(position, 1);
            owner.MoveRegister(XRegisterTag, 0, XRegisterTag, 2);
            owner.EmitConstant(literalKey, 0);
            owner.EmitCallLine(find, CallLocation(input, location));
            Call(find.Module, find.Function, 3);
            owner.Op(IsTuple, LabelTag, missing);
            owner.EmitTypedRegister(0, AtomTypeBits | TupleTypeBits);
            owner.Op(TestArity, LabelTag, missing);
            owner.Operand(XRegisterTag, 0);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.EmitTupleElement(1, 0);
            owner.Op(IsList, LabelTag, missing);
            owner.Operand(XRegisterTag, 0);
            owner.EmitFrameReturn();

            Mark(missing);
            owner.EmitConstant(Nil.Value, 0);
            owner.EmitFrameReturn();

            return true;
        }

        private bool IsListGuard(Expr.Call guard, string variable) =>
            guard is { Module: null or ErlangModuleNames.Module, DynamicRemote: false, Function: "is_list",
                Arguments: [Expr.Variable tested] } && tested.Name == variable
            && (guard.Module is not null || !entries.ContainsKey(("is_list", 1)));

        private static bool IsNilFallback(Clause clause) =>
            clause is { Guard: null, Patterns: [Pattern.Variable { Name: VariableScopeNames.Wildcard }] }
            && AttributeLiteral.TryNormalizeConstant(clause.Body, out Term fallback) && fallback is Nil;

        private bool TryEmitSignedListLength(FunctionDefinition function)
        {
            if (function.Clauses is not [
                { Guard: null, Patterns: [Pattern.Variable input],
                  Body: Expr.Sequence { Items: [
                    Expr.Match { Pattern: Pattern.Variable bytes, Value: Expr.If { Clauses: [
                        { Guard: Expr.Binary { Operator: ErlangOperators.GreaterOrEqual, Left: Expr.Variable tested,
                            Right: Expr.Literal { Value: Integer zero } },
                          Body: Expr.Call { Module: null, DynamicRemote: false, Arguments: [Expr.Variable pos, var posSeed] } positive },
                        { Guard: Expr.Literal { Value: Atom { Name: "true" } },
                          Body: Expr.Call { Module: null, DynamicRemote: false, Arguments: [Expr.Variable neg, var negSeed] } negative }
                    ] } },
                    Expr.Tuple { Items: [Expr.Variable result, Expr.Call
                        { Module: null or ErlangModuleNames.Module, DynamicRemote: false, Function: "length",
                          Arguments: [Expr.Variable measured] } length] }] } }]
                || !DistinctBindings(input, bytes) || !zero.Value.IsZero
                || tested.Name != input.Name || pos.Name != input.Name || neg.Name != input.Name
                || result.Name != bytes.Name || measured.Name != bytes.Name
                || !AttributeLiteral.TryNormalizeConstant(posSeed, out Term posConstant) || posConstant is not Nil
                || !AttributeLiteral.TryNormalizeConstant(negSeed, out Term negConstant) || negConstant is not Nil
                || length.Module is null && entries.ContainsKey(("length", 1))
                || !entries.TryGetValue((positive.Function, 2), out int posEntry)
                || !entries.TryGetValue((negative.Function, 2), out int negEntry))
            {
                return false;
            }

            int negativeBranch = Label();
            int join = Label();
            owner.Op(Allocate, BeamWire.UnsignedTag, 0);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.Op(IsGreaterOrEqual, LabelTag, negativeBranch);
            if (HasOnlyLocalCalls(function, 1, IsGuardedIntegerCall))
            {
                owner.EmitTypedRegister(0, IntegerTypeBits);
            }
            else
            {
                owner.Operand(XRegisterTag, 0);
            }
            owner.LiteralOperand(zero);
            owner.EmitConstant(posConstant, 1);
            owner.EmitCallLine(positive, location);
            owner.EmitFieldCall(2, posEntry);
            JumpTo(join);

            Mark(negativeBranch);
            owner.EmitConstant(negConstant, 1);
            owner.EmitCallLine(negative, location);
            owner.EmitFieldCall(2, negEntry);

            Mark(join);
            owner.EmitCallLine(length, location);
            owner.Op(GcBifOne, LabelTag, 0);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, length.Function, 1));
            if (ReadSignedOctetAccumulator(localFunctions[(positive.Function, 2)]) is not null
                && ReadSignedOctetAccumulator(localFunctions[(negative.Function, 2)]) is not null)
            {
                owner.EmitTypedRegister(0, ConsTypeBits);
            }
            else
            {
                owner.Operand(XRegisterTag, 0);
            }
            owner.Operand(XRegisterTag, 1);
            owner.Op(TestHeap, BeamWire.UnsignedTag, 3);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.Op(PutTuple, XRegisterTag, 0);
            owner.instructions.WriteByte(ExtendedListOperand);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.Operand(XRegisterTag, 0);
            owner.Operand(XRegisterTag, 1);
            owner.EmitFrameReturn();

            return true;
        }
    }
}
