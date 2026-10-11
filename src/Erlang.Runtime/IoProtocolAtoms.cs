namespace Erlang;

internal static class IoProtocolAtoms
{
    public static readonly Atom Request = Term.A(IoProtocolNames.Request);
    public static readonly Atom Reply = Term.A(IoProtocolNames.Reply);
    public static readonly Atom PutCharacters = Term.A(IoProtocolNames.PutCharacters);
    public static readonly Atom Unicode = Term.A(IoProtocolNames.Unicode);
    public static readonly Atom FormatterModule = Term.A(IoProtocolNames.FormatterModule);
    public static readonly Atom FormatterFunction = Term.A(IoProtocolNames.FormatterFunction);
    public static readonly Atom Failure = Term.A(IoProtocolNames.Failure);
}
