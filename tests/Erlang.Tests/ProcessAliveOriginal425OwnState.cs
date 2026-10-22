namespace Erlang.Tests;

internal static class ProcessAliveOriginal425OwnState
{
    internal static void Check(Term value)
    {
        Term[] rows = Cons.Items(value).ToArray();
        ProcessAliveOriginal425Access.Require(rows.Length == ProcessAliveOriginal425Names.OwnCases.Length);
        for (int index = 0; index < rows.Length; index++)
        {
            ProcessAliveOriginal425Access.Require(rows[index] is TupleTerm { Items.Count: 2 });
            var row = (TupleTerm)rows[index];
            ProcessAliveOriginal425Access.Require(row.Items[0].Equals(Term.A(ProcessAliveOriginal425Names.OwnCases[index]))
                && row.Items[1] is TupleTerm { Items.Count: 2 });
            var result = (TupleTerm)row.Items[1];
            // The unchanged source checks exact invocation arguments and the top
            // native frame before returning this proof. Retain complete raw data.
            ProcessAliveOriginal425Access.Require(result.Items[0].Equals(Term.A(ProcessAliveOriginal425Names.True))
                && result.Items[1] is TupleTerm { Items.Count: 4 });
            var caught = (TupleTerm)result.Items[1];
            ProcessAliveOriginal425Access.Require(caught.Items[0].Equals(Term.A(ProcessAliveOriginal425Names.Exception))
                && caught.Items[1].Equals(Term.A(ProcessAliveOriginal425Names.Error))
                && caught.Items[2].Equals(Term.A(ProcessAliveOriginal425Names.BadArgument)));
            ProcessAliveOriginal425Access.Require(Cons.Items(caught.Items[3]).Any());
        }
    }
}
