using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitCheckedPairListCallbacks(FunctionDefinition function, int functionInfo)
        {
            if (owner.ModuleOptimizationDisabled || !owner.SsaTypeInferenceEnabled
                || function.Arity is not (2 or 3)
                || function.Clauses is not
                [
                { Guard: null } cons,
                {
                    Guard: Expr.Call
                    {
                        Module: null or ErlangModuleNames.Module,
                        DynamicRemote: false,
                        Function: AutoImportNames.IsFunction,
                        Arguments: [Expr.Variable checkedFun, Expr.Literal { Value: Integer requiredArity }]
                    } guard
                } empty
                ]
                || requiredArity.Value != function.Arity || !owner.ResolvesErlangBif(guard))
            {
                return false;
            }

            int input = function.Arity - 1;
            if (cons.Patterns[input] is not Pattern.List { Items: [var pair], Tail: Pattern.Variable tail }
                || ListEntryTest(empty.Patterns[input]) != IsNil
                || cons.Patterns.Take(input).Any(pattern => pattern is not global::Erlang.Pattern.Variable)
                || empty.Patterns.Take(input).Any(pattern => pattern is not global::Erlang.Pattern.Variable)
                || !entries.TryGetValue((function.Name, function.Arity), out int entry))
            {
                return false;
            }

            Pattern.Variable? alias = null;
            if (pair is AliasPattern wrapper)
            {
                if (!TryReadPatternAlias(wrapper, out var bound, out pair))
                {
                    return false;
                }

                alias = bound;
            }

            if (pair is not Pattern.Tuple { Items: [Pattern.Variable key, Pattern.Variable value] })
            {
                return false;
            }

            var incoming = cons.Patterns.Take(input).Cast<Pattern.Variable>().ToArray();
            var emptyBindings = empty.Patterns.Take(input).Cast<Pattern.Variable>().ToArray();
            Pattern.Variable[] names = [.. incoming, key, value, tail];
            if (alias is not null)
            {
                names = [.. names, alias];
            }

            if (!DistinctBindings(names) || !DistinctBindings(emptyBindings)
                || checkedFun.Name != emptyBindings[0].Name)
            {
                return false;
            }

            Expr.Apply callback;
            Expr.Call recur;
            Expr.Case? filter = null;
            if (function.Arity == 3)
            {
                if (cons.Body is not Expr.Call { Arguments: [_, Expr.Apply step, _] } fold
                    || !IsPairListCallback(
                        step,
                        incoming[0].Name,
                        key.Name,
                        value.Name,
                        incoming[1].Name
                    )
                    || !IsPairListRecursion(
                        fold,
                        function.Name,
                        incoming[0].Name,
                        tail.Name,
                        step
                    )
                    || empty.Body is not Expr.Variable accumulator || accumulator.Name != emptyBindings[1].Name)
                {
                    return false;
                }

                callback = step;
                recur = fold;
            }
            else
            {
                if (!AttributeLiteral.TryNormalizeConstant(empty.Body, out Term zero) || zero is not Nil)
                {
                    return false;
                }

                if (cons.Body is Expr.List
                    {
                        Items: [Expr.Tuple { Items: [Expr.Variable retainedKey, Expr.Apply map] }],
                        Tail: Expr.Call mapped
                    }
                    && retainedKey.Name == key.Name && IsPairListCallback(
                        map,
                        incoming[0].Name,
                        key.Name,
                        value.Name
                    )
                    && IsPairListRecursion(
                        mapped,
                        function.Name,
                        incoming[0].Name,
                        tail.Name
                    ))
                {
                    callback = map;
                    recur = mapped;
                }
                else if (cons.Body is Expr.Case
                {
                    Value: Expr.Apply predicate,
                    Clauses:
                        [
                        {
                            Guard: null,
                            Patterns: [Pattern.Literal { Value: Atom { Name: ErlangBooleanAtoms.True } }],
                            Body: Expr.List { Items: [Expr.Variable retained], Tail: Expr.Call included }
                        },
                        {
                            Guard: null,
                            Patterns: [Pattern.Literal { Value: Atom { Name: ErlangBooleanAtoms.False } }],
                            Body: Expr.Call skipped
                        }
                        ]
                } selection && alias is not null && retained.Name == alias.Name
                    && IsPairListCallback(
                        predicate,
                        incoming[0].Name,
                        key.Name,
                        value.Name
                    )
                    && IsPairListRecursion(
                        included,
                        function.Name,
                        incoming[0].Name,
                        tail.Name
                    )
                    && IsPairListRecursion(
                        skipped,
                        function.Name,
                        incoming[0].Name,
                        tail.Name
                    ))
                {
                    callback = predicate;
                    recur = included;
                    filter = selection;
                }
                else
                {
                    return false;
                }
            }

            int exhausted = Label();
            int badPredicate = filter is null ? 0 : Label();
            int head = function.Arity;
            owner.EmitNonemptyList(input, exhausted);
            owner.EmitListParts(
                input,
                XRegisterTag,
                head,
                XRegisterTag,
                head + 1
            );
            owner.EmitTupleCheck(head, 2, functionInfo);
            if (function.Arity == 3)
            {
                EmitCheckedPairListFold(callback, entry);
            }
            else if (filter is not null)
            {
                EmitCheckedPairListFilter(
                    callback,
                    recur,
                    entry,
                    badPredicate
                );
            }
            else
            {
                EmitCheckedPairListMap(callback, recur);
            }

            Mark(exhausted);
            owner.EmitNilTest(XRegisterTag, input, functionInfo);
            owner.Op(IsFunctionWithArity, LabelTag, functionInfo);
            owner.Operand(XRegisterTag, 0);
            owner.LiteralOperand(Term.I(function.Arity));
            if (function.Arity == 3)
            {
                owner.MoveToX0(XRegisterTag, 1);
            }
            else
            {
                owner.EmitConstant(Nil.Value, 0);
            }

            owner.instructions.WriteByte(Return);
            if (filter is not null)
            {
                Mark(badPredicate);
                EmitCaseEnd(CaseSourceLocation(filter));
            }

            return true;
        }

        private static bool IsPairListCallback(Expr.Apply call, string target, params string[] arguments) =>
            call.Function is Expr.Variable function && function.Name == target
            && call.Arguments.Count == arguments.Length
            && call.Arguments.Zip(arguments, (argument, name) => argument is Expr.Variable variable
                && variable.Name == name).All(matches => matches);

        private static bool IsPairListRecursion(
            Expr.Call call,
            string target,
            string callback,
            string tail,
            Expr? result = null
        ) =>
            call.Module is null && !call.DynamicRemote && call.Function == target
            && call.Arguments.Count == (result is null ? 2 : 3)
            && call.Arguments[0] is Expr.Variable fun && fun.Name == callback
            && call.Arguments[^1] is Expr.Variable remaining && remaining.Name == tail
            && (result is null || ReferenceEquals(call.Arguments[1], result));

        private void EmitCheckedPairListFold(Expr.Apply callback, int entry)
        {
            const int frame = 2;
            EmitFrameAllocation(5, frame);
            owner.MoveRegister(
                XRegisterTag,
                4,
                YRegisterTag,
                0
            );
            owner.MoveRegister(
                XRegisterTag,
                0,
                YRegisterTag,
                1
            );
            owner.EmitTuplePart(3, 0, 0);
            owner.EmitTuplePart(3, 1, 2);
            Load(1, 3);
            owner.Op(Swap, XRegisterTag, 1);
            owner.Operand(XRegisterTag, 2);
            EmitPairListCallback(callback, 3);
            owner.MoveRegister(
                XRegisterTag,
                0,
                XRegisterTag,
                1
            );
            Load(0, 2);
            Load(1);
            owner.Op(TailLocalCallWithFrame, BeamWire.UnsignedTag, 3);
            owner.Operand(LabelTag, entry);
            owner.Operand(BeamWire.UnsignedTag, frame);
        }

        private void EmitCheckedPairListMap(Expr.Apply callback, Expr.Call recur)
        {
            EmitFrameAllocation(4, 3);
            owner.MoveRegister(
                XRegisterTag,
                3,
                YRegisterTag,
                1
            );
            owner.MoveRegister(
                XRegisterTag,
                0,
                YRegisterTag,
                2
            );
            owner.Op(GetTupleElement, XRegisterTag, 2);
            owner.Operand(BeamWire.UnsignedTag, 0);
            owner.Operand(YRegisterTag, 0);
            owner.EmitTuplePart(2, 1, 1);
            owner.MoveRegister(
                XRegisterTag,
                0,
                XRegisterTag,
                2
            );
            Load(0);
            BeamSourceLocation after = EmitPairListCallback(callback, 2);
            owner.Op(Swap, YRegisterTag, 2);
            owner.Operand(XRegisterTag, 0);
            Load(1, 1);
            owner.MoveRegister(
                YRegisterTag,
                0,
                YRegisterTag,
                1
            );
            owner.Op(TrimFrame, BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.EmitCallLine(recur, after);
            Call(null, recur.Function, 2);
            owner.Op(TestHeap, BeamWire.UnsignedTag, 5);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.EmitTupleHeader(XRegisterTag, 2, 1);
            owner.Operand(YRegisterTag, 0);
            owner.Operand(YRegisterTag, 1);
            owner.Op(PutList, XRegisterTag, 1);
            owner.Operand(XRegisterTag, 0);
            owner.Operand(XRegisterTag, 0);
            owner.EmitFrameReturn(2);
        }

        private void EmitCheckedPairListFilter(
            Expr.Apply callback,
            Expr.Call recur,
            int entry,
            int badPredicate
        )
        {
            const int frame = 3;
            EmitFrameAllocation(4, frame);
            owner.MoveRegister(
                XRegisterTag,
                2,
                YRegisterTag,
                0
            );
            owner.MoveRegister(
                XRegisterTag,
                3,
                YRegisterTag,
                1
            );
            owner.MoveRegister(
                XRegisterTag,
                0,
                YRegisterTag,
                2
            );
            owner.EmitTuplePart(2, 0, 0);
            owner.EmitTuplePart(2, 1, 1);
            Load(2, 2);
            BeamSourceLocation after = EmitPairListCallback(callback, 2);
            int include = Label();
            int skip = Label();
            owner.Op(SelectValue, XRegisterTag, 0);
            owner.Operand(LabelTag, badPredicate);
            owner.instructions.WriteByte(ExtendedListOperand);
            owner.Operand(BeamWire.UnsignedTag, 4);
            owner.LiteralOperand(Term.A(ErlangBooleanAtoms.False));
            owner.Operand(LabelTag, skip);
            owner.LiteralOperand(Term.A(ErlangBooleanAtoms.True));
            owner.Operand(LabelTag, include);

            Mark(include);
            Load(1, 1);
            Load(2);
            owner.MoveRegister(
                YRegisterTag,
                0,
                YRegisterTag,
                2
            );
            owner.Op(TrimFrame, BeamWire.UnsignedTag, 2);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.EmitCallLine(recur, after);
            Call(null, recur.Function, 2);
            owner.Op(TestHeap, BeamWire.UnsignedTag, ConsHeapWords);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.Op(PutList, YRegisterTag, 0);
            owner.Operand(XRegisterTag, 0);
            owner.Operand(XRegisterTag, 0);
            owner.EmitFrameReturn(1);

            Mark(skip);
            Load(1, 1);
            Load(2);
            owner.Op(TailLocalCallWithFrame, BeamWire.UnsignedTag, 2);
            owner.Operand(LabelTag, entry);
            owner.Operand(BeamWire.UnsignedTag, frame);
        }

        private BeamSourceLocation EmitPairListCallback(Expr.Apply callback, int arity)
        {
            var previous = location;
            owner.EmitArithmeticLine(callback.SourceLine, ref previous);
            owner.Op(CallFun, BeamWire.UnsignedTag, arity);

            return previous;
        }
    }
}
