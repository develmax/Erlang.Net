using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitEntryTupleArithmeticResult(Clause clause)
        {
            if (clause.Guard is not null || !HasUnconditionalParameters(clause) || clause.Body is not Expr.Sequence
                {
                    Items: [
                        Expr.Match { Pattern: Pattern.Tuple pattern, Value: Expr.Call read },
                        Expr.Tuple result
                    ]
                }
                || read is not { Module: null, DynamicRemote: false }
                || pattern.Items.Count >= FirstReservedXRegister - 1
                || pattern.Items.Any(item => item is not Pattern.Variable _)
                || result.Items.OfType<Expr.Binary>().ToArray() is not [var arithmetic]
                || arithmetic.Operator is not (ErlangOperators.Plus or ErlangOperators.Minus or ErlangOperators.Multiply
                    or ErlangOperators.IntegerDivide or ErlangOperators.Remainder)
                || !ReturnsTuple(
                    read,
                    pattern.Items.Count,
                    [],
                    exact: true
                ))
            {
                return false;
            }

            string[] parameters = clause.Patterns.Cast<Pattern.Variable>().Select(parameter => parameter.Name).ToArray();
            var fields = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int field = 0; field < pattern.Items.Count; field++)
            {
                string name = ((Pattern.Variable)pattern.Items[field]).Name;
                if (name != VariableScopeNames.Wildcard && (parameters.Contains(name) || !fields.TryAdd(name, field)))
                {
                    return false;
                }
            }

            Expr[] inputs = [.. result.Items.Where(item => !ReferenceEquals(item, arithmetic)), arithmetic.Left, arithmetic.Right];
            if (inputs.Any(item => item is Expr.Variable variable
                    ? variable.Name == VariableScopeNames.Wildcard || !fields.ContainsKey(variable.Name) && !parameters.Contains(variable.Name)
                    : !AttributeLiteral.TryNormalizeConstant(item, out _)))
            {
                return false;
            }

            int[] saved = inputs.OfType<Expr.Variable>().Select(variable => Array.IndexOf(parameters, variable.Name))
                .Where(index => index >= 0).Distinct().OrderDescending().ToArray();
            if (ReadEntrySelector(read, parameters, saved) is not { } selector)
            {
                return false;
            }

            string[] arithmeticFields = new[] { arithmetic.Left, arithmetic.Right }.OfType<Expr.Variable>()
                .Select(variable => variable.Name).Where(fields.ContainsKey).Distinct().ToArray();
            string[] deferredFields = inputs.OfType<Expr.Variable>().Select(variable => variable.Name)
                .Where(name => fields.ContainsKey(name) && !arithmeticFields.Contains(name)).Distinct().ToArray();
            var registers = new Dictionary<string, int>(StringComparer.Ordinal);
            int nextRegister = 1;
            int trimmed = EmitEntrySelector(selector, read, parameters);
            foreach (string name in arithmeticFields)
            {
                owner.EmitTuplePart(0, fields[name], nextRegister);
                registers.Add(name, nextRegister++);
            }

            int live = nextRegister;
            string? reused = arithmeticFields.FirstOrDefault(name =>
                !result.Items.OfType<Expr.Variable>().Any(variable => variable.Name == name));
            int destination = deferredFields.Length == 0 && reused is null ? 0
                : reused is null ? nextRegister++ : registers[reused];
            var line = CallLocation(read, location);
            owner.EmitArithmeticLine(arithmetic.SourceLine, ref line);
            owner.Op(GcBifTwo, LabelTag, 0);
            owner.Operand(BeamWire.UnsignedTag, live);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, arithmetic.Operator, 2));
            PairFacts facts = pattern.Items.Count == 2 ? InferPairFacts(read) : PairFacts.Unknown;
            EmitOperand(arithmetic.Left, arithmeticInput: true);
            EmitOperand(arithmetic.Right, arithmeticInput: true);
            owner.Operand(XRegisterTag, destination);

            owner.Op(TestHeap, BeamWire.UnsignedTag, result.Items.Count + 1);
            owner.Operand(BeamWire.UnsignedTag, nextRegister);
            for (int index = 0; index < deferredFields.Length; index++)
            {
                string name = deferredFields[index];
                int register = index == deferredFields.Length - 1 ? 0 : nextRegister++;
                owner.EmitTuplePart(0, fields[name], register);
                registers.Add(name, register);
            }

            owner.EmitTupleHeader(XRegisterTag, result.Items.Count);
            foreach (Expr item in result.Items)
            {
                EmitOperand(item, arithmeticInput: false);
            }

            owner.EmitFrameReturn(saved.Length - trimmed);

            return true;

            void EmitOperand(Expr expression, bool arithmeticInput)
            {
                if (ReferenceEquals(expression, arithmetic))
                {
                    owner.Operand(XRegisterTag, destination);
                }
                else if (expression is Expr.Variable variable)
                {
                    if (!registers.TryGetValue(variable.Name, out int register))
                    {
                        owner.Operand(YRegisterTag, Array.IndexOf(saved, Array.IndexOf(parameters, variable.Name)) - trimmed);
                    }
                    else if (arithmeticInput && fields[variable.Name] == 1 && (facts & PairFacts.Length) != 0)
                    {
                        owner.EmitTypedRegister(register, IntegerTypeBits,
                            lower: (facts & PairFacts.PositiveLength) == PairFacts.PositiveLength ? 1 : 0);
                    }
                    else
                    {
                        owner.Operand(XRegisterTag, register);
                    }
                }
                else
                {
                    AttributeLiteral.TryNormalizeConstant(expression, out Term constant);
                    owner.LiteralOperand(constant);
                }
            }
        }
    }
}
