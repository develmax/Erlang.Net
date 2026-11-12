using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private ModuleDefinition? SourceModule => sourceModule;

    private sealed partial class GeneralEmitter
    {
        private Dictionary<string, int>? generatorKeyScope;

        private bool TryEmitDirectMapPattern(FunctionDefinition function)
        {
            if (function.Clauses is not [
                { Guard: null, Patterns: [MapPattern { Fields: [{ Key: Expr.Literal key, Value: Pattern.Variable binding }] }],
                  Body: Expr.Variable result },
                { Guard: null, Patterns: [Pattern.Variable { }], Body: Expr.Literal fallback }
            ] || binding.Name != result.Name)
            {
                return false;
            }

            int failure = Label();
            EmitMapTypeTest(0, failure);
            owner.Op(GetMapElements, LabelTag, failure);
            owner.EmitTypedRegister(0, MapTypeBits);
            owner.instructions.WriteByte(ExtendedListOperand);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.LiteralOperand(key.Value);
            owner.Operand(XRegisterTag, 0);
            owner.instructions.WriteByte(Return);
            Mark(failure);
            owner.EmitConstant(fallback.Value, 0);
            owner.instructions.WriteByte(Return);

            return true;
        }

        private void EmitMapPattern(MapPattern map, int value, int failure)
        {
            owner.Op(IsMap, LabelTag, failure);
            owner.Operand(YRegisterTag, value);
            var bindings = variables;
            int?[] keys;
            try
            {
                variables = generatorKeyScope ?? bindings;
                keys = map.Fields.Select(field => GuardSlot(field.Key, failure)).ToArray();
            }
            finally
            {
                variables = bindings;
            }
            for (int index = 0; index < map.Fields.Count; index++)
            {
                MapPatternField field = map.Fields[index];
                owner.Op(GetMapElements, LabelTag, failure);
                owner.Operand(YRegisterTag, value);
                owner.instructions.WriteByte(ExtendedListOperand);
                owner.Operand(BeamWire.UnsignedTag, 2);
                GuardOperand(field.Key, keys[index]);
                owner.Operand(XRegisterTag, 0);
                Pattern(field.Value, Save(), failure);
            }
        }

        private void EmitListQualifiers(Expr.ListComprehension comprehension)
        {
            if (comprehension.Items.Count != 1)
            {
                throw new NotSupportedException(Unsupported);
            }

            EmitComprehensionList(comprehension.Items, comprehension.Qualifiers);
        }

        private void EmitComprehensionList(IReadOnlyList<Expr> items, IReadOnlyList<ComprehensionQualifier> qualifiers)
        {
            owner.EmitConstant(Nil.Value, 0);
            int accumulator = Save();
            EmitComprehensionQualifiers(qualifiers, items, () =>
            {
                foreach (Expr output in items)
                {
                    Expression(output);
                    owner.Op(TestHeap, BeamWire.UnsignedTag, 2);
                    owner.Operand(BeamWire.UnsignedTag, 1);
                    owner.Op(PutList, XRegisterTag, 0);
                    owner.Operand(YRegisterTag, accumulator);
                    owner.Operand(XRegisterTag, 0);
                    owner.MoveRegister(XRegisterTag, 0, YRegisterTag, accumulator);
                }
            });
            Load(accumulator);
            owner.EmitConstant(Nil.Value, 1);
            Call("lists", "reverse", 2);
        }

        private void EmitComprehensionQualifiers(
            IReadOnlyList<ComprehensionQualifier> qualifiers,
            IReadOnlyList<Expr> outputs,
            Action emitOutput
        )
        {
            if (qualifiers.Any(qualifier => qualifier is not (ComprehensionQualifier.Generator
                or ComprehensionQualifier.MapGenerator or ComprehensionQualifier.BinaryGenerator
                or ComprehensionQualifier.Filter or ComprehensionQualifier.Zip)))
            {
                throw new NotSupportedException(Unsupported);
            }

            int done = Label();
            var scope = new Dictionary<string, int>(variables, StringComparer.Ordinal);
            EmitQualifier(0, done);
            Mark(done);
            variables = scope;

            void EmitQualifier(int index, int reject)
            {
                if (index == qualifiers.Count)
                {
                    emitOutput();

                    return;
                }

                if (qualifiers[index] is ComprehensionQualifier.Filter filter)
                {
                    EmitFilter(
                        filter.Expression,
                        reject,
                        LocalFunctionEntries.QualifierExpressions(qualifiers.Skip(index + 1)).Concat(outputs)
                    );
                    EmitQualifier(index + 1, reject);

                    return;
                }

                if (qualifiers[index] is ComprehensionQualifier.MapGenerator mapGenerator)
                {
                    EmitMapGenerator(mapGenerator, reject, next => EmitQualifier(index + 1, next));
                    return;
                }

                if (qualifiers[index] is ComprehensionQualifier.BinaryGenerator binaryGenerator)
                {
                    EmitBinaryGenerator(binaryGenerator, reject, next => EmitQualifier(index + 1, next));
                    return;
                }

                if (qualifiers[index] is ComprehensionQualifier.Zip zip)
                {
                    EmitListZip(zip, reject, next => EmitQualifier(index + 1, next));
                    return;
                }

                var generator = (ComprehensionQualifier.Generator)qualifiers[index];
                var outer = new Dictionary<string, int>(variables, StringComparer.Ordinal);
                Expression(generator.Source);
                int current = Save();
                int loop = Label();
                int end = Label();
                int mismatch = generator.Strict ? Label() : loop;
                Mark(loop);
                Load(current);
                owner.EmitNonemptyList(0, end);
                owner.EmitListParts(0, XRegisterTag, 1, XRegisterTag, 0);
                owner.MoveRegister(XRegisterTag, 0, YRegisterTag, current);
                owner.MoveRegister(XRegisterTag, 1, XRegisterTag, 0);
                int item = Save();
                MatchGeneratorPattern(generator.Pattern, item, mismatch);
                EmitQualifier(index + 1, loop);
                JumpTo(loop);
                if (generator.Strict)
                {
                    Mark(mismatch);
                    Load(item);
                    owner.Op(BadMatch, XRegisterTag, 0);
                }

                Mark(end);
                Load(current);
                int invalid = Label();
                owner.EmitNilTest(XRegisterTag, 0, invalid);
                JumpTo(reject);
                Mark(invalid);
                ErrorTuple(ComprehensionErrorReasons.BadGenerator, current);
                variables = outer;
            }
        }

        private void MatchGeneratorPattern(Pattern pattern, int item, int failure) =>
            MatchGeneratorPatterns([(pattern, item)], failure);

        private void MatchGeneratorPatterns(IReadOnlyList<(Pattern Pattern, int Slot)> captures, int failure)
        {
            var previous = generatorKeyScope;
            generatorKeyScope = new(variables, StringComparer.Ordinal);
            try
            {
                foreach (var (pattern, _) in captures)
                {
                    ShadowPattern(pattern);
                }
                foreach (var (pattern, slot) in captures)
                {
                    Pattern(pattern, slot, failure);
                }
            }
            finally
            {
                generatorKeyScope = previous;
            }
        }

        private void EmitFilter(Expr expression, int reject, IEnumerable<Expr> continuation)
        {
            if (ComprehensionGuard.IsTest(expression, owner.SourceModule))
            {
                Guard(expression, reject);

                return;
            }

            EmitWithContinuation(expression, continuation, guardFailure: null, returnValue: true);
            int testFalse = Label();
            int passed = Label();
            owner.EmitAtomTest(0, "true", testFalse);
            JumpTo(passed);
            Mark(testFalse);
            int invalid = Label();
            owner.EmitAtomTest(0, "false", invalid);
            JumpTo(reject);
            Mark(invalid);
            ErrorTuple(ComprehensionErrorReasons.BadFilter, Save());
            Mark(passed);
        }
    }
}
