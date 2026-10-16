namespace Erlang.Otp;

internal static class OtpLoggerTerms
{
    public static readonly MapTerm Empty = new([]);
    public static readonly Atom Ok = Term.A(OtpLoggerNames.Ok);
    public static readonly Atom Stop = Term.A(OtpLoggerNames.Stop);
    public static readonly Atom Ignore = Term.A(OtpLoggerNames.Ignore);
    public static readonly Atom Primary = Term.A(OtpLoggerNames.Primary);
    public static readonly Atom Default = Term.A(OtpLoggerNames.Default);
    public static readonly Atom Undefined = Term.A(OtpLoggerNames.Undefined);

    public static MapTerm Map(params (string Key, Term Value)[] entries)
        => new(entries.Select(entry => new KeyValuePair<Term, Term>(Term.A(entry.Key), entry.Value)));

    public static Term Get(MapTerm map, string key) => map.Get(Term.A(key));

    public static MapTerm Put(MapTerm map, string key, Term value)
        => Merge(map, Map((key, value)));

    public static MapTerm Merge(MapTerm left, MapTerm right) => new(left.Entries.Concat(right.Entries));

    public static MapTerm Without(MapTerm map, params string[] keys)
        => new(map.Entries.Where(entry => entry.Key is not Atom atom || !keys.Contains(atom.Name)));

    public static Term Error(string reason, Term detail)
        => Term.Tuple(Term.A(OtpLoggerNames.Error), Term.Tuple(Term.A(reason), detail));

    public static int Severity(Term level, bool configuration = false) => level switch
    {
        Atom { Name: OtpLoggerNames.None } when configuration => -1,
        Atom { Name: OtpLoggerNames.Emergency } => 0,
        Atom { Name: OtpLoggerNames.Alert } => 1,
        Atom { Name: OtpLoggerNames.Critical } => 2,
        Atom { Name: OtpLoggerNames.Error } => 3,
        Atom { Name: OtpLoggerNames.Warning } => 4,
        Atom { Name: OtpLoggerNames.Notice } => 5,
        Atom { Name: OtpLoggerNames.Info } => 6,
        Atom { Name: OtpLoggerNames.Debug } => 7,
        Atom { Name: OtpLoggerNames.All } when configuration => 10,
        _ => throw new ErlangException(ErlangErrorReasons.FunctionClause)
    };

    // Erlang is_list tests a list cell or nil; it does not require a proper tail.
    public static bool IsList(Term value) => value is Cons or Nil;

    public static bool IsString(Term value) => IsList(value) || value is BitString { IsBinary: true };

    public static bool IsReport(Term value) => value is MapTerm || value is Cons { Head: TupleTerm } && IsList(value);

    public static bool IsFormat(Term value) => value is Atom || IsString(value);

    public static bool IsMessage(Term value)
        => value is TupleTerm { Items: [var first, var second] } &&
            (IsFormat(first) && IsList(second) ||
             first is Atom { Name: OtpLoggerNames.Report } && IsReport(second) ||
             first is Atom { Name: OtpLoggerNames.String } && IsString(second));

    public static MapTerm PrimaryDefaults() => Map(
        (OtpLoggerNames.Level, Term.A(OtpLoggerNames.Notice)),
        (OtpLoggerNames.Metadata, Empty),
        (OtpLoggerNames.Filters, Nil.Value),
        (OtpLoggerNames.FilterDefault, Term.A(OtpLoggerNames.Log))
    );

    public static MapTerm HandlerDefaults(Term id, Term module) => Map(
        (OtpLoggerNames.Id, id),
        (OtpLoggerNames.Module, module),
        (OtpLoggerNames.Level, Term.A(OtpLoggerNames.All)),
        (OtpLoggerNames.Filters, Nil.Value),
        (OtpLoggerNames.FilterDefault, Term.A(OtpLoggerNames.Log)),
        (OtpLoggerNames.Formatter, Term.Tuple(Term.A(OtpLoggerNames.LoggerFormatter), Empty))
    );
}
