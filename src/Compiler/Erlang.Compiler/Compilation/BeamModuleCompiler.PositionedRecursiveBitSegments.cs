using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private sealed record PositionedConsHead(
            (Term? Constant, int? Field)[] Fields,
            EntryConstructedArgument[] Wrappers
        )
        {
            internal int HeapWords => ConsHeapWords + Fields.Length + 1 + Wrappers.Sum(wrapper => wrapper.HeapWords);
        }

        private bool TryEmitPositionedRecursiveBitSegments(FunctionDefinition function)
        {
            if (!CanUseEntryTypeFacts() || recursiveClosure is not null || owner.SourceModule?.DeclaredNifs is { Count: > 0 }
                || function.Clauses is not
                [
                {
                    Guard: null,
                    Patterns: [BitPattern { Segments: [var first, var suffix] }],
                    Body: Expr.List
                    {
                        Items: [var hotHead],
                        Tail: Expr.Call
                        {
                            Module: null, DynamicRemote: false, Arguments: [Expr.Variable next]
                        } recur
                    }
                },
                {
                    Guard: null, Patterns: [BitPattern { Segments.Count: 0 }],
                    Body: Expr.Literal { Value: Nil } or Expr.List { Items.Count: 0, Tail: null }
                },
                {
                    Guard: null,
                    Patterns: [Erlang.Pattern.Variable raw],
                    Body: Expr.Sequence
                    {
                        Items:
                            [
                                Expr.Match
                            {
                                Pattern: Erlang.Pattern.Variable width,
                                Value: Expr.Call
                                {
                                    Function: AutoImportNames.BitSize, DynamicRemote: false,
                                    Arguments: [Expr.Variable measured]
                                } measure
                            },
                                Expr.Match
                            {
                                Pattern: BitPattern { Segments: [var final] },
                                Value: Expr.Variable input
                            } extraction,
                                Expr.List { Items: [var shortHead], Tail: null }
                            ]
                    }
                }
                ]
                || first.Value is not Erlang.Pattern.Variable read || !ReadUnsignedWidth(first.Specification, out int bits)
                || suffix is not
                {
                    Value: Erlang.Pattern.Variable tail,
                    Specification: { Type: BitSegmentTypes.Binary, Size: null, Unit: BitSyntaxDefaults.BitStringUnit }
                }
                || !DistinctBindings(read, tail) || recur.Function != function.Name || next.Name != tail.Name
                || final is not
                {
                    Value: Erlang.Pattern.Variable remaining,
                    Specification:
                    {
                        Type: BitSegmentTypes.Integer, Unit: BitSyntaxDefaults.NumericUnit,
                        Endian: BitByteOrders.Big, Signed: false, Size: Expr.Variable size
                    }
                }
                || !DistinctBindings(raw, width, remaining) || measured.Name != raw.Name || input.Name != raw.Name
                || size.Name != width.Name || !owner.ResolvesErlangBif(measure)
                || ReadPositionedConsHead(hotHead, [read.Name]) is not { } full
                || ReadPositionedConsHead(shortHead, [remaining.Name, width.Name]) is not { } partial
                || !entries.TryGetValue((function.Name, 1), out int entry)
                || !HasBinaryLocalArgument(function, recur, materializedBitstringOnly: true))
            {
                return false;
            }

            const int context = 0;
            const int checkpoint = 1;
            const int sizeRegister = 2;
            const int remainingRegister = 3;
            int functionInfo = owner.nextLabel - 2;
            int retry = Label();
            int shortInput = Label();
            int invalid = Label();
            var previous = location;
            EmitBitMatchStart(context, functionInfo, knownBinary: true);
            owner.Op(GetBinaryPosition, XRegisterTag, context);
            owner.Operand(XRegisterTag, checkpoint);
            owner.Operand(BeamWire.UnsignedTag, 1);
            EmitBitMatchHeader(
                context,
                retry,
                9,
                bits,
                BitSyntaxDefaults.BitStringUnit
            );
            EmitBitSegment(
                first.Specification,
                bits,
                1,
                XRegisterTag,
                checkpoint
            );
            EmitFrameAllocation(arity: 2, size: 1);
            owner.MoveRegister(
                XRegisterTag,
                checkpoint,
                YRegisterTag,
                0
            );
            owner.EmitCallLine(recur, previous);
            previous = CallLocation(recur, previous);
            owner.EmitFieldCall(1, entry);
            EmitHead(full, firstDestination: 1, tailIsEmpty: false);
            owner.EmitFrameReturn(1);

            Mark(retry);
            RestorePosition();
            EmitBitMatchHeader(
                context,
                shortInput,
                2,
                0,
                tailUnit: null
            );
            owner.EmitConstant(Nil.Value, context);
            owner.instructions.WriteByte(Return);

            Mark(shortInput);
            owner.EmitCallLine(measure, previous);
            previous = CallLocation(measure, previous);
            EmitSizeInstruction(measure.Function, failure: 0, live: 2);
            owner.EmitTypedRegister(context, MatchableTypeBits, unit: BitSyntaxDefaults.BitStringUnit);
            owner.Operand(XRegisterTag, sizeRegister);
            owner.Op(GetBinaryInteger, LabelTag, invalid);
            owner.EmitTypedRegister(context, MatchableTypeBits, unit: BitSyntaxDefaults.BitStringUnit);
            owner.Operand(BeamWire.UnsignedTag, remainingRegister);
            owner.EmitTypedRegister(
                sizeRegister,
                IntegerTypeBits,
                0,
                MaximumBitSize
            );
            owner.Operand(BeamWire.UnsignedTag, BitSyntaxDefaults.NumericUnit);
            owner.Operand(BeamWire.UnsignedTag, 0);
            owner.Operand(XRegisterTag, remainingRegister);
            EmitBitMatchHeader(
                context,
                invalid,
                2,
                0,
                tailUnit: null
            );
            EmitHead(partial, firstDestination: 0, tailIsEmpty: true);
            owner.instructions.WriteByte(Return);

            Mark(invalid);
            RestorePosition();
            owner.Op(GetBinaryTail, XRegisterTag, context);
            owner.Operand(XRegisterTag, context);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.EmitArithmeticLine(extraction.SourceLine, ref previous);
            owner.Op(BadMatch, XRegisterTag, context);

            return true;

            void RestorePosition()
            {
                owner.Op(SetBinaryPosition, XRegisterTag, context);
                owner.Operand(XRegisterTag, checkpoint);
            }

            void EmitHead(PositionedConsHead head, int firstDestination, bool tailIsEmpty)
            {
                owner.Op(TestHeap, BeamWire.UnsignedTag, head.HeapWords);
                owner.Operand(BeamWire.UnsignedTag, tailIsEmpty ? remainingRegister + 1 : 1);
                foreach (var wrapper in head.Wrappers)
                {
                    EmitTuple(firstDestination + wrapper.Register, wrapper.Operands.Length);
                    foreach (var (source, constant) in wrapper.Operands)
                    {
                        if (source is int value)
                        {
                            owner.Operand(
                                tailIsEmpty ? XRegisterTag : YRegisterTag,
                                tailIsEmpty ? remainingRegister - value : 0
                            );
                        }
                        else
                        {
                            owner.LiteralOperand(constant!);
                        }
                    }
                }

                EmitTuple(firstDestination, head.Fields.Length);
                foreach (var (constant, field) in head.Fields)
                {
                    if (field is int value)
                    {
                        owner.Operand(XRegisterTag, firstDestination + value);
                    }
                    else
                    {
                        owner.LiteralOperand(constant!);
                    }
                }

                owner.Op(PutList, XRegisterTag, firstDestination);
                owner.Operand(tailIsEmpty ? AtomTag : XRegisterTag, 0);
                owner.Operand(XRegisterTag, 0);
            }

            void EmitTuple(int destination, int fieldCount)
            {
                owner.Op(PutTuple, XRegisterTag, destination);
                owner.instructions.WriteByte(ExtendedListOperand);
                owner.Operand(BeamWire.UnsignedTag, fieldCount);
            }
        }

        private static PositionedConsHead? ReadPositionedConsHead(Expr expression, string[] bindings)
        {
            if (AsTuple(expression) is not { } tuple)
            {
                return null;
            }

            var fields = new (Term? Constant, int? Field)[tuple.Items.Count];
            var wrappers = new List<EntryConstructedArgument>();
            for (int index = 0; index < fields.Length; index++)
            {
                Expr item = tuple.Items[index];
                if (AttributeLiteral.TryNormalizeConstant(item, out Term constant))
                {
                    fields[index] = (constant, null);
                    continue;
                }

                int binding = wrappers.Count;
                if (binding >= bindings.Length || AsTuple(item) is not { } child
                    || ReadEntryConstructedArgument(child, binding, bindings) is not { } wrapper
                    || wrapper.Operands.Count(operand => operand.Source == binding) != 1
                    || wrapper.Operands.Any(operand => operand.Source is int source && source != binding))
                {
                    return null;
                }

                fields[index] = (null, binding);
                wrappers.Add(wrapper);
            }

            return wrappers.Count == bindings.Length ? new(fields, [.. wrappers]) : null;

            static Expr.Tuple? AsTuple(Expr value) => value switch
            {
                Expr.Tuple tuple => tuple,
                Expr.RecordConstruction record => RecordExpressions.Create(record),
                _ => null
            };
        }
    }
}
