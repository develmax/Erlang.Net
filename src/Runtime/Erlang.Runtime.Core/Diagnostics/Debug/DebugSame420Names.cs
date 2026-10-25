using System.Numerics;
namespace Erlang;

internal static class DebugSame420Names
{
    internal const string Module = "erts_debug", Function = "same", Foreign = "same requires its actual owning live actor", WrongArguments = "same native entry requires two ordinary terms";
    internal const int ImmediateTagBits = 4;
    internal static readonly BigInteger SmallMagnitude = BigInteger.One << ((IntPtr.Size * 8) - ImmediateTagBits - 1);
}
