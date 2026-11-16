using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitRecordFieldDispatch(FunctionDefinition function)
        {
            return function.Arity == 2 && function.Clauses.Count >= 2
                && (TryEmitRecordProjectionDispatch(function.Clauses) || TryEmitRecordAccessTailDispatch(function.Clauses));
        }

        private bool TryEmitRecordProjectionDispatch(IReadOnlyList<Clause> clauses)
        {
            var branches = new SortedDictionary<Atom, (Atom Tag, int Arity, int Field)>(AtomDispatchOrder);
            foreach (Clause clause in clauses)
            {
                if (clause is not
                    {
                        Guard: null,
                        Patterns: [Pattern.Literal { Value: Atom key }, Pattern.Tuple { Items: [Pattern.Literal { Value: Atom tag }, ..] } tuple],
                        Body: Expr.Variable result
                    }
                    || result.Name == VariableScopeNames.Wildcard)
                {
                    return false;
                }

                int field = -1;
                for (int index = 1; index < tuple.Items.Count; index++)
                {
                    if (tuple.Items[index] is Pattern.Variable { Name: VariableScopeNames.Wildcard })
                    {
                        continue;
                    }

                    if (field != -1 || tuple.Items[index] is not Pattern.Variable variable || variable.Name != result.Name)
                    {
                        return false;
                    }

                    field = index;
                }

                if (field < 0 || !branches.TryAdd(key, (tag, tuple.Items.Count, field)))
                {
                    return false;
                }
            }

            int failure = owner.nextLabel - 2;
            var targets = branches.Values.Distinct().Reverse().ToDictionary(target => target, _ => Label());
            owner.EmitMembershipSelect(0, failure, branches.Select(branch => ((Term)branch.Key, targets[branch.Value])));
            foreach (var (target, label) in targets)
            {
                Mark(label);
                owner.EmitRecordTest(
                    failure,
                    XRegisterTag,
                    1,
                    target.Tag,
                    target.Arity
                );
                owner.EmitTuplePart(1, target.Field, 0);
                owner.instructions.WriteByte(Return);
            }

            return true;
        }

        private bool TryEmitRecordAccessTailDispatch(IReadOnlyList<Clause> clauses)
        {
            if (clauses is not
                [
                    _, _, ..,
                {
                    Guard: null, Patterns: [Erlang.Pattern.Variable, Pattern.Literal { Value: Atom absent }],
                    Body: Expr.Literal { Value: Atom absentResult }
                },
                {
                    Guard: null, Patterns: [Pattern.Variable left, Pattern.Variable right],
                    Body: Expr.Tuple { Items: [Expr.Variable first, Expr.Variable second] }
                } fallback
                ]
                || !absent.Equals(absentResult) || left.Name != first.Name || right.Name != second.Name
                || !HasUnconditionalParameters(fallback))
            {
                return false;
            }

            var branches = new SortedDictionary<Atom, (Expr.RecordAccess Access, int Entry, Term? Suppressed)>(AtomDispatchOrder);
            foreach (Clause clause in clauses.SkipLast(2))
            {
                Expr body = clause.Body;
                Expr.RecordAccess? checkedAccess = null;
                Term? suppressed = null;
                if (body is Expr.Case
                    {
                        Value: Expr.RecordAccess { SourceLine: > 0 } checkedField,
                        Clauses:
                        [
                        {
                            Guard: null, Patterns: [Pattern.Literal { Value: var sentinel }],
                            Body: Expr.Literal { Value: Atom rejected }
                        },
                        { Guard: null, Patterns: [Pattern.Variable { Name: VariableScopeNames.Wildcard }] } remaining
                        ]
                    } && rejected.Equals(absentResult))
                {
                    body = remaining.Body;
                    checkedAccess = checkedField;
                    suppressed = sentinel;
                }

                if (clause is not
                    {
                        Guard: null,
                        Patterns: [AliasPattern alias, Pattern.Variable record]
                    }
                    || body is not Expr.Call
                    {
                        Module: null, DynamicRemote: false,
                        Arguments:
                            [
                                Expr.Call
                            {
                                Module: null or ErlangModuleNames.Module,
                                DynamicRemote: false, Function: AutoImportNames.AtomToList,
                                Arguments: [Expr.Variable converted]
                            } conversion,
                                Expr.RecordAccess { Value: Expr.Variable accessed, SourceLine: > 0 } access
                            ]
                    } call
                    || record.Name == VariableScopeNames.Wildcard || record.Name != accessed.Name
                    || checkedAccess is not null && (checkedAccess.Value is not Expr.Variable tested || tested.Name != record.Name
                        || checkedAccess.Name != access.Name || checkedAccess.Arity != access.Arity || checkedAccess.Index != access.Index)
                    || !owner.ResolvesErlangBif(conversion)
                    || !entries.TryGetValue((call.Function, 2), out int entry))
                {
                    return false;
                }

                Pattern other = alias.Left is Pattern.Variable ? alias.Right : alias.Left;
                Pattern binding = alias.Left is Pattern.Variable ? alias.Left : alias.Right;
                if (other is not Pattern.Literal { Value: Atom key }
                    || binding is not Pattern.Variable variable || variable.Name != converted.Name
                    || variable.Name == record.Name || variable.Name == VariableScopeNames.Wildcard
                    || !branches.TryAdd(key, (checkedAccess ?? access, entry, suppressed)))
                {
                    return false;
                }
            }

            var ordered = branches.Reverse().ToArray();
            var labels = ordered.ToDictionary(branch => branch.Key, _ => Label());
            int otherKey = Label();
            int absentValue = ordered.Any(branch => branch.Value.Suppressed is not null) ? Label() : 0;
            int tuple = Label();
            var badRecords = ordered.Select(branch => AccessLocation(branch.Value.Access))
                .Distinct().ToDictionary(source => source, _ => Label());
            owner.EmitMembershipSelect(0, otherKey, branches.Select(branch => ((Term)branch.Key, labels[branch.Key])));
            foreach (var (key, branch) in ordered)
            {
                Mark(labels[key]);
                Expr.RecordAccess access = branch.Access;
                owner.EmitRecordTest(
                    badRecords[AccessLocation(access)],
                    XRegisterTag,
                    1,
                    Term.A(access.Name),
                    access.Arity
                );
                if (branch.Suppressed is { } sentinel)
                {
                    owner.EmitTuplePart(1, access.Index, 0);
                    owner.Op(ExactNotEqualTest, LabelTag, absentValue);
                    owner.Operand(XRegisterTag, 0);
                    owner.LiteralOperand(sentinel);
                    owner.MoveRegister(
                        XRegisterTag,
                        0,
                        XRegisterTag,
                        1
                    );
                }
                else
                {
                    owner.EmitTuplePart(1, access.Index, 1);
                }
                owner.EmitConstant(Term.String(key.Name), 0);
                owner.Op(TailLocalCall, BeamWire.UnsignedTag, 2);
                owner.Operand(LabelTag, branch.Entry);
            }

            Mark(otherKey);
            owner.EmitAtomTest(1, absent.Name, tuple);
            if (absentValue != 0)
            {
                Mark(absentValue);
            }

            owner.EmitConstant(absentResult, 0);
            owner.instructions.WriteByte(Return);
            Mark(tuple);
            owner.Op(TestHeap, BeamWire.UnsignedTag, 3);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.EmitTupleHeader(XRegisterTag, 2);
            owner.Operand(XRegisterTag, 0);
            owner.Operand(XRegisterTag, 1);
            owner.instructions.WriteByte(Return);
            foreach (var (source, label) in badRecords)
            {
                Mark(label);
                owner.EmitLine(source);
                owner.Op(BadRecord, XRegisterTag, 1);
            }

            return true;

            static BeamSourceLocation AccessLocation(Expr.RecordAccess access) =>
                new(access.PreprocessorFile, access.SourceLine);
        }

        private static readonly IComparer<Atom> AtomDispatchOrder =
            Comparer<Atom>.Create((left, right) => TermOrder.Compare(left, right, exact: true));
    }
}
