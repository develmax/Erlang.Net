using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitInlineTablePairEncoder(FunctionDefinition function)
        {
            if (owner.ModuleOptimizationDisabled || !owner.SsaTypeInferenceEnabled || recursiveClosure is not null
                || function.Clauses is not
                [
                    {
                        Guard: null, Patterns: [Pattern.Variable input, Pattern.Variable tags],
                        Body: Expr.Sequence { Items: var items }
                    }
                ]
                || items.Count != 8
                || items[0] is not Expr.Match
                {
                    Pattern: Pattern.Tuple
                    { Items: [Pattern.Variable { Name: VariableScopeNames.Wildcard }, Pattern.Variable key, Pattern.Variable field] },
                    Value: Expr.Variable original
                } unpack
                || items[1] is not Expr.Match
                {
                    Pattern: Pattern.Variable callback,
                    Value: Expr.Call { Module: not null, DynamicRemote: false, Arguments: [Expr.Variable selector] } select
                }
                || ReadTableEncoderPair(items[2]) is not { } first
                || first.Call.Arguments is not [Expr.Variable encodedKey, var keyOptions]
                || items[3] is not Expr.Match
                {
                    Pattern: Pattern.Tuple { Items: [Pattern.Variable open, Pattern.Variable { Name: VariableScopeNames.Wildcard }] },
                    Value: Expr.Apply
                    {
                        Function: Expr.Variable applied,
                        Arguments: [Expr.Literal { Value: Atom parameter }, Expr.Variable argument, var context]
                    } apply
                } openMatch
                || ReadTableEncoderPair(items[4]) is not { } second
                || second.Call.Arguments is not [Expr.Variable payload, var openOptions]
                || !DistinctBindings(
                    input,
                    tags,
                    key,
                    field,
                    callback,
                    open,
                    first.Bytes,
                    first.Length,
                    second.Bytes,
                    second.Length
                )
                || original.Name != input.Name || selector.Name != key.Name || encodedKey.Name != key.Name
                || applied.Name != callback.Name || argument.Name != field.Name || payload.Name != open.Name
                || !TryResolveOrdinaryCallTarget(select, out string? module) || module is null or ErlangModuleNames.Module
                || !AttributeLiteral.TryNormalizeConstant(keyOptions, out Term keyTags)
                || !AttributeLiteral.TryNormalizeConstant(openOptions, out Term openTags)
                || !AttributeLiteral.TryNormalizeConstant(context, out Term callbackContext)
                || !entries.TryGetValue((first.Call.Function, 2), out int firstEntry)
                || !entries.TryGetValue((second.Call.Function, 2), out int secondEntry)
                || !ReturnsTuple(
                    first.Call,
                    2,
                    [],
                    exact: true
                ) || !ReturnsTuple(
                    second.Call,
                    2,
                    [],
                    exact: true
                ))
            {
                return false;
            }

            var pairs = new HashSet<string>(StringComparer.Ordinal)
                { first.Bytes.Name, first.Length.Name, second.Bytes.Name, second.Length.Name };
            var bound = new HashSet<string>(pairs, StringComparer.Ordinal)
                { input.Name, tags.Name, key.Name, field.Name, callback.Name, open.Name };
            if (ReadDeferredPairTail(
                new Expr.Sequence(items.Skip(5).ToArray()),
                tags.Name,
                bound,
                pairs
            ) is not
            {
                List.Items: [Expr.Variable left, Expr.Variable right], Operands: [var firstLength, var secondLength]
            } tail
                || left.Name != first.Bytes.Name || right.Name != second.Bytes.Name
                || firstLength != first.Length.Name || secondLength != second.Length.Name)
            {
                return false;
            }

            int badClosure = Label();
            int badInput = Label();
            owner.EmitTupleCheck(0, 3, badInput);
            EmitFrameAllocation(2, 3);
            owner.MoveRegister(
                XRegisterTag,
                1,
                YRegisterTag,
                2
            );
            ProjectField(1, 1);
            ProjectField(2, 0);
            owner.MoveToX0(YRegisterTag, 1);
            owner.EmitCallLine(select, location);
            Call(module, select.Function, 1);
            owner.Op(Swap, YRegisterTag, 1);
            owner.Operand(XRegisterTag, 0);
            owner.EmitConstant(keyTags, 1);
            var previous = CallLocation(select, location);
            owner.EmitCallLine(first.Call, previous);
            owner.EmitFieldCall(2, firstEntry);
            owner.MoveRegister(
                YRegisterTag,
                0,
                XRegisterTag,
                1
            );
            owner.MoveRegister(
                XRegisterTag,
                0,
                YRegisterTag,
                0
            );
            owner.EmitConstant(callbackContext, 2);
            owner.MoveRegister(
                YRegisterTag,
                1,
                XRegisterTag,
                3
            );
            owner.MoveRegister(
                YRegisterTag,
                0,
                YRegisterTag,
                1
            );
            owner.Op(TrimFrame, BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.EmitConstant(parameter, 0);
            previous = CallLocation(first.Call, previous);
            owner.EmitArithmeticLine(apply.SourceLine, ref previous);
            owner.Op(CallFun, BeamWire.UnsignedTag, 3);
            owner.EmitTupleCheck(0, 2, badClosure);
            owner.EmitTuplePart(0, 0, 0);
            owner.EmitConstant(openTags, 1);
            owner.EmitCallLine(second.Call, previous);
            owner.EmitFieldCall(2, secondEntry);
            EmitTwoDeferredPairResult(
                tail.Sum,
                tail.Entry,
                2,
                1,
                InferPairAdditionType(first.Call),
                InferPairAdditionType(second.Call)
            );
            Mark(badClosure);
            previous = location;
            owner.EmitArithmeticLine(openMatch.SourceLine, ref previous);
            owner.Op(BadMatch, XRegisterTag, 0);
            Mark(badInput);
            previous = location;
            owner.EmitArithmeticLine(unpack.SourceLine, ref previous);
            owner.Op(BadMatch, XRegisterTag, 0);

            return true;

            void ProjectField(int fieldIndex, int slot)
            {
                owner.Op(GetTupleElement, XRegisterTag, 0);
                owner.Operand(BeamWire.UnsignedTag, fieldIndex);
                owner.Operand(YRegisterTag, slot);
            }
        }
    }
}
