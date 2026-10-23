namespace Erlang;

internal sealed record ErlDriverChildReservation455(
    ulong Attempt,
    ulong Port,
    ulong Owner,
    byte[] PortIdentityEtf,
    byte[] OwnerIdentityEtf
);
