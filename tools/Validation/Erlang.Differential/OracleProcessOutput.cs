namespace Erlang.Differential;

public sealed record OracleProcessOutput(string StandardOutput, string StandardError, int ExitCode)
{
    public ReadOnlyMemory<byte> StandardOutputBytes { get; init; }
    public ReadOnlyMemory<byte> StandardErrorBytes { get; init; }
}
