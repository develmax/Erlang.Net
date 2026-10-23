namespace Erlang;

internal static class PacketHttpAtoms
{
    public static readonly Atom Request = Term.A(PacketHttpResultNames.Request);
    public static readonly Atom Response = Term.A(PacketHttpResultNames.Response);
    public static readonly Atom Header = Term.A(PacketHttpResultNames.Header);
    public static readonly Atom EndOfHeaders = Term.A(PacketHttpResultNames.EndOfHeaders);
    public static readonly Atom Error = Term.A(PacketHttpResultNames.Error);
    public static readonly Atom AbsolutePath = Term.A(PacketHttpResultNames.AbsolutePath);
    public static readonly Atom AbsoluteUri = Term.A(PacketHttpResultNames.AbsoluteUri);
    public static readonly Atom Scheme = Term.A(PacketHttpResultNames.Scheme);
    public static readonly Atom HttpScheme = Term.A(PacketHttpResultNames.HttpScheme);
    public static readonly Atom HttpsScheme = Term.A(PacketHttpResultNames.HttpsScheme);
    public static readonly Atom Star = Term.A(PacketHttpResultNames.Star);
    public static readonly Atom UndefinedPort = Term.A(PacketHttpResultNames.UndefinedPort);
}
