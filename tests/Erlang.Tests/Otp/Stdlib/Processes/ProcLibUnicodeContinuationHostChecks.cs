using System.Reflection;
using Erlang.Otp;

namespace Erlang.Tests;

public static class ProcLibUnicodeContinuationHostChecks
{
    public static Task Run()
    {
        var invalid = new BitString([ProcLibUnicodeContinuationHostNames.InvalidUtf8Byte]);
        Term emptyPrefix = Term.List(invalid);
        for (int index = 0; index < ProcLibUnicodeContinuationHostNames.EmptySegments; index++)
            emptyPrefix = new Cons(Nil.Value, emptyPrefix);
        Term[] inputs = [emptyPrefix, Term.List(Term.List(Term.I(ProcLibUnicodeContinuationHostNames.PrefixCharacter)), emptyPrefix)];
        Type characters = typeof(GenServer).Assembly.GetType(ProcLibUnicodeContinuationHostNames.CharactersType, true)!;
        MethodInfo measure = characters.GetMethod(ProcLibUnicodeContinuationHostNames.Measure)!;
        foreach (Term input in inputs)
        {
            try
            {
                measure.Invoke(null, [input, Term.A(ProcLibUnicodeContinuationHostNames.Unicode)]);
                throw new InvalidOperationException(ProcLibUnicodeContinuationHostNames.MissingRefusal);
            }
            catch (TargetInvocationException exception) when (exception.InnerException is ErlangException failure)
            {
                Term expected = Term.Tuple(Term.A(ErlangErrorReasons.BadArgument), invalid);
                if (failure.ExceptionClass != ErlangExceptionClasses.Error || !failure.Reason.Equals(expected))
                    throw new InvalidOperationException(ProcLibUnicodeContinuationHostNames.WrongRemainder, failure);
            }
        }

        return Task.CompletedTask;
    }
}
