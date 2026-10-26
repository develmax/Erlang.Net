namespace Erlang.Differential;

public sealed record OracleExecutionResult<T>(T Fixture, string Output);
