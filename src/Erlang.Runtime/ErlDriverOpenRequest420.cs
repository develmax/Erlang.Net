namespace Erlang;

internal sealed record ErlDriverOpenRequest420(
    byte[] CommandBytes,
    int OpenFlags,
    bool SoftEof,
    byte[] PortIdentityEtf,
    byte[] ConnectedIdentityEtf
);
