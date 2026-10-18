namespace Erlang;

internal static class ProplistFamilyRegistrations
{

    internal static void Register(ModuleRegistry registry)
    {
        void Add(
            string name,
            int arity,
            Func<ProcessContext,
            IReadOnlyList<Term>,
            ValueTask<Term>> body
        ) => registry.Register(
            ProplistFamilyNames.Module,
            name,
            arity,
            (context, arguments) => body(context, arguments)
        );
        Add(
            ProplistFamilyNames.Property,
            1,
            (_, a) => ValueTask.FromResult(ProplistFamilyLookup.Property(a[0]))
        );
        Add(
            ProplistFamilyNames.Property,
            2,
            (_, a) => ValueTask.FromResult(ProplistFamilyLookup.Property(a[0], a[1]))
        );
        Add(
            ProplistFamilyNames.Unfold,
            1,
            (c, a) => ProplistFamilyLookup.Transform(
            c,
            a[0],
            true
        )
        );
        Add(
            ProplistFamilyNames.Compact,
            1,
            (c, a) => ProplistFamilyLookup.Transform(
            c,
            a[0],
            false
        )
        );
        foreach (string operation in new[]
        {
            ProplistFamilyNames.Lookup,
            ProplistFamilyNames.LookupAll,
            ProplistFamilyNames.IsDefined,
            ProplistFamilyNames.GetAllValues,
            ProplistFamilyNames.AppendValues,
            ProplistFamilyNames.GetBool
        }

        )
            Add(
                operation,
                2,
                (c, a) => ProplistFamilyLookup.Find(
                    c,
                    a[0],
                    a[1],
                    operation
                )
            );
        Add(
            ProplistFamilyNames.GetKeys,
            1,
            (c, a) => ProplistFamilyLookup.Keys(c, a[0])
        );
        Add(
            ProplistFamilyNames.Delete,
            2,
            (c, a) => ProplistFamilyLookup.Delete(
            c,
            a[0],
            a[1]
        )
        );
        Add(
            ProplistFamilyNames.Aliases,
            2,
            (c, a) => ProplistFamilySubstitution.Apply(
                c,
                a[0],
                a[1],
                false
            )
        );
        Add(
            ProplistFamilyNames.Negations,
            2,
            (c, a) => ProplistFamilySubstitution.Apply(
                c,
                a[0],
                a[1],
                true
            )
        );
        Add(
            ProplistFamilyNames.Expand,
            2,
            (c, a) => ProplistFamilyExpansion.Expand(
            c,
            a[0],
            a[1]
        )
        );
        Add(
            ProplistFamilyNames.Normalize,
            2,
            (c, a) => ProplistFamilyProjection.Normalize(
            c,
            a[0],
            a[1]
        )
        );
        Add(
            ProplistFamilyNames.Split,
            2,
            (c, a) => ProplistFamilyProjection.Split(
            c,
            a[0],
            a[1]
        )
        );
        Add(
            ProplistFamilyNames.FromMap,
            1,
            (c, a) => ValueTask.FromResult(ProplistFamilyProjection.FromMap(c, a[0]))
        );
        Add(
            ProplistFamilyNames.ToMap,
            1,
            (c, a) => ProplistFamilyProjection.ToMap(c, a[0])
        );
        Add(
            ProplistFamilyNames.ToMap,
            2,
            (c, a) => ProplistFamilyProjection.ToMap(
            c,
            a[0],
            a[1]
        )
        );
    }
}
