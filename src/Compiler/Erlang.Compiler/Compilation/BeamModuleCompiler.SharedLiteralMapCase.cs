using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private (Dictionary<Term, int> Slots, int Failure)? EmitSharedLiteralMapCase(
            IReadOnlyList<Clause> clauses,
            int source
        )
        {
            if (!owner.SsaTypeInferenceEnabled || owner.ModuleOptimizationDisabled
                || owner.receiveScratchRegisters is not null || generatorKeyScope is not null
                || clauses.Count < 2 || ReadLiteralMapCaseKeys(clauses[0]) is not { } keys)
            {
                return null;
            }

            for (int index = 1; index < clauses.Count; index++)
            {
                if (ReadLiteralMapCaseKeys(clauses[index]) is not { } compared || !keys.SequenceEqual(compared))
                {
                    return null;
                }
            }

            int failure = Label();
            var slots = new Dictionary<Term, int>();
            owner.Op(IsMap, LabelTag, failure);
            owner.Operand(YRegisterTag, source);
            foreach (Term key in keys)
            {
                // Keep existing scalar result placement. The common logical
                // projections should not assign both physical destinations here.
                owner.Op(GetMapElements, LabelTag, failure);
                owner.EmitTypedRegister(source, MapTypeBits, registerTag: YRegisterTag);
                owner.instructions.WriteByte(ExtendedListOperand);
                owner.Operand(BeamWire.UnsignedTag, 2);
                owner.LiteralOperand(key);
                owner.Operand(XRegisterTag, 0);
                slots.Add(key, Save());
            }

            return (slots, failure);
        }

        private static Term[]? ReadLiteralMapCaseKeys(Clause clause)
        {
            if (clause.Patterns is not [MapPattern { Fields.Count: > 0 } pattern])
            {
                return null;
            }

            var keys = new Term[pattern.Fields.Count];
            for (int index = 0; index < pattern.Fields.Count; index++)
            {
                MapPatternField field = pattern.Fields[index];
                if (field.Key is not Expr.Literal { Value: Atom or Integer } literal
                    || field.Value is not (Erlang.Pattern.Variable or Erlang.Pattern.Any
                        or Erlang.Pattern.Literal { Value: Atom or Integer }))
                {
                    return null;
                }

                keys[index] = literal.Value;
            }

            Array.Sort(keys, (left, right) => TermOrder.Compare(left, right, exact: true));
            for (int index = 1; index < keys.Length; index++)
            {
                if (keys[index - 1].Equals(keys[index]))
                {
                    return null;
                }
            }

            return keys;
        }
    }
}
