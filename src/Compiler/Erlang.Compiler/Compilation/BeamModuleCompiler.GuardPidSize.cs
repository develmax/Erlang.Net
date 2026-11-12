using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private const int MaxTupleArity = 0xFFFFFF;

    private static bool IsSimpleGuardCall(Expr expression) =>
        expression is Expr.Call
        {
            Module: null or ErlangModuleNames.Module,
            DynamicRemote: false,
            Function: AutoImportNames.IsPid or AutoImportNames.TupleSize or AutoImportNames.IsMap or AutoImportNames.IsNumber
                or AutoImportNames.Hd or AutoImportNames.Node or AutoImportNames.IsPort or AutoImportNames.IsFloat
                or AutoImportNames.IsBitstring or AutoImportNames.IsReference,
            Arguments.Count: 1
        } or Expr.Call
        {
            Module: null or ErlangModuleNames.Module,
            DynamicRemote: false,
            Function: AutoImportNames.IsMapKey or AutoImportNames.MapGet,
            Arguments.Count: 2
        } or Expr.Call { Module: null or ErlangModuleNames.Module, DynamicRemote: false, Function: AutoImportNames.Node or AutoImportNames.Self, Arguments.Count: 0 };

    private sealed partial class GeneralEmitter
    {
        private bool IsGuardBif(Expr.Call call, string name) =>
            call.Function == name && call.Arguments.Count == 1 && !call.DynamicRemote
            && (call.Module == ErlangModuleNames.Module || call.Module is null && !entries.ContainsKey((name, 1)));

        private bool IsZeroGuardBif(Expr.Call call) =>
            call is { Function: AutoImportNames.Node or AutoImportNames.Self, DynamicRemote: false, Arguments.Count: 0 }
            && (call.Module == ErlangModuleNames.Module || call.Module is null && !entries.ContainsKey((call.Function, 0)));

        private void EmitZeroGuardBif(string function, int destination)
        {
            owner.Op(BifZero, BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, function, 0));
            owner.Operand(XRegisterTag, destination);
        }

        private bool TryEmitEntryTupleGuardAlternatives(Clause clause, Expr.GuardAlternatives guard, int failure)
        {
            if (guard.Items.Count != 3 || !HasUnconditionalParameters(clause))
            {
                return false;
            }

            var parameters = clause.Patterns.Cast<Pattern.Variable>().Select(parameter => parameter.Name).ToArray();
            var groups = new List<(int Register, int? Arity)[]>();
            foreach (Expr alternative in guard.Items)
            {
                var tests = new List<(int Register, int? Arity)>();
                foreach (Expr test in Conjuncts(alternative))
                {
                    if (ReadTupleArityGuard(test) is { Value: Expr.Variable tuple, Arity: var arity }
                        && Array.IndexOf(parameters, tuple.Name) is int register && register >= 0)
                    {
                        tests.Add((register, arity));
                    }
                    else if (ReadEntryTypeGuard(clause, test) is { Opcode: IsFloat, Register: var floatRegister })
                    {
                        tests.Add((floatRegister, null));
                    }
                    else
                    {
                        return false;
                    }
                }

                if (!tests.Any(test => test.Arity.HasValue)
                    || parameters.Length + groups.Count + tests.Count > FirstReservedXRegister)
                {
                    return false;
                }
                groups.Add(tests.ToArray());
            }

            int baseRegister = parameters.Length;
            for (int groupIndex = 0; groupIndex < groups.Count; groupIndex++)
            {
                int result = baseRegister + groupIndex;
                int rejected = Label();
                int done = Label();
                var tests = groups[groupIndex];
                for (int index = 0; index < tests.Length; index++)
                {
                    int destination = result + index;
                    var (register, arity) = tests[index];
                    owner.Op(BifOne, LabelTag, arity.HasValue ? rejected : 0);
                    owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module,
                        arity.HasValue ? AutoImportNames.TupleSize : AutoImportNames.IsFloat, 1));
                    owner.Operand(XRegisterTag, register);
                    owner.Operand(XRegisterTag, destination);
                    if (arity is int size)
                    {
                        owner.Op(BifTwo, LabelTag, 0);
                        owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, ErlangOperators.ExactEqual, 2));
                        owner.EmitTypedRegister(destination, IntegerTypeBits, lower: 0, upper: MaxTupleArity);
                        owner.Operand(BeamWire.IntegerTag, size);
                        owner.Operand(XRegisterTag, destination);
                    }
                }
                for (int index = tests.Length - 2; index >= 0; index--)
                {
                    EmitBooleanBif("and", result + index, result + index + 1);
                }
                JumpTo(done);
                Mark(rejected);
                owner.EmitConstant(Term.A("false"), result);
                Mark(done);
            }

            EmitBooleanBif("or", baseRegister + 1, baseRegister + 2);
            int accepted = Label();
            EmitBooleanTest(baseRegister, "false", accepted);
            EmitBooleanTest(baseRegister + 1, "true", failure);
            Mark(accepted);
            return true;

            void EmitBooleanBif(string operation, int left, int right)
            {
                owner.Op(BifTwo, LabelTag, 0);
                owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, operation, 2));
                owner.EmitTypedRegister(left, AtomTypeBits);
                owner.EmitTypedRegister(right, AtomTypeBits);
                owner.Operand(XRegisterTag, left);
            }

            void EmitBooleanTest(int register, string value, int rejected)
            {
                owner.Op(ExactEqualTest, LabelTag, rejected);
                owner.EmitTypedRegister(register, AtomTypeBits);
                owner.LiteralOperand(Term.A(value));
            }

            static IEnumerable<Expr> Conjuncts(Expr expression) =>
                expression is Expr.Binary { Operator: ErlangOperators.AndAlso } conjunction
                    ? Conjuncts(conjunction.Left).Concat(Conjuncts(conjunction.Right))
                    : [expression];
        }

        private (Expr Value, int Arity)? ReadTupleArityGuard(Expr guard)
        {
            if (guard is not Expr.Binary { Operator: ErlangOperators.ExactEqual } comparison)
            {
                return null;
            }

            var (call, literal) = comparison.Left is Expr.Call
                ? (comparison.Left, comparison.Right)
                : (comparison.Right, comparison.Left);

            return call is Expr.Call size && IsGuardBif(size, AutoImportNames.TupleSize)
                && literal is Expr.Literal { Value: Integer arity }
                && arity.Value >= 0 && arity.Value <= MaxTupleArity
                ? (size.Arguments[0], (int)arity.Value)
                : null;
        }

        private void EmitTupleArityGuard(int register, int arity, int failure)
        {
            owner.Op(IsTuple, LabelTag, failure);
            owner.Operand(XRegisterTag, register);
            owner.Op(TestArity, LabelTag, failure);
            owner.Operand(XRegisterTag, register);
            owner.Operand(BeamWire.UnsignedTag, arity);
        }

        private void EmitUnaryGuardBif(Expr.Call call, int failure)
        {
            Expr operand = call.Arguments[0];
            int? slot = GuardSlot(operand, failure);
            EmitUnaryGuardInstruction(call.Function, failure);
            GuardOperand(operand, slot);
            owner.Operand(XRegisterTag, 0);
        }

        private void EmitUnaryGuardInstruction(string function, int failure)
        {
            owner.Op(BifOne, LabelTag, failure);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, function, 1));
        }

        private bool TryEmitDirectTypeOrArityGuard(FunctionDefinition function)
        {
            if (function.Clauses is not [
                { Guard: { } guard, Body: Expr.Literal accepted } first,
                { Guard: null, Body: Expr.Literal rejected } fallback
            ] || !HasUnconditionalParameters(first) || !HasUnconditionalParameters(fallback))
            {
                return false;
            }

            var arity = ReadTupleArityGuard(guard);
            Expr? operand = arity?.Value;
            byte test = 0;
            if (guard is Expr.Call call && IsGuardBif(call, call.Function))
            {
                test = call.Function switch
                {
                    AutoImportNames.IsPid => IsPid,
                    AutoImportNames.IsPort => IsPort,
                    AutoImportNames.IsFloat => IsFloat,
                    AutoImportNames.IsMap => IsMap,
                    AutoImportNames.IsNumber => IsNumber,
                    _ => 0
                };
                if (test != 0)
                {
                    operand = call.Arguments[0];
                }
            }
            if (operand is not Expr.Variable variable)
            {
                return false;
            }

            for (int register = 0; register < first.Patterns.Count; register++)
            {
                if (first.Patterns[register] is not Pattern.Variable parameter || parameter.Name != variable.Name)
                {
                    continue;
                }

                int failure = Label();
                if (arity is { } tuple)
                {
                    EmitTupleArityGuard(register, tuple.Arity, failure);
                }
                else
                {
                    owner.Op(test, LabelTag, failure);
                    owner.Operand(XRegisterTag, register);
                }
                owner.EmitConstant(accepted.Value, 0);
                owner.instructions.WriteByte(Return);
                Mark(failure);
                owner.EmitConstant(rejected.Value, 0);
                owner.instructions.WriteByte(Return);

                return true;
            }

            return false;
        }
    }
}
