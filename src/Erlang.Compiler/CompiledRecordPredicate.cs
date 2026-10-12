using System.Numerics;

namespace Erlang.Compiler;

internal sealed record CompiledRecordPredicate(Atom Tag, BigInteger Size, bool StaticApply);
