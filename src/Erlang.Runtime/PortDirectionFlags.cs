namespace Erlang;

[Flags]
internal enum PortDirectionFlags
{
    None = 0,
    Read = 1,
    Write = 2,
    Both = Read | Write
}
