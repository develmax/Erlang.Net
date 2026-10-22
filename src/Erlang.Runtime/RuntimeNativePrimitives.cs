namespace Erlang;

[Flags]
public enum RuntimeNativePrimitives
{
    None = 0,
    PersistentTermCore = 1,
    EtsStorageCore = 2,
    RegexCore411 = 4
}
