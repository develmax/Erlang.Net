namespace Erlang.Tests;

// Coverage/role authentication is separate from the exact whole-wire comparison.
// Nothing here removes an error frame, map field or reference from retained ETF.
internal static class OriginalCounters610Protocol
{
    internal static OriginalCounters610Observation Read(Term worker)
    {
        var outer = Tuple(worker, 2);
        Require(outer.Items[1].Equals(Term.A(OriginalCounters610Fields.Normal)));
        var result = Tuple(outer.Items[0], 2);
        Require(result.Items[0].Equals(Term.A(OriginalCounters610Fields.Returned)));
        var body = Tuple(result.Items[1], 2);
        Require(body.Items[0].Equals(Term.A(OriginalCounters610Fields.Counters591)));
        var fields = Fields(
            body.Items[1],
            [OriginalCounters610Fields.Profile, OriginalCounters610Fields.Families, OriginalCounters610Fields.InvalidNew, OriginalCounters610Fields.NativeFamily, OriginalCounters610Fields.SchedulerHandoff]
        );
        var profileFields = Fields(
            fields[OriginalCounters610Fields.Profile],
            [OriginalCounters610Fields.Wordsize, OriginalCounters610Fields.Schedulers, OriginalCounters610Fields.SchedulersOnline, OriginalCounters610Fields.DebugCompiled]
        );
        var profile = new OriginalCounters610Profile(
            Int(profileFields[OriginalCounters610Fields.Wordsize]),
            Int(profileFields[OriginalCounters610Fields.Schedulers]),
            Int(profileFields[OriginalCounters610Fields.SchedulersOnline]),
            Boolean(profileFields[OriginalCounters610Fields.DebugCompiled])
        );
        Require(profile.WordSize == OriginalCounters610Names.RequiredWordSize && profile.Schedulers > 0
            && profile.Schedulers <= OriginalCounters610Names.MaximumSchedulers && profile.Online > 0 && profile.Online <= profile.Schedulers);
        var roles = new Dictionary<string, ReferenceTerm>(StringComparer.Ordinal);
        Term[] families = List(fields[OriginalCounters610Fields.Families]);
        Require(families.Length == 3);
        for (int index = 0; index < 3; index++)
        {
            var family = Tuple(families[index], 2);
            Term expected = index == 0 ? Nil.Value : Term.List(Term.A(index == 1 ? OriginalCounters610Fields.Atomics : OriginalCounters610Fields.WriteConcurrency));
            Require(family.Items[0].Equals(expected));
            var f = Fields(
                family.Items[1],
                [OriginalCounters610Fields.HandleWitness, OriginalCounters610Fields.Initial, OriginalCounters610Fields.Wrap, OriginalCounters610Fields.SubWrap, OriginalCounters610Fields.UnsignedIncrement, OriginalCounters610Fields.CompleteInfo, OriginalCounters610Fields.InvalidGet, OriginalCounters610Fields.InvalidPut, OriginalCounters610Fields.InvalidAdd]
            );
            var wrapper = Tuple(f[OriginalCounters610Fields.HandleWitness], 2);
            Require(wrapper.Items[0].Equals(Term.A(index < 2 ? OriginalCounters610Fields.Atomics : OriginalCounters610Fields.WriteConcurrency)));
            Role(roles, OriginalCounters610Names.RoleNames[index], wrapper.Items[1]);
            Require(List(f[OriginalCounters610Fields.Initial]).Length == 9 && List(f[OriginalCounters610Fields.Initial]).All(x => x.Equals(Term.I(0))));
            Require(f[OriginalCounters610Fields.Wrap].Equals(Term.I(long.MinValue)) && f[OriginalCounters610Fields.SubWrap].Equals(Term.I(long.MaxValue))
                && f[OriginalCounters610Fields.UnsignedIncrement].Equals(Term.I(6)) && f[OriginalCounters610Fields.CompleteInfo] is MapTerm);
            Raised(f[OriginalCounters610Fields.InvalidGet], 5);
            Raised(f[OriginalCounters610Fields.InvalidPut], 4);
            Raised(f[OriginalCounters610Fields.InvalidAdd], 4);
            foreach (Term error in List(f[OriginalCounters610Fields.InvalidGet])) FirstArgument(
                error,
                OriginalCounters610Fields.Counters,
                OriginalCounters610Fields.Get,
                f[OriginalCounters610Fields.HandleWitness]
            );
            foreach (Term error in List(f[OriginalCounters610Fields.InvalidPut])) FirstArgument(
                error,
                OriginalCounters610Fields.Counters,
                OriginalCounters610Fields.Put,
                f[OriginalCounters610Fields.HandleWitness]
            );
            foreach (Term error in List(f[OriginalCounters610Fields.InvalidAdd])) FirstArgument(
                error,
                OriginalCounters610Fields.Counters,
                OriginalCounters610Fields.Add,
                f[OriginalCounters610Fields.HandleWitness]
            );
        }
        Raised(fields[OriginalCounters610Fields.InvalidNew], 8);
        var native = Fields(
            fields[OriginalCounters610Fields.NativeFamily],
            [OriginalCounters610Fields.HandleWitness, OriginalCounters610Fields.WrongKindWitness, OriginalCounters610Fields.Initial, OriginalCounters610Fields.Added, OriginalCounters610Fields.Put, OriginalCounters610Fields.CompleteInfo, OriginalCounters610Fields.NewErrors, OriginalCounters610Fields.WrongKind, OriginalCounters610Fields.BadReference, OriginalCounters610Fields.IndexErrors]
        );
        Role(roles, OriginalCounters610Names.RoleNames[3], native[OriginalCounters610Fields.HandleWitness]);
        Role(roles, OriginalCounters610Names.RoleNames[4], native[OriginalCounters610Fields.WrongKindWitness]);
        Require(native[OriginalCounters610Fields.Initial].Equals(Term.I(0)) && native[OriginalCounters610Fields.Added].Equals(Term.I(-1))
            && native[OriginalCounters610Fields.Put].Equals(Term.I(long.MinValue)) && native[OriginalCounters610Fields.CompleteInfo] is MapTerm);
        Raised(native[OriginalCounters610Fields.NewErrors], 5);
        Raised(native[OriginalCounters610Fields.IndexErrors], 5);
        RaisedOne(native[OriginalCounters610Fields.WrongKind]);
        RaisedOne(native[OriginalCounters610Fields.BadReference]);
        foreach (Term error in List(native[OriginalCounters610Fields.IndexErrors])) FirstArgument(
            error,
            OriginalCounters610Fields.ErtsInternal,
            OriginalCounters610Fields.CountersGet,
            native[OriginalCounters610Fields.HandleWitness]
        );
        FirstArgument(
            native[OriginalCounters610Fields.WrongKind],
            OriginalCounters610Fields.ErtsInternal,
            OriginalCounters610Fields.CountersGet,
            native[OriginalCounters610Fields.WrongKindWitness]
        );
        FirstArgument(
            native[OriginalCounters610Fields.BadReference],
            OriginalCounters610Fields.ErtsInternal,
            OriginalCounters610Fields.CountersInfo,
            Term.A(OriginalCounters610Fields.NotAReference)
        );
        var scheduler = Fields(
            fields[OriginalCounters610Fields.SchedulerHandoff],
            [OriginalCounters610Fields.HandleWitness, OriginalCounters610Fields.Lanes, OriginalCounters610Fields.Iterations, OriginalCounters610Fields.Base, OriginalCounters610Fields.AfterWorkers, OriginalCounters610Fields.AfterPut, OriginalCounters610Fields.AfterAdd, OriginalCounters610Fields.DownReasons, OriginalCounters610Fields.CompleteInfo]
        );
        var schedulerHandle = Tuple(scheduler[OriginalCounters610Fields.HandleWitness], 2);
        Require(schedulerHandle.Items[0].Equals(Term.A(OriginalCounters610Fields.WriteConcurrency)));
        Role(roles, OriginalCounters610Names.RoleNames[5], schedulerHandle.Items[1]);
        int lanes = Math.Min(4, profile.Online);
        Require(List(scheduler[OriginalCounters610Fields.Lanes]).Select(Int).SequenceEqual(Enumerable.Range(1, lanes))
            && scheduler[OriginalCounters610Fields.Iterations].Equals(Term.I(200)) && scheduler[OriginalCounters610Fields.Base].Equals(Term.I(17))
            && scheduler[OriginalCounters610Fields.AfterWorkers].Equals(Term.I(17 + 200 * lanes))
            && scheduler[OriginalCounters610Fields.AfterPut].Equals(Term.I(-19)) && scheduler[OriginalCounters610Fields.AfterAdd].Equals(Term.I(-17))
            && scheduler[OriginalCounters610Fields.CompleteInfo] is MapTerm);
        Term[] reasons = List(scheduler[OriginalCounters610Fields.DownReasons]);
        Require(reasons.Length == lanes && reasons.All(x => x.Equals(Term.A(OriginalCounters610Fields.Normal))));
        Require(roles.Values.Select(DescribeKey).Distinct(StringComparer.Ordinal).Count() == 6);

        return new(profile, roles);
    }

    internal static void Require(bool value)
    {
        if (!value) throw new InvalidDataException(OriginalCounters610Names.Failure);
    }

    internal static Dictionary<string, Term> Fields(Term value, string[] expected)
    {
        var fields = new Dictionary<string, Term>(StringComparer.Ordinal);
        foreach (Term entry in List(value))
        {
            var pair = Tuple(entry, 2);
            Require(pair.Items[0] is Atom);
            Require(fields.TryAdd(((Atom)pair.Items[0]).Name, pair.Items[1]));
        }
        Require(fields.Keys.ToHashSet(StringComparer.Ordinal).SetEquals(expected));

        return fields;
    }

    private static void Role(Dictionary<string, ReferenceTerm> roles, string name, Term value)
    {
        Require(value is ReferenceTerm);
        var reference = (ReferenceTerm)value;
        OriginalAtomics590Identity.ValidateDescriptor(reference);
        Require(roles.TryAdd(name, reference));
    }

    internal static string DescribeKey(ReferenceTerm value)
        => value.Node + OriginalCounters610Fields.ReferenceDescriptorSeparator + value.Creation + OriginalCounters610Fields.ReferenceDescriptorSeparator + string.Join(OriginalCounters610Fields.ReferenceWordSeparator, value.Words);

    internal static TupleTerm Tuple(Term value, int arity)
    {
        Require(value is TupleTerm tuple && tuple.Items.Count == arity);

        return (TupleTerm)value;
    }

    internal static Term[] List(Term value)
    {
        var result = new List<Term>();
        while (value is Cons cell)
        {
            result.Add(cell.Head);
            value = cell.Tail;
        }
        Require(value is Nil);

        return result.ToArray();
    }

    internal static int Int(Term value)
    {
        Require(value is Integer integer && integer.Value >= int.MinValue && integer.Value <= int.MaxValue);

        return (int)((Integer)value).Value;
    }

    private static bool Boolean(Term value)
    {
        Require(value is Atom { Name: OriginalCounters610Fields.True or OriginalCounters610Fields.False });

        return ((Atom)value).Name == OriginalCounters610Fields.True;
    }

    private static void Raised(Term value, int count)
    {
        Term[] entries = List(value);
        Require(entries.Length == count);
        foreach (Term entry in entries) RaisedOne(entry);
    }

    private static void RaisedOne(Term value)
    {
        var outcome = Tuple(value, 4);
        Require(outcome.Items[0].Equals(Term.A(OriginalCounters610Fields.Raised)) && outcome.Items[1].Equals(Term.A(OriginalCounters610Fields.Error))
            && outcome.Items[3] is Cons);
        foreach (Term frame in List(outcome.Items[3]))
        {
            var fields = Tuple(frame, 4);
            Require(fields.Items[0] is Atom && fields.Items[1] is Atom);
            Require(fields.Items[2] is Integer or Nil or Cons);
            _ = List(fields.Items[3]);
        }
    }

    private static void FirstArgument(
        Term error,
        string module,
        string function,
        Term expected
    )
    {
        var outcome = Tuple(error, 4);
        Term[] stack = List(outcome.Items[3]);
        Require(stack.Length > 0);
        var frame = Tuple(stack[0], 4);
        Require(frame.Items[0].Equals(Term.A(module)) && frame.Items[1].Equals(Term.A(function)));
        Term[] arguments = List(frame.Items[2]);
        Require(arguments.Length > 0 && arguments[0].Equals(expected));
    }
}
