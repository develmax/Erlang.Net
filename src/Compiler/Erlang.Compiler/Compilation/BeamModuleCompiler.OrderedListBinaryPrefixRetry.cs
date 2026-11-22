using System.Buffers.Binary;
using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private sealed record OpenBinaryListClause(byte[] Prefix, Expr.Call Reverse);

        private bool TryEmitOrderedListBinaryPrefixRetry(FunctionDefinition function)
        {
            const int squeezedBytes = 2;
            const int embeddedStringBytes = MaxEnsuredStringBits / BitSyntaxDefaults.BinaryUnit;
            if (!owner.SsaTypeInferenceEnabled || owner.ModuleOptimizationDisabled || recursiveClosure is not null
                || owner.OnLoadFunction is not null || owner.IsDeclaredNative((function.Name, function.Arity))
                || function.Arity != 2 || function.Clauses is not [var shorter, var longer, var other]
                || ReadOpenBinaryListClause(shorter) is not { } first || ReadOpenBinaryListClause(longer) is not { } second
                || first.Prefix.Length <= embeddedStringBytes
                || second.Prefix.Length - first.Prefix.Length <= squeezedBytes + embeddedStringBytes
                || !second.Prefix.AsSpan().StartsWith(first.Prefix)
                || !IsPlainListPrefixRecurrence(other, function.Name)
                || !entries.TryGetValue((function.Name, function.Arity), out int entry))
            {
                return false;
            }

            // The cons match keeps the original head for the raw fallback.
            // The binary clauses share the prefix and continue from the same cursor.
            int functionInfo = owner.nextLabel - 2;
            int retry = Label();
            int fallback = Label();
            const int context = 3;
            const int integer = 4;
            owner.Op(IsNonemptyList, LabelTag, functionInfo);
            owner.Operand(XRegisterTag, 0);
            owner.Op(GetList, XRegisterTag, 0);
            owner.Operand(XRegisterTag, 2);
            owner.Operand(XRegisterTag, 0);
            owner.Op(StartBinaryMatch, LabelTag, fallback);
            owner.Operand(XRegisterTag, 2);
            owner.Operand(BeamWire.UnsignedTag, context);
            owner.Operand(XRegisterTag, context);
            owner.EmitBinaryString(first.Prefix, fallback, context);
            EmitByteAlignedTail(context, retry);
            EmitBinaryListReverseResult(first.Reverse);

            Mark(retry);
            const int squeezedBits = squeezedBytes * BitSyntaxDefaults.BinaryUnit;
            EmitBitMatchHeader(
                context,
                fallback,
                9,
                squeezedBits,
                BitSyntaxDefaults.NumericUnit
            );
            owner.Operand(AtomTag, owner.Intern(BitSegmentTypes.Integer));
            owner.Operand(BeamWire.UnsignedTag, integer);
            owner.Operand(AtomTag, NilAtomIndex);
            owner.Operand(BeamWire.UnsignedTag, squeezedBits);
            owner.Operand(BeamWire.UnsignedTag, BitSyntaxDefaults.NumericUnit);
            owner.Operand(XRegisterTag, integer);
            owner.Op(ExactEqualTest, LabelTag, fallback);
            owner.EmitTypedRegister(
                integer,
                IntegerTypeBits,
                0,
                ushort.MaxValue
            );
            ReadOnlySpan<byte> extension = second.Prefix.AsSpan(first.Prefix.Length);
            owner.Operand(BeamWire.IntegerTag, BinaryPrimitives.ReadUInt16BigEndian(extension));
            owner.EmitBinaryString(extension[squeezedBytes..], fallback, context);
            EmitByteAlignedTail(context, fallback);
            EmitBinaryListReverseResult(second.Reverse);

            Mark(fallback);
            Heap(ConsHeapWords, context);
            owner.Op(PutList, XRegisterTag, 2);
            owner.Operand(XRegisterTag, 1);
            owner.Operand(XRegisterTag, 1);
            EmitTailLocal(entry, function.Arity, frame: null);

            return true;
        }

        private static OpenBinaryListClause? ReadOpenBinaryListClause(Clause clause)
        {
            if (clause is not
                {
                    Guard: null,
                    Patterns:
                    [
                        global::Erlang.Pattern.List { Items: [BitPattern pattern], Tail: global::Erlang.Pattern.Variable tail },
                        global::Erlang.Pattern.Variable state
                    ],
                    Body: Expr.Tuple
                    {
                        Items:
                        [
                            Expr.Call
                        {
                            Module: ListModuleNames.Module, Function: ListModuleNames.Reverse,
                            DynamicRemote: false, Arguments: [Expr.Variable list]
                        } reverse,
                            Expr.Variable rest
                        ]
                    }
                }
                || !DistinctBindings(tail, state) || list.Name != state.Name || rest.Name != tail.Name
                || pattern.Segments.Count < 2 || pattern.Segments[^1] is not
                {
                    Value: global::Erlang.Pattern.Variable { Name: VariableScopeNames.Wildcard },
                    Specification:
                    {
                        Type: BitSegmentTypes.Binary, Size: null, Unit: BitSyntaxDefaults.BinaryUnit,
                        Signed: false, Endian: BitByteOrders.Big
                    }
                }
                || ReadBinaryLiteral(new BitPattern(pattern.Segments.SkipLast(1).ToArray())) is not { } prefix)
            {
                return null;
            }

            return new(prefix, reverse);
        }

        private static bool IsPlainListPrefixRecurrence(Clause clause, string function) =>
            clause is
            {
                Guard: null,
                Patterns:
                [
                    global::Erlang.Pattern.List
                {
                    Items: [global::Erlang.Pattern.Variable head],
                    Tail: global::Erlang.Pattern.Variable tail
                },
                    global::Erlang.Pattern.Variable state
                ],
                Body: Expr.Call
                {
                    Module: null, DynamicRemote: false, Function: var target,
                    Arguments: [Expr.Variable rest, Expr.List { Items: [Expr.Variable item], Tail: Expr.Variable list }]
                }
            }
            && target == function && DistinctBindings(head, tail, state)
            && rest.Name == tail.Name && item.Name == head.Name && list.Name == state.Name;

        private void EmitByteAlignedTail(int context, int failure) =>
            EmitBitMatchHeader(
                context,
                failure,
                3,
                0,
                BitSyntaxDefaults.BinaryUnit
            );

        private void EmitBinaryListReverseResult(Expr.Call reverse)
        {
            owner.Op(Allocate, BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.MoveRegister(
                XRegisterTag,
                0,
                YRegisterTag,
                0
            );
            owner.MoveRegister(
                XRegisterTag,
                1,
                XRegisterTag,
                0
            );
            owner.EmitCallLine(reverse, location);
            owner.Op(ExternalCall, BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(ListModuleNames.Module, ListModuleNames.Reverse, 1));
            Heap(3, 1);
            owner.EmitTupleHeader(XRegisterTag, 2);
            owner.Operand(XRegisterTag, 0);
            owner.Operand(YRegisterTag, 0);
            owner.Op(Deallocate, BeamWire.UnsignedTag, 1);
            owner.instructions.WriteByte(Return);
        }
    }
}
