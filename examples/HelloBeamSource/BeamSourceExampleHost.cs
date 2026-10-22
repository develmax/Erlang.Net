using System.Numerics;
using Erlang;

namespace HelloBeamSource;

internal static class BeamSourceExampleHost
{
    internal static void Register(ProcessRuntime runtime)
        => runtime.Modules.Register(
            BeamSourceExampleNames.Host,
            BeamSourceExampleNames.Sum,
            1,
            (_, arguments) => ValueTask.FromResult(Sum(arguments[0]))
        );

    private static Term Sum(Term values)
    {
        BigInteger result = BigInteger.Zero;
        foreach (Term value in Cons.Items(values))
        {
            if (value is not Integer integer)
                throw new ErlangException(ErlangErrorReasons.BadArgument);
            result += integer.Value;
        }

        return new Integer(result);
    }
}
