namespace Erlang;

internal static class MathNative
{
    internal static bool TryCapture(
        string module,
        string function,
        int arity,
        out ErlangFunction body
    )
    {
        if (module == MathNativeNames.Module && arity == 1 && UnaryFunction(function) is not null)
        {
            body = (_, arguments) => ValueTask.FromResult(Unary(function, arguments[0]));

            return true;
        }
        if (module == MathNativeNames.Module && arity == 2
            && function is MathNativeNames.Atan2 or MathNativeNames.Fmod or MathNativeNames.Pow)
        {
            body = (_, arguments) => ValueTask.FromResult(Binary(function, arguments[0], arguments[1]));

            return true;
        }
        body = null!;

        return false;
    }

    internal static Term Unary(string function, Term argument)
    {
        double value = Number(argument);
        Func<double, double> operation = UnaryFunction(function) ?? throw new NotSupportedException(MathNativeNames.UnsupportedOperation);

        return Result(operation(value));
    }

    internal static Term Binary(string function, Term first, Term second)
    {
        // Preserve OTP's left-to-right argument conversion error precedence.
        double left = Number(first);
        double right = Number(second);
        double result = function switch
        {
            MathNativeNames.Atan2 => Math.Atan2(left, right),
            MathNativeNames.Fmod => left % right,
            MathNativeNames.Pow => Math.Pow(left, right),
            _ => throw new NotSupportedException(MathNativeNames.UnsupportedOperation)
        };

        return Result(result);
    }

    private static Func<double, double>? UnaryFunction(string function) => function switch
    {
        MathNativeNames.Acos => Math.Acos,
        MathNativeNames.Acosh => Math.Acosh,
        MathNativeNames.Asin => Math.Asin,
        MathNativeNames.Asinh => Math.Asinh,
        MathNativeNames.Atan => Math.Atan,
        MathNativeNames.Atanh => Math.Atanh,
        MathNativeNames.Ceil => Math.Ceiling,
        MathNativeNames.Cos => Math.Cos,
        MathNativeNames.Cosh => Math.Cosh,
        MathNativeNames.Erf => MathHostSpecialFunctions.Erf,
        MathNativeNames.Erfc => MathHostSpecialFunctions.Erfc,
        MathNativeNames.Exp => Math.Exp,
        MathNativeNames.Floor => Math.Floor,
        MathNativeNames.Log => Math.Log,
        MathNativeNames.Log2 => Math.Log2,
        MathNativeNames.Log10 => Math.Log10,
        MathNativeNames.Sin => Math.Sin,
        MathNativeNames.Sinh => Math.Sinh,
        MathNativeNames.Sqrt => Math.Sqrt,
        MathNativeNames.Tan => Math.Tan,
        MathNativeNames.Tanh => Math.Tanh,
        _ => null
    };

    private static double Number(Term argument)
    {
        if (argument is FloatTerm floating)
            return floating.Value;
        if (argument is not Integer integer)
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        if (!integer.TryToDouble(out double converted))
            throw new ErlangException(ErlangErrorReasons.BadArithmetic);

        return converted;
    }

    private static Term Result(double value)
    {
        if (!double.IsFinite(value))
            throw new ErlangException(ErlangErrorReasons.BadArithmetic);

        return new FloatTerm(value);
    }
}
