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
                Term[] arguments = [binary, Term.I(ErlangHashCheckParameters.FullRange)];
                await runtime.Modules.Call(
                    context,
                    ErlangModuleNames.Module,
                    ErlangHashCheckParameters.ImprovedFunction,
                    arguments
                );
                int startingThread = Environment.CurrentManagedThreadId;
                long before = GC.GetAllocatedBytesForCurrentThread();
                ValueTask<Term> pending = runtime.Modules.Call(
                    context,
                    ErlangModuleNames.Module,
                    ErlangHashCheckParameters.ImprovedFunction,
                    arguments
                );
                bool synchronous = pending.IsCompletedSuccessfully;
                if (!synchronous)
                    throw new InvalidDataException(ErlangHashCheckDiagnostics.AsynchronousMeasurement);
                Term result = pending.GetAwaiter().GetResult();
                long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
                int endingThread = Environment.CurrentManagedThreadId;
                if (startingThread != endingThread)
                    throw new InvalidDataException(ErlangHashCheckDiagnostics.ThreadChanged(startingThread, endingThread));
                if (result is not Integer || allocated > ErlangHashCheckParameters.AllocationBudgetBytes)
                    throw new InvalidDataException(ErlangHashCheckDiagnostics.AllocationExceeded(allocated, result.GetType().Name, startingThread));

                Term tuple = Nil.Value;
                for (int index = 0; index < ErlangHashCheckParameters.TupleDepth; index++)
                    tuple = Term.Tuple(tuple);
                Term map = Nil.Value;
                for (int index = 0; index < ErlangHashCheckParameters.MapDepth; index++)
                    map = new MapTerm(new[] { new KeyValuePair<Term, Term>(Term.I(0), map) });
                foreach (Term value in new[] { tuple, map })
                    foreach (string function in new[] { ErlangHashCheckParameters.ImprovedFunction, ErlangHashCheckParameters.LegacyFunction })
                    {
                        Term hash = await runtime.Modules.Call(
                            context,
                            ErlangModuleNames.Module,
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

    public static async Task AllocationPositiveControl()
    {
        await using var runtime = new ProcessRuntime();
        runtime.Modules.Register(
            ErlangHashAllocationControlNames.Module,
            ErlangHashAllocationControlNames.Copy,
            ErlangHashAllocationControlNames.Arity,
            (context, arguments) => runtime.Modules.Call(
                context,
                ErlangModuleNames.Module,
                ErlangHashCheckParameters.ImprovedFunction,
                new BitString(((BitString)arguments[0]).ToArray()),
                arguments[1]
            )
        );
        Exception? failure = null;
        var process = runtime.Spawn(context =>
        {
            try
            {
                var binary = new BitString(new byte[ErlangHashCheckParameters.BinaryBytes]);
                Term[] arguments = [binary, Term.I(ErlangHashCheckParameters.FullRange)];
                Term expected = runtime.Modules.Call(
                    context,
                    ErlangModuleNames.Module,
                    ErlangHashCheckParameters.ImprovedFunction,
                    arguments
                ).GetAwaiter().GetResult();
                runtime.Modules.Call(
                    context,
                    ErlangHashAllocationControlNames.Module,
                    ErlangHashAllocationControlNames.Copy,
                    arguments
                ).GetAwaiter().GetResult();
                int startingThread = Environment.CurrentManagedThreadId;
                long before = GC.GetAllocatedBytesForCurrentThread();
                ValueTask<Term> pending = runtime.Modules.Call(
                    context,
                    ErlangHashAllocationControlNames.Module,
                    ErlangHashAllocationControlNames.Copy,
                    arguments
                );
                if (!pending.IsCompletedSuccessfully)
                    throw new InvalidDataException(ErlangHashCheckDiagnostics.AsynchronousMeasurement);
                Term result = pending.GetAwaiter().GetResult();
                long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
                int endingThread = Environment.CurrentManagedThreadId;
                if (startingThread != endingThread)
                    throw new InvalidDataException(ErlangHashCheckDiagnostics.ThreadChanged(startingThread, endingThread));
                if (allocated <= ErlangHashCheckParameters.AllocationBudgetBytes ||
                    allocated < ErlangHashCheckParameters.BinaryBytes)
                    throw new InvalidDataException(ErlangHashCheckDiagnostics.CopyNotDetected);
                if (result is not Integer || !result.Equals(expected))
                    throw new InvalidDataException(ErlangHashCheckDiagnostics.CopyChangedHash);
                Console.WriteLine(ErlangHashCheckDiagnostics.CopyAllocationObserved(allocated));
            }
            catch (Exception exception)
            {
                failure = exception;
                throw;
            }

            return ValueTask.FromResult<Term>(Nil.Value);
        });
        await process.Completion;
        if (failure is not null)
            throw failure;
    }
}
