using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitSavedSelectorPayloadScratch(
            Pattern pattern,
            int selector,
            int failure,
            bool checkedShape
        )
        {
            if (pattern is not Erlang.Pattern.Tuple { Items: [Erlang.Pattern.Literal { Value: Atom tag }, var payload, ..] } tuple
                || !tuple.Items.Skip(2).All(IsDiscardedScratchPattern)
                || payload is not Erlang.Pattern.Literal && (payload is not Erlang.Pattern.List { Tail: null, Items.Count: > 0 } list
                    || !list.Items.All(IsDiscardedScratchTaggedTuple)))
            {
                return false;
            }

            // The complete selector remains available in its Y register on every failure edge.
            // These pattern tests consume each projected value before any call, allocation or binding.
            if (!checkedShape)
            {
                owner.EmitRecordTest(
                    failure,
                    YRegisterTag,
                    selector,
                    tag,
                    tuple.Items.Count
                );
            }

            owner.Op(GetTupleElement, YRegisterTag, selector);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.Operand(XRegisterTag, 0);
            if (payload is Erlang.Pattern.Literal literal)
            {
                Pattern(
                    literal,
                    0,
                    failure,
                    valueTag: XRegisterTag
                );

                return true;
            }

            foreach (Pattern item in ((Erlang.Pattern.List)payload).Items)
            {
                owner.Op(IsNonemptyList, LabelTag, failure);
                owner.Operand(XRegisterTag, 0);
                owner.Op(GetList, XRegisterTag, 0);
                owner.Operand(XRegisterTag, 1);
                owner.Operand(XRegisterTag, 0);

                var head = (Erlang.Pattern.Tuple)item;
                HasTupleTag(head, out Atom headTag);
                owner.EmitRecordTest(
                    failure,
                    XRegisterTag,
                    1,
                    headTag,
                    head.Items.Count
                );
            }

            owner.EmitNilTest(XRegisterTag, 0, failure);

            return true;
        }

        private static bool IsDiscardedScratchTaggedTuple(Pattern item) =>
            item is Erlang.Pattern.Tuple tuple && HasTupleTag(tuple, out _) && tuple.Items.Skip(1).All(IsDiscardedScratchPattern);

        private static bool IsDiscardedScratchPattern(Pattern item) =>
            item is Erlang.Pattern.Any or Erlang.Pattern.Variable { Name: VariableScopeNames.Wildcard };
    }
}
