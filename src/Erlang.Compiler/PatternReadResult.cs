namespace Erlang.Compiler;

internal readonly record struct PatternReadResult(bool Complete, bool Matched, int Consumed);
