using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private sealed record StateCall(
            Expr.Call Call,
            List<(int Destination, Term Value)> Arguments,
            Expr.Match? Check,
            Atom? Expected
        );

        private bool TryEmitStateReturningDispatch(FunctionDefinition function)
        {
            if (function.Clauses.Count < 3 || function.Clauses[^1] is not
                {
                    Guard: null,
                    Patterns: [Pattern.Variable ignored, Pattern.Variable state],
                    Body: Expr.Tuple { Items: [Expr.Literal { Value: Atom tag }, Expr.Variable carried] }
                } || !DistinctBindings(state) || ignored.Name == state.Name || carried.Name != state.Name)
            {
                return false;
            }

            var cases = new Dictionary<Atom, List<StateCall>>();
            foreach (Clause clause in function.Clauses.SkipLast(1))
            {
                if (clause is not
                    {
                        Guard: null, Patterns: [Pattern.Literal { Value: Atom key }, Pattern.Variable input],
                        Body: Expr.Sequence
                        {
                            Items: [_, .., Expr.Tuple
                            {
                                Items: [Expr.Literal { Value: Atom branchTag }, Expr.Variable returned]
                            }]
                        } sequence
                    } || input.Name != returned.Name || !DistinctBindings(input) || !branchTag.Equals(tag)
                    || ReadStateCalls(sequence) is not { } calls || !cases.TryAdd(key, calls))
                {
                    return false;
                }
            }

            var comparer = Comparer<Atom>.Create((left, right) => TermOrder.Compare(left, right, exact: true));
            var branches = cases.OrderByDescending(pair => pair.Key, comparer)
                .Select(pair => (pair.Key, Calls: pair.Value, Label: Label())).ToArray();
            int fallback = Label();
            owner.Op(SelectValue, XRegisterTag, 0);
            owner.Operand(LabelTag, fallback);
            owner.instructions.WriteByte(ExtendedListOperand);
            owner.Operand(BeamWire.UnsignedTag, branches.Length * 2);
            foreach (var branch in branches.Reverse())
            {
                owner.LiteralOperand(branch.Key);
                owner.Operand(LabelTag, branch.Label);
            }

            var failures = new List<(int Label, BeamSourceLocation Location)>();
            foreach (var branch in branches)
            {
                Mark(branch.Label);
                owner.Op(Allocate, BeamWire.UnsignedTag, 1);
                owner.Operand(BeamWire.UnsignedTag, 2);
                owner.MoveRegister(
                    XRegisterTag,
                    1,
                    YRegisterTag,
                    0
                );
                var previous = location;
                foreach (var (call, arguments, check, expected) in branch.Calls)
                {
                    owner.EmitIndependentCallMoves(arguments);
                    owner.EmitCallLine(call, previous);
                    previous = CallLocation(call, previous);
                    Call(call.Module, call.Function, arguments.Count);
                    if (expected is not null)
                    {
                        int failed = Label();
                        var checkLocation = check!.SourceLine == 0 ? previous : new(check.PreprocessorFile, check.SourceLine);
                        failures.Add((failed, checkLocation));
                        owner.EmitAtomTest(0, expected.Name, failed);
                    }
                }
                EmitStateResult(
                    tag,
                    YRegisterTag,
                    0,
                    0
                );
                owner.EmitFrameReturn(1);
            }

            Mark(fallback);
            EmitStateResult(
                tag,
                XRegisterTag,
                1,
                2
            );
            owner.instructions.WriteByte(Return);
            foreach (var (failed, checkLocation) in failures)
            {
                Mark(failed);
                owner.EmitLine(checkLocation);
                owner.Op(BadMatch, XRegisterTag, 0);
            }

            return true;
        }

        private static List<StateCall>? ReadStateCalls(Expr.Sequence sequence)
        {
            var calls = new List<StateCall>();
            foreach (Expr expression in sequence.Items.SkipLast(1))
            {
                var check = expression as Expr.Match;
                if (check is not null && check.Pattern is not Pattern.Literal { Value: Atom })
                {
                    return null;
                }
                if ((check?.Value ?? expression) is not Expr.Call { Module: not null, DynamicRemote: false } call)
                {
                    return null;
                }
                var arguments = new List<(int Destination, Term Value)>();
                foreach (Expr argument in call.Arguments)
                {
                    if (!AttributeLiteral.TryNormalizeConstant(argument, out Term constant))
                    {
                        return null;
                    }
                    arguments.Add((arguments.Count, constant));
                }
                calls.Add(new(
                    call,
                    arguments,
                    check,
                    (check?.Pattern as Pattern.Literal)?.Value as Atom
                ));
            }

            return calls;
        }

        private void EmitStateResult(
            Atom tag,
            int registerTag,
            int register,
            int live
        )
        {
            owner.Op(TestHeap, BeamWire.UnsignedTag, 3);
            owner.Operand(BeamWire.UnsignedTag, live);
            owner.EmitTagMatchTuple(0, (AtomTag, owner.Intern(tag.Name)), (registerTag, register));
        }
    }
}
