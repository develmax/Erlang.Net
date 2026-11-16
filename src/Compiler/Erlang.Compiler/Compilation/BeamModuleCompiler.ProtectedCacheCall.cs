using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitProtectedCacheCall(FunctionDefinition function)
        {
            if (function.Clauses is not [{
                Guard: null, Body: Expr.Try {
                    After: null, Body: Expr.Call { Module: not null, DynamicRemote: false } call,
                    Catches: [{ Guard: null, Patterns: [Pattern.Tuple { Items: [
                        Pattern.Variable { Name: VariableScopeNames.Wildcard },
                        Pattern.Variable { Name: VariableScopeNames.Wildcard },
                        Pattern.Variable { Name: VariableScopeNames.Wildcard }
                    ] }], Body: var fallback }]
                } attempt
            } clause] || !HasUnconditionalParameters(clause) || call.Arguments.Count != function.Arity)
            {
                return false;
            }

            var parameters = clause.Patterns.Cast<Pattern.Variable>().ToArray();
            ConstructorStep? construction = null;
            for (int index = 0; index < parameters.Length; index++)
            {
                if (call.Arguments[index] is Expr.Variable variable && variable.Name == parameters[index].Name)
                {
                    continue;
                }

                if (index != 1 || function.Arity != 2 || parameters[index].Name == VariableScopeNames.Wildcard
                    || !TryReadConstructor(call.Arguments[index],
                        new Dictionary<string, (Term? Constant, ConstructorStep? Step)>(StringComparer.Ordinal)
                        {
                            [parameters[index].Name] = (null, null)
                        },
                        out var argument) || argument.Step is null)
                {
                    return false;
                }

                construction = argument.Step;
            }

            bool constantFallback = AttributeLiteral.TryNormalizeConstant(fallback, out Term fallbackValue);
            int saved = fallback is Expr.Variable savedArgument
                ? Array.FindIndex(parameters, parameter => parameter.Name == savedArgument.Name) : -1;
            if (attempt.Clauses is [
                { Guard: null, Patterns: [Pattern.List { Tail: null, Items: [Pattern.Tuple { Items: [
                    Pattern.Variable key, Pattern.Variable value
                ] }] }], Body: Expr.Variable found },
                { Guard: null, Patterns: [var empty], Body: var absent }
            ] && function.Arity == 2 && construction is null && key.Name == parameters[1].Name
                && found.Name == value.Name && DistinctBindings(parameters[0], parameters[1], value)
                && IsEmptyListPattern(empty) && constantFallback
                && AttributeLiteral.TryNormalizeConstant(absent, out Term absentValue))
            {
                EmitProtectedKeyLookup(call, absentValue, fallbackValue);
                return true;
            }

            if (attempt.Clauses is not [{ Guard: null, Patterns: [Pattern.Variable result], Body: Expr.Variable returned }]
                || result.Name == VariableScopeNames.Wildcard || result.Name != returned.Name
                || parameters.Any(parameter => parameter.Name == result.Name)
                || (!constantFallback && saved < 0) || (construction is not null && saved >= 0))
            {
                return false;
            }

            int caught = Label();
            int token = saved < 0 ? 0 : 1;
            int slots = token + 1;
            if (construction is null)
            {
                owner.Op(Allocate, BeamWire.UnsignedTag, slots);
                owner.Operand(BeamWire.UnsignedTag, function.Arity);
            }
            else
            {
                EmitProtectedArgumentConstruction(construction);
            }

            if (saved >= 0)
            {
                owner.MoveRegister(XRegisterTag, saved, YRegisterTag, 0);
            }

            EmitProtectedCacheInvocation(call, caught, token);
            owner.EmitFrameReturn(slots);
            Mark(caught);
            owner.Op(TryCase, YRegisterTag, token);
            if (constantFallback)
            {
                owner.EmitConstant(fallbackValue, 0);
            }
            else
            {
                Load(0);
            }

            owner.EmitFrameReturn(slots);

            return true;
        }

        private void EmitProtectedCacheInvocation(Expr.Call call, int caught, int token)
        {
            owner.Op(TryEnter, YRegisterTag, token);
            owner.Operand(LabelTag, caught);
            owner.EmitCallLine(call, location);
            owner.EmitStaticRemoteCall(call);
            owner.Op(TryEnd, YRegisterTag, token);
        }

        private void EmitProtectedKeyLookup(Expr.Call call, Term absent, Term fallback)
        {
            int empty = Label();
            int caught = Label();
            int unmatched = Label();
            owner.Op(Allocate, BeamWire.UnsignedTag, 2);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.MoveRegister(XRegisterTag, 1, YRegisterTag, 0);
            EmitProtectedCacheInvocation(call, caught, 1);
            owner.EmitNonemptyList(0, empty);
            owner.EmitListParts(0, XRegisterTag, 1, XRegisterTag, 2);
            owner.EmitTupleCheck(1, 2, unmatched);
            owner.EmitNilTest(XRegisterTag, 2, unmatched);
            owner.EmitTuplePart(1, 0, 2);
            owner.Op(ExactEqualTest, LabelTag, unmatched);
            owner.Operand(XRegisterTag, 2);
            owner.Operand(YRegisterTag, 0);
            owner.EmitTuplePart(1, 1, 0);
            owner.EmitFrameReturn(2);
            Mark(empty);
            owner.EmitNilTest(XRegisterTag, 0, unmatched);
            owner.EmitConstant(absent, 0);
            owner.EmitFrameReturn(2);
            Mark(caught);
            owner.Op(TryCase, YRegisterTag, 1);
            owner.EmitConstant(fallback, 0);
            owner.EmitFrameReturn(2);
            Mark(unmatched);
            owner.EmitLine(CallLocation(call, location));
            owner.Op(TryCaseEnd, XRegisterTag, 0);
        }

        private void EmitProtectedArgumentConstruction(ConstructorStep construction)
        {
            var steps = new Stack<ConstructorStep>();
            int heap = 0;
            for (ConstructorStep? step = construction; step is not null; step = step.Input)
            {
                steps.Push(step);
                heap += step.HeapWords;
            }

            owner.Op(AllocateHeap, BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, heap);
            owner.Operand(BeamWire.UnsignedTag, 2);
            foreach (var step in steps)
            {
                if (step.IsTuple)
                {
                    owner.EmitTupleHeader(XRegisterTag, step.Operands.Length, 1);
                }
                else
                {
                    owner.instructions.WriteByte(PutList);
                }

                foreach (var operand in step.Operands)
                {
                    if (operand is null)
                    {
                        owner.Operand(XRegisterTag, 1);
                    }
                    else
                    {
                        owner.LiteralOperand(operand);
                    }
                }

                if (!step.IsTuple)
                {
                    owner.Operand(XRegisterTag, 1);
                }
            }
        }
    }
}
