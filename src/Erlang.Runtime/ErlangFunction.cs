using System.Numerics;

namespace Erlang;

public delegate ValueTask<Term> ErlangFunction(ProcessContext context, IReadOnlyList<Term> arguments);
