using Erlang;
using static Erlang.Tests.LibraryRuntimePrimitiveNames;

namespace Erlang.Tests;

internal static class LibraryRuntimePrimitiveMfaContracts
{
    internal static bool Owns(string module, string function, int arity)
        => module == LibraryMapProtocolNames.Internal && (function == Compare && arity == 2
            || function == LibraryMapProtocolNames.Next && arity == 3
            || function is ComprehensionIterator or ComprehensionRefill && arity == 1)
            || module == LibraryCollectionsNames.MathModule && function == Log && arity == 1;

    internal static async Task Run(string module, string function)
    {
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            var map = new MapTerm(new Dictionary<Term, Term> { [Term.I(1)] = Term.I(2) });
            Term pair = Term.Tuple(Term.I(1), Term.I(2), Term.A(LibraryMapProtocolNames.None));
            Term result = function switch
            {
                Compare => await runtime.Modules.Call(
                    context,
                    module,
                    function,
                    Term.I(1),
                    Term.I(2)
                ),
                LibraryMapProtocolNames.Next => await runtime.Modules.Call(
                    context,
                    module,
                    function,
                    Term.I(0),
                    map,
                    Term.A(LibraryMapProtocolNames.IteratorMode)
                ),
                ComprehensionIterator => await runtime.Modules.Call(
                    context,
                    module,
                    function,
                    map
                ),
                ComprehensionRefill => await runtime.Modules.Call(
                    context,
                    module,
                    function,
                    new Cons(Term.I(0), map)
                ),
                Log => await runtime.Modules.Call(
                    context,
                    module,
                    function,
                    Term.I(1)
                ),
                _ => throw new InvalidOperationException(Failure)
            };
            Term expected = function == Compare ? Term.I(-1) : function == Log ? new FloatTerm(0) : pair;
            if (!result.Equals(expected))
                throw new InvalidOperationException(Failure);
            if (function == ComprehensionIterator
                && !(await runtime.Modules.Call(
                    context,
                    module,
                    function,
                    Term.A(InvalidIterator)
                )).Equals(Nil.Value))
                throw new InvalidOperationException(Failure);

            return true;
        });
    }
}
