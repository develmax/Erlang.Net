using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private void EmitListZip(ComprehensionQualifier.Zip zip, int reject, Action<int> emitNext)
        {
            if (zip.Generators.Any(generator => generator is not ComprehensionQualifier.Generator))
            {
                throw new NotSupportedException(Unsupported);
            }

            var generators = zip.Generators.Cast<ComprehensionQualifier.Generator>().ToArray();
            var outer = new Dictionary<string, int>(variables, StringComparer.Ordinal);
            var cursors = new int[generators.Length];
            var tails = new int[generators.Length];
            var row = new (Pattern Pattern, int Slot)[generators.Length];
            for (int index = 0; index < generators.Length; index++)
            {
                Expression(generators[index].Source);
                cursors[index] = Save();
            }

            variables = new(outer, StringComparer.Ordinal);
            int loop = Label();
            int end = Label();
            int invalid = Label();
            int advance = Label();
            bool canSkip = generators.Any(generator => !generator.Strict);
            int mismatch = canSkip ? Label() : invalid;

            Mark(loop);
            for (int index = 0; index < generators.Length; index++)
            {
                Load(cursors[index]);
                owner.EmitNonemptyList(0, end);
                owner.EmitListParts(0, XRegisterTag, 1, XRegisterTag, 0);
                tails[index] = Save();
                owner.MoveRegister(XRegisterTag, 1, XRegisterTag, 0);
                row[index] = (generators[index].Pattern, Save());
            }

            MatchGeneratorPatterns(row, mismatch);
            emitNext(advance);
            if (canSkip)
            {
                JumpTo(advance);
                Mark(mismatch);
                variables = new(outer, StringComparer.Ordinal);
                var strictVariables = generators.Where(generator => generator.Strict)
                    .SelectMany(generator => Semantics.Variables(generator.Pattern)).ToHashSet(StringComparer.Ordinal);
                var skip = generators.Select((generator, index) => (
                    generator.Strict ? generator.Pattern : ZipSkipPattern.Create(generator.Pattern, strictVariables),
                    row[index].Slot
                )).ToArray();
                MatchGeneratorPatterns(skip, invalid);
            }

            Mark(advance);
            for (int index = 0; index < cursors.Length; index++)
            {
                owner.MoveRegister(YRegisterTag, tails[index], YRegisterTag, cursors[index]);
            }

            JumpTo(loop);
            Mark(end);
            foreach (int cursor in cursors)
            {
                Load(cursor);
                owner.EmitNilTest(XRegisterTag, 0, invalid);
            }

            JumpTo(reject);
            Mark(invalid);
            EmitBadZipLists(cursors);
            variables = outer;
        }

        private void EmitBadZipLists(int[] cursors)
        {
            owner.Op(TestHeap, BeamWire.UnsignedTag, cursors.Length + 1);
            owner.Operand(BeamWire.UnsignedTag, 0);
            owner.Op(PutTuple, XRegisterTag, 0);
            owner.instructions.WriteByte(ExtendedListOperand);
            owner.Operand(BeamWire.UnsignedTag, cursors.Length);
            foreach (int cursor in cursors)
            {
                owner.Operand(YRegisterTag, cursor);
            }

            ErrorTuple(ComprehensionErrorReasons.BadGenerators, Save());
        }
    }
}
