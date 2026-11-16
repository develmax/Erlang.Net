using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitRegisterLookupCase(FunctionDefinition function)
        {
            if (function.Clauses is not
                [{
                    Guard: null, Body: Expr.Case
                    {
                        Value: Expr.Call { Module: not (null or ErlangModuleNames.Module), DynamicRemote: false } lookup,
                        Clauses:
                        [
                            {
                                Guard: null, Patterns: [Pattern.List
                                {
                                    Items: [Pattern.Tuple { Items: [Pattern.Variable key, Pattern.Variable value] }],
                                    Tail: null
                                }],
                                Body: Expr.Tuple { Items: [Expr.Literal { Value: Atom tag }, Expr.Variable selected] }
                            },
                            {
                                Guard: null, Patterns: [Pattern.List { Items.Count: 0, Tail: null } or Pattern.Literal { Value: Nil }],
                                Body: Expr.Call { Module: null, DynamicRemote: false } miss
                            }
                        ]
                    }
                } clause]
                || !HasUnconditionalParameters(clause) || value.Name != selected.Name
                || value.Name == VariableScopeNames.Wildcard
                || !entries.TryGetValue((miss.Function, miss.Arguments.Count), out int target))
            {
                return false;
            }

            var parameters = clause.Patterns.Cast<Pattern.Variable>().Select(parameter => parameter.Name).ToArray();
            int keyRegister = Array.IndexOf(parameters, key.Name);
            if (keyRegister < 0 || key.Name == VariableScopeNames.Wildcard || parameters.Contains(value.Name)
                || lookup.Arguments.Count > parameters.Length)
            {
                return false;
            }

            for (int index = 0; index < lookup.Arguments.Count; index++)
            {
                if (lookup.Arguments[index] is not Expr.Variable argument || argument.Name != parameters[index]
                    || argument.Name == VariableScopeNames.Wildcard)
                {
                    return false;
                }
            }

            var outgoing = new (int Register, Term? Constant)[miss.Arguments.Count];
            var preserved = new SortedSet<int> { keyRegister };
            for (int index = 0; index < outgoing.Length; index++)
            {
                Expr argument = miss.Arguments[index];
                if (argument is Expr.Variable variable && variable.Name != VariableScopeNames.Wildcard
                    && Array.IndexOf(parameters, variable.Name) is int register && register >= 0)
                {
                    outgoing[index] = (register, null);
                    preserved.Add(register);
                }
                else if (AttributeLiteral.TryNormalizeConstant(argument, out Term constant))
                {
                    outgoing[index] = (-1, constant);
                }
                else
                {
                    return false;
                }
            }

            int frame = preserved.Count;
            int[] saved = preserved.Reverse().ToArray();
            int empty = Label();
            int failed = Label();
            owner.Op(Allocate, BeamWire.UnsignedTag, frame);
            owner.Operand(BeamWire.UnsignedTag, function.Arity);
            for (int slot = 0; slot < saved.Length; slot++)
            {
                owner.MoveRegister(XRegisterTag, saved[slot], YRegisterTag, slot);
            }

            owner.EmitCallLine(lookup, location);
            owner.EmitStaticRemoteCall(lookup);
            owner.EmitNonemptyList(0, empty);
            owner.EmitListParts(0, XRegisterTag, 1, XRegisterTag, 2);
            EmitTupleShapeTest(XRegisterTag, 1, 2, failed);
            owner.EmitNilTest(XRegisterTag, 2, failed);
            owner.EmitTuplePart(1, 0, 2);
            owner.Op(ExactEqualTest, LabelTag, failed);
            owner.Operand(XRegisterTag, 2);
            owner.Operand(YRegisterTag, Array.IndexOf(saved, keyRegister));
            owner.Op(TestHeap, BeamWire.UnsignedTag, 3);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.EmitTuplePart(1, 1, 0);
            owner.EmitTagMatchTuple(0, (AtomTag, owner.Intern(tag.Name)), (XRegisterTag, 0));
            owner.EmitFrameReturn(frame);

            Mark(empty);
            owner.EmitNilTest(XRegisterTag, 0, failed);
            foreach (int destination in IndependentCallMoveOrder(outgoing.Length))
            {
                var (register, constant) = outgoing[destination];
                if (constant is null)
                {
                    Load(Array.IndexOf(saved, register), destination);
                }
                else
                {
                    owner.EmitConstant(constant, destination);
                }
            }

            owner.Op(TailLocalCallWithFrame, BeamWire.UnsignedTag, outgoing.Length);
            owner.Operand(LabelTag, target);
            owner.Operand(BeamWire.UnsignedTag, frame);
            Mark(failed);
            owner.Op(CaseEnd, XRegisterTag, 0);

            return true;
        }
    }
}
