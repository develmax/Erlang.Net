using Erlang.Compiler;
using Erlang.Differential;

namespace Erlang.Tests;

internal static class SourceBifContinuationHostChecks
{
    internal static async Task Run()
    {
        var assembly = typeof(SourceBifContinuationHostChecks).Assembly;
        string resource = assembly.GetManifestResourceNames().Single(name => name.EndsWith(SourceBifContinuationHostNames.Fixture, StringComparison.Ordinal));
        using Stream stream = assembly.GetManifestResourceStream(resource)!;
        using var reader = new StreamReader(stream);
        using var artifact = GeneratedModuleCompiler.Compile(await reader.ReadToEndAsync(), SourceBifContinuationHostNames.Fixture);
        // Enumerable.Repeat is materialized exactly once by TupleTerm; no second giant input array is constructed.
        var tuple = new TupleTerm(Enumerable.Repeat<Term>(Term.A(SourceBifContinuationHostNames.Value), SourceBifContinuationHostParameters.TupleLength));
        await Canonical(artifact.Definition, tuple);
        await Overridden(artifact.Definition, tuple);
    }

    private static async Task Canonical(ModuleDefinition module, TupleTerm tuple)
    {
        await using var runtime = new ProcessRuntime();
        module.Register(runtime.Modules);
        int cleanup = 0;
        runtime.Modules.Register(
            SourceBifContinuationHostNames.Observer,
            SourceBifContinuationHostNames.Cleanup,
            SourceBifContinuationHostParameters.CleanupArity,
            (_, _) =>
            {
                Interlocked.Increment(ref cleanup);
                return ValueTask.FromResult<Term>(Term.A(SourceBifContinuationHostNames.Done));
            }
        );
        var failures = new List<Exception>();
        var completed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var actor = runtime.Spawn(async context =>
        {
            async Task Value(string function, Term argument, Term expected)
            {
                try
                {
                    Term actual = await Execution.InvokeAsync(module, function, context, [argument]);
                    Require(actual.Equals(expected));
                }
                catch (Exception error)
                {
                    failures.Add(new InvalidOperationException(function, error));
                }
            }
            async Task Failure(string function, bool anonymous, int line)
            {
                try
                {
                    await Execution.InvokeAsync(module, function, context, [Term.A(SourceBifContinuationHostNames.Invalid)]);
                    throw new InvalidOperationException(SourceBifContinuationHostNames.Failure);
                }
                catch (ErlangException error)
                {
                    try
                    {
                        Require(error.ExceptionClass == SourceBifContinuationHostNames.Error && error.Reason.Equals(Term.A(SourceBifContinuationHostNames.BadArg)));
                        Term[] frames = Cons.Items(error.StackTraceTerm).ToArray();
                        Require(frames.Length >= 2 && frames[0] is TupleTerm { Items.Count: 4 });
                        var native = (TupleTerm)frames[0];
                        Require(native.Items[0].Equals(Term.A(SourceBifContinuationHostNames.Erlang))
                            && native.Items[1].Equals(Term.A(SourceBifContinuationHostNames.Element))
                            && native.Items[2].Equals(Term.List(Term.I(SourceBifContinuationHostParameters.ElementIndex), Term.A(SourceBifContinuationHostNames.Invalid))));
                        Require(native.Items[3].Equals(Term.List(Term.Tuple(
                            Term.A(SourceBifContinuationHostNames.ErrorInfo),
                            new MapTerm(new Dictionary<Term, Term>
                            {
                                [Term.A(SourceBifContinuationHostNames.FormatterKey)] = Term.A(SourceBifContinuationHostNames.Formatter)
                            })
                        ))));
                        Require(frames[1] is TupleTerm { Items.Count: 4 });
                        var caller = (TupleTerm)frames[1];
                        Require(caller.Items[0].Equals(Term.A(SourceBifContinuationHostNames.Module)));
                        Require(caller.Items[1] is Atom name && (anonymous
                            ? name.Name.StartsWith(SourceBifContinuationHostNames.AnonymousPrefix, StringComparison.Ordinal)
                            : name.Name == function));
                        if (!anonymous)
                            Require(caller.Items[2].Equals(Term.I(SourceBifContinuationHostParameters.FunctionArity)));
                        Require(caller.Items[3].Equals(Term.List(
                            Term.Tuple(Term.A(SourceBifContinuationHostNames.File), Term.String(SourceBifContinuationHostNames.Fixture)),
                            Term.Tuple(Term.A(SourceBifContinuationHostNames.Line), Term.I(line))
                        )));
                    }
                    catch (Exception validation)
                    {
                        failures.Add(new InvalidOperationException(function, validation));
                    }
                }
                catch (Exception error)
                {
                    failures.Add(new InvalidOperationException(function, error));
                }
            }
            Term nativeValue = Term.A(SourceBifContinuationHostNames.Value);
            Term continued = Term.Tuple(Term.A(SourceBifContinuationHostNames.Continued), nativeValue);
            await Value(SourceBifContinuationHostNames.Direct, tuple, nativeValue);
            await Value(SourceBifContinuationHostNames.Forwarded, tuple, nativeValue);
            await Value(SourceBifContinuationHostNames.Protected, tuple, continued);
            await Value(SourceBifContinuationHostNames.Protected, Term.A(SourceBifContinuationHostNames.Invalid), Term.A(SourceBifContinuationHostNames.Caught));
            await Value(SourceBifContinuationHostNames.OfBody, tuple, nativeValue);
            await Value(SourceBifContinuationHostNames.AfterBody, tuple, continued);
            if (Volatile.Read(ref cleanup) != SourceBifContinuationHostParameters.CleanupsAfterSuccess)
                failures.Add(new InvalidOperationException(SourceBifContinuationHostNames.Failure));
            await Value(SourceBifContinuationHostNames.Anonymous, tuple, Term.Tuple(Term.A(SourceBifContinuationHostNames.Outer), nativeValue));
            await Failure(SourceBifContinuationHostNames.Direct, false, SourceBifContinuationHostParameters.DirectLine);
            await Failure(SourceBifContinuationHostNames.OfBody, false, SourceBifContinuationHostParameters.OfLine);
            await Failure(SourceBifContinuationHostNames.AfterBody, false, SourceBifContinuationHostParameters.AfterLine);
            if (Volatile.Read(ref cleanup) != SourceBifContinuationHostParameters.CleanupsAfterFailure)
                failures.Add(new InvalidOperationException(SourceBifContinuationHostNames.Failure));
            await Failure(SourceBifContinuationHostNames.Anonymous, true, SourceBifContinuationHostParameters.AnonymousLine);
            completed.TrySetResult();
            return Term.A(SourceBifContinuationHostNames.Done);
        });
        await completed.Task.WaitAsync(SourceBifContinuationHostParameters.Deadline);
        Require((await actor.Completion.WaitAsync(SourceBifContinuationHostParameters.Deadline)).Equals(Term.A(SourceBifContinuationHostNames.Normal)));
        if (failures.Count != 0)
            throw new AggregateException(SourceBifContinuationHostNames.Failure, failures);
    }

    private static async Task Overridden(ModuleDefinition module, TupleTerm tuple)
    {
        await using var runtime = new ProcessRuntime();
        module.Register(runtime.Modules);
        int invoked = 0;
        runtime.Modules.Apply(registry =>
        {
            registry.DeleteModule(SourceBifContinuationHostNames.Erlang);
            registry.Register(
                SourceBifContinuationHostNames.Erlang,
                SourceBifContinuationHostNames.Element,
                SourceBifContinuationHostParameters.ElementArity,
                (_, arguments) =>
                {
                    Require(arguments[0].Equals(Term.I(SourceBifContinuationHostParameters.ElementIndex)) && ReferenceEquals(arguments[1], tuple));
                    Interlocked.Increment(ref invoked);
                    return ValueTask.FromResult<Term>(Term.A(SourceBifContinuationHostNames.Custom));
                }
            );
        });
        var completed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Exception? failure = null;
        var actor = runtime.Spawn(async context =>
        {
            try
            {
                Term expected = Term.Tuple(Term.A(SourceBifContinuationHostNames.Continued), Term.A(SourceBifContinuationHostNames.Custom));
                foreach (string function in new[] { SourceBifContinuationHostNames.Direct, SourceBifContinuationHostNames.Forwarded })
                {
                    Require((await Execution.InvokeAsync(module, function, context, [tuple])).Equals(expected));
                }
                Require(Volatile.Read(ref invoked) == SourceBifContinuationHostParameters.OverrideCalls);
            }
            catch (Exception error)
            {
                failure = error;
            }
            completed.TrySetResult();
            return Term.A(SourceBifContinuationHostNames.Done);
        });
        await completed.Task.WaitAsync(SourceBifContinuationHostParameters.Deadline);
        Require((await actor.Completion.WaitAsync(SourceBifContinuationHostParameters.Deadline)).Equals(Term.A(SourceBifContinuationHostNames.Normal)));
        if (failure is not null)
            throw new InvalidOperationException(SourceBifContinuationHostNames.Failure, failure);
    }

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(SourceBifContinuationHostNames.Failure);
    }
}
