using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private const long OtpSizeUpperBound = (1L << 58) - 1;

    private static bool IsListOrFunctionGuardCall(Expr expression) =>
        expression is Expr.Call
        {
            Module: null or ErlangModuleNames.Module, DynamicRemote: false,
            Function: AutoImportNames.Length or AutoImportNames.IsFunction or AutoImportNames.ByteSize or AutoImportNames.MapSize
                or AutoImportNames.Abs or AutoImportNames.BitSize or AutoImportNames.Trunc or AutoImportNames.Float or AutoImportNames.Min or AutoImportNames.Max
                or AutoImportNames.Round or AutoImportNames.Ceil or AutoImportNames.Floor
        };

    private sealed partial class GeneralEmitter
    {
        private int EmitGuardedBifTailPrefix(IReadOnlyList<Clause> clauses, int functionInfo)
        {
            int emitted = 0;
            while (emitted < clauses.Count)
            {
                Clause clause = clauses[emitted];
                if (emitted + 1 < clauses.Count
                    && TryEmitComplementaryArithmeticClauses(clause, clauses[emitted + 1], emitted + 2 == clauses.Count, functionInfo))
                {
                    emitted += 2;
                    continue;
                }

                if (TryEmitEntryArithmeticClause(clause, emitted + 1 == clauses.Count, functionInfo))
                {
                    emitted++;
                    continue;
                }

                if (!HasUnconditionalParameters(clause) || clause.Guard is not Expr.Call
                    {
                        Module: null or ErlangModuleNames.Module, DynamicRemote: false,
                        Arguments: [Expr.Variable tested]
                    } guard
                    || !owner.ResolvesErlangBif(guard)
                    || clause.Body is not Expr.Call { Module: null, DynamicRemote: false } tail
                    || !entries.TryGetValue((tail.Function, tail.Arguments.Count), out int entry)
                    || tail.Arguments.OfType<Expr.Call>().ToArray() is not [var bif]
                    || bif is not { Module: null or ErlangModuleNames.Module, Arguments: [Expr.Variable input] }
                    || tested.Name != input.Name || !IsGcUnaryGuardCall(bif))
                {
                    break;
                }

                (byte opcode, int type) = guard.Function switch
                {
                    AutoImportNames.IsFloat => (IsFloat, FloatTypeBits),
                    AutoImportNames.IsInteger => (IsInteger, IntegerTypeBits),
                    AutoImportNames.IsNumber => (IsNumber, NumberTypeBits),
                    _ => default
                };
                var parameters = clause.Patterns.Cast<Pattern.Variable>().Select(parameter => parameter.Name).ToArray();
                int source = Array.IndexOf(parameters, input.Name);
                if (opcode == 0 || source < 0 || input.Name == VariableScopeNames.Wildcard)
                {
                    break;
                }

                const string result = "\0guarded-bif-result";
                var arguments = tail.Arguments.Select(argument => ReferenceEquals(argument, bif)
                    ? new Expr.Variable(result) : argument).ToArray();
                parameters[source] = result;
                if (ReadEntryCall(tail with { Arguments = arguments }, parameters) is not { } plan)
                {
                    break;
                }

                emitted++;
                int failure = emitted == clauses.Count ? functionInfo : Label();
                owner.Op(opcode, LabelTag, failure);
                owner.Operand(XRegisterTag, source);
                owner.EmitCallLine(bif, location);
                EmitSizeInstruction(bif.Function, 0, clause.Patterns.Count);
                owner.EmitTypedRegister(source, type);
                owner.Operand(XRegisterTag, source);
                owner.EmitOrderedTailMoves(plan.Moves, plan.Constants);
                owner.Op(TailLocalCall, BeamWire.UnsignedTag, tail.Arguments.Count);
                owner.Operand(LabelTag, entry);
                if (failure != functionInfo)
                {
                    Mark(failure);
                }
            }

            return emitted;
        }

        private bool IsFunctionArityCall(Expr.Call call) =>
            call is { Function: AutoImportNames.IsFunction, Arguments.Count: 2, DynamicRemote: false }
            && (call.Module == ErlangModuleNames.Module || call.Module is null && !entries.ContainsKey((AutoImportNames.IsFunction, 2)));

        private bool IsSizeGuardCall(Expr.Call call) =>
            call.Function is AutoImportNames.Length or AutoImportNames.ByteSize && IsGuardBif(call, call.Function)
            || call is { Function: AutoImportNames.MapSize, Arguments.Count: 1, DynamicRemote: false }
                && owner.ResolvesErlangBif(call);

        private bool IsGcUnaryGuardCall(Expr.Call call) =>
            IsSizeGuardCall(call)
            || call is
            {
                Function: AutoImportNames.Abs or AutoImportNames.BitSize or AutoImportNames.Trunc or AutoImportNames.Float
                    or AutoImportNames.Round or AutoImportNames.Ceil or AutoImportNames.Floor,
                Arguments.Count: 1,
                DynamicRemote: false
            } && owner.ResolvesErlangBif(call);

        private void EmitGcUnaryGuard(Expr.Call call, int failure)
        {
            Expr value = call.Arguments[0];
            int? slot = GuardSlot(value, failure);
            EmitSizeInstruction(call.Function, failure, ReceiveGuardLiveRegisters);
            GuardOperand(value, slot);
            owner.Operand(XRegisterTag, 0);
        }

        private void EmitBodySizeBif(Expr.Call call)
        {
            int operandBase = nextSlot;
            Expr operand = call.Arguments[0];
            int? slot = operand is Expr.Variable variable && variables.TryGetValue(variable.Name, out int saved)
                ? saved : null;
            bool direct = slot.HasValue || AttributeLiteral.TryNormalizeConstant(operand, out _);
            if (!direct)
            {
                Expression(operand);
            }

            owner.EmitCallLine(call, owner.lastEmittedLine);
            EmitSizeInstruction(call.Function, 0, Math.Max(ReceiveGuardLiveRegisters, direct ? 0 : 1));
            if (direct)
            {
                GuardOperand(operand, slot);
            }
            else
            {
                owner.Operand(XRegisterTag, 0);
            }
            owner.Operand(XRegisterTag, 0);
            ReleaseOperandSlots(operandBase);
        }

        private void EmitSizeInstruction(string function, int failure, int live)
        {
            owner.Op(GcBifOne, LabelTag, failure);
            owner.Operand(BeamWire.UnsignedTag, live);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, function, 1));
        }

        private void EmitFunctionArityGuardValue(Expr.Call call, int failure)
        {
            Expr function = call.Arguments[0];
            Expr arity = call.Arguments[1];
            int? functionSlot = GuardSlot(function, failure);
            int? aritySlot = GuardSlot(arity, failure);
            EmitFunctionArityInstruction(failure);
            GuardOperand(function, functionSlot);
            GuardOperand(arity, aritySlot);
            owner.Operand(XRegisterTag, 0);
        }

        private void EmitFunctionArityInstruction(int failure)
        {
            owner.Op(BifTwo, LabelTag, failure);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, AutoImportNames.IsFunction, 2));
        }

        private bool TryEmitDirectListOrFunctionGuard(FunctionDefinition function)
        {
            if (function.Clauses is not [
                { Guard: { } guard, Body: Expr.Literal accepted } first,
                { Guard: null, Body: Expr.Literal rejected } fallback
            ] || !HasUnconditionalParameters(first) || !HasUnconditionalParameters(fallback))
            {
                return false;
            }

            int failure;
            if (guard is Expr.Call call && call.Arguments.All(IsCallOperand)
                && (IsGuardBif(call, AutoImportNames.IsFunction) || IsFunctionArityCall(call)))
            {
                failure = Label();
                EmitDirectFunctionGuard(first, call, failure);
            }
            else if (guard is Expr.Binary
                {
                    Operator: ErlangOperators.ExactEqual,
                    Left: Expr.Call { Arguments: [Expr.Variable list] } length,
                    Right: Expr.Literal { Value: Integer expected }
                } && IsSizeGuardCall(length))
            {
                failure = Label();
                int register = ArgumentRegister(first, list.Name);
                EmitSizeInstruction(length.Function, failure, register + 1);
                owner.Operand(XRegisterTag, register);
                owner.Operand(XRegisterTag, 0);
                owner.Op(ExactEqualTest, LabelTag, failure);
                owner.EmitTypedRegister(0, IntegerTypeBits, 0, OtpSizeUpperBound);
                owner.LiteralOperand(expected);
            }
            else
            {
                return false;
            }

            EmitConstantGuardBranches(accepted, rejected, failure);

            return true;
        }

        private void EmitDirectFunctionGuard(Clause clause, Expr.Call call, int failure)
        {
            if (call.Arguments.Count == 1)
            {
                owner.Op(IsFunction, LabelTag, failure);
                EmitDirectOperand(clause, call.Arguments[0]);
                return;
            }
            if (call.Arguments[1] is Expr.Literal { Value: Integer arity } && arity.Value >= 0 && arity.Value <= byte.MaxValue)
            {
                owner.Op(IsFunctionWithArity, LabelTag, failure);
                EmitDirectOperand(clause, call.Arguments[0]);
                owner.LiteralOperand(arity);
                return;
            }

            EmitFunctionArityInstruction(failure);
            EmitDirectOperand(clause, call.Arguments[0]);
            EmitDirectOperand(clause, call.Arguments[1]);
            owner.Operand(XRegisterTag, 0);
            owner.EmitAtomTest(0, "true", failure);
        }
    }
}
