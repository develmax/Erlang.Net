using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private sealed record DictionaryRecordTail(
            Atom Tag,
            int TupleArity,
            int Field,
            Atom? Selector,
            Expr.Call Report,
            Atom LookupKey,
            List<(int Destination, Term Value)> Constants,
            Expr.Call Tail,
            Atom? TailItem
        );

        private bool TryEmitSplitDictionaryRecordTail(FunctionDefinition function)
        {
            if (owner.ModuleOptimizationDisabled || !owner.SsaTypeInferenceEnabled || recursiveClosure is not null
                || owner.OnLoadFunction is not null || owner.IsDeclaredNative((function.Name, function.Arity))
                || function.Clauses is not
                [
                {
                    Guard: null,
                    Patterns: [global::Erlang.Pattern.Variable original, var firstSelector],
                    Body: Expr.Call { Module: null, DynamicRemote: false, Arguments: [Expr.Variable forwarded] } forward
                },
                    var literal,
                    var general
                ]
                || original.Name == VariableScopeNames.Wildcard || forwarded.Name != original.Name
                || !entries.TryGetValue((forward.Function, 1), out int forwardEntry)
                || ReadDictionaryRecordSelector(firstSelector) is not (Atom first, var firstAlias)
                || firstAlias == original.Name
                || ReadDictionaryRecordTail(literal, expectLiteral: true) is not { Selector: { } selected } known
                || ReadDictionaryRecordTail(general, expectLiteral: false) is not { } generalPlan
                || first.Equals(selected) || !known.Tag.Equals(generalPlan.Tag)
                || known.TupleArity != generalPlan.TupleArity || known.Field != generalPlan.Field
                || known.Tail.Module != generalPlan.Tail.Module || known.Tail.Function != generalPlan.Tail.Function)
            {
                return false;
            }

            int failure = owner.nextLabel - 2;
            int tagged = Label();
            int other = Label();
            owner.EmitAtomTest(1, first.Name, tagged);
            EmitTailLocal(forwardEntry, 1, frame: null);

            Mark(tagged);
            EmitDetailRecordTest(
                0,
                known.TupleArity,
                known.Tag,
                failure
            );
            owner.EmitTuplePart(0, known.Field, 0);
            owner.EmitAtomTest(1, selected.Name, other);
            EmitArm(known, selected);

            Mark(other);
            EmitArm(generalPlan, null);

            return true;

            void EmitArm(DictionaryRecordTail arm, Atom? value)
            {
                // The original item is no longer a root in the literal arm.
                // The general arm still needs it for both calls.
                int frame = value is null ? 2 : 1;
                EmitFrameAllocation(frame, frame, frame * ConsHeapWords);
                owner.MoveRegister(
                    XRegisterTag,
                    0,
                    YRegisterTag,
                    0
                );
                if (value is null)
                {
                    owner.MoveRegister(
                        XRegisterTag,
                        1,
                        YRegisterTag,
                        1
                    );
                }

                owner.EmitDictionaryLookup(0, arm.LookupKey);
                if (value is null)
                {
                    owner.Op(PutList, XRegisterTag, 1);
                    owner.LiteralOperand(Nil.Value);
                    owner.Operand(XRegisterTag, 1);
                }

                owner.Op(PutList, YRegisterTag, 0);
                if (value is null)
                {
                    owner.Operand(XRegisterTag, 1);
                }
                else
                {
                    owner.LiteralOperand(new Cons(value, Nil.Value));
                }

                owner.Operand(XRegisterTag, arm.Report.Arguments.Count - 1);
                owner.EmitIndependentCallMoves(arm.Constants);
                owner.EmitCallLine(arm.Report, location);
                owner.EmitStaticRemoteCall(arm.Report);

                if (arm.TailItem is { } constant)
                {
                    owner.EmitConstant(constant, 1);
                }
                else
                {
                    owner.MoveRegister(
                        YRegisterTag,
                        1,
                        XRegisterTag,
                        1
                    );
                }

                owner.MoveToX0(YRegisterTag, 0);
                owner.EmitCallLine(arm.Tail, CallLocation(arm.Report, location));
                owner.Op(TailExternalCallWithFrame, BeamWire.UnsignedTag, 2);
                owner.Operand(BeamWire.UnsignedTag, owner.Import(arm.Tail.Module!, arm.Tail.Function, 2));
                owner.Operand(BeamWire.UnsignedTag, frame);
            }
        }

        private DictionaryRecordTail? ReadDictionaryRecordTail(Clause clause, bool expectLiteral)
        {
            if (clause is not
                {
                    Guard: null,
                    Patterns:
                    [
                        global::Erlang.Pattern.Tuple { Items: [global::Erlang.Pattern.Literal { Value: Atom tag }, ..] } tuple,
                        var itemPattern
                    ],
                    Body: Expr.Sequence
                    {
                        Items:
                    [
                        Expr.Call
                    {
                        Module: not (null or ErlangModuleNames.Module),
                        DynamicRemote: false,
                        Arguments: [Expr.Call
                        {
                            Module: null or ErlangModuleNames.Module,
                            DynamicRemote: false,
                            Function: AutoImportNames.Get,
                            Arguments: [Expr.Literal { Value: Atom lookupKey }]
                        } lookup, .., Expr.List { Tail: null, Items: [Expr.Variable field, var item] }]
                    } report,
                        Expr.Call
                    {
                        Module: not (null or ErlangModuleNames.Module),
                        DynamicRemote: false,
                        Arguments: [Expr.Variable receiver, var tailItem]
                    } tail
                    ]
                    }
                }
                || field.Name == VariableScopeNames.Wildcard || receiver.Name != field.Name
                || !owner.ResolvesErlangBif(lookup) || report.Arguments.Count is < 4 or >= FirstReservedXRegister)
            {
                return null;
            }

            int fieldIndex = -1;
            for (int index = 1; index < tuple.Items.Count; index++)
            {
                if (IsSharedEntryWildcard(tuple.Items[index]))
                {
                    continue;
                }

                if (fieldIndex >= 0 || tuple.Items[index] is not global::Erlang.Pattern.Variable variable || variable.Name != field.Name)
                {
                    return null;
                }

                fieldIndex = index;
            }

            if (fieldIndex < 0)
            {
                return null;
            }

            Atom? selector = null;
            Atom? constantItem = null;
            if (expectLiteral)
            {
                if (ReadDictionaryRecordSelector(itemPattern) is not (Atom atom, var alias) || alias == field.Name
                    || (item is not Expr.Variable named || alias != named.Name)
                        && (item is not Expr.Literal { Value: Atom literal } || !literal.Equals(atom))
                    || tailItem is not Expr.Literal { Value: Atom constant })
                {
                    return null;
                }

                selector = atom;
                constantItem = constant;
            }
            else if (itemPattern is not global::Erlang.Pattern.Variable input || input.Name == VariableScopeNames.Wildcard
                || input.Name == field.Name || item is not Expr.Variable argument || argument.Name != input.Name
                || tailItem is not Expr.Variable forwarded || forwarded.Name != input.Name)
            {
                return null;
            }

            var constants = new List<(int Destination, Term Value)>();
            for (int index = 1; index < report.Arguments.Count - 1; index++)
            {
                if (!AttributeLiteral.TryNormalizeConstant(report.Arguments[index], out Term constant))
                {
                    return null;
                }

                constants.Add((index, constant));
            }

            return new(
                tag,
                tuple.Items.Count,
                fieldIndex,
                selector,
                report,
                lookupKey,
                constants,
                tail,
                constantItem
            );
        }

        private static (Atom Atom, string? Alias)? ReadDictionaryRecordSelector(global::Erlang.Pattern pattern)
        {
            if (pattern is AliasPattern alias && TryReadPatternAlias(alias, out var binding, out var shape)
                && shape is global::Erlang.Pattern.Literal { Value: Atom selected })
            {
                return (selected, binding.Name);
            }

            return pattern is global::Erlang.Pattern.Literal { Value: Atom literal } ? (literal, null) : null;
        }
    }
}
