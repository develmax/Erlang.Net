using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitNestedListAccumulator(FunctionDefinition function)
        {
            if (!owner.SsaTypeInferenceEnabled || owner.ModuleOptimizationDisabled || recursiveClosure is not null
                || owner.OnLoadFunction is not null || owner.IsDeclaredNative((function.Name, 2))
                || function.Clauses is not
                [
                {
                    Guard: null,
                    Patterns: [Pattern.List { Items.Count: 0, Tail: null } or Pattern.Literal { Value: Nil },
                            Pattern.Variable finished],
                    Body: Expr.Call { Arguments: [Expr.Variable result] } finish
                },
                {
                    Guard: null,
                    Patterns: [Pattern.List { Items: var prefix, Tail: Pattern.Variable tail },
                            Pattern.Variable accumulator],
                    Body: Expr.Call
                    {
                        Module: null, DynamicRemote: false,
                        Arguments: [Expr.Variable rest, Expr.List
                        {
                            Items: [Expr.Variable scalar], Tail: Expr.Variable previous
                        }]
                    } recur
                },
                {
                    Guard: null,
                    Patterns: [Pattern.List { Items: var fallback, Tail: Pattern.Variable fallbackTail },
                            Pattern.Variable fallbackAcc],
                    Body: Expr.Call
                    {
                        Module: null, DynamicRemote: false,
                        Arguments: [Expr.Variable fallbackRest, Expr.List
                        {
                            Items: [Expr.Tuple tuple], Tail: Expr.Variable fallbackPrevious
                        }]
                    } fallbackCall
                }
                ]
                || finished.Name == VariableScopeNames.Wildcard || finished.Name != result.Name
                || prefix.Count < 2 || prefix.Count != fallback.Count || prefix.Count + 2 >= FirstReservedXRegister
                || prefix[^1] is not Pattern.Variable final || !DistinctBindings(tail, accumulator, final)
                || scalar.Name != final.Name || rest.Name != tail.Name || previous.Name != accumulator.Name
                || fallbackRest.Name != fallbackTail.Name || fallbackPrevious.Name != fallbackAcc.Name
                || function.Name != recur.Function || function.Name != fallbackCall.Function
                || !entries.TryGetValue((function.Name, 2), out int entry)
                || !TryResolveOrdinaryCall(finish, out string? finishModule))
            {
                return false;
            }

            int width = prefix.Count;
            var literals = new List<Term>();
            foreach (Pattern position in prefix.Take(width - 1))
            {
                if (position is not Pattern.Literal { Value: Integer literal })
                {
                    return false;
                }

                literals.Add(literal);
            }

            var fields = fallback.OfType<Pattern.Variable>().ToArray();
            if (fields.Length != width || !DistinctBindings([fallbackTail, fallbackAcc, .. fields])
                || tuple.Items.Count < width || !tuple.Items.Skip(tuple.Items.Count - width)
                    .SequenceEqual(fields.Select(field => new Expr.Variable(field.Name))))
            {
                return false;
            }

            var padding = new List<Term>();
            foreach (Expr item in tuple.Items.Take(tuple.Items.Count - width))
            {
                if (!AttributeLiteral.TryNormalizeConstant(item, out Term literal))
                {
                    return false;
                }

                padding.Add(literal);
            }

            bool proper = HasProperListEntry(
                function,
                0,
                followForwarding: true,
                acceptBinaryListResult: true
            );
            const int list = 0;
            const int acc = 1;
            const int first = 2;
            const int restOfList = 3;
            const int lookahead = 4;
            const int lookaheadTail = 5;
            int info = owner.nextLabel - 2;
            int other = Label();
            int empty = Label();

            owner.EmitNonemptyList(list, empty);
            Extract(list, first, restOfList);
            TestLiteral(first, literals[0]);
            owner.EmitNonemptyList(restOfList, info);
            int unread = restOfList;
            foreach (Term literal in literals.Skip(1))
            {
                // Preserve the original suffix for the fallback. Later lookahead values are dead after each comparison.
                Extract(unread, lookahead, lookaheadTail);
                TestLiteral(lookahead, literal);
                unread = lookaheadTail;
                owner.EmitNonemptyList(unread, other);
            }

            TestHeapSpace(ConsHeapWords, unread + 1);
            Extract(unread, list, first);
            PrependAndRecur(first);

            Mark(other);
            for (int index = 1; index < width - 1; index++)
            {
                owner.EmitNonemptyList(restOfList, info);
                Extract(restOfList, lookahead + index - 1, restOfList);
            }

            owner.EmitNonemptyList(restOfList, info);
            TestHeapSpace(tuple.Items.Count + 1 + ConsHeapWords, width + 2);
            Extract(restOfList, list, restOfList);
            owner.EmitTupleHeader(XRegisterTag, tuple.Items.Count, list);
            foreach (Term literal in padding)
            {
                owner.LiteralOperand(literal);
            }

            owner.Operand(XRegisterTag, first);
            for (int index = 1; index < width - 1; index++)
            {
                owner.Operand(XRegisterTag, lookahead + index - 1);
            }

            owner.Operand(XRegisterTag, list);
            PrependAndRecur(restOfList);

            Mark(empty);
            if (!proper)
            {
                owner.EmitNilTest(XRegisterTag, list, info);
            }

            owner.MoveRegister(
                XRegisterTag,
                acc,
                XRegisterTag,
                list
            );
            EmitRegisterTailTarget(finish with { Module = finishModule }, 1);

            return true;

            void Extract(int source, int head, int suffix) =>
                owner.EmitListParts(
                    source,
                    XRegisterTag,
                    head,
                    XRegisterTag,
                    suffix
                );

            void TestLiteral(int register, Term literal)
            {
                owner.Op(ExactEqualTest, LabelTag, other);
                owner.Operand(XRegisterTag, register);
                owner.LiteralOperand(literal);
            }

            void TestHeapSpace(int words, int live)
            {
                owner.Op(TestHeap, BeamWire.UnsignedTag, words);
                owner.Operand(BeamWire.UnsignedTag, live);
            }

            void PrependAndRecur(int suffix)
            {
                owner.Op(PutList, XRegisterTag, list);
                owner.Operand(XRegisterTag, acc);
                owner.Operand(XRegisterTag, acc);
                owner.MoveRegister(
                    XRegisterTag,
                    suffix,
                    XRegisterTag,
                    list
                );
                owner.Op(TailLocalCall, BeamWire.UnsignedTag, 2);
                owner.Operand(LabelTag, entry);
            }
        }
    }
}
