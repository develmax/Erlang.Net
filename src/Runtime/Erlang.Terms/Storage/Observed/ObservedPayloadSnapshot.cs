using System.Numerics;

namespace Erlang;

internal readonly record struct ObservedPayloadSnapshot(BigInteger Atom, BigInteger Binary, BigInteger Code);
