using System.Numerics;
using Erlang;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class MathNative392Checks
{
    private const string Native = "MathNative";
    private const string UnaryMethod = "Unary";
    private const string BinaryMethod = "Binary";
    private const string Log = "log";
    private const string Sqrt = "sqrt";
    private const string Ceil = "ceil";
    private const string Pow = "pow";
    private const string Fmod = "fmod";
    private const string Erf = "erf";
    private const string Erfc = "erfc";
    private const string Invalid = "bad_math_input";

    private static FloatTerm Unary(string name, Term input)
        => (FloatTerm)Call(
            Type(Native),
            UnaryMethod,
            name,
            input
        );

    private static FloatTerm Binary(string name, Term first, Term second)
        => (FloatTerm)Call(
            Type(Native),
            BinaryMethod,
            name,
            first,
            second
        );

    internal static Task ErrorsAndConversion()
    {
        ExpectReason(() => Unary(Log, Term.A(Invalid)), ErlangErrorReasons.BadArgument);
        ExpectReason(() => Unary(Sqrt, Term.I(-1)), ErlangErrorReasons.BadArithmetic);
        var overflow = new Integer(BigInteger.One << 1024);
        ExpectReason(() => Unary(Ceil, overflow), ErlangErrorReasons.BadArithmetic);
        ExpectReason(() => Binary(Pow, overflow, Term.A(Invalid)), ErlangErrorReasons.BadArithmetic);
        ExpectReason(() => Binary(Pow, Term.A(Invalid), overflow), ErlangErrorReasons.BadArgument);
        Require(BitConverter.DoubleToUInt64Bits(Unary(Ceil, new Integer((BigInteger.One << 60) + 128)).Value) == 0x43b0000000000000UL);
        Require(BitConverter.DoubleToUInt64Bits(Unary(Ceil, new Integer((BigInteger.One << 60) + 129)).Value) == 0x43b0000000000001UL);

        return Task.CompletedTask;
    }

    internal static Task SignedZeroUnderflowAndFmod()
    {
        FloatTerm negativeZero = new(BitConverter.UInt64BitsToDouble(0x8000000000000000UL));
        Require(BitConverter.DoubleToUInt64Bits(Unary(Sqrt, negativeZero).Value) == 0x8000000000000000UL);
        Require(BitConverter.DoubleToUInt64Bits(Unary(Ceil, negativeZero).Value) == 0x8000000000000000UL);
        Require(BitConverter.DoubleToUInt64Bits(Binary(Fmod, Term.I(-4), Term.I(2)).Value) == 0x8000000000000000UL);
        Require(Binary(Fmod, new FloatTerm(5.5), Term.I(2)).Value == 1.5);
        Require(Binary(Pow, new FloatTerm(1.0e-200), Term.I(2)).Value == 0.0);

        return Task.CompletedTask;
    }

    internal static Task HostSpecialFunctions()
    {
        Require(Unary(Erf, Term.I(0)).Value == 0.0);
        Require(Unary(Erfc, Term.I(0)).Value == 1.0);
        Require(Unary(Erf, new FloatTerm(1.0e300)).Value == 1.0);
        Require(Unary(Erfc, new FloatTerm(-1.0e300)).Value == 2.0);
        FloatTerm negativeZero = new(BitConverter.UInt64BitsToDouble(0x8000000000000000UL));
        Require(BitConverter.DoubleToUInt64Bits(Unary(Erf, negativeZero).Value) == 0x8000000000000000UL);

        return Task.CompletedTask;
    }

    private static void ExpectReason(Action action, string reason)
    {
        bool observed = false;
        try
        {
            action();
        }
        catch (ErlangException error)
        {
            observed = error.Reason is Atom atom && atom.Name == reason;
        }
        Require(observed);
    }
}
