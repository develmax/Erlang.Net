namespace Erlang;

internal static class NativeRecordRegistrations
{
    public static void Register(ModuleRegistry registry)
    {
        registry.Register(
            NativeRecordNames.Module,
            NativeRecordNames.GetDefinition,
            2,
            (context, args) =>
        {
            if (args[0] is not Atom module || args[1] is not Atom name || context.Runtime.Modules.NativeRecordDefinition(module.Name, name.Name) is not { } definition)
                throw new ErlangException(ErlangErrorReasons.BadArgument);

            return ValueTask.FromResult(NativeRecordLanguage.Definition(definition));
        }
        );
        registry.Register(
            NativeRecordNames.Module,
            NativeRecordNames.Create,
            4,
            (_, args) => ValueTask.FromResult(Create(args))
        );
        registry.Register(
            NativeRecordNames.Module,
            NativeRecordNames.Update,
            4,
            (_, args) => ValueTask.FromResult(Update(args))
        );
        registry.Register(
            NativeRecordNames.Module,
            NativeRecordNames.Get,
            2,
            (_, args) => ValueTask.FromResult(Get(args))
        );
        registry.Register(
            NativeRecordNames.Module,
            NativeRecordNames.GetModule,
            1,
            (_, args) => ValueTask.FromResult<Term>(Require(args[0], false).Module)
        );
        registry.Register(
            NativeRecordNames.Module,
            NativeRecordNames.GetName,
            1,
            (_, args) => ValueTask.FromResult<Term>(Require(args[0], false).Name)
        );
        registry.Register(
            NativeRecordNames.Module,
            NativeRecordNames.GetFieldNames,
            1,
            (_, args) => ValueTask.FromResult(Cons.From(Require(args[0], true).FieldNames))
        );
        registry.Register(
            NativeRecordNames.Module,
            NativeRecordNames.IsExported,
            1,
            (_, args) => ValueTask.FromResult<Term>(CoreModules.Boolean(Require(args[0], true).IsExported))
        );
    }

    private static NativeRecordTerm Require(Term value, bool badRecord)
    {
        if (value is NativeRecordTerm record)
            return record;

        throw badRecord ? Failure(NativeRecordNames.BadRecord, value) : new ErlangException(ErlangErrorReasons.BadArgument);
    }

    private static ErlangException Failure(string reason, Term value) => new(Term.Tuple(Term.A(reason), value));

    private static Term Create(IReadOnlyList<Term> args)
    {
        if (args[0] is not Atom module || args[1] is not Atom name)
            throw Failure(NativeRecordNames.BadRecord, args[1]);
        if (args[3] is not MapTerm options)
            throw Failure(ErlangErrorReasons.BadMap, args[3]);
        if (options.Entries.Count != 1 || !options.TryGet(Term.A(NativeRecordNames.ExportedOption), out var exported) || exported is not Atom { Name: ErlangBooleanAtoms.True or ErlangBooleanAtoms.False })
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        var pairs = Cons.Items(args[2]).ToArray();
        var fields = new List<KeyValuePair<Atom, Term>>();
        foreach (var pair in pairs)
        {
            if (pair is not TupleTerm { Items.Count: 2 } tuple)
                throw new ErlangException(ErlangErrorReasons.BadArgument);
            if (tuple.Items[0] is not Atom key)
                throw Failure(NativeRecordNames.BadField, Term.Tuple(Term.Tuple(module, name), tuple.Items[0]));
            fields.Add(new(key, tuple.Items[1]));
        }

        return new NativeRecordTerm(
            module,
            name,
            exported.Equals(Term.A(ErlangBooleanAtoms.True)),
            fields
        );
    }

    private static Term Get(IReadOnlyList<Term> args)
    {
        var record = Require(args[1], false);
        if (args[0] is Atom key)
        {
            for (int index = 0; index < record.FieldNames.Count; index++)
            {
                if (record.FieldNames[index].Equals(key))
                    return record.Values[index];
            }
        }

        throw new ErlangException(ErlangErrorReasons.BadArgument);
    }

    private static Term Update(IReadOnlyList<Term> args)
    {
        var record = Require(args[0], true);
        if (args[2] is not Atom { Name: NativeRecordNames.Wildcard } && (!record.Module.Equals(args[1]) || !record.Name.Equals(args[2])))
            throw Failure(NativeRecordNames.BadRecord, args[2]);
        if (args[3] is not MapTerm updates)
            throw Failure(ErlangErrorReasons.BadMap, args[3]);
        if (updates.Entries.Count == 0)
            return record;
        foreach (var entry in updates.Entries.OrderBy(
            entry => entry.Key,
            Comparer<Term>.Create((left, right) => left is Atom a && right is Atom b ? AtomNameRegistry.IndexOf(a.Name).CompareTo(AtomNameRegistry.IndexOf(b.Name)) : TermOrder.Compare(left, right, true))
        ))
        {
            if (entry.Key is not Atom atom || !record.FieldNames.Contains(atom))
                throw Failure(NativeRecordNames.BadField, Term.Tuple(Term.Tuple(record.Module, record.Name), entry.Key));
        }

        return new NativeRecordTerm(
            record.Module,
            record.Name,
            record.IsExported,
            record.FieldNames.Select((field, index) => new KeyValuePair<Atom, Term>(field, updates.TryGet(field, out var value) ? value! : record.Values[index]))
        );
    }
}
