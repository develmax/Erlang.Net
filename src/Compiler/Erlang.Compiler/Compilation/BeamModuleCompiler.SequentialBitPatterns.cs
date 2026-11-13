using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitGuardedBinarySplit(FunctionDefinition function)
        {
            if (function.Clauses is not [
                {
                    Guard: { } guard,
                    Patterns: [Pattern.Variable { }, Pattern.Variable { }],
                    Body: Expr.Sequence
                    {
                        Items:
                        [
                            Expr.Match { Pattern: BitPattern { Segments: [var prefix, var suffix] }, Value: Expr.Variable source } match,
                            var continuation
                        ]
                    }
                } clause
                ]
                || !HasUnconditionalParameters(clause)
                || prefix is not
                {
                    Value: Pattern.Variable part,
                    Specification: { Type: BitSegmentTypes.Binary, Size: Expr.Variable size, Unit: BitSyntaxDefaults.BinaryUnit }
                }
                || suffix is not
                {
                    Value: Pattern.Variable tail,
                    Specification: { Type: BitSegmentTypes.Binary, Size: null, Unit: BitSyntaxDefaults.BinaryUnit }
                }
                || !DistinctBindings(part, tail, new Pattern.Variable(source.Name), new Pattern.Variable(size.Name)))
            {
                return false;
            }

            var parameters = clause.Patterns.Cast<Pattern.Variable>().Select(parameter => parameter.Name).ToArray();
            int input = Array.IndexOf(parameters, source.Name);
            int count = Array.IndexOf(parameters, size.Name);
            if (input < 0 || count < 0
                || PredicateConjuncts(guard).Select(test => ReadEntryTypeGuard(clause, test)).ToArray() is not [var left, var right]
                || !(left == (IsBinary, input) && right == (IsInteger, count)
                    || left == (IsInteger, count) && right == (IsBinary, input)))
            {
                return false;
            }

            bool tuple = continuation is Expr.Tuple;
            var captures = new[] { part.Name, tail.Name };
            if (input < count)
            {
                Array.Reverse(captures);
            }

            var call = continuation is Expr.Call target ? ReadEntryCall(target, captures) : null;
            if (tuple
                ? ((Expr.Tuple)continuation).Items is not [Expr.Variable first, Expr.Variable second]
                    || !(first.Name == part.Name && second.Name == tail.Name || first.Name == tail.Name && second.Name == part.Name)
                : call is not { Module: null })
            {
                return false;
            }

            int functionInfo = owner.nextLabel - 2;
            if (EmitEntryGuardPrefix(clause, guard, functionInfo) is not null)
            {
                throw new InvalidOperationException(Unsupported);
            }

            int failure = Label();
            int context = function.Arity;
            int live = context + 1;
            owner.Op(StartBinaryMatchKnown, AtomTag, owner.Intern("no_fail"));
            owner.Operand(BeamWire.UnsignedTag, context);
            owner.Operand(XRegisterTag, input);
            owner.Operand(XRegisterTag, context);
            owner.Op(GetBinarySegment, LabelTag, failure);
            owner.EmitTypedRegister(context, MatchableTypeBits);
            owner.Operand(BeamWire.UnsignedTag, live);
            owner.EmitTypedRegister(count, IntegerTypeBits);
            owner.Operand(BeamWire.UnsignedTag, BitSyntaxDefaults.BinaryUnit);
            owner.Operand(BeamWire.UnsignedTag, 0);
            owner.Operand(XRegisterTag, count);
            owner.Op(GetBinaryTail, XRegisterTag, context);
            owner.Operand(XRegisterTag, input);
            owner.Operand(BeamWire.UnsignedTag, live);
            if (continuation is Expr.Tuple returned)
            {
                owner.Op(TestHeap, BeamWire.UnsignedTag, 3);
                owner.Operand(BeamWire.UnsignedTag, context);
                owner.EmitTupleHeader(XRegisterTag, 2);
                foreach (Expr.Variable field in returned.Items.Cast<Expr.Variable>())
                {
                    owner.Operand(XRegisterTag, field.Name == part.Name ? count : input);
                }

                owner.instructions.WriteByte(Return);
            }
            else
            {
                owner.EmitOrderedTailMoves(call!.Moves, call.Constants);
                EmitRegisterTailTarget(call.Call, call.Call.Arguments.Count);
            }

            Mark(failure);
            var previous = location;
            owner.EmitArithmeticLine(match.SourceLine, ref previous);
            owner.Op(BadMatch, XRegisterTag, input);

            return true;
        }

        private bool TryEmitProvenBinarySplit(BitPattern pattern, int source, int failure)
        {
            if (pattern.Segments.Count < 2)
            {
                return false;
            }

            var captures = new HashSet<string>(StringComparer.Ordinal);
            var fields = new List<(string? Name, int? CountSlot, Term? Count)>();
            for (int index = 0; index < pattern.Segments.Count; index++)
            {
                var (value, specification) = pattern.Segments[index];
                if (specification is not { Type: BitSegmentTypes.Binary, Unit: BitSyntaxDefaults.BinaryUnit }
                    || value is not Pattern.Variable binding)
                {
                    return false;
                }

                string? name = binding.Name == VariableScopeNames.Wildcard ? null : binding.Name;
                if (name is not null && (variables.ContainsKey(name)
                    || deferredPairMembers.ContainsKey(name) || !captures.Add(name)))
                {
                    return false;
                }

                switch (specification.Size)
                {
                    case Expr.Variable size when variables.TryGetValue(size.Name, out int count):
                        fields.Add((name, count, null));
                        break;
                    case Expr.Literal { Value: Integer length } when FixedSize(specification) is not null:
                        fields.Add((name, null, length));
                        break;
                    case null when index == pattern.Segments.Count - 1:
                        fields.Add((name, null, null));
                        break;
                    default:
                        return false;
                }
            }

            // Byte-sized extracts preserve alignment, and none of the operands clobber X0.
            owner.Op(StartBinaryMatchKnown, AtomTag, owner.Intern("no_fail"));
            owner.Operand(BeamWire.UnsignedTag, 0);
            owner.Operand(YRegisterTag, source);
            owner.Operand(XRegisterTag, 0);
            int fixedStart = fields.FindLastIndex(field => field.CountSlot.HasValue) + 1;
            var fixedSuffix = fields.Skip(fixedStart).ToArray();
            bool hasTail = fields[^1] is (_, null, null);
            long suffixWidth = fixedSuffix.Sum(field => field.Count is Integer length
                ? (long)length.Value * BitSyntaxDefaults.BinaryUnit : 0L);
            bool combineSuffix = fixedSuffix.Any(field => field.Count is not null) && suffixWidth <= int.MaxValue;
            int separateFields = combineSuffix ? fixedStart : fields.Count;
            foreach (var (name, countSlot, count) in fields.Take(separateFields))
            {
                int destination = name is null ? -1 : Slot();
                if (countSlot is null && count is null)
                {
                    if (name is not null)
                    {
                        owner.Op(GetBinaryTail, XRegisterTag, 0);
                        owner.Operand(YRegisterTag, destination);
                        owner.Operand(BeamWire.UnsignedTag, 1);
                    }
                }
                else
                {
                    owner.Op(name is null ? SkipBinaryBits : GetBinarySegment, LabelTag, failure);
                    owner.EmitTypedRegister(0, MatchableTypeBits);
                    if (name is not null)
                    {
                        owner.Operand(BeamWire.UnsignedTag, 1);
                    }

                    if (countSlot is int slot)
                    {
                        owner.Operand(YRegisterTag, slot);
                    }
                    else
                    {
                        owner.LiteralOperand(count!);
                    }

                    owner.Operand(BeamWire.UnsignedTag, BitSyntaxDefaults.BinaryUnit);
                    owner.Operand(BeamWire.UnsignedTag, 0);
                    if (name is not null)
                    {
                        owner.Operand(YRegisterTag, destination);
                    }
                }

                if (name is not null)
                {
                    variables.Add(name, destination);
                }
            }

            if (combineSuffix)
            {
                int operands = fixedSuffix.Sum(field => field.Count is null ? field.Name is null ? 0 : 4
                    : field.Name is null ? 2 : 6);
                EmitBitMatchHeader(0, failure, operands + (hasTail ? 3 : 2), (int)suffixWidth,
                    hasTail ? BitSyntaxDefaults.BitStringUnit : null);
                foreach (var (name, _, count) in fixedSuffix)
                {
                    if (count is Integer length)
                    {
                        if (name is null)
                        {
                            owner.Operand(AtomTag, owner.Intern(SkipBits));
                            owner.Operand(BeamWire.UnsignedTag, length.Value * BitSyntaxDefaults.BinaryUnit);
                        }
                        else
                        {
                            int destination = Slot();
                            EmitBitSegment(new BitSegment(new Expr.Literal(Nil.Value), null,
                                Type: BitSegmentTypes.Binary, Unit: BitSyntaxDefaults.BinaryUnit),
                                (int)length.Value, 1, YRegisterTag, destination);
                            variables.Add(name, destination);
                        }
                    }
                    else if (name is not null)
                    {
                        int destination = Slot();
                        owner.Operand(AtomTag, owner.Intern(ReadBinaryTail));
                        owner.Operand(BeamWire.UnsignedTag, 1);
                        owner.Operand(BeamWire.UnsignedTag, BitSyntaxDefaults.BitStringUnit);
                        owner.Operand(YRegisterTag, destination);
                        variables.Add(name, destination);
                    }
                }
            }
            else if (!hasTail)
            {
                EmitBitMatchHeader(0, failure, 2, 0, null);
            }

            return true;
        }
        private void EmitSequentialBitPattern(BitPattern pattern, int value, int failure, bool knownBinary)
        {
            if (knownBinary && TryEmitProvenBinarySplit(pattern, value, failure))
            {
                return;
            }

            int context = Slot();
            Load(value);
            EmitBitMatchStart(0, failure, knownBinary);
            owner.MoveRegister(XRegisterTag, 0, YRegisterTag, context);

            for (int index = 0; index < pattern.Segments.Count; index++)
            {
                var segment = pattern.Segments[index];
                BitSegment specification = segment.Specification;
                if (specification is { Type: BitSegmentTypes.Binary, Size: null })
                {
                    if (index != pattern.Segments.Count - 1)
                    {
                        throw new NotSupportedException(Unsupported);
                    }

                    Load(context);
                    EmitBitMatchHeader(0, failure, 3, 0, specification.Unit);
                    if (!IsWildcard(segment.Value))
                    {
                        int tail = Slot();
                        owner.Op(GetBinaryTail, XRegisterTag, 0);
                        owner.Operand(YRegisterTag, tail);
                        owner.Operand(BeamWire.UnsignedTag, 1);
                        Pattern(segment.Value, tail, failure);
                    }
                    return;
                }
                int destination = ReadSequentialBitSegment(specification, context, failure);
                Pattern(segment.Value, destination, failure);
            }

            Load(context);
            EmitBitMatchHeader(0, failure, 2, 0, null);
        }

        private int ReadSequentialBitSegment(BitSegment specification, int context, int failure)
        {
            byte opcode = specification.Type switch
            {
                BitSegmentTypes.Integer => GetBinaryInteger,
                BitSegmentTypes.Float => GetBinaryFloat,
                BitSegmentTypes.Binary => GetBinarySegment,
                BitSegmentTypes.Utf8 => GetBinaryUtf8,
                BitSegmentTypes.Utf16 => GetBinaryUtf16,
                BitSegmentTypes.Utf32 => GetBinaryUtf32,
                _ => throw new NotSupportedException(Unsupported)
            };
            bool unicode = opcode is GetBinaryUtf8 or GetBinaryUtf16 or GetBinaryUtf32;
            int? size = unicode || specification.Size is null ? null : ValueSlot(specification.Size, failure);
            int destination = Slot();
            Load(context);
            owner.Op(opcode, LabelTag, failure);
            owner.Operand(XRegisterTag, 0);
            owner.Operand(BeamWire.UnsignedTag, 1);
            if (!unicode)
            {
                if (size is int slot)
                {
                    owner.Operand(YRegisterTag, slot);
                }
                else
                {
                    owner.LiteralOperand(specification.Type switch
                    {
                        BitSegmentTypes.Binary => Term.A("all"),
                        BitSegmentTypes.Float => Term.I(BitSyntaxDefaults.FloatWidth),
                        _ => Term.I(BitSyntaxDefaults.IntegerWidth)
                    });
                }
                owner.Operand(BeamWire.UnsignedTag, specification.Unit);
            }
            int flags = specification.Type is BitSegmentTypes.Binary or BitSegmentTypes.Utf8 ? 0
                : specification.Endian switch
                {
                    BitByteOrders.Little => 2,
                    BitByteOrders.Native => 16,
                    _ => 0
                };
            if (opcode == GetBinaryInteger && specification.Signed)
            {
                flags |= 4;
            }
            owner.Operand(BeamWire.UnsignedTag, flags);
            owner.Operand(YRegisterTag, destination);

            return destination;
        }
    }
}
