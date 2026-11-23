using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitClosedIntegerListCase(FunctionDefinition function)
        {
            if (!owner.SsaTypeInferenceEnabled || owner.ModuleOptimizationDisabled || recursiveClosure is not null
                || function.Clauses is not
                [
                    {
                        Guard: null,
                        Patterns: [Pattern.Variable scope, Pattern.Variable input],
                        Body: Expr.Case
                        {
                            Value: Expr.Variable scopeSource,
                            Clauses:
                            [
                            {
                                Guard: null,
                                Patterns: [Pattern.Literal { Value: Atom scopeKey }],
                                Body: Expr.Case { Value: Expr.Variable listSource, Clauses: [_, _, ..] } listCase
                            }
                            ]
                        } scopeCase
                    }
                ]
                || !DistinctBindings(scope, input) || scope.Name != scopeSource.Name || input.Name != listSource.Name)
            {
                return false;
            }

            int? prefix = null;
            var choices = new SortedDictionary<int, Term>();
            var results = new HashSet<Term>();
            foreach (Clause branch in listCase.Clauses)
            {
                if (branch is not
                    {
                        Guard: null,
                        Patterns:
                        [
                            Pattern.List
                        {
                            Items:
                                [
                                    Pattern.Literal { Value: Integer first },
                                    Pattern.Literal { Value: Integer second }
                                ],
                            Tail: null
                        }
                        ]
                    }
                    || first.Value < int.MinValue || first.Value > int.MaxValue
                    || second.Value < int.MinValue || second.Value > int.MaxValue
                    || (prefix.HasValue && prefix.Value != (int)first.Value)
                    || !AttributeLiteral.TryNormalizeConstant(branch.Body, out Term result)
                    || !choices.TryAdd((int)second.Value, result) || !results.Add(result))
                {
                    return false;
                }

                prefix = (int)first.Value;
            }

            var branches = choices.Reverse().Select(choice => (choice.Key, choice.Value, Target: Label())).ToArray();
            int badList = Label();
            int badScope = Label();

            owner.Op(ExactEqualTest, LabelTag, badScope);
            int scopeType = ReadAtomOrTupleEntryType(function, 0);
            if (scopeType == 0)
            {
                owner.Operand(XRegisterTag, 0);
            }
            else
            {
                owner.EmitTypedRegister(0, scopeType);
            }

            owner.LiteralOperand(scopeKey);

            // The atom failure owns the original X0. After that check, X0 is free
            // for both heads, while the list failure keeps the unmodified X1.
            owner.EmitNonemptyList(1, badList);
            owner.EmitListParts(
                1,
                XRegisterTag,
                0,
                XRegisterTag,
                2
            );
            owner.Op(ExactEqualTest, LabelTag, badList);
            owner.Operand(XRegisterTag, 0);
            owner.Operand(BeamWire.IntegerTag, prefix.GetValueOrDefault());
            owner.EmitNonemptyList(2, badList);
            owner.EmitListParts(
                2,
                XRegisterTag,
                0,
                XRegisterTag,
                2
            );

            EmitAritySelect(0, branches.Length, badList);
            foreach (var branch in branches.Reverse())
            {
                owner.Operand(BeamWire.IntegerTag, branch.Key);
                owner.Operand(LabelTag, branch.Target);
            }

            foreach (var branch in branches)
            {
                Mark(branch.Target);
                owner.Op(IsNil, LabelTag, badList);
                owner.Operand(XRegisterTag, 2);
                owner.EmitConstant(branch.Value, 0);
                owner.instructions.WriteByte(Return);
            }

            foreach (var (failure, at, register) in new[] { (badList, listCase, 1), (badScope, scopeCase, 0) })
            {
                Mark(failure);
                if (CaseSourceLocation(at) is { } source && source != owner.lastEmittedLine)
                {
                    owner.EmitLine(source);
                }

                owner.Op(CaseEnd, XRegisterTag, register);
            }

            return true;
        }
    }
}
