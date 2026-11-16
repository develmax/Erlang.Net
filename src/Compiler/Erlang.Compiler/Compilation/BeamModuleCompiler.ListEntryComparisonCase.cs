using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitPrefixedCallResultCase(Expr body, string[] parameters)
        {
            if (body is not Expr.Case
                {
                    Value: Expr.Binary { Operator: ErlangOperators.Append, Left: var prefix, Right: Expr.Call read },
                    Clauses:
                    [
                        { Guard: null, Patterns: [Pattern.Literal { Value: Cons key }], Body: var matched },
                        { Guard: null, Patterns: [Pattern.Variable binding], Body: Expr.Variable result }
                    ]
                }
                || binding.Name == VariableScopeNames.Wildcard || binding.Name != result.Name || parameters.Contains(binding.Name)
                || !AttributeLiteral.TryNormalizeConstant(prefix, out Term constant)
                || constant is not Cons { Tail: Nil } cell || !cell.Head.Equals(key.Head)
                || !AttributeLiteral.TryNormalizeConstant(matched, out Term accepted)
                || ReadEntryCall(read, parameters) is not { } call)
            {
                return false;
            }

            EmitFrameAllocation(EntryCallLiveCount(call, parameters), 0);
            EmitEntryCall(call);
            int otherwise = Label();
            owner.Op(ExactEqualTest, LabelTag, otherwise);
            if (call.Module is null && ReturnsProperList(read, []))
            {
                owner.EmitTypedRegister(0, ProperListTypeBits);
            }
            else
            {
                owner.Operand(XRegisterTag, 0);
            }

            owner.LiteralOperand(key.Tail);
            owner.EmitConstant(accepted, 0);
            owner.EmitFrameReturn(0);
            Mark(otherwise);
            owner.Op(TestHeap, BeamWire.UnsignedTag, ConsHeapWords);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.instructions.WriteByte(PutList);
            owner.LiteralOperand(cell.Head);
            owner.Operand(XRegisterTag, 0);
            owner.Operand(XRegisterTag, 0);
            owner.EmitFrameReturn(0);

            return true;

            bool ReturnsProperList(Expr expression, HashSet<(string Name, int Arity)> active)
            {
                if (expression is Expr.Binary { Operator: ErlangOperators.Append, Right: var suffix })
                {
                    return ReturnsProperList(suffix, active);
                }

                if (expression is Expr.List list)
                {
                    return list.Tail is null || ReturnsProperList(list.Tail, active);
                }

                if (expression is Expr.Call { Module: null, DynamicRemote: false } local)
                {
                    var key = (local.Function, local.Arguments.Count);
                    if (!localFunctions.TryGetValue(key, out var function) || owner.IsDeclaredNative(key))
                    {
                        return false;
                    }

                    if (!active.Add(key))
                    {
                        // Recursive calls can only return a value from a checked clause.
                        return true;
                    }

                    bool proper = function.Clauses.All(clause => ReturnsProperList(clause.Body, active));
                    active.Remove(key);

                    return proper;
                }

                return expression is Expr.Literal { Value: Nil };
            }
        }

        private bool TryEmitListEntryComparisonCase(
            FunctionDefinition function,
            Clause clause,
            (Pattern.Variable Variable, int Register)[] inputs,
            string[] parameters,
            int firstFrame,
            Dictionary<int, int> frames,
            (int Register, int Head)? reusedTail
        )
        {
            if (clause.Body is not Expr.Case
                {
                    Clauses: [
                    { Guard: null, Patterns: [var pattern], Body: var matched },
                    { Guard: null, Patterns: [Pattern.Variable { Name: VariableScopeNames.Wildcard }], Body: var fallback }
                    ]
                } decision)
            {
                return false;
            }

            var record = decision.Value as Expr.RecordAccess;
            if ((record?.Value ?? decision.Value) is not Expr.Variable { Name: not VariableScopeNames.Wildcard } selector
                || Array.IndexOf(parameters, selector.Name) is not (>= 0 and var source)
                || record is not null && parameters.Length >= FirstReservedXRegister)
            {
                return false;
            }

            int compared = pattern is Pattern.Variable variable && variable.Name != VariableScopeNames.Wildcard
                ? Array.IndexOf(parameters, variable.Name) : -1;
            if (compared < 0 && pattern is not Erlang.Pattern.Literal)
            {
                return false;
            }

            int badRecord = 0;
            int selectorRegister = source;
            if (record is not null)
            {
                badRecord = Label();
                selectorRegister = parameters.Length;
                EmitDetailRecordTest(
                    source,
                    record.Arity,
                    Term.A(record.Name),
                    badRecord
                );
                owner.EmitTuplePart(source, record.Index, selectorRegister);
            }

            int otherwise = Label();
            owner.Op(ExactEqualTest, LabelTag, otherwise);
            owner.Operand(XRegisterTag, selectorRegister);
            if (compared >= 0)
            {
                owner.Operand(XRegisterTag, compared);
            }
            else
            {
                owner.LiteralOperand(((Pattern.Literal)pattern).Value);
            }

            EmitArm(matched, firstFrame, record is null ? (pattern as Pattern.Literal)?.Value : null);
            Mark(otherwise);
            EmitArm(fallback, firstFrame + 1, null);
            if (record is not null)
            {
                Mark(badRecord);
                var failureLocation = new BeamSourceLocation(decision.PreprocessorFile, decision.SourceLine);
                if (failureLocation.Line > 0)
                {
                    owner.EmitLine(failureLocation);
                }

                owner.Op(BadRecord, XRegisterTag, source);
            }

            return true;

            void EmitArm(Expr body, int frame, Term? selectorValue)
            {
                if (TryEmitEntryArithmeticCall(
                    function,
                    clause,
                    body,
                    parameters,
                    source,
                    selectorValue
                ))
                {
                    return;
                }

                if (body is Expr.Call call && ReadEntryCall(call, parameters, allowControlTransfer: true) is { } tail)
                {
                    EmitRegisterTailCall(tail);
                }
                else if (body is Expr.Variable returned && Array.IndexOf(parameters, returned.Name) is >= 0 and var register)
                {
                    owner.MoveRegister(
                        XRegisterTag,
                        register,
                        XRegisterTag,
                        0
                    );
                    owner.instructions.WriteByte(Return);
                }
                else if (AttributeLiteral.TryNormalizeConstant(body, out Term constant))
                {
                    owner.EmitConstant(constant, 0);
                    owner.instructions.WriteByte(Return);
                }
                else if (body is Expr.Tuple && ReadEntryConstructedArgument(body, 0, parameters) is { } tuple)
                {
                    owner.Op(TestHeap, BeamWire.UnsignedTag, tuple.HeapWords);
                    owner.Operand(BeamWire.UnsignedTag, tuple.Operands.Max(operand => operand.Source.GetValueOrDefault(-1)) + 1);
                    EmitEntryConstruction(tuple);
                    owner.instructions.WriteByte(Return);
                }
                else
                {
                    var arm = clause with { Body = body, Guard = null };
                    var used = ReadUsedEntryParameters(arm with
                    {
                        Patterns = inputs.Select(input => (Pattern)input.Variable).ToArray()
                    });
                    var liveInputs = inputs.Where(input => used is null || used.Contains(input.Variable.Name)).ToArray();
                    EmitEntryClauseBody(
                        arm,
                        liveInputs,
                        frame,
                        frames,
                        reusedTail,
                        deferred: null,
                        literalDispatch: false
                    );
                }
            }
        }
    }
}
