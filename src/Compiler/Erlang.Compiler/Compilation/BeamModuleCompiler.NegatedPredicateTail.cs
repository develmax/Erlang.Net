using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        internal static bool HasNegatedPredicateTail(
            FunctionDefinition function,
            IReadOnlyDictionary<(string Name, int Arity), FunctionDefinition> functions
        ) => ReadNegatedPredicateTail(function, functions) is not null;

        private static (Expr.Call Test, Expr.Call Tail)? ReadNegatedPredicateTail(
            FunctionDefinition function,
            IReadOnlyDictionary<(string Name, int Arity), FunctionDefinition> functions
        )
        {
            if (function.Clauses is not [
                {
                    Guard: null, Patterns: [Pattern.Variable parameter],
                    Body: Expr.Binary
                    {
                        Operator: ErlangOperators.AndAlso,
                        Left: Expr.Unary
                        {
                            Operator: ErlangOperators.Not, Operand: Expr.Call
                            {
                                Module: null, DynamicRemote: false, Arguments: [Expr.Variable tested]
                            } test
                        },
                        Right: Expr.Call { Module: null, DynamicRemote: false, Arguments: [Expr.Variable forwarded] } tail
                    }
                }] || parameter.Name == VariableScopeNames.Wildcard
                || tested.Name != parameter.Name || forwarded.Name != parameter.Name
                || !functions.TryGetValue((test.Function, 1), out var predicate)
                || predicate.Clauses is not [
                    {
                        Body: Expr.Binary
                        {
                            Operator: ErlangOperators.ExactEqual or ErlangOperators.ExactNotEqual
                        }
                    }]
                || !functions.ContainsKey((tail.Function, 1)))
            {
                return null;
            }

            return (test, tail);
        }

        private bool TryEmitNegatedPredicateTail(FunctionDefinition function)
        {
            if (ReadNegatedPredicateTail(function, localFunctions) is not { } flow)
            {
                return false;
            }

            int tail = Label();
            owner.Op(Allocate, BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.MoveRegister(
                XRegisterTag,
                0,
                YRegisterTag,
                0
            );
            owner.EmitCallLine(flow.Test, location);
            owner.EmitFieldCall(1, entries[(flow.Test.Function, 1)]);
            owner.EmitAtomTest(0, "true", tail);
            owner.EmitConstant(new Atom("false"), 0);
            owner.EmitFrameReturn(1);

            Mark(tail);
            owner.MoveRegister(
                YRegisterTag,
                0,
                XRegisterTag,
                0
            );
            owner.Op(TailLocalCallWithFrame, BeamWire.UnsignedTag, 1);
            owner.Operand(LabelTag, entries[(flow.Tail.Function, 1)]);
            owner.Operand(BeamWire.UnsignedTag, 1);

            return true;
        }
    }
}
