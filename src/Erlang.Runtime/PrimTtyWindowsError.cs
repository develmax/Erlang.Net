namespace Erlang;

// Raw Win32 result, deliberately not yet translated to a guest error atom.
internal readonly record struct PrimTtyWindowsError(string Operation, uint Code);
