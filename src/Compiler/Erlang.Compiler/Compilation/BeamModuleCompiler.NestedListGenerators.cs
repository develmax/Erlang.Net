using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed record NestedListGenerators(
        string Outer,
        string Inner,
        bool ProperOuter,
        bool AtomOuter,
        bool Pair,
        bool OuterFirst
    );

    private readonly Dictionary<string, NestedListGenerators> nestedListGenerators = new(StringComparer.Ordinal);

    private (FunctionDefinition Parent, IReadOnlyList<(FunctionDefinition Function, BeamSourceLocation Location)> Helpers)?
        ExtractNestedListGenerators(
            FunctionDefinition function,
            BeamSourceLocation fallback,
            HashSet<(string Name, int Arity)> declared
        )
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled
            || function.Clauses is not [var clause] || clause.Guard is not null || !HasUnconditionalParameters(clause))
        {
            return null;
        }

        var expressions = LocalFunctionEntries.Walk(clause.Body).ToArray();
        if (expressions.Any(expression => expression is Expr.Fun or Expr.Try or Expr.Catch or Expr.Receive or Expr.Maybe)
            || expressions.OfType<Expr.ListComprehension>().ToArray() is not [
            {
                Items: [var item],
                Qualifiers: [
                        ComprehensionQualifier.Generator { Strict: false, Pattern: Pattern.Variable outer, Source: var first } owner,
                        ComprehensionQualifier.Generator { Strict: false, Pattern: Pattern.Variable inner, Source: Expr.Variable right } nested,
                        ..
                    ]
            } comprehension
            ] || !DistinctBindings(outer, inner)
            || !clause.Patterns.OfType<Pattern.Variable>().Any(parameter => parameter.Name == right.Name)
            || outer.Name == right.Name || inner.Name == right.Name)
        {
            return null;
        }

        bool proper = false;
        bool atomsOnly = false;
        if (first is Expr.Variable variable)
        {
            if (outer.Name == variable.Name || inner.Name == variable.Name
                || !clause.Patterns.OfType<Pattern.Variable>().Any(parameter => parameter.Name == variable.Name))
            {
                return null;
            }
        }
        else
        {
            if (!AttributeLiteral.TryNormalizeConstant(first, out Term literal) || literal is not Cons)
            {
                return null;
            }

            atomsOnly = true;
            while (literal is Cons cell)
            {
                atomsOnly &= cell.Head is Atom;
                literal = cell.Tail;
            }

            if (literal is not Nil)
            {
                return null;
            }

            proper = true;
        }

        bool pair = false;
        bool outerFirst = false;
        Expr? guard = null;
        if (comprehension.Qualifiers.Count == 2 && item is Expr.Tuple
            {
                Items: [Expr.Variable leftField, Expr.Variable rightField]
            })
        {
            outerFirst = leftField.Name == outer.Name && rightField.Name == inner.Name;
            pair = outerFirst || leftField.Name == inner.Name && rightField.Name == outer.Name;
            if (!pair)
            {
                return null;
            }
        }
        else if (comprehension.Qualifiers is [_, _, ComprehensionQualifier.Filter
        {
            Expression: Expr.Binary
            {
                Operator: ErlangOperators.ExactEqual,
                Left: Expr.Variable left,
                Right: Expr.Variable operand
            } comparison
        }] && left.Name == outer.Name && operand.Name == inner.Name
            && item is Expr.Variable mapped && mapped.Name == outer.Name)
        {
            guard = comparison;
        }
        else
        {
            return null;
        }

        string outerName = MappingName(function.Name, function.Arity);
        string innerName = MappingName(
            function.Name,
            function.Arity,
            1,
            1
        );
        if (declared.Contains((outerName, 2)) || declared.Contains((innerName, 4)))
        {
            return null;
        }

        const string stackTail = "@BeamComprehensionTail";
        const string retainedTail = "@BeamOuterListRemainder";
        var saved = new Pattern.Variable(right.Name);
        var outerRemainder = new Pattern.Variable(retainedTail);
        var outerLoop = BuildRecursiveMapping(
            outerName,
            outer,
            item,
            [saved]
        );
        var outerClauses = outerLoop.Clauses.ToArray();
        outerClauses[0] = outerClauses[0] with
        {
            Body = new Expr.Call(null, innerName, [
                right,
                right,
                new Expr.Variable(stackTail),
                new Expr.Variable(outer.Name)
            ])
        };
        outerLoop = outerLoop with { Clauses = outerClauses };
        var innerLoop = BuildRecursiveMapping(
            innerName,
            inner,
            item,
            [saved, outerRemainder, outer],
            filter: guard is null ? null : (guard, true)
        );
        var innerClauses = innerLoop.Clauses.ToArray();
        innerClauses[^2] = innerClauses[^2] with
        {
            Patterns = [new Pattern.List([], null), saved, outerRemainder, new Pattern.Variable(VariableScopeNames.Wildcard)],
            Body = new Expr.Call(null, outerName, [new Expr.Variable(retainedTail), right])
        };
        innerLoop = innerLoop with { Clauses = innerClauses };

        var firstLocation = new BeamSourceLocation(
            owner.GeneratorFile ?? comprehension.PreprocessorFile ?? fallback.File,
            owner.GeneratorLine == 0 ? fallback.Line : owner.GeneratorLine
        );
        var innerLocation = new BeamSourceLocation(
            nested.GeneratorFile ?? firstLocation.File,
            nested.GeneratorLine == 0 ? firstLocation.Line : nested.GeneratorLine
        );
        var call = new Expr.Call(null, outerName, [first, right])
        {
            SourceLine = firstLocation.Line,
            PreprocessorFile = firstLocation.File
        };
        Expr body = BeamTargetedInlining.Rewrite(
            clause.Body,
            expression => expression,
            rewriteScope: expression => ReferenceEquals(expression, comprehension) ? call : null
        );
        var plan = new NestedListGenerators(
            outerName,
            innerName,
            proper,
            atomsOnly,
            pair,
            outerFirst
        );
        nestedListGenerators.Add(outerName, plan);
        nestedListGenerators.Add(innerName, plan);
        declared.Add((outerName, 2));
        declared.Add((innerName, 4));
        recursiveDefinitionOrder.Add((function.Name, function.Arity), [(outerName, 2), (innerName, 4)]);

        return (function with { Clauses = [clause with { Body = body }] },
                [(outerLoop, firstLocation), (innerLoop, innerLocation)]);
    }

    private sealed partial class GeneralEmitter
    {
        private bool TryEmitNestedListGenerators(FunctionDefinition function)
        {
            if (!owner.nestedListGenerators.TryGetValue(function.Name, out var plan)
                || !entries.TryGetValue((plan.Outer, 2), out int outer)
                || !entries.TryGetValue((plan.Inner, 4), out int inner))
            {
                return false;
            }

            int exhausted = Label();
            owner.EmitNonemptyList(0, exhausted);
            if (function.Name == plan.Outer)
            {
                owner.EmitListParts(
                    0,
                    XRegisterTag,
                    3,
                    XRegisterTag,
                    2
                );
                owner.MoveToX0(XRegisterTag, 1);
                Tail(4, inner);
                Mark(exhausted);
                if (plan.ProperOuter)
                {
                    owner.instructions.WriteByte(Return);
                }
                else
                {
                    int invalid = Label();
                    owner.EmitNilTest(XRegisterTag, 0, invalid);
                    owner.instructions.WriteByte(Return);
                    Mark(invalid);
                    owner.EmitListMappingError(1, 0, ErlangErrorReasons.BadGenerator);
                }

                return true;
            }

            if (plan.Pair)
            {
                EmitFrameAllocation(4, 2);
                owner.MoveRegister(
                    XRegisterTag,
                    3,
                    YRegisterTag,
                    1
                );
                owner.EmitListParts(
                    0,
                    YRegisterTag,
                    0,
                    XRegisterTag,
                    0
                );
                owner.EmitFieldCall(4, inner);
                owner.Op(TestHeap, BeamWire.UnsignedTag, ConsHeapWords + 3);
                owner.Operand(BeamWire.UnsignedTag, 1);
                owner.EmitTupleHeader(XRegisterTag, 2, 1);
                owner.Operand(YRegisterTag, plan.OuterFirst ? 1 : 0);
                owner.Operand(YRegisterTag, plan.OuterFirst ? 0 : 1);
                Put(XRegisterTag, 1, 2);
            }
            else
            {
                int skip = Label();
                owner.EmitListParts(
                    0,
                    XRegisterTag,
                    4,
                    XRegisterTag,
                    0
                );
                owner.Op(ExactEqualTest, LabelTag, skip);
                if (plan.AtomOuter)
                {
                    owner.EmitTypedRegister(3, AtomTypeBits);
                }
                else
                {
                    owner.Operand(XRegisterTag, 3);
                }

                owner.Operand(XRegisterTag, 4);
                EmitFrameAllocation(4, 1);
                owner.MoveRegister(
                    XRegisterTag,
                    3,
                    YRegisterTag,
                    0
                );
                owner.EmitRecursiveCons(inner, 4);
                Mark(skip);
                Tail(4, inner);
            }

            Mark(exhausted);
            int bad = Label();
            owner.EmitNilTest(XRegisterTag, 0, bad);
            owner.MoveToX0(XRegisterTag, 2);
            Tail(2, outer);
            Mark(bad);
            owner.EmitListMappingError(1, 0, ErlangErrorReasons.BadGenerator);

            return true;

            void Tail(int arity, int target)
            {
                owner.Op(TailLocalCall, BeamWire.UnsignedTag, arity);
                owner.Operand(LabelTag, target);
            }

            void Put(byte tag, int index, int frame)
            {
                owner.Op(PutList, tag, index);
                owner.Operand(XRegisterTag, 0);
                owner.Operand(XRegisterTag, 0);
                owner.EmitFrameReturn(frame);
            }
        }
    }
}
