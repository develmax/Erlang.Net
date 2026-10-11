using Erlang;

namespace Erlang.Tests;

internal static class ErlangHashHostChecks
{
    public static async Task Resources()
    {
        await using var runtime = new ProcessRuntime();
        Exception? failure = null;
        var process = runtime.Spawn(async context =>
        {
            try
            {
                var binary = new BitString(new byte[ErlangHashCheckParameters.BinaryBytes]);
                await runtime.Modules.Call(
                    context,
                    "erlang",
                    "phash2",
                    binary,
                    Term.I(ErlangHashCheckParameters.FullRange)
                );
                long before = GC.GetAllocatedBytesForCurrentThread();
                Term result = await runtime.Modules.Call(
                    context,
                    "erlang",
                    "phash2",
                    binary,
                    Term.I(ErlangHashCheckParameters.FullRange)
                );
                long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
                if (result is not Integer || allocated > ErlangHashCheckParameters.AllocationBudgetBytes)
                    throw new InvalidDataException(ErlangHashCheckDiagnostics.BinaryCopied);

                Term tuple = Nil.Value;
                for (int index = 0; index < ErlangHashCheckParameters.TupleDepth; index++)
                    tuple = Term.Tuple(tuple);
                Term map = Nil.Value;
                for (int index = 0; index < ErlangHashCheckParameters.MapDepth; index++)
                    map = new MapTerm(new[] { new KeyValuePair<Term, Term>(Term.I(0), map) });
                foreach (Term value in new[] { tuple, map })
                    foreach (string function in new[] { "phash2", "phash" })
                    {
                        Term hash = await runtime.Modules.Call(
                            context,
                            "erlang",
                            function,
                            value,
                            Term.I(ErlangHashCheckParameters.FullRange)
                        );
                        if (hash is not Integer)
                            throw new InvalidDataException(ErlangHashCheckDiagnostics.DeepTraversal);
                    }
            }
            catch (Exception exception)
            {
                failure = exception;
                throw;
            }

            return Nil.Value;
        });
        await process.Completion;
        if (failure is not null)
            throw failure;
    }
}
