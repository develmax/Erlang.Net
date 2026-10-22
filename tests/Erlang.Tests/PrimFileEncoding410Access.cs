using System.Reflection;
using Erlang;

namespace Erlang.Tests;

internal static class PrimFileEncoding410Access
{
    internal static ErlangFunction? Capture(ProcessRuntime runtime, string function, int arity = 1)
    {
        object?[] args = [runtime.Modules, PrimFileEncoding410Names.ModuleName, function, arity, null];
        bool captured = (bool)typeof(ProcessRuntime).Assembly.GetType(PrimFileEncoding410Names.NativeType, true)!
            .GetMethod(PrimFileEncoding410Names.Capture, BindingFlags.Static | BindingFlags.NonPublic)!
            .Invoke(null, args)!;

        return captured ? (ErlangFunction)args[4]! : null;
    }

    internal static async Task BadArgument(Func<ValueTask<Term>> call)
    {
        bool rejected = false;
        try
        {
            _ = await call();
        }
        catch (ErlangException error)
        {
            rejected = error.Reason.Equals(Term.A(ErlangErrorReasons.BadArgument));
        }
        EtsFoundation405Access.Require(rejected);
    }
}
