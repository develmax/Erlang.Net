using System.Numerics;

namespace Erlang.Compiler;

public sealed record BitPatternSegment(Pattern Value, BitSegment Specification);
