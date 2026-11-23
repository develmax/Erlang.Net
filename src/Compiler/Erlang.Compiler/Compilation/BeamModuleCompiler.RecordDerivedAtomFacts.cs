namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private static bool HasBooleanOperatorResult(Expr expression) => expression is
        Expr.Unary { Operator: ErlangOperators.Not } or Expr.Binary
        {
            Operator: ErlangOperators.And or ErlangOperators.Or or ErlangOperators.Xor
                or ErlangOperators.ExactEqual or ErlangOperators.ExactNotEqual
                or ErlangOperators.NumericEqual or ErlangOperators.NumericNotEqual
                or ErlangOperators.Less or ErlangOperators.LessOrEqual
                or ErlangOperators.Greater or ErlangOperators.GreaterOrEqual
        };

    private bool HasBooleanCallResult(Expr.Call call, bool isLocal)
    {
        if (call.DynamicRemote || call.Module is null && isLocal)
        {
            return false;
        }

        // OTP beam_call_types distinguishes successful result types from
        // argument failure. Each of these calls can still raise.
        int arity = call.Arguments.Count;
        string? module = call.Module ?? ResolveExternalModule(call.Function, arity);

        return module == ListModuleNames.Module && (call.Function, arity) is
            (ListModuleNames.All or ListModuleNames.Any or ListModuleNames.Member
                or ListModuleNames.Prefix or ListModuleNames.Suffix, 2) or (ListModuleNames.KeyMember, 3);
    }

    private sealed partial class ProperListEntryFacts
    {
        private static readonly Fact BooleanSuccess = new(
            Shape.NonList,
            Atoms: new AtomDomain([Term.A(ErlangBooleanAtoms.False), Term.A(ErlangBooleanAtoms.True)])
        );

        private sealed class AtomDomain(Atom[] values) : IEquatable<AtomDomain>
        {
            internal Atom[] Values { get; } = values;

            public bool Equals(AtomDomain? other) =>
                other is not null && Values.SequenceEqual(other.Values);

            public override bool Equals(object? obj) => obj is AtomDomain other && Equals(other);

            public override int GetHashCode()
            {
                var hash = new HashCode();
                foreach (Atom atom in Values)
                {
                    hash.Add(atom);
                }

                return hash.ToHashCode();
            }

            internal static AtomDomain? Join(AtomDomain? left, AtomDomain? right)
            {
                if (left is null || right is null)
                {
                    return null;
                }

                if (left.Equals(right))
                {
                    return left;
                }

                Atom[] merged = left.Values.Concat(right.Values).Distinct()
                    .OrderBy(atom => atom.Name, StringComparer.Ordinal).Take(MaximumFiniteAtomSetSize + 1).ToArray();

                return merged.Length <= MaximumFiniteAtomSetSize ? new(merged) : null;
            }
        }

        private sealed class RecordAtomFields(string name, AtomDomain?[] fields) : IEquatable<RecordAtomFields>
        {
            internal string Name { get; } = name;
            internal AtomDomain?[] Fields { get; } = fields;

            internal bool Matches(string record, int arity) => Name == record && Fields.Length == arity;

            public bool Equals(RecordAtomFields? other) =>
                other is not null && Matches(other.Name, other.Fields.Length) && Enumerable.SequenceEqual(Fields, other.Fields);

            public override bool Equals(object? obj) => obj is RecordAtomFields other && Equals(other);

            public override int GetHashCode()
            {
                var hash = new HashCode();
                hash.Add(Name, StringComparer.Ordinal);
                foreach (AtomDomain? field in Fields)
                {
                    hash.Add(field);
                }

                return hash.ToHashCode();
            }

            internal static RecordAtomFields? Join(RecordAtomFields? left, RecordAtomFields? right)
            {
                if (left is null || right is null || !left.Matches(right.Name, right.Fields.Length))
                {
                    return null;
                }

                if (left.Equals(right))
                {
                    return left;
                }

                return Create(left.Name, left.Fields.Zip(right.Fields, AtomDomain.Join).ToArray());
            }

            internal static RecordAtomFields? Create(string name, AtomDomain?[] fields) =>
                fields.Any(field => field is not null) ? new(name, fields) : null;
        }

        private Fact ReadRecordAtoms(Expr.RecordConstruction record, Dictionary<string, Fact> scope)
        {
            var fields = new AtomDomain?[record.Definition.Fields.Count + 1];
            var indices = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int index = 0; index < record.Definition.Fields.Count; index++)
            {
                RecordField field = record.Definition.Fields[index];
                indices.Add(field.Name, index + 1);
                if (field.Value is Expr.Literal { Value: Atom atom })
                {
                    fields[index + 1] = new AtomDomain([atom]);
                }
            }

            foreach (RecordField field in record.Fields)
            {
                Fact value = Read(field.Value, new(scope, StringComparer.Ordinal));
                if (value == Fact.Unreachable)
                {
                    return Fact.Unreachable;
                }

                fields[indices[field.Name]] = value.Atoms;
            }

            return new Fact(Shape.NonList, RecordAtoms: RecordAtomFields.Create(record.Definition.Name, fields));
        }

        private Fact ReadUpdatedRecordAtoms(Expr.RecordUpdate update, Dictionary<string, Fact> scope)
        {
            Fact original = Read(update.Value, new(scope, StringComparer.Ordinal));
            if (original == Fact.Unreachable)
            {
                return original;
            }

            var fields = original.RecordAtoms is { } record && record.Matches(update.Name, update.Arity)
                ? record.Fields.ToArray()
                : new AtomDomain?[update.Arity];
            foreach (RecordUpdateField field in update.Fields)
            {
                Fact value = Read(field.Value, new(scope, StringComparer.Ordinal));
                if (value == Fact.Unreachable)
                {
                    return Fact.Unreachable;
                }

                fields[field.Index] = value.Atoms;
            }

            return new Fact(Shape.NonList, RecordAtoms: RecordAtomFields.Create(update.Name, fields));
        }

        private Fact ReadRecordFieldAtoms(Expr.RecordAccess access, Dictionary<string, Fact> scope)
        {
            Fact original = Read(access.Value, scope);
            if (original == Fact.Unreachable)
            {
                return original;
            }

            return original.RecordAtoms is { } record && record.Matches(access.Name, access.Arity)
                && record.Fields[access.Index] is { } atoms
                ? new Fact(Shape.NonList, Atoms: atoms)
                : Fact.Unknown;
        }
    }
}
