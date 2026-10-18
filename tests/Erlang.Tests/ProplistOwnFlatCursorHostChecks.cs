using Erlang.Compiler;
using Erlang.Differential;

namespace Erlang.Tests;

internal static class ProplistOwnFlatCursorHostChecks
{
    internal static async Task Run(ProcessContext context, GeneratedModuleArtifact compiled)
    {
        string prefix = ProplistOwnFlatCursorNames.AtomPrefix + Guid.NewGuid().ToString(ProplistOwnFlatCursorNames.UniqueFormat);
        Atom first = Term.A(prefix + ProplistOwnFlatCursorNames.FirstSuffix);
        Atom second = Term.A(prefix + ProplistOwnFlatCursorNames.SecondSuffix);
        ProplistOwnCursorAssertions.Require(
            AtomNameRegistry.IndexOf(first.Name) < AtomNameRegistry.IndexOf(second.Name),
            ProplistOwnFlatCursorNames.Failure
        );
        MapTerm map = new([
            new KeyValuePair<Term, Term>(first, first),
            new KeyValuePair<Term, Term>(second, second)
        ]);
        Term physical = Cons.From(new[] { first, second }
            .OrderBy(key => AtomNameRegistry.IndexOf(key.Name))
            .Select(key => Term.Tuple(key, key)));
        for (int path = 0; path <= ProplistOwnFlatCursorNames.AtomCount; path++)
        {
            Term cursor = new Cons(Term.I(path), map);
            await SameRoutes(
                context,
                compiled,
                cursor,
                physical
            );
        }
        await SameRoutes(
            context,
            compiled,
            map,
            physical
        );
        Term ordered = await context.Runtime.Modules.Call(
            context,
            ProplistOwnCursorNames.Maps,
            ProplistOwnFlatCursorNames.Iterator,
            map,
            Term.A(ProplistOwnFlatCursorNames.Ordered)
        );
        Term lexical = Term.List(Term.Tuple(second, second), Term.Tuple(first, first));
        await SameRoutes(
            context,
            compiled,
            ordered,
            lexical
        );
    }

    private static async Task SameRoutes(
        ProcessContext context,
        GeneratedModuleArtifact compiled,
        Term input,
        Term expected
    )
    {
        Term actual = await ProplistOwnCursorAssertions.Read(context, input);
        ProplistOwnCursorAssertions.Require(actual.Equals(expected), ProplistOwnFlatCursorNames.Failure);
        Term listed = await context.Runtime.Modules.Call(
            context,
            ProplistOwnCursorNames.Maps,
            ProplistIteratorTestNames.ToList,
            input
        );
        ProplistOwnCursorAssertions.Require(listed.Equals(expected), ProplistOwnFlatCursorNames.Failure);
        foreach (string function in new[] { ProplistOwnCursorNames.Read, ProplistOwnCursorNames.Generate })
        {
            Term generated = await Execution.InvokeAsync(
                compiled.Definition,
                function,
                context,
                [input]
            );
            ProplistOwnCursorAssertions.Require(generated.Equals(expected), ProplistOwnFlatCursorNames.Failure);
        }
    }
}
