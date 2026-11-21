using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private readonly HashSet<int> registerSelectorEntries = [];

    private sealed partial class GeneralEmitter
    {
        private bool TryEmitRegisterSelectorContinuation(Expr.Sequence sequence)
        {
            if (!owner.SsaTypeInferenceEnabled || owner.ModuleOptimizationDisabled
                || sequence.Items is not [
                    Expr.Match { Pattern: Pattern.Variable selector, Value: Expr.Case producer },
                    Expr.Case { Value: Expr.Variable tested, Clauses: [var literal, var projected, var guarded, var fallback] } decision
                ]
                || selector.Name != tested.Name || selector.Name == VariableScopeNames.Wildcard
                || variables.ContainsKey(selector.Name) || deferredPairMembers.ContainsKey(selector.Name)
                || literal is not
                {
                    Guard: null,
                    Patterns: [Pattern.Tuple { Items: [Pattern.Literal { Value: Atom tag }, Pattern.Literal value] }]
                }
                || projected is not
                {
                    Guard: null,
                    Patterns: [Pattern.Tuple { Items: [Pattern.Literal { Value: Atom secondTag }, Pattern.Variable payload] }],
                    Body: Expr.Tuple projectionResult
                }
                || tag.Name != secondTag.Name || payload.Name == selector.Name || payload.Name == VariableScopeNames.Wildcard
                || variables.ContainsKey(payload.Name) || deferredPairMembers.ContainsKey(payload.Name)
                || guarded is not
                {
                    Guard: { } guard,
                    Patterns: [Pattern.Variable { Name: VariableScopeNames.Wildcard }],
                    Body: Expr.Case { Value: Expr.Call continuation } read
                }
                || fallback is not
                {
                    Guard: null, Patterns: [Pattern.Variable { Name: VariableScopeNames.Wildcard }], Body: Expr.Tuple fallbackResult
                }
                || ReadForwardGuard(guard, [selector.Name]) is not ForwardGuard.Test { Tag: null } typeCheck
                || ReadUsedEntryParameters(new Clause([selector], null, literal.Body)) is not { Count: 0 }
                || ReadUsedEntryParameters(new Clause([selector], null, read with { Value = new Expr.Literal(Nil.Value) }))
                    is not { Count: 0 }
                || ClosureCaptures.ReadBranchExports(
                    [new Clause([], null, producer)],
                    variables.Keys.Concat(deferredPairMembers.Keys),
                    [decision]
                ).Length != 0
                || ReadCaseTuple(
                    projectionResult,
                    payload.Name,
                    variables,
                    reuseX0ForSelector: true
                ) is not { } known
                || ReadCaseTuple(
                    fallbackResult,
                    selector.Name,
                    variables,
                    reuseX0ForSelector: true
                ) is not { } otherwise
                || !TryResolveOrdinaryCallTarget(continuation, out string? module)
                || ReadCallArgumentConstructions(
                    continuation.Arguments,
                    -1,
                    variables,
                    inputs: new Dictionary<string, int>(StringComparer.Ordinal) { [selector.Name] = 0 }
                ) is not { } arguments
                || ReadUsedEntryParameters(new Clause(
                    variables.Keys.Select(name => (Pattern)new Pattern.Variable(name)).ToArray(),
                    null,
                    read with { Value = new Expr.Literal(Nil.Value) }
                )) is not { } surviving)
            {
                return false;
            }

            UsesRegisterSelectorContinuation = true;
            EmitCase(producer, null, reuseBranchSlots: true);
            int otherPayload = Label();
            int otherType = Label();
            int otherValue = Label();
            owner.EmitRecordTest(
                otherType,
                XRegisterTag,
                0,
                tag,
                2
            );
            owner.EmitTuplePart(0, 1, 0);
            owner.Op(ExactEqualTest, LabelTag, otherPayload);
            owner.Operand(XRegisterTag, 0);
            owner.LiteralOperand(value.Value);
            int branchSlot = nextSlot;
            EmitReturnExpression(literal.Body, reuseCallSlots: true, returningResult: true);
            owner.EmitFrameReturn(frameSize);
            nextSlot = branchSlot;

            Mark(otherPayload);
            EmitCaseTuple(known);
            owner.EmitFrameReturn(frameSize);

            Mark(otherType);
            EmitForwardGuard(typeCheck, otherValue);
            EmitCallArgumentConstructions(arguments, -1);
            var live = variables.Where(binding => surviving.Contains(binding.Key))
                .Select(binding => binding.Value).ToHashSet();
            int[] dead = variables.Values.Where(slot => !live.Contains(slot)).Distinct().Order().ToArray();
            owner.EmitSlotKills(dead);
            owner.EmitCallLine(continuation, CaseSourceLocation(decision) ?? location);
            Call(module, continuation.Function, continuation.Arguments.Count);
            if (!TryEmitTerminalTaggedCase(read, allowPreparedCallContinuation: true))
            {
                EmitCase(
                    read,
                    null,
                    inputPrepared: true,
                    reuseBranchSlots: true,
                    returningResult: true
                );
                owner.EmitFrameReturn(frameSize);
            }

            nextSlot = branchSlot;

            Mark(otherValue);
            EmitCaseTuple(otherwise);
            owner.EmitFrameReturn(frameSize);

            return true;
        }
    }
}
