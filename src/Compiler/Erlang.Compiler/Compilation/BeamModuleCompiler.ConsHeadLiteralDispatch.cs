using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitConsHeadLiteralDispatch(FunctionDefinition function, int functionInfo)
        {
            if (owner.ModuleOptimizationDisabled || recursiveClosure is not null
                || function.Arity >= FirstReservedXRegister
                || function.Clauses is not
                [
                { Guard: null } first,
                { Guard: null } second,
                { Guard: null } consFallback,
                { Guard: null } empty
                ])
            {
                return false;
            }

            int input = ReadSharedHeadConsInput(function.Clauses);
            if (input < 0 || first.Patterns[input] is not Pattern.List
                { Items: [Pattern.Literal { Value: Atom firstKey }], Tail: Pattern.Variable firstTail }
                || second.Patterns[input] is not Pattern.List
                { Items: [Pattern.Literal { Value: Atom secondKey }], Tail: Pattern.Variable secondTail }
                || firstKey.Equals(secondKey)
                || consFallback.Patterns[input] is not Pattern.List
                {
                    Items: [Pattern.Variable { Name: VariableScopeNames.Wildcard }],
                    Tail: Pattern.Variable { Name: not VariableScopeNames.Wildcard } fallbackTail
                }
                || ListEntryTest(empty.Patterns[input]) != IsNil
                || !HasDistinctVariables(first, input, firstTail)
                || !HasDistinctVariables(second, input, secondTail)
                || !HasDistinctVariables(consFallback, input, fallbackTail)
                || !HasDistinctVariables(empty, input))
            {
                return false;
            }

            if (ReadArm(first, firstTail.Name) is not { } left
                || ReadArm(second, secondTail.Name) is not { } right
                || ReadArm(consFallback, fallbackTail.Name) is not { Tail: { } } miss
                || ReadArm(empty, VariableScopeNames.Wildcard) is not { Tail: null } nonCons
                || SameTail(left.Tail, miss.Tail) || SameTail(right.Tail, miss.Tail)
                || SameTail(left.Tail, right.Tail))
            {
                return false;
            }

            bool sharedLiteral = left.Literal is not null && left.Literal.Equals(right.Literal);
            if (sharedLiteral && firstKey.Name is ErlangBooleanAtoms.True or ErlangBooleanAtoms.False
                && secondKey.Name is ErlangBooleanAtoms.True or ErlangBooleanAtoms.False)
            {
                // beam_jump selects is_boolean when the two boolean keys share a target.
                return false;
            }

            if (TermOrder.Compare(firstKey, secondKey, exact: true) > 0)
            {
                (firstKey, secondKey) = (secondKey, firstKey);
                (left, right) = (right, left);
            }

            int upper = Label();
            int lower = sharedLiteral ? upper : Label();
            int consMiss = Label();
            int nonConsInput = Label();
            int head = function.Arity;
            owner.EmitNonemptyList(input, nonConsInput);
            owner.EmitListParts(
                input,
                XRegisterTag,
                head,
                XRegisterTag,
                input
            );
            owner.EmitMembershipSelect(head, consMiss, [(firstKey, lower), (secondKey, upper)]);
            Mark(upper);
            EmitBody(right);
            if (!sharedLiteral)
            {
                Mark(lower);
                EmitBody(left);
            }

            Mark(consMiss);
            EmitBody(miss);
            Mark(nonConsInput);
            owner.EmitNilTest(XRegisterTag, input, functionInfo);
            EmitBody(nonCons);

            return true;

            (EntryCall? Tail, Term? Literal, int Register)? ReadArm(Clause clause, string tail)
            {
                string[] registers = clause.Patterns.Select((pattern, index) =>
                    index == input ? tail : ((Pattern.Variable)pattern).Name).ToArray();
                if (AttributeLiteral.TryNormalizeConstant(clause.Body, out Term constant))
                {
                    return (null, constant, -1);
                }

                if (clause.Body is Expr.Variable { Name: not VariableScopeNames.Wildcard } binding
                    && Array.IndexOf(registers, binding.Name) is int register && register >= 0)
                {
                    return (null, null, register);
                }

                if (tail != VariableScopeNames.Wildcard
                    && clause.Body is Expr.Call { Module: null, DynamicRemote: false } call
                    && call.Function == function.Name && call.Arguments.Count == function.Arity
                    && call.Arguments[input] is Expr.Variable remaining && remaining.Name == tail
                    && ReadEntryCall(call, registers) is { } target)
                {
                    return (target, null, -1);
                }

                return null;
            }

            void EmitBody((EntryCall? Tail, Term? Literal, int Register) arm)
            {
                if (arm.Tail is { } call)
                {
                    EmitRegisterTailCall(call);

                    return;
                }

                if (arm.Literal is not null)
                {
                    owner.EmitConstant(arm.Literal, 0);
                }
                else if (arm.Register != 0)
                {
                    owner.MoveRegister(
                        XRegisterTag,
                        arm.Register,
                        XRegisterTag,
                        0
                    );
                }

                owner.instructions.WriteByte(Return);
            }

            static bool SameTail(EntryCall? firstCall, EntryCall? secondCall) => firstCall is not null && secondCall is not null
                && firstCall.Moves.SequenceEqual(secondCall.Moves) && firstCall.Constants.SequenceEqual(secondCall.Constants);
        }
    }
}
