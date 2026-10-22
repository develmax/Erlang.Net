namespace Erlang.Tests;

internal enum BeamInitializedRootShape
{
    GcOnly,
    TrimGc,
    TransportEscape,
    TupleEscape,
    TrimEscape,
    ReallocateUninitialized,
    OverwriteThenRead
}
