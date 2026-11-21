using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private sealed record RetainedRecordChoice(Expr.Call Call, Atom Item, Expr Fallback);

        private bool TryEmitRetainedRecordProjectionChain(FunctionDefinition function)
        {
            if (owner.ModuleOptimizationDisabled || !owner.SsaTypeInferenceEnabled || owner.OnLoadFunction is not null
                || owner.SourceModule?.DeclaredNifs is { Count: > 0 }
                || function.Clauses is not
                [
                    {
                        Guard: null,
                        Patterns: [Pattern.Variable { Name: not VariableScopeNames.Wildcard } input],
                        Body: Expr.Case
                        {
                            Value: Expr.Call { Arguments: [Expr.Literal firstKey, Expr.RecordAccess data] } firstLookup,
                            Clauses:
                            [
                            { Guard: null, Patterns: [Pattern.Tuple tuple], Body: Expr.Tuple reply },
                            {
                                Guard: null, Patterns: [Pattern.Literal { Value: Atom absent }], Body: Expr.Case
                                {
                                    Value: Expr.Call { Arguments: [Expr.Literal secondKey, Expr.RecordAccess sameData] } secondLookup,
                                    Clauses:
                                        [
                                        { Guard: null, Patterns: [Pattern.Literal { Value: Atom missing }], Body: Expr.Sequence chain },
                                        { Guard: null, Patterns: [Pattern.Variable other], Body: Expr.Tuple partial }
                                        ]
                                }
                            }
                            ]
                        } initial
                    }
                ]
                || !absent.Equals(missing) || other.Name == input.Name
                || data.Value is not Expr.Variable record || record.Name != input.Name
                || !SameField(sameData, data) || !HasDiscardedTupleFields(tuple, record.Name)
                || reply.Items is not [Expr.Literal { Value: Atom response }, Expr.RecordAccess returned] || !SameField(returned, data)
                || !IsRecordReply(partial, response, data)
                || !TryResolveOrdinaryCallTarget(firstLookup, out string? firstModule) || firstModule is null
                || !TryResolveOrdinaryCallTarget(secondLookup, out string? secondModule) || secondModule is null)
            {
                return false;
            }

            if (chain.Items is not
                [
                    Expr.Match { Pattern: Pattern.Variable first, Value: Expr.Call seed },
                    Expr.Match { Pattern: Pattern.Variable second, Value: Expr.Call transform },
                    Expr.Match { Pattern: Pattern.Variable retained, Value: Expr.Call retain },
                    Expr.Match { Pattern: Pattern.Variable selected, Value: Expr.Call lookup },
                    Expr.Case choose
                ]
                || !DistinctBindings([input, first, second, retained, selected])
                || seed.Arguments is not [Expr.RecordAccess f0, Expr.RecordAccess f1, Expr.RecordAccess f2]
                || !SameRecord(f0, data) || !SameRecord(f1, data) || !SameRecord(f2, data)
                || transform.Arguments is not [Expr.Variable transient] || transient.Name != first.Name
                || retain.Arguments is not [Expr.RecordAccess retainedField, Expr.Variable prior, Expr.Literal retainValue]
                || !SameRecord(retainedField, data) || prior.Name != second.Name
                || !ReadRetainedRecordLookup(
                    lookup,
                    data,
                    out var lookupField,
                    out var lookupKey,
                    out var lookupModule
                )
                || ReadRetainedRecordChoice(
                    choose,
                    data,
                    response,
                    [input, first, second, retained, selected]
                ) is not { } primary
                || primary.Call.Arguments is not [Expr.RecordAccess primaryField, Expr.Variable retainedInput, Expr.Variable selectedInput]
                || !SameRecord(primaryField, data) || retainedInput.Name != retained.Name || selectedInput.Name != selected.Name
                || primary.Fallback is not Expr.Sequence
                {
                    Items: [
                        Expr.Match { Pattern: Pattern.Variable saved, Value: Expr.Call fallbackLookup },
                        Expr.Case fallbackCase
                    ]
                }
                || !DistinctBindings([input, first, second, retained, selected, saved])
                || !ReadRetainedRecordLookup(
                    fallbackLookup,
                    data,
                    out var fallbackField,
                    out var fallbackKey,
                    out var fallbackModule
                )
                || ReadRetainedRecordChoice(
                    fallbackCase,
                    data,
                    response,
                    [input, first, second, retained, selected, saved]
                ) is not { } fallback
                || fallback.Call.Arguments is not [Expr.RecordAccess left, Expr.RecordAccess middle, Expr.Variable savedInput]
                || !SameRecord(left, data) || !SameRecord(middle, data)
                || savedInput.Name != saved.Name || !IsRecordReply(fallback.Fallback, response, data)
                || !TryResolveOrdinaryCallTarget(seed, out string? seedModule) || seedModule is null
                || !TryResolveOrdinaryCallTarget(transform, out string? transformModule) || transformModule is null
                || !TryResolveOrdinaryCallTarget(retain, out string? retainModule) || retainModule is null)
            {
                return false;
            }

            int badRecord = Label();
            int badCase = Label();
            int noInitialValue = Label();
            int defaultReply = Label();
            int alternative = Label();

            owner.EmitRecordTest(
                badRecord,
                XRegisterTag,
                0,
                Term.A(data.Name),
                data.Arity
            );
            EmitFrameAllocation(1, 2);
            owner.InitStackSlot(0);
            owner.MoveRegister(
                XRegisterTag,
                0,
                YRegisterTag,
                1
            );
            owner.EmitTuplePart(0, data.Index, 1);
            owner.EmitConstant(firstKey.Value, 0);
            var previous = location;
            EmitStep(firstLookup, firstModule);
            owner.Op(IsTuple, LabelTag, noInitialValue);
            owner.Operand(XRegisterTag, 0);
            owner.Op(TestArity, LabelTag, badCase);
            owner.Operand(XRegisterTag, 0);
            owner.Operand(BeamWire.UnsignedTag, tuple.Items.Count);
            EmitReply(false, null);

            Mark(noInitialValue);
            EmitEquality(absent, badCase);
            Project(data, 1);
            owner.EmitConstant(secondKey.Value, 0);
            EmitStep(secondLookup, secondModule);
            EmitEquality(missing, defaultReply);

            Project(f0, 0);
            Project(f1, 1);
            Project(f2, 2);
            EmitStep(seed, seedModule);
            EmitStep(transform, transformModule);
            Project(retainedField, 1);
            owner.EmitConstant(retainValue.Value, 2);
            owner.Op(Swap, XRegisterTag, 0);
            owner.Operand(XRegisterTag, 1);
            EmitStep(retain, retainModule);
            Project(lookupField, 1);
            owner.MoveRegister(
                XRegisterTag,
                0,
                YRegisterTag,
                0
            );
            owner.MoveRegister(
                XRegisterTag,
                1,
                XRegisterTag,
                0
            );
            owner.EmitConstant(lookupKey, 1);
            EmitStep(lookup, lookupModule);
            Project(primaryField, 1);
            owner.MoveRegister(
                XRegisterTag,
                0,
                XRegisterTag,
                2
            );
            owner.MoveRegister(
                XRegisterTag,
                1,
                XRegisterTag,
                0
            );
            owner.MoveRegister(
                YRegisterTag,
                0,
                XRegisterTag,
                1
            );
            owner.InitStackSlot(0);
            EmitStep(primary.Call, null);
            EmitChoice(primary.Item, alternative);

            Mark(alternative);
            Project(fallbackField, 0);
            owner.EmitConstant(fallbackKey, 1);
            EmitStep(fallbackLookup, fallbackModule);
            Project(middle, 1);
            Project(left, 2);
            owner.Op(Swap, XRegisterTag, 0);
            owner.Operand(XRegisterTag, 2);
            EmitStep(fallback.Call, null);
            EmitChoice(fallback.Item, defaultReply);

            Mark(defaultReply);
            EmitReply(false, null);
            Mark(badCase);
            var caseFailure = new BeamSourceLocation(initial.PreprocessorFile ?? location.File, initial.SourceLine);
            owner.EmitLine(caseFailure);
            owner.Op(CaseEnd, XRegisterTag, 0);
            Mark(badRecord);
            var recordFailure = new BeamSourceLocation(data.PreprocessorFile ?? caseFailure.File, data.SourceLine);
            if (data.SourceLine != 0 && recordFailure != caseFailure)
            {
                owner.EmitLine(recordFailure);
            }
            owner.Op(BadRecord, XRegisterTag, 0);

            return true;

            void Project(Expr.RecordAccess field, int register) =>
                owner.EmitTuplePart(
                    1,
                    field.Index,
                    register,
                    YRegisterTag
                );

            void EmitStep(Expr.Call call, string? module)
            {
                owner.EmitCallLine(call, previous);
                Call(module, call.Function, call.Arguments.Count);
                previous = CallLocation(call, previous);
            }

            void EmitEquality(Atom value, int rejected)
            {
                owner.Op(ExactEqualTest, LabelTag, rejected);
                owner.Operand(XRegisterTag, 0);
                owner.LiteralOperand(value);
            }

            void EmitChoice(Atom item, int rejected)
            {
                owner.Op(IsTuple, LabelTag, rejected);
                owner.EmitTypedRegister(0, AtomTypeBits | TupleTypeBits);
                EmitReply(true, item);
            }

            void EmitReply(bool hasItem, Atom? item)
            {
                owner.Op(TestHeap, BeamWire.UnsignedTag, hasItem ? 8 : 3);
                owner.Operand(BeamWire.UnsignedTag, hasItem ? 1 : 0);
                if (hasItem)
                {
                    Project(data, 1);
                    owner.EmitTuplePart(0, 1, 0);
                    owner.EmitTupleHeader(XRegisterTag, 2);
                    owner.LiteralOperand(item!);
                    owner.Operand(XRegisterTag, 0);
                    owner.Op(PutList, XRegisterTag, 0);
                    owner.Operand(XRegisterTag, 1);
                    owner.Operand(XRegisterTag, 0);
                }
                else
                {
                    Project(data, 0);
                }

                owner.EmitTupleHeader(XRegisterTag, 2);
                owner.LiteralOperand(response);
                owner.Operand(XRegisterTag, 0);
                owner.EmitFrameReturn(2);
            }
        }

        private bool ReadRetainedRecordLookup(
            Expr.Call call,
            Expr.RecordAccess data,
            out Expr.RecordAccess field,
            out Term key,
            out string? module
        )
        {
            field = null!;
            key = null!;
            module = null;
            if (call.Arguments is not [Expr.RecordAccess record, Expr.Literal literal] || !SameRecord(record, data)
                || !TryResolveOrdinaryCallTarget(call, out module) || module is null)
            {
                return false;
            }

            field = record;
            key = literal.Value;

            return true;
        }

        private RetainedRecordChoice? ReadRetainedRecordChoice(
            Expr.Case choice,
            Expr.RecordAccess data,
            Atom reply,
            IReadOnlyList<Pattern.Variable> existing
        )
        {
            if (choice is not
                {
                    Value: Expr.Call call,
                    Clauses:
                    [
                    {
                        Guard: null, Patterns: [Pattern.Tuple { Items: [Pattern.Literal { Value: Atom tag }, Pattern.Variable found] }],
                        Body: Expr.Tuple
                        {
                            Items:
                                [
                                    Expr.Literal { Value: Atom constructed },
                                    Expr.List
                                {
                                    Items: [Expr.Tuple { Items: [Expr.Literal { Value: Atom item }, Expr.Variable value] }],
                                    Tail: Expr.RecordAccess tail
                                }
                                ]
                        }
                    },
                    { Guard: null, Patterns: [Pattern.Literal { Value: Atom alternative }], Body: var fallback }
                    ]
                }
                || !reply.Equals(constructed) || found.Name != value.Name || found.Name == VariableScopeNames.Wildcard
                || existing.Any(binding => binding.Name == found.Name) || !SameField(tail, data)
                || !ReturnsTaggedOrAtom(call, tag, alternative))
            {
                return null;
            }

            return new(call, item, fallback);
        }

        private bool ReturnsTaggedOrAtom(Expr.Call call, Atom tag, Atom alternative)
        {
            var target = (call.Function, call.Arguments.Count);
            if (call.Module is not null || call.DynamicRemote || owner.IsDeclaredNative(target)
                || !localFunctions.TryGetValue(target, out var function))
            {
                return false;
            }

            bool foundTag = false;
            bool foundAtom = false;
            foreach (var clause in function.Clauses)
            {
                switch (clause.Body)
                {
                    case Expr.Tuple { Items: [Expr.Literal { Value: Atom tagged }, _] } when tagged.Equals(tag):
                        foundTag = true;
                        break;
                    case Expr.Literal { Value: Atom scalar } when scalar.Equals(alternative):
                        foundAtom = true;
                        break;
                    case Expr.Call { Module: null, DynamicRemote: false } recursive
                        when (recursive.Function, recursive.Arguments.Count) == target:
                        break;
                    default:
                        return false;
                }
            }

            return foundTag && foundAtom;
        }

        private static bool SameRecord(Expr.RecordAccess field, Expr.RecordAccess reference) =>
            field.Name == reference.Name && field.Arity == reference.Arity
                && field.Value is Expr.Variable source && reference.Value is Expr.Variable root && source.Name == root.Name;

        private static bool SameField(Expr.RecordAccess field, Expr.RecordAccess reference) =>
            field.Index == reference.Index && SameRecord(field, reference);

        private static bool IsRecordReply(Expr expression, Atom response, Expr.RecordAccess data) =>
            expression is Expr.Tuple { Items: [Expr.Literal { Value: Atom result }, Expr.RecordAccess returned] }
                && response.Equals(result) && SameField(returned, data);

        private static bool HasDiscardedTupleFields(Pattern.Tuple tuple, string input)
        {
            var names = new HashSet<string>(StringComparer.Ordinal) { input };

            return tuple.Items.All(item => item is Erlang.Pattern.Any or Pattern.Variable { Name: VariableScopeNames.Wildcard }
                || item is Pattern.Variable variable && names.Add(variable.Name));
        }
    }
}
