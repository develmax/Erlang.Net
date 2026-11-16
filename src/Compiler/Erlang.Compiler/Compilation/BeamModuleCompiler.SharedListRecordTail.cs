using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitSharedListRecordTail(FunctionDefinition function, int functionInfo)
        {
            if (function.Clauses is not
                [
                { Guard: null, Body: Expr.Call { DynamicRemote: false } updateCall } first,
                { Guard: null, Body: Expr.Call { DynamicRemote: false } consCall } second,
                { Guard: null } empty
                ]
                || function.Arity >= FirstReservedXRegister
                || updateCall.Arguments.Count != function.Arity
                || updateCall.Arguments.OfType<Expr.RecordUpdate>().ToArray() is not
                    [Expr.RecordUpdate { Value: Expr.Variable record, Fields.Count: > 0 } update])
            {
                return false;
            }

            int input = Array.FindIndex(empty.Patterns.ToArray(), pattern => ListEntryTest(pattern) == IsNil);
            if (input < 0
                || first.Patterns[input] is not Pattern.List { Items: [Pattern.Literal key], Tail: Pattern.Variable firstTail }
                || second.Patterns[input] is not Pattern.List { Items: [Pattern.Variable head], Tail: Pattern.Variable secondTail }
                || !ReadBindings(first, firstTail, out var firstInputs)
                || !ReadBindings(second, secondTail, out var secondInputs)
                || !ReadBindings(empty, new Pattern.Variable(VariableScopeNames.Wildcard), out var emptyInputs)
                || head.Name != VariableScopeNames.Wildcard && !secondInputs.TryAdd(head.Name, function.Arity)
                || !firstInputs.TryGetValue(record.Name, out int recordRegister) || recordRegister == input
                || !ReferenceEquals(updateCall.Arguments[recordRegister], update))
            {
                return false;
            }

            var fields = new List<(int Index, Term Value)>();
            foreach (var field in update.Fields.OrderBy(field => field.Index))
            {
                if (!AttributeLiteral.TryNormalizeConstant(field.Value, out Term value))
                {
                    return false;
                }

                fields.Add((field.Index + 1, value));
            }

            for (int index = 0; index < function.Arity; index++)
            {
                if (index != recordRegister
                    && (updateCall.Arguments[index] is not Expr.Variable variable
                        || !firstInputs.TryGetValue(variable.Name, out int register) || register != index))
                {
                    return false;
                }
            }

            var normalized = updateCall with
            {
                Arguments = updateCall.Arguments.Select(argument => ReferenceEquals(argument, update) ? record : argument).ToArray()
            };
            if (!TryResolveOrdinaryCall(normalized, out string? module)
                || !TryEmitRegisterConstructedArgument(consCall, secondInputs, validateOnly: true)
                || ReadCallArgumentConstructions(
                    [empty.Body],
                    -1,
                    new Dictionary<string, int>(),
                    inputs: emptyInputs
                )
                    is not { } emptyResult
                || !HasProvenRecordInput(
                    function,
                    recordRegister,
                    update.Name,
                    update.Arity
                ))
            {
                return false;
            }

            int otherHead = Label();
            int nonCons = Label();
            owner.EmitNonemptyList(input, nonCons);
            owner.EmitListParts(
                input,
                XRegisterTag,
                function.Arity,
                XRegisterTag,
                input
            );
            owner.Op(ExactEqualTest, LabelTag, otherHead);
            owner.Operand(XRegisterTag, function.Arity);
            owner.LiteralOperand(key.Value);
            owner.Op(TestHeap, BeamWire.UnsignedTag, update.Arity + 1);
            owner.Operand(BeamWire.UnsignedTag, function.Arity);
            owner.Op(UpdateRecord, AtomTag, owner.Intern("reuse"));
            owner.Operand(BeamWire.UnsignedTag, update.Arity);
            owner.EmitTypedRegister(recordRegister, TupleTypeBits);
            owner.Operand(XRegisterTag, recordRegister);
            owner.instructions.WriteByte(ExtendedListOperand);
            owner.Operand(BeamWire.UnsignedTag, fields.Count * 2);
            foreach (var (index, value) in fields)
            {
                owner.Operand(BeamWire.UnsignedTag, index);
                owner.LiteralOperand(value);
            }

            EmitRegisterTailTarget(updateCall with { Module = module }, function.Arity);
            Mark(otherHead);
            TryEmitRegisterConstructedArgument(consCall, secondInputs);
            Mark(nonCons);
            owner.EmitNilTest(XRegisterTag, input, functionInfo);
            EmitCallArgumentConstructions(emptyResult, -1);
            owner.instructions.WriteByte(Return);

            return true;

            bool ReadBindings(Clause clause, Pattern.Variable list, out Dictionary<string, int> inputs)
            {
                inputs = new(StringComparer.Ordinal);
                for (int index = 0; index < function.Arity; index++)
                {
                    Pattern pattern = index == input ? list : clause.Patterns[index];
                    if (pattern is not Pattern.Variable variable
                        || variable.Name != VariableScopeNames.Wildcard && !inputs.TryAdd(variable.Name, index))
                    {
                        return false;
                    }
                }

                return true;
            }
        }
    }
}
