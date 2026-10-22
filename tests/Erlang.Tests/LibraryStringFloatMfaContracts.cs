using Erlang;
using static Erlang.Tests.LibraryStringFloatNames;

namespace Erlang.Tests;

internal static class LibraryStringFloatMfaContracts
{
    internal static bool Owns(string module, string function, int arity)
        => module == Module && function == Function && arity == 1;

    internal static async Task Run()
    {
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term input = Cons.From(Input.Select(character => Term.I(character)));
            Term tail = Cons.From(Tail.Select(character => Term.I(character)));
            Term actual = await runtime.Modules.Call(
                context,
                Module,
                Function,
                input
            );
            Term invalid = await runtime.Modules.Call(
                context,
                Module,
                Function,
                Term.A(Invalid)
            );
            if (!actual.Equals(Term.Tuple(new FloatTerm(1.25), tail))
                || !invalid.Equals(Term.Tuple(Term.A(Error), Term.A(NotList))))
                throw new InvalidOperationException(Failure);

            return true;
        });
    }
}
