using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitActiveSocketCheck(FunctionDefinition function)
        {
            if (function.Clauses is not [{
                Guard: null, Patterns: [Pattern.Variable options, AliasPattern alias],
                Body: Expr.Case { Value: Expr.Call { Module: not null, DynamicRemote: false,
                    Arguments: [Expr.Literal { Value: Atom key }, Expr.Variable input] } lookup,
                    Clauses: [var low, var high, {
                        Patterns: [Pattern.Tuple { Items: [
                            Pattern.Variable { Name: VariableScopeNames.Wildcard }, Pattern.Variable increment
                        ] }], Guard: Expr.Call integer,
                        Body: Expr.Sequence { Items: [
                            Expr.Match { Pattern: Pattern.Tuple { Items: [
                                Pattern.Literal { Value: Atom ok }, Pattern.Tuple { Items: [
                                    Pattern.Literal { Value: Atom optionTag }, ..
                                ] } optionRecord, Pattern.Variable { Name: VariableScopeNames.Wildcard }
                            ] }, Value: Expr.Call { Module: not null, DynamicRemote: false, Arguments: [Expr.Variable listener] } read } readMatch,
                            Expr.Case { Value: Expr.Variable active, Clauses: [var atomCase, var aboveCase, var belowCase, var unchanged] }
                        ] }
                    }, var fallback]
                }
            }] || !TryReadPatternAlias(alias, out var socket, out var shape)
                || shape is not Pattern.Tuple { Items: [Pattern.Literal { Value: Atom socketTag }, ..] } record
                || ReadSocketListenerFields(record, listener.Name) is not { } fields
                || !DistinctBindings(options, socket, increment, new(listener.Name), new(active.Name))
                || input.Name != options.Name || !IsTypeCheck(integer, AutoImportNames.IsInteger, increment.Name)
                || owner.ResolvesErlangBif(lookup) || owner.ResolvesErlangBif(read)
                || ReadLimit(low, ErlangOperators.Less) is not { } minimum || ReadLimit(high, ErlangOperators.Greater) is not { } maximum
                || minimum.Bound == long.MinValue || maximum.Bound == long.MaxValue || minimum.Bound > maximum.Bound
                || !IsFallback(fallback, out var result) || !IsFallback(unchanged, out var otherResult) || !result.Equals(otherResult)
                || atomCase is not { Patterns: [Pattern.Variable atom], Guard: Expr.Binary { Operator: ErlangOperators.AndAlso,
                    Left: Expr.Call atomCheck, Right: Expr.Binary { Operator: ErlangOperators.LessOrEqual,
                        Left: Expr.Variable count, Right: var atomBound } }, Body: Expr.Binary atomSend }
                || !DistinctBindings(options, socket, increment, new(listener.Name), new(active.Name), atom)
                || !IsTypeCheck(atomCheck, AutoImportNames.IsAtom, atom.Name) || count.Name != increment.Name
                || !ReadBound(atomBound, out long threshold)
                || ReadSumGuard(aboveCase, ErlangOperators.Greater) is not long upper || upper != maximum.Bound
                || ReadSumGuard(belowCase, ErlangOperators.LessOrEqual) is not long lower || lower != threshold
                || ReadThrow(aboveCase.Body) is not { } overflow || belowCase.Body is not Expr.Binary integerSend
                || ReadSend(atomSend) is not { } firstTag || ReadSend(integerSend) is not { } secondTag
                || ReadActiveField(optionRecord, active.Name) is not int activeField)
            {
                return false;
            }

            int functionInfo = owner.nextLabel - 2;
            int upperCheck = Label();
            int lookupActive = Label();
            int integerActive = Label();
            int sumCheck = Label();
            int finish = Label();
            int badMatch = Label();
            if (!AlwaysReceivesTaggedArgument(function, 1, socketTag, record.Items.Count))
            {
                EmitDetailRecordTest(1, record.Items.Count, socketTag, functionInfo);
            }

            owner.EmitTuplePart(1, fields.Handle, 2);
            owner.EmitTupleCheck(2, 2, functionInfo);
            owner.EmitTuplePart(1, fields.Config, 3);
            EmitDetailRecordTest(3, fields.Arity, fields.Tag, functionInfo);
            owner.Op(Allocate, BeamWire.UnsignedTag, 3);
            owner.Operand(BeamWire.UnsignedTag, 3);
            owner.InitStackSlot(0);
            owner.MoveRegister(XRegisterTag, 2, YRegisterTag, 1);
            owner.MoveRegister(XRegisterTag, 1, YRegisterTag, 2);
            owner.MoveRegister(XRegisterTag, 0, XRegisterTag, 1);
            owner.EmitConstant(key, 0);
            var previous = location;
            EmitWindowCall(lookup, ref previous);
            owner.EmitTupleCheck(0, 2, finish);
            owner.Op(GetTupleElement, XRegisterTag, 0);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.Operand(YRegisterTag, 0);
            owner.Op(IsInteger, LabelTag, finish);
            owner.Operand(YRegisterTag, 0);
            owner.Op(IsGreaterOrEqual, LabelTag, upperCheck);
            owner.LiteralOperand(Term.I(minimum.Bound - 1));
            owner.EmitTypedRegister(0, IntegerTypeBits, registerTag: YRegisterTag);
            EmitThrow(minimum.Throw, previous);
            Mark(upperCheck);
            owner.Op(IsGreaterOrEqual, LabelTag, lookupActive);
            owner.EmitTypedRegister(0, IntegerTypeBits, lower: minimum.Bound, registerTag: YRegisterTag);
            owner.LiteralOperand(Term.I(maximum.Bound + 1));
            EmitThrow(maximum.Throw, previous);
            Mark(lookupActive);
            LoadTupleField(1, 0, XRegisterTag, 0);
            owner.InitStackSlot(1);
            EmitWindowCall(read, ref previous);
            var readLocation = previous;
            EmitDetailRecordTest(0, 3, ok, badMatch);
            owner.EmitTuplePart(0, 1, 1);
            EmitDetailRecordTest(1, optionRecord.Items.Count, optionTag, badMatch);
            owner.EmitTuplePart(1, activeField, 0);
            owner.Op(IsAtom, LabelTag, integerActive);
            owner.Operand(XRegisterTag, 0);
            owner.Op(IsGreaterOrEqual, LabelTag, integerActive);
            owner.LiteralOperand(Term.I(threshold));
            owner.EmitTypedRegister(0, IntegerTypeBits, minimum.Bound, maximum.Bound, YRegisterTag);
            EmitPassive(firstTag, atomSend.SourceLine, readLocation);
            Mark(integerActive);
            owner.Op(IsInteger, LabelTag, finish);
            owner.Operand(XRegisterTag, 0);
            owner.Op(GcBifTwo, LabelTag, finish);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, ErlangOperators.Plus, 2));
            owner.EmitTypedRegister(0, IntegerTypeBits);
            owner.EmitTypedRegister(0, IntegerTypeBits, minimum.Bound, maximum.Bound, YRegisterTag);
            owner.Operand(XRegisterTag, 0);
            owner.Op(IsGreaterOrEqual, LabelTag, sumCheck);
            owner.EmitTypedRegister(0, IntegerTypeBits);
            owner.LiteralOperand(Term.I(maximum.Bound + 1));
            EmitThrow(overflow, readLocation);
            Mark(sumCheck);
            owner.Op(IsGreaterOrEqual, LabelTag, finish);
            owner.LiteralOperand(Term.I(threshold));
            owner.EmitTypedRegister(0, IntegerTypeBits, upper: maximum.Bound);
            EmitPassive(secondTag, integerSend.SourceLine, readLocation);
            Mark(finish);
            owner.EmitConstant(result, 0);
            owner.EmitFrameReturn(3);
            Mark(badMatch);
            owner.EmitLine(new(readMatch.PreprocessorFile ?? readLocation.File, readMatch.SourceLine));
            owner.Op(BadMatch, XRegisterTag, 0);

            return true;

            (long Bound, (Expr.Call Call, Atom Value) Throw)? ReadLimit(Clause clause, string operation)
            {
                if (clause is not { Patterns: [Pattern.Tuple { Items: [
                    Pattern.Variable { Name: VariableScopeNames.Wildcard }, Pattern.Variable value
                ] }], Guard: Expr.Binary { Operator: ErlangOperators.AndAlso, Left: Expr.Call test,
                    Right: Expr.Binary { Left: Expr.Variable operand, Right: var boundary } compare } }
                    || !DistinctBindings(options, socket, value, new(listener.Name))
                    || compare.Operator != operation || operand.Name != value.Name || !IsTypeCheck(test, AutoImportNames.IsInteger, value.Name)
                    || !ReadBound(boundary, out long bound) || ReadThrow(clause.Body) is not { } thrown)
                {
                    return null;
                }

                return (bound, thrown);
            }

            long? ReadSumGuard(Clause clause, string operation) => clause is {
                Patterns: [Pattern.Variable value], Guard: Expr.Binary { Operator: ErlangOperators.AndAlso, Left: Expr.Call test,
                    Right: Expr.Binary { Left: Expr.Binary { Operator: ErlangOperators.Plus,
                        Left: Expr.Variable left, Right: Expr.Variable right }, Right: var boundary } compare }
            } && DistinctBindings(options, socket, increment, new(listener.Name), new(active.Name), value)
                && compare.Operator == operation && left.Name == value.Name && right.Name == increment.Name
                && IsTypeCheck(test, AutoImportNames.IsInteger, value.Name) && ReadBound(boundary, out long bound) ? bound : null;

            Atom? ReadSend(Expr.Binary send) => send is { Operator: ErlangOperators.Send,
                Left: Expr.Call { Module: null or ErlangModuleNames.Module, Function: AutoImportNames.Self, DynamicRemote: false, Arguments: [] } self,
                Right: Expr.Tuple { Items: [Expr.Literal { Value: Atom tag }, Expr.Variable source] }
            } && source.Name == socket.Name && owner.ResolvesErlangBif(self) ? tag : null;
        }

        private bool IsTypeCheck(Expr.Call call, string function, string variable) =>
            call is { Module: null or ErlangModuleNames.Module, DynamicRemote: false, Arguments: [Expr.Variable input] }
            && call.Function == function && input.Name == variable && owner.ResolvesErlangBif(call);

        private static bool ReadBound(Expr expression, out long bound)
        {
            if (AttributeLiteral.TryNormalizeConstant(expression, out Term term) && term is Integer integer
                && integer.Value >= long.MinValue && integer.Value <= long.MaxValue)
            {
                bound = (long)integer.Value;

                return true;
            }

            bound = 0;
            return false;
        }

        private static bool IsFallback(Clause clause, out Atom atom)
        {
            if (clause is { Guard: null, Patterns: [Pattern.Variable { Name: VariableScopeNames.Wildcard }],
                Body: Expr.Literal { Value: Atom result } })
            {
                atom = result;

                return true;
            }

            atom = null!;
            return false;
        }

        private (Expr.Call Call, Atom Value)? ReadThrow(Expr expression) =>
            expression is Expr.Call { Module: null or ErlangModuleNames.Module, DynamicRemote: false,
                Function: AutoImportNames.Throw, Arguments: [Expr.Literal { Value: Atom value }] } call
            && owner.ResolvesErlangBif(call) ? (call, value) : null;

        private static int? ReadActiveField(Pattern.Tuple record, string active)
        {
            int? found = null;
            for (int index = 1; index < record.Items.Count; index++)
            {
                if (record.Items[index] is Pattern.Variable variable && variable.Name == active && found is null)
                {
                    found = index;
                }
                else if (record.Items[index] is not Pattern.Variable { Name: VariableScopeNames.Wildcard })
                {
                    return null;
                }
            }

            return found;
        }

        private void EmitThrow((Expr.Call Call, Atom Value) thrown, BeamSourceLocation previous)
        {
            owner.EmitConstant(thrown.Value, 0);
            EmitConfigurationTail(thrown.Call with { Module = ErlangModuleNames.Module }, 3, previous);
        }

        private void EmitPassive(Atom tag, int line, BeamSourceLocation previous)
        {
            owner.Op(TestHeap, BeamWire.UnsignedTag, 3);
            owner.Operand(BeamWire.UnsignedTag, 0);
            owner.Op(BifZero, BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, AutoImportNames.Self, 0));
            owner.Operand(XRegisterTag, 0);
            owner.EmitTagMatchTuple(1, (AtomTag, owner.Intern(tag.Name)), (YRegisterTag, 2));
            owner.EmitArithmeticLine(line, ref previous);
            owner.instructions.WriteByte(Send);
            owner.EmitFrameReturn(3);
        }
    }
}
