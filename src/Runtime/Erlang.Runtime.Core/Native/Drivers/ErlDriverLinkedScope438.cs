namespace Erlang;

// Internal authenticated parent/child IPC, not part of the ErlDrv native ABI.
internal sealed record ErlDriverLinkedScope438(string Session, ulong OwnerImage, ulong Token);
