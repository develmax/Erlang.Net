namespace Erlang;

internal static class PacketDecodeAtoms
{
    public static readonly Atom Ok = Term.A(PacketDecodeResultNames.Ok);
    public static readonly Atom Error = Term.A(PacketDecodeResultNames.Error);
    public static readonly Atom More = Term.A(PacketDecodeResultNames.More);
    public static readonly Atom Invalid = Term.A(PacketDecodeResultNames.Invalid);
    public static readonly Atom Undefined = Term.A(PacketDecodeResultNames.Undefined);
    public static readonly Atom SslTls = Term.A(PacketDecodeResultNames.SslTls);
}
