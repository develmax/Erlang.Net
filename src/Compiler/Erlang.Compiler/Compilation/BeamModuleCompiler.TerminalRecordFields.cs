using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private sealed record TerminalRecordField(
            int Arity,
            Atom Tag,
            int Field,
            int Destination,
            int TagRegister,
            EntryCall? Call
        );

        private bool TryEmitTerminalRecordFields(FunctionDefinition function)
        {
            if (owner.ModuleOptimizationDisabled || !owner.SsaTypeInferenceEnabled || function.Clauses.Count < 2
                || function.Arity is 0 or >= FirstReservedXRegister)
            {
                return false;
            }

            if (function.Clauses is [var first, var last] && HasUnconditionalParameters(first)
                && ReadEntryTypeGuard(first, first.Guard) is { Opcode: IsList, Register: var list }
                && first.Body is Expr.Call direct
                && ReadEntryCall(direct, first.Patterns.Cast<Pattern.Variable>().Select(parameter => parameter.Name).ToArray()) is { } listTail
                && ReadTerminalRecordField(last, list) is { Call: not null } record)
            {
                int fallback = owner.nextLabel - 2;
                int recordLabel = Label();
                owner.Op(IsList, LabelTag, recordLabel);
                owner.Operand(XRegisterTag, list);
                EmitRegisterTailCall(listTail);
                Mark(recordLabel);
                EmitDetailRecordTest(
                    list,
                    record.Arity,
                    record.Tag,
                    fallback
                );
                EmitTerminalRecordField(record, list);

                return true;
            }

            Clause otherwise = function.Clauses[^1];
            if (function.Clauses.Count < 3 || otherwise.Guard is not null || !HasUnconditionalParameters(otherwise)
                || !AttributeLiteral.TryNormalizeConstant(otherwise.Body, out Term defaultValue))
            {
                return false;
            }

            Clause[] cases = function.Clauses.SkipLast(1).ToArray();
            int input = Enumerable.Range(0, function.Arity)
                .FirstOrDefault(index => cases.All(clause => clause.Patterns[index] is AliasPattern), -1);
            if (input < 0)
            {
                return false;
            }

            var plans = new List<TerminalRecordField>();
            foreach (Clause clause in cases)
            {
                if (ReadTerminalRecordField(clause, input) is not { } plan
                    || plans.Any(previous => previous.Arity == plan.Arity))
                {
                    return false;
                }

                plans.Add(plan);
            }

            var targets = plans.OrderByDescending(plan => plan.Arity)
                .Select(plan => (Plan: plan, Label: Label())).ToArray();
            int failure = Label();
            owner.EmitTupleArityDispatch(
                failure,
                targets.Reverse().Select(target => (target.Plan.Arity, target.Label)).ToArray(),
                input
            );
            foreach (var (arm, label) in targets)
            {
                Mark(label);
                owner.EmitTuplePart(input, 0, arm.TagRegister);
                owner.EmitAtomTest(arm.TagRegister, arm.Tag.Name, failure);
                EmitTerminalRecordField(arm, input);
            }
            Mark(failure);
            owner.EmitLiteralReturn(defaultValue);

            return true;
        }

        private TerminalRecordField? ReadTerminalRecordField(Clause clause, int input)
        {
            if (clause.Guard is not null || clause.Patterns[input] is not AliasPattern alias
                || !TryReadPatternAlias(alias, out var whole, out var shape) || whole.Name == VariableScopeNames.Wildcard
                || shape is not Pattern.Tuple { Items: [Pattern.Literal { Value: Atom tag }, ..] } tuple
                || tuple.Items.Skip(1).Any(item => item is not Pattern.Variable { Name: VariableScopeNames.Wildcard }))
            {
                return null;
            }

            var parameters = new string[clause.Patterns.Count];
            var names = new HashSet<string>(StringComparer.Ordinal) { whole.Name };
            for (int index = 0; index < parameters.Length; index++)
            {
                if (index == input)
                {
                    parameters[index] = VariableScopeNames.Wildcard;
                }
                else if (clause.Patterns[index] is Pattern.Variable variable
                    && (variable.Name == VariableScopeNames.Wildcard || names.Add(variable.Name)))
                {
                    parameters[index] = variable.Name;
                }
                else
                {
                    return null;
                }
            }

            if (clause.Body is Expr.RecordAccess field && Matches(field))
            {
                return new(
                    tuple.Items.Count,
                    tag,
                    field.Index,
                    0,
                    input == 0 ? 1 : 0,
                    null
                );
            }

            if (clause.Body is not Expr.Call call)
            {
                return null;
            }

            var projections = call.Arguments.Select((argument, index) => (argument, index))
                .Where(item => item.argument is Expr.RecordAccess).ToArray();
            if (projections is not [{ argument: Expr.RecordAccess operand, index: var destination }]
                || !Matches(operand)
                || ReadEntryCall(call, parameters, preparedOperand: destination) is not { } tail)
            {
                return null;
            }

            var live = call.Arguments.OfType<Expr.Variable>().Select(variable => Array.IndexOf(parameters, variable.Name)).ToHashSet();
            if (live.Contains(destination))
            {
                return null;
            }

            int scratch = Enumerable.Range(0, FirstReservedXRegister)
                .FirstOrDefault(register => register != input && !live.Contains(register), -1);
            if (scratch < 0)
            {
                return null;
            }

            return new(
                tuple.Items.Count,
                tag,
                operand.Index,
                destination,
                scratch,
                tail
            );

            bool Matches(Expr.RecordAccess record) =>
                record.Name == tag.Name && record.Arity == tuple.Items.Count
                && record.Value is Expr.Variable variable && variable.Name == whole.Name;
        }

        private void EmitTerminalRecordField(TerminalRecordField plan, int input)
        {
            owner.EmitTuplePart(input, plan.Field, plan.Destination);
            if (plan.Call is { } tail)
            {
                EmitRegisterTailCall(tail);
            }
            else
            {
                owner.instructions.WriteByte(Return);
            }
        }
    }
}
