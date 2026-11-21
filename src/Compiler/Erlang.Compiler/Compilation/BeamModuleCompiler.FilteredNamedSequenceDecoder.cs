using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitFilteredNamedSequenceDecoder(
            IReadOnlyList<Expr> items,
            Pattern.Variable first,
            Pattern.Variable second,
            Pattern.Variable list,
            Expr.Call read
        )
        {
            if (!owner.SsaTypeInferenceEnabled || owner.ModuleOptimizationDisabled
                || items is not [_, ..,
                    Expr.Match
                {
                    Pattern: Pattern.Variable checkedTail,
                    Value: Expr.Call
                    {
                        Module: null,
                        DynamicRemote: false,
                        Arguments: [Expr.Variable extra, var filterOptions]
                    } filter
                },
                    var tailCheck,
                    Expr.Match
                {
                    Pattern: Pattern.Variable output,
                    Value: Expr.Tuple { Items: [Expr.Literal { Value: Atom tag }, ..] } tuple
                },
                    Expr.Variable returned]
                || tuple.Items.Count < 6 || items.Count != tuple.Items.Count * 2 + 1
                || returned.Name != output.Name
                || !DistinctBindings(
                    first,
                    second,
                    list,
                    checkedTail,
                    output
                )
                || !owner.emptyComprehensionScans.Contains((filter.Function, 2))
                || ReadLocalSingletonArgument(filterOptions) is not Nil
                || !entries.TryGetValue((filter.Function, 2), out int scanner)
                || ReadCheckedDecoderTail(tailCheck, checkedTail.Name) is null)
            {
                return false;
            }

            var reserved = new HashSet<string>(StringComparer.Ordinal)
                { first.Name, second.Name, list.Name, output.Name, checkedTail.Name };
            var required = new List<PrefixedDecodeField>();
            string current = list.Name;
            int member = 1;
            int item = 1;
            while (member < tuple.Items.Count - 2)
            {
                if (ReadRequiredPart(
                    items[item],
                    items[item + 1],
                    current,
                    tuple.Items[member],
                    reserved
                ) is not { } field)
                {
                    return false;
                }

                SequenceRead? rename = null;
                SequenceRead? decoder;
                if (member == 1)
                {
                    if (ReadSequenceNamedValue(field.Read, field.Payload.Name, reserved) is not { } named)
                    {
                        return false;
                    }

                    decoder = named.Decode;
                    rename = named.Rename;
                }
                else
                {
                    decoder = ReadSequenceCall(field.Read, field.Payload.Name);
                }

                if (decoder is null)
                {
                    return false;
                }

                required.Add(new(
                    field,
                    decoder.Call,
                    decoder.Entry,
                    decoder.Options,
                    rename
                ));
                current = field.Rest.Name;
                member++;
                item += 2;
            }

            var suffix = new DefaultSequenceField[2];
            for (int index = 0; index < suffix.Length; index++)
            {
                if (items[item++] is not Expr.Match
                    {
                        Pattern: Pattern.Tuple { Items: [Pattern.Variable value, Pattern.Variable remaining] },
                        Value: Expr.Case test
                    }
                    || tuple.Items[member++] is not Expr.Variable result || result.Name != value.Name
                    || !DistinctBindings(value, remaining) || !reserved.Add(value.Name) || !reserved.Add(remaining.Name)
                    || ReadDefaultSequenceField(test, current, reserved) is not { } optional)
                {
                    return false;
                }

                suffix[index] = optional;
                current = remaining.Name;
            }

            if (current != extra.Name)
            {
                return false;
            }

            int frame = tuple.Items.Count;
            var firstLabels = (Missing: Label(), Joined: Label());
            var lastLabels = (Missing: Label(), Joined: Label());
            int firstFailure = owner.nextLabel + required.Count - 1;
            owner.nextLabel = firstFailure + 1;
            EmitGeneralFrame(2, slots: frame);
            owner.EmitCallLine(read, location);
            Call(read.Module, read.Function, 2);
            var results = new List<int>();
            var failures = new List<(int Label, int? Slot, Expr.Match Match)>();
            var tail = EmitRequiredSequencePrefix(
                required,
                frame - 1,
                firstFailure,
                CallLocation(read, location),
                results,
                failures
            );

            int slot = tail.Tail;
            int middle = slot - 1;
            results.Add(slot);
            results.Add(middle);
            BeamSourceLocation previous = EmitTaggedOptionalDecoder(
                suffix[0],
                null,
                slot,
                middle,
                firstLabels,
                tail.Location,
                sourceInX0: false,
                leaveInRegisters: false
            );
            previous = EmitTaggedOptionalDecoder(
                suffix[1],
                null,
                middle,
                0,
                lastLabels,
                previous,
                sourceInX0: false,
                leaveInRegisters: true
            );
            owner.EmitConstant(Nil.Value, 1);
            owner.EmitCallLine(filter, previous);
            owner.EmitFieldCall(2, scanner);
            owner.Op(TestHeap, BeamWire.UnsignedTag, tuple.Items.Count + 1);
            owner.Operand(BeamWire.UnsignedTag, 0);
            owner.EmitTupleHeader(XRegisterTag, tuple.Items.Count);
            owner.LiteralOperand(tag);
            foreach (int result in results)
            {
                owner.Operand(YRegisterTag, result);
            }

            owner.EmitFrameReturn(frame);
            EmitDecoderFailures(failures);

            return true;
        }
    }
}
