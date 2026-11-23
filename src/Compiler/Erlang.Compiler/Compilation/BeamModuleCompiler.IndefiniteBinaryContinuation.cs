using System.Numerics;
using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitIndefiniteBinaryContinuation(FunctionDefinition function)
        {
            if (!owner.SsaTypeInferenceEnabled || owner.ModuleOptimizationDisabled || owner.OnLoadFunction is not null
                || recursiveClosure is not null || function.Clauses is not
                [
                {
                    Guard: null, Patterns: [BitPattern terminal],
                    Body: Expr.Tuple { Items: [Expr.Literal { Value: Atom tag }, Expr.Variable returned] }
                },
                {
                    Guard: null, Patterns: [Pattern.Variable input],
                    Body: Expr.Sequence { Items: [var first, var second, Expr.Call recur] }
                }
                ]
                || owner.IsDeclaredNative((function.Name, 1))
                || ReadIndefiniteBinaryEnd(terminal, returned) is not { } ending
                || ReadCheckedBinaryNext(first, input.Name, tag) is not { } tagRead
                || ReadCheckedBinaryNext(second, tagRead.Remainder.Name, tag) is not { } lengthRead
                || !DistinctBindings(input, tagRead.Remainder, lengthRead.Remainder)
                || recur is not { Module: null, DynamicRemote: false, Arguments: [Expr.Variable next] }
                || recur.Function != function.Name || next.Name != lengthRead.Remainder.Name
                || !entries.TryGetValue((function.Name, 1), out int loopEntry)
                || !localFunctions.TryGetValue((tagRead.Call.Function, 1), out var dispatcher)
                || !localFunctions.TryGetValue((lengthRead.Call.Function, 1), out var consumer)
                || !entries.TryGetValue((dispatcher.Name, 1), out int dispatchEntry)
                || !entries.TryGetValue((consumer.Name, 1), out int consumeEntry)
                || !ProvesResumedTaggedBinaryPair(dispatcher, tag)
                || owner.IsDeclaredNative((consumer.Name, 1))
                || ReadBinaryLengthConsumer(consumer) is not { } read || !read.ResultTag.Equals(tag)
                || read.Forward.Function != function.Name || !HasIndefiniteBinaryCallers())
            {
                return false;
            }

            int advance = Label();
            EmitBitMatchStart(context: 0, failure: 0, knownBinary: true);
            owner.Op(GetBinaryPosition, XRegisterTag, 0);
            owner.Operand(XRegisterTag, 1);
            owner.Operand(BeamWire.UnsignedTag, 1);
            EmitBitMatchHeader(
                0,
                advance,
                11,
                ending.Width,
                BitSyntaxDefaults.BitStringUnit
            );
            EmitFixedBitEquality(ending.Width, ending.Value);
            owner.Operand(AtomTag, owner.Intern(ReadBinaryTail));
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, BitSyntaxDefaults.BitStringUnit);
            owner.Operand(XRegisterTag, 0);
            EmitTaggedRegisterResult(tag);
            owner.instructions.WriteByte(Return);

            Mark(advance);
            EmitFrameAllocation(2, 0);
            owner.Op(SetBinaryPosition, XRegisterTag, 0);
            owner.Operand(XRegisterTag, 1);
            owner.EmitCallLine(tagRead.Call, location);
            owner.EmitFieldCall(1, dispatchEntry);
            owner.EmitTupleElement(1, 0);
            owner.EmitCallLine(lengthRead.Call, CallLocation(tagRead.Call, location));
            owner.EmitFieldCall(1, consumeEntry);
            owner.EmitTupleElement(1, 0);
            EmitTailLocal(loopEntry, arity: 1, frame: 0);

            return true;

            bool HasIndefiniteBinaryCallers()
            {
                bool incoming = false;

                return HasOnlyLocalCalls(
                    function,
                    1,
                    (caller, call) =>
                    {
                        if (caller.Name == consumer.Name && ReferenceEquals(read.Forward, call))
                        {
                            incoming = true;

                            return true;
                        }

                        return caller.Name == function.Name && ReferenceEquals(recur, call);
                    }
                ) && incoming;
            }
        }

        private bool ProvesResumedTaggedBinaryPair(FunctionDefinition dispatcher, Atom tag)
        {
            return !owner.IsDeclaredNative((dispatcher.Name, 1))
                && ReadBinaryPrefixSwitch(dispatcher) is { } prefix && prefix.ResultTag.Equals(tag)
                && prefix.Forward.Function != dispatcher.Name
                && localFunctions.TryGetValue((prefix.Forward.Function, 1), out var target)
                && !owner.IsDeclaredNative((target.Name, 1))
                && ReadBinaryPrefixSwitch(target) is { } loop && loop.Forward.Function == target.Name
                && loop.ResultTag.Equals(tag) && HasProvenBinaryPrefixCallers(target, loop);
        }

        private static (Expr.Call Call, Pattern.Variable Remainder)? ReadCheckedBinaryNext(Expr item, string input, Atom tag) =>
            item is Expr.Match
            {
                Pattern: Pattern.Tuple { Items: [Pattern.Literal { Value: Atom actual }, Pattern.Variable rest] },
                Value: Expr.Call { Module: null, DynamicRemote: false, Arguments: [Expr.Variable source] } call
            } && source.Name == input && actual.Equals(tag) ? (call, rest) : null;

        private static (int Width, BigInteger Value)? ReadIndefiniteBinaryEnd(BitPattern pattern, Expr.Variable returned)
        {
            if (pattern.Segments is not
                [
                    ..,
                {
                    Value: Pattern.Variable tail,
                    Specification: { Type: BitSegmentTypes.Binary, Unit: BitSyntaxDefaults.BinaryUnit, Size: null }
                }
                ]
                || returned.Name != tail.Name || !DistinctBindings(tail))
            {
                return null;
            }

            int width = 0;
            BigInteger prefix = BigInteger.Zero;
            foreach (var segment in pattern.Segments.Take(pattern.Segments.Count - 1))
            {
                if (FixedSize(segment.Specification) is not int size
                    || size > MaximumEqualityCommandBits || segment.Specification.Unit != BitSyntaxDefaults.NumericUnit
                    || width + size > MaximumEqualityCommandBits || ReadFixedBitEquality(segment, size) is not { } bits)
                {
                    return null;
                }

                prefix = (prefix << size) | bits;
                width += size;
            }

            return width != 0 && width % BitSyntaxDefaults.BinaryUnit == 0 ? (width, prefix) : null;
        }
    }
}
