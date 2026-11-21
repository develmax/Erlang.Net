using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private sealed record SizeCheckedChoiceArm(
            Term Key,
            Atom Tag,
            DecodeCall Decode,
            CheckedFieldSize? Size
        );

        private bool TryEmitSizeCheckedTaggedChoice(
            IReadOnlyList<(Term Key, Pattern.Variable? Field, Expr Body)> branches,
            string fallback,
            Expr rejected
        )
        {
            if (rejected is not Expr.Call
                {
                    DynamicRemote: false,
                    Function: AutoImportNames.Exit,
                    Arguments: [var reason]
                } exit
                || !IsDecoderIntrinsic(exit)
                || ReadDecoderExitTags(reason, fallback) is not { } tags)
            {
                return false;
            }

            var arms = new List<SizeCheckedChoiceArm>();
            foreach (var (key, field, body) in branches)
            {
                if (field is null || !DistinctBindings(field)
                    || body is not Expr.Tuple { Items: [Expr.Literal { Value: Atom tag }, var value] })
                {
                    return false;
                }

                var bound = variables.Keys.Concat(deferredPairMembers.Keys).ToHashSet(StringComparer.Ordinal);
                bound.Add(field.Name);
                CheckedFieldSize? size = ReadCheckedFieldSize(value, bound, out Expr expression);
                if (ReadDecodeCall(expression, field.Name) is not { Convert: null } decode)
                {
                    return false;
                }

                arms.Add(new(
                    key,
                    tag,
                    decode,
                    size
                ));
            }

            if (!arms.Any(arm => arm.Size is not null))
            {
                return false;
            }

            var labels = new (int Entry, int Failure)[arms.Count];
            for (int index = arms.Count - 1; index >= 0; index--)
            {
                labels[index] = (Label(), arms[index].Size is null ? 0 : Label());
            }

            int otherwise = Label();
            owner.EmitTupleCheck(0, 2, otherwise);
            owner.EmitTupleElement(0, 1);
            owner.EmitTupleElement(1, 2);
            EmitAritySelect(1, arms.Count, otherwise);
            for (int index = 0; index < arms.Count; index++)
            {
                owner.LiteralOperand(arms[index].Key);
                owner.Operand(LabelTag, labels[index].Entry);
            }

            for (int index = arms.Count - 1; index >= 0; index--)
            {
                SizeCheckedChoiceArm arm = arms[index];
                Mark(labels[index].Entry);
                owner.EmitConstant(arm.Decode.Tags, 1);
                owner.MoveRegister(
                    XRegisterTag,
                    2,
                    XRegisterTag,
                    0
                );
                EmitDecodeInvocation(arm.Decode, location);
                if (arm.Size is { } size)
                {
                    EmitCheckedFieldMeasurement(size.Measure, CallLocation(arm.Decode.Decode, location));
                    owner.Op(ExactEqualTest, LabelTag, labels[index].Failure);
                    owner.EmitTypedRegister(
                        1,
                        IntegerTypeBits,
                        0,
                        MaximumBitSize
                    );
                    owner.LiteralOperand(size.Count);
                }

                EmitTaggedRegisterResult(arm.Tag);
                owner.EmitFrameReturn(frameSize);
                if (arm.Size is { } failure)
                {
                    Mark(labels[index].Failure);
                    owner.EmitConstant(failure.Reason, 0);
                    owner.EmitCallLine(failure.Failure, location);
                    EmitCheckedDecoderExit(failure.Failure, frameSize);
                }
            }

            Mark(otherwise);
            EmitCheckedDecoderTailExit(
                exit,
                tags,
                XRegisterTag,
                0,
                frameSize
            );

            return true;
        }
    }
}
