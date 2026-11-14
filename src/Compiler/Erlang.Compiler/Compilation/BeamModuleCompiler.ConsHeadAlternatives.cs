using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitConsHeadAlternatives(FunctionDefinition function, int functionInfo)
        {
            if (function.Arity >= FirstReservedXRegister
                || function.Clauses is not
                [
                    { Guard: { } guard, Body: Expr.Call call } cons,
                    { Guard: null } rejected,
                    { Guard: null } empty
                ]
                || !AttributeLiteral.TryNormalizeConstant(rejected.Body, out Term failureValue)
                || !AttributeLiteral.TryNormalizeConstant(empty.Body, out Term emptyValue))
            {
                return false;
            }

            int input = Array.FindIndex(cons.Patterns.ToArray(), pattern => pattern is Erlang.Pattern.List);
            if (input < 0 || cons.Patterns[input] is not Erlang.Pattern.List
                { Items: [Erlang.Pattern.Variable head], Tail: Erlang.Pattern.Variable tail }
                || rejected.Patterns[input] is not Erlang.Pattern.List
                { Items: [Erlang.Pattern.Variable unusedHead], Tail: Erlang.Pattern.Variable unusedTail }
                || ListEntryTest(empty.Patterns[input]) != IsNil
                || !HasDistinctVariables(cons, input, head, tail)
                || !HasDistinctVariables(rejected, input, unusedHead, unusedTail)
                || !HasDistinctVariables(empty, input))
            {
                return false;
            }

            string[] parameters = cons.Patterns.Select((pattern, index) => index == input
                ? tail.Name : ((Erlang.Pattern.Variable)pattern).Name).ToArray();
            var keys = new List<(Term Value, bool Exact)>();
            if (!ReadKeys(guard) || keys.Count < 2
                || ReadEntryCall(call, parameters, allowControlTransfer: true) is not { } target)
            {
                return false;
            }

            int success = Label();
            int failure = Label();
            int noCons = Label();
            int headRegister = function.Arity;
            owner.EmitNonemptyList(input, noCons);
            bool usesTail = call.Arguments.OfType<Expr.Variable>().Any(argument => argument.Name == tail.Name);
            if (usesTail)
            {
                owner.EmitListParts(input, XRegisterTag, headRegister, XRegisterTag, input);
            }
            else
            {
                owner.Op(GetHead, XRegisterTag, input);
                owner.Operand(XRegisterTag, headRegister);
            }

            for (int index = 0; index < keys.Count; index++)
            {
                var (value, exact) = keys[index];
                bool last = index == keys.Count - 1;
                byte opcode = exact
                    ? last ? ExactEqualTest : ExactNotEqualTest
                    : last ? NumericEqualTest : NumericNotEqualTest;
                owner.Op(opcode, LabelTag, last ? failure : success);
                owner.Operand(XRegisterTag, headRegister);
                owner.LiteralOperand(value);
            }

            Mark(success);
            EmitRegisterTailCall(target);
            Mark(failure);
            owner.EmitConstant(failureValue, 0);
            owner.instructions.WriteByte(Return);
            Mark(noCons);
            if (!HasProperListEntry(function, input))
            {
                owner.EmitNilTest(XRegisterTag, input, functionInfo);
            }

            owner.EmitConstant(emptyValue, 0);
            owner.instructions.WriteByte(Return);

            return true;

            bool ReadKeys(Expr expression)
            {
                if (expression is Expr.GuardAlternatives guards)
                {
                    return guards.Items.All(ReadKeys);
                }

                if (expression is Expr.Binary { Operator: ErlangOperators.OrElse } alternatives)
                {
                    return ReadKeys(alternatives.Left) && ReadKeys(alternatives.Right);
                }

                if (expression is not Expr.Binary
                    { Operator: ErlangOperators.NumericEqual or ErlangOperators.ExactEqual, Left: var left, Right: var right } comparison)
                {
                    return false;
                }

                Expr? constant = left is Expr.Variable tested && tested.Name == head.Name ? right
                    : right is Expr.Variable other && other.Name == head.Name ? left : null;
                if (constant is null || !AttributeLiteral.TryNormalizeConstant(constant, out Term value)
                    || value is not (Atom or Integer))
                {
                    return false;
                }

                keys.Add((value, comparison.Operator == ErlangOperators.ExactEqual));

                return true;
            }
        }

        private static bool HasDistinctVariables(Clause clause, int excluded, params Erlang.Pattern.Variable[] fields)
        {
            var variables = clause.Patterns.Where((_, index) => index != excluded).ToArray();
            return variables.All(pattern => pattern is Erlang.Pattern.Variable)
                && HasUnconditionalParameters(clause with { Patterns = [.. variables, .. fields] });
        }
    }
}
