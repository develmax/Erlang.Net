namespace Erlang;

// Public pinned erl_driver.h term construction protocol.
internal enum ErlDriverTermTag420 : ulong
{
    Nil = 1,
    Atom = 2,
    Integer = 3,
    Port = 4,
    Binary = 5,
    String = 6,
    Tuple = 7,
    List = 8,
    StringCons = 9,
    Pid = 10,
    Float = 11,
    ExternalTerm = 12,
    Unsigned = 13,
    BufferBinary = 14,
    Integer64 = 15,
    Unsigned64 = 16,
    Map = 17
}
