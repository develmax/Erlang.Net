namespace Erlang;

internal sealed class ErlDriverSelectArm420(ErlDriverSelectRow420 row)
{
    internal ErlDriverSelectRow420 Row { get; } = row;
    internal ulong Generation { get; } = row.Generation;
    internal ErlDriverPort420? Reader { get; } = row.Reader;
    internal ErlDriverPort420? Writer { get; } = row.Writer;
    internal ulong ReadGeneration { get; } = row.ReadGeneration;
    internal ulong WriteGeneration { get; } = row.WriteGeneration;
    internal ErlDriverSelection420? Wait { get; set; }
}
