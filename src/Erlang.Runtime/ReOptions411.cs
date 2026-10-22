namespace Erlang;

internal static class ReOptions411
{
    private static Term[] Items(Term options)
    {
        try
        {
            return Cons.Items(options).ToArray();
        }
        catch (ErlangException)
        {
            throw new ReBadOption411();
        }
    }

    private static bool ExecutionOnly(string name) => name is ReOptionNames411.Notempty or ReOptionNames411.NotemptyAtstart
        or ReOptionNames411.Notbol or ReOptionNames411.Noteol or ReOptionNames411.Global or ReOptionNames411.ReportErrors;

    private static bool HeldTuple(TupleTerm tuple)
    {
        if (tuple.Items.Count == 3 && tuple.Items[0].Equals(Term.A(ReOptionNames411.Capture))) return true;
        if (tuple.Items.Count != 2 || tuple.Items[0] is not Atom key) throw new ReBadOption411();
        if (key.Name == ReProtocolNames411.Newline)
        {
            if (tuple.Items[1] is not Atom { Name: ReOptionNames411.Cr or ReOptionNames411.Crlf or ReOptionNames411.Lf or ReOptionNames411.Nul or ReOptionNames411.Anycrlf or ReOptionNames411.Any }) throw new ReBadOption411();

            return false;
        }
        if (key.Name == ReOptionNames411.Capture) return true;
        if (key.Name is ReOptionNames411.Offset or ReOptionNames411.MatchLimit or ReOptionNames411.MatchLimitRecursion)
        {
            if (tuple.Items[1] is not Integer number || number.Value < 0 || number.Value > int.MaxValue) throw new ReBadOption411();

            return true;
        }
        throw new ReBadOption411();
    }

    internal static bool Compile(Term options)
    {
        bool unicode = false;
        foreach (var option in Items(options))
        {
            if (option is Atom { Name: ReAtomNames411.Unicode }) unicode = true;
            else if (option is Atom atom && ReNames411.ValidHeldOptions.Contains(atom.Name))
            {
                if (ExecutionOnly(atom.Name)) throw new ReBadOption411();
                throw new NotSupportedException(ReNames411.Unsupported);
            }
            else if (option is TupleTerm tuple)
            {
                if (HeldTuple(tuple)) throw new ReBadOption411();
                throw new NotSupportedException(ReNames411.Unsupported);
            }
            else throw new ReBadOption411();
        }

        return unicode;
    }

    internal static void Run(Term options)
    {
        if (options is Nil) return;
        foreach (var option in Items(options))
        {
            if (option is Atom atom && (atom.Name == ReAtomNames411.Unicode || ReNames411.ValidHeldOptions.Contains(atom.Name))) continue;
            if (option is TupleTerm tuple)
            {
                HeldTuple(tuple);
                continue;
            }
            throw new ReBadOption411();
        }
        throw new NotSupportedException(ReNames411.Unsupported);
    }
}
