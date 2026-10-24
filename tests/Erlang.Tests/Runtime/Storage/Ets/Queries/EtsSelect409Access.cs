using System.Reflection;
using Erlang;

namespace Erlang.Tests;

internal static class EtsSelect409Access
{
    internal static IReadOnlyDictionary<(string Function, int Arity), ErlangFunction> Bodies(ProcessRuntime runtime)
        => (IReadOnlyDictionary<(string Function, int Arity), ErlangFunction>)typeof(ProcessRuntime).Assembly
            .GetType(EtsSelect409Names.FacetType, true)!
            .GetMethod(EtsSelect409Names.CreateBodies, BindingFlags.Static | BindingFlags.NonPublic)!
            .Invoke(null, [runtime])!;

    internal static ValueTask<Term> Call(
        IReadOnlyDictionary<(string Function, int Arity), ErlangFunction> bodies,
        ProcessContext context,
        string name,
        params Term[] arguments
    ) => bodies[(name, arguments.Length)](context, arguments);

    internal static async Task BadArgument(Func<ValueTask<Term>> operation)
    {
        bool caught = false;
        try
        {
            _ = await operation();
        }
        catch (ErlangException error)
        {
            caught = error.Reason.Equals(Term.A(ErlangErrorReasons.BadArgument));
            EtsFoundation405Access.Require(error.StackTraceTerm is Cons);
        }
        EtsFoundation405Access.Require(caught);
    }
}
