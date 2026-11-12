using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private void EmitBinaryGenerator(ComprehensionQualifier.BinaryGenerator generator, int reject, Action<int> emitBody)
        {
            var segments = generator.Pattern.Segments;
            if (segments.Any(segment => FixedSize(segment.Specification) is null))
            {
                EmitSizedBinaryGenerator(generator, reject, emitBody);
                return;
            }

            var sizes = new int[segments.Count];
            int width = 0;
            for (int index = 0; index < segments.Count; index++)
            {
                BitSegment specification = segments[index].Specification;
                if (FixedSize(specification) is not int size || size * specification.Unit > int.MaxValue - width)
                {
                    throw new NotSupportedException(Unsupported);
                }

                sizes[index] = size;
                width += size * specification.Unit;
            }
            if (width == 0)
            {
                throw new NotSupportedException(Unsupported);
            }

            var outer = new Dictionary<string, int>(variables, StringComparer.Ordinal);
            Expression(generator.Source);
            int source = Save();
            int invalid = Label();
            int loop = Label();
            int exhausted = generator.Strict ? Label() : reject;
            int mismatch = generator.Strict ? Label() : loop;
            EmitBitMatchStart(0, invalid);
            int context = Save();
            int position = generator.Strict ? Slot() : 0;
            Mark(loop);
            Load(context);
            if (generator.Strict)
            {
                owner.Op(GetBinaryPosition, XRegisterTag, 0);
                owner.Operand(YRegisterTag, position);
                owner.Operand(BeamWire.UnsignedTag, 1);
            }

            int operandCount = segments.Sum(segment => IsWildcard(segment.Value) ? 2 : 6);
            EmitBitMatchHeader(0, exhausted, operandCount + 3, width, BitSyntaxDefaults.BitStringUnit);
            var captures = new List<(Pattern Pattern, int Slot)>();
            for (int index = 0; index < segments.Count; index++)
            {
                var segment = segments[index];
                if (IsWildcard(segment.Value))
                {
                    owner.Operand(AtomTag, owner.Intern(SkipBits));
                    owner.Operand(BeamWire.UnsignedTag, sizes[index] * segment.Specification.Unit);
                    continue;
                }

                int slot = Slot();
                EmitBitSegment(segment.Specification, sizes[index], 1, YRegisterTag, slot);
                captures.Add((segment.Value, slot));
            }

            MatchGeneratorPatterns(captures, mismatch);
            emitBody(loop);
            JumpTo(loop);

            if (generator.Strict)
            {
                Mark(exhausted);
                Load(context);
                EmitBitMatchHeader(0, mismatch, 2, 0, null);
                JumpTo(reject);
                Mark(mismatch);
                Load(context);
                owner.Op(SetBinaryPosition, XRegisterTag, 0);
                owner.Operand(YRegisterTag, position);
                owner.Op(GetBinaryTail, XRegisterTag, 0);
                owner.Operand(XRegisterTag, 0);
                owner.Operand(BeamWire.UnsignedTag, 1);
                owner.Op(BadMatch, XRegisterTag, 0);
            }

            Mark(invalid);
            ErrorTuple(ComprehensionErrorReasons.BadGenerator, source);
            variables = outer;
        }

        private void EmitSizedBinaryGenerator(ComprehensionQualifier.BinaryGenerator generator, int reject, Action<int> emitBody)
        {
            var segments = generator.Pattern.Segments;
            if (segments.Any(segment => segment.Specification.Type is not (BitSegmentTypes.Integer or BitSegmentTypes.Binary)
                || FixedSize(segment.Specification) is null && segment.Specification.Size is not Expr.Variable))
            {
                throw new NotSupportedException(Unsupported);
            }

            var outer = new Dictionary<string, int>(variables, StringComparer.Ordinal);
            Expression(generator.Source);
            int source = Save();
            int invalid = Label();
            int loop = Label();
            int exhausted = generator.Strict ? Label() : reject;
            int mismatch = generator.Strict ? Label() : loop;
            EmitBitMatchStart(0, invalid);
            int context = Save();
            int position = generator.Strict ? Slot() : 0;
            Mark(loop);
            if (generator.Strict)
            {
                Load(context);
                owner.Op(GetBinaryPosition, XRegisterTag, 0);
                owner.Operand(YRegisterTag, position);
                owner.Operand(BeamWire.UnsignedTag, 1);
            }

            var captures = new List<(Pattern Pattern, int Slot)>();
            var bound = new HashSet<string>(StringComparer.Ordinal);
            foreach (var segment in segments)
            {
                int slot = ReadSequentialBitSegment(segment.Specification, context, exhausted);
                captures.Add((segment.Value, slot));
                if (segment.Value is Pattern.Variable variable && !IsWildcard(variable) && bound.Add(variable.Name))
                {
                    variables[variable.Name] = slot;
                }
            }

            variables = new(outer, StringComparer.Ordinal);
            MatchGeneratorPatterns(captures, mismatch);
            emitBody(loop);
            JumpTo(loop);

            if (generator.Strict)
            {
                Mark(exhausted);
                Load(context);
                owner.Op(SetBinaryPosition, XRegisterTag, 0);
                owner.Operand(YRegisterTag, position);
                EmitBitMatchHeader(0, mismatch, 2, 0, null);
                JumpTo(reject);
                Mark(mismatch);
                Load(context);
                owner.Op(SetBinaryPosition, XRegisterTag, 0);
                owner.Operand(YRegisterTag, position);
                owner.Op(GetBinaryTail, XRegisterTag, 0);
                owner.Operand(XRegisterTag, 0);
                owner.Operand(BeamWire.UnsignedTag, 1);
                owner.Op(BadMatch, XRegisterTag, 0);
            }

            Mark(invalid);
            ErrorTuple(ComprehensionErrorReasons.BadGenerator, source);
            variables = outer;
        }
    }
}
