using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitPackedConsArithmetic(FunctionDefinition function)
        {
            if (!owner.SsaTypeInferenceEnabled || owner.ModuleOptimizationDisabled || recursiveClosure is not null
                || function.Arity is not (1 or 3) || function.Clauses.Count != (function.Arity == 1 ? 2 : 3)
                || function.Clauses[0] is not
                {
                    Guard: null,
                    Patterns: [Pattern.List { Items: var items, Tail: Pattern.Variable tail }, ..],
                    Body: Expr.Sequence
                    {
                        Items: [
                            Expr.Match { Pattern: Pattern.Variable packed, Value: var expression },
                            Expr.Call
                        {
                            Module: null, DynamicRemote: false,
                            Arguments: [Expr.Variable rest, Expr.List { Items: var incremented, Tail: null }, var count]
                        } recur
                        ]
                    }
                } head
                || items.Count != BitSyntaxDefaults.BinaryUnit || items.Any(item => item is not Pattern.Variable { })
                || !DistinctBindings([.. items.Cast<Pattern.Variable>(), tail, packed]) || rest.Name != tail.Name
                || !entries.TryGetValue((recur.Function, 3), out int recursiveEntry))
            {
                return false;
            }

            bool hasAccumulator = function.Arity == 3;
            if (hasAccumulator && recur.Function != function.Name)
            {
                return false;
            }

            var stages = new List<(Expr.Binary? Shift, Expr.Binary? Combine)>();
            if (!ReadPack(expression, items.Count - 1) || stages.Count != items.Count)
            {
                return false;
            }

            if (function.Clauses[^1] is not
                {
                    Guard: null,
                    Patterns: [Pattern.Variable residual, ..],
                    Body: Expr.Sequence
                    {
                        Items: [
                            Expr.Match
                        {
                            Pattern: Pattern.Tuple { Items: [Pattern.Variable unused, Pattern.Variable lastByte] },
                            Value: Expr.Call
                            {
                                Module: null, DynamicRemote: false,
                                Arguments: [
                                        Expr.Variable buffer, Expr.Literal { Value: Integer shift },
                                        Expr.Literal { Value: Integer seed }
                                    ]
                            } remainder
                        },
                            Expr.Tuple { Items: [var total, Expr.Variable sentUnused, Expr.List bytes] }
                        ]
                    }
                } fallback
                || !DistinctBindings(residual, unused, lastByte) || buffer.Name != residual.Name
                || sentUnused.Name != unused.Name || bytes.Tail is not null
                || !EntriesForTail(remainder, out int remainderEntry)
                || !ReturnsTuple(
                    remainder,
                    2,
                    [],
                    exact: true
                ))
            {
                return false;
            }

            Expr.Binary? mainCount = null;
            Expr.Binary? fallbackCount = null;
            if (hasAccumulator)
            {
                if (head.Patterns is not [_, Pattern.Variable acc, Pattern.Variable size]
                    || fallback.Patterns is not [_, Pattern.Variable saved, Pattern.Variable oldCount]
                    || !DistinctBindings([.. items.Cast<Pattern.Variable>(), tail, packed, acc, size])
                    || !DistinctBindings(
                        residual,
                        unused,
                        lastByte,
                        saved,
                        oldCount
                    )
                    || incremented is not [Expr.Variable carry, Expr.Variable number] || carry.Name != acc.Name || number.Name != packed.Name
                    || ReadIncrement(count, size.Name) is not { } step || ReadIncrement(total, oldCount.Name) is not { } extra
                    || bytes.Items is not [Expr.Variable current, Expr.Variable octet] || current.Name != saved.Name || octet.Name != lastByte.Name
                    || function.Clauses[1] is not
                    {
                        Guard: null,
                        Patterns: [
                            Pattern.List { Items.Count: 0, Tail: null } or Pattern.Literal { Value: Nil },
                            Pattern.Variable finalList,
                            Pattern.Variable finalLength
                        ],
                        Body: Expr.Tuple
                        {
                            Items: [
                                Expr.Variable resultLength, Expr.Literal { Value: Integer zero }, Expr.Variable resultBytes
                            ]
                        }
                    }
                    || !DistinctBindings(finalList, finalLength) || !zero.Value.IsZero
                    || resultLength.Name != finalLength.Name || resultBytes.Name != finalList.Name)
                {
                    return false;
                }

                mainCount = step;
                fallbackCount = extra;
            }
            else if (incremented is not [Expr.Variable number] || number.Name != packed.Name
                || count is not Expr.Literal { Value: Integer firstCount } || !firstCount.Value.IsOne
                || total is not Expr.Literal { Value: Integer initial } || !initial.Value.IsOne
                || bytes.Items is not [Expr.Variable octet] || octet.Name != lastByte.Name)
            {
                return false;
            }

            var typeFacts = ReadLocalArgumentFacts(function, listArithmetic: true);
            if ((typeFacts?[0] & PairFacts.BitList) != PairFacts.BitList
                || hasAccumulator && (typeFacts?[2] & PairFacts.Positive) != PairFacts.Positive)
            {
                return false;
            }

            int fallbackLabel = Label();
            int empty = hasAccumulator ? Label() : fallbackLabel;
            int first = function.Arity;
            int list = first + 1;
            int temporary = first + 2;
            int fullLive = function.Arity + items.Count;
            for (int index = 0; index < items.Count; index++)
            {
                int source = index == 0 ? 0 : list;
                int output = index == 0 ? first : index == items.Count - 1 ? 0 : list + index;
                owner.Op(IsNonemptyList, LabelTag, index == 0 ? empty : fallbackLabel);
                owner.Operand(XRegisterTag, source);
                owner.Op(GetList, XRegisterTag, source);
                owner.Operand(XRegisterTag, output);
                owner.Operand(XRegisterTag, list);
            }

            var previous = location;
            long maxPacked = 0;
            for (int index = 0; index < stages.Count; index++)
            {
                var (shiftOperation, combine) = stages[index];
                bool last = index == stages.Count - 1;
                int source = index == 0 ? first : last ? 0 : list + index;
                int destination = index == 0 ? first : last ? 0 : temporary;
                long contribution = 1L << (stages.Count - index - 1);
                if (shiftOperation is not null)
                {
                    Arithmetic(shiftOperation, fullLive);
                    owner.EmitTypedRegister(
                        source,
                        IntegerTypeBits,
                        0,
                        1
                    );
                    owner.LiteralOperand(((Expr.Literal)shiftOperation.Right).Value);
                    owner.Operand(XRegisterTag, destination);
                }

                if (combine is not null)
                {
                    int live = last ? list + 1 : index == stages.Count - 2 ? temporary + 1 : fullLive;
                    int result = last ? 0 : first;
                    Arithmetic(combine, live);
                    owner.EmitTypedRegister(
                        first,
                        IntegerTypeBits,
                        0,
                        maxPacked
                    );
                    owner.EmitTypedRegister(
                        destination,
                        IntegerTypeBits,
                        0,
                        contribution
                    );
                    owner.Operand(XRegisterTag, result);
                }

                maxPacked += contribution;
            }

            if (mainCount is not null)
            {
                Arithmetic(mainCount, list + 1);
                owner.EmitTypedRegister(2, IntegerTypeBits, lower: 1);
                owner.Operand(BeamWire.IntegerTag, 1);
                owner.Operand(XRegisterTag, 2);
            }

            owner.Op(TestHeap, BeamWire.UnsignedTag, hasAccumulator ? 4 : 2);
            owner.Operand(BeamWire.UnsignedTag, list + 1);
            owner.Op(PutList, XRegisterTag, 0);
            owner.Operand(AtomTag, NilAtomIndex);
            owner.Operand(XRegisterTag, hasAccumulator ? 0 : 1);
            if (hasAccumulator)
            {
                owner.Op(PutList, XRegisterTag, 1);
                owner.Operand(XRegisterTag, 0);
                owner.Operand(XRegisterTag, 1);
            }
            owner.MoveToX0(XRegisterTag, list);
            if (!hasAccumulator)
            {
                owner.EmitConstant(((Expr.Literal)count).Value, 2);
            }

            EmitTailLocal(recursiveEntry, 3, frame: null);

            Mark(fallbackLabel);
            EmitFrameAllocation(function.Arity, hasAccumulator ? 2 : 0);
            if (hasAccumulator)
            {
                owner.MoveRegister(
                    XRegisterTag,
                    2,
                    YRegisterTag,
                    0
                );
                owner.MoveRegister(
                    XRegisterTag,
                    1,
                    YRegisterTag,
                    1
                );
            }

            owner.EmitConstant(seed, 2);
            owner.EmitConstant(shift, 1);
            owner.EmitCallLine(remainder, location);
            owner.EmitFieldCall(3, remainderEntry);
            if (fallbackCount is not null)
            {
                previous = CallLocation(remainder, location);
                Arithmetic(fallbackCount, 1);
                owner.EmitTypedRegister(
                    0,
                    IntegerTypeBits,
                    lower: 1,
                    registerTag: YRegisterTag
                );
                owner.Operand(BeamWire.IntegerTag, 1);
                owner.Operand(XRegisterTag, 1);
            }

            owner.Op(TestHeap, BeamWire.UnsignedTag, hasAccumulator ? 8 : 6);
            owner.Operand(BeamWire.UnsignedTag, hasAccumulator ? 2 : 1);
            int unusedRegister = hasAccumulator ? 2 : 1;
            owner.EmitTuplePart(0, 0, unusedRegister);
            owner.EmitTuplePart(0, 1, 0);
            owner.Op(PutList, XRegisterTag, 0);
            owner.Operand(AtomTag, NilAtomIndex);
            owner.Operand(XRegisterTag, 0);
            if (hasAccumulator)
            {
                owner.Op(PutList, YRegisterTag, 1);
                owner.Operand(XRegisterTag, 0);
                owner.Operand(XRegisterTag, 0);
            }

            owner.EmitTupleHeader(XRegisterTag, 3);
            if (hasAccumulator)
            {
                owner.Operand(XRegisterTag, 1);
            }
            else
            {
                owner.LiteralOperand(((Expr.Literal)total).Value);
            }

            owner.Operand(XRegisterTag, unusedRegister);
            owner.Operand(XRegisterTag, 0);
            owner.EmitFrameReturn(hasAccumulator ? 2 : 0);
            if (hasAccumulator)
            {
                Mark(empty);
                owner.Op(TestHeap, BeamWire.UnsignedTag, 4);
                owner.Operand(BeamWire.UnsignedTag, 3);
                owner.EmitTupleHeader(XRegisterTag, 3);
                owner.Operand(XRegisterTag, 2);
                owner.Operand(BeamWire.IntegerTag, 0);
                owner.Operand(XRegisterTag, 1);
                owner.instructions.WriteByte(Return);
            }

            return true;

            bool ReadPack(Expr piece, int index)
            {
                Expr.Binary? combined = null;
                if (index != 0)
                {
                    if (piece is not Expr.Binary { Operator: ErlangOperators.BitwiseOr } join || !ReadPack(join.Left, index - 1))
                    {
                        return false;
                    }

                    combined = join;
                    piece = join.Right;
                }

                int amount = items.Count - index - 1;
                Expr.Binary? shiftExpression = null;
                if (amount > 0)
                {
                    if (piece is not Expr.Binary
                        {
                            Operator: ErlangOperators.ShiftLeft, Right: Expr.Literal { Value: Integer shiftCount }
                        } bits || shiftCount.Value != amount)
                    {
                        return false;
                    }

                    shiftExpression = bits;
                    piece = bits.Left;
                }

                if (piece is not Expr.Variable bit || ((Pattern.Variable)items[index]).Name != bit.Name)
                {
                    return false;
                }

                stages.Add((shiftExpression, combined));

                return true;
            }

            bool EntriesForTail(Expr.Call call, out int entry) => entries.TryGetValue((call.Function, 3), out entry);

            static Expr.Binary? ReadIncrement(Expr value, string input) => value is Expr.Binary
            {
                Operator: ErlangOperators.Plus,
                Left: Expr.Variable source,
                Right: Expr.Literal { Value: Integer increment }
            } sum && source.Name == input && increment.Value.IsOne ? sum : null;

            void Arithmetic(Expr.Binary operation, int live)
            {
                owner.EmitArithmeticLine(operation.SourceLine, ref previous);
                owner.Op(GcBifTwo, LabelTag, 0);
                owner.Operand(BeamWire.UnsignedTag, live);
                owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, operation.Operator, 2));
            }
        }
    }
}
