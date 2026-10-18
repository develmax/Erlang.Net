using Erlang.Differential;

namespace Erlang.Tests;

internal static class StaticApplyRegressionHostChecks
{
    internal static async Task Run()
    {
        var failures = new List<Exception>();
        async Task Check(Func<Task> action)
        {
            try
            {
                await action();
            }
            catch (Exception error)
            {
                failures.Add(error);
            }
        }
        await Check(async () => Execution(await RunSource(StaticApplyRegressionSources.Execution, StaticApplyRegressionSources.ExecutionFile)));
        await Check(async () => Require((await RunSource(StaticApplyRegressionSources.Imported, StaticApplyRegressionSources.ImportedFile)).Equals(Term.Tuple(Nil.Value, Nil.Value))));
        await Check(async () => Require((await RunSource(StaticApplyRegressionSources.Shadow, StaticApplyRegressionSources.ShadowFile)).Equals(Term.A(StaticApplyRegressionNames.Shadow))));
        await Check(async () => Require((await RunSource(StaticApplyRegressionSources.Other, StaticApplyRegressionSources.OtherFile, true)).Equals(Term.A(StaticApplyRegressionNames.OtherResult))));
        await Check(async () =>
        {
            try
            {
                await RunSource(StaticApplyRegressionSources.Suppressed, StaticApplyRegressionSources.SuppressedFile);
            }
            catch (ErlangException error)
            {
                Require(error.ExceptionClass == ErlangExceptionClasses.Error
                    && error.Reason.Equals(Term.A(ErlangErrorReasons.UndefinedFunction)));
                return;
            }
            throw new InvalidOperationException(StaticApplyRegressionNames.Failure);
        });
        if (failures.Count != 0)
            throw new AggregateException(StaticApplyRegressionNames.Failure, failures);
    }

    private static async Task<Term> RunSource(string source, string file, bool other = false)
    {
        using var artifact = GeneratedModuleCompiler.Compile(source, file);
        await using var runtime = new ProcessRuntime();
        int calls = 0;
        if (other)
            runtime.Modules.Register(StaticApplyRegressionNames.OtherModule, StaticApplyRegressionNames.Apply,
                StaticApplyRegressionNames.ApplyArity, (_, arguments) =>
                {
                    Interlocked.Increment(ref calls);
                    Require(arguments.Count == StaticApplyRegressionNames.ApplyArity
                        && arguments[0].Equals(Term.A(QueueModuleNames.Module))
                        && arguments[1].Equals(Term.A(QueueModuleNames.New)) && arguments[2] is Nil);
                    return ValueTask.FromResult<Term>(Term.A(StaticApplyRegressionNames.OtherResult));
                });
        artifact.Definition.Register(runtime.Modules);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        Term result = await actor.InvokeAsync(context => runtime.Modules.Call(context, artifact.Definition.Name, StaticApplyRegressionNames.Entry).AsTask());
        if (other)
            Require(calls == 1);
        return result;
    }

    private static void Execution(Term result)
    {
        Require(result is TupleTerm { Items.Count: 7 });
        var values = (TupleTerm)result;
        Require(values.Items[0].Equals(Term.Tuple(Term.A(StaticApplyRegressionNames.First), Term.A(StaticApplyRegressionNames.Second)))
            && values.Items[1].Equals(Term.List(Term.A(StaticApplyRegressionNames.First), Term.A(StaticApplyRegressionNames.Second)))
            && values.Items[2].Equals(Term.Tuple(Nil.Value, Nil.Value)));
        QueueFailure(values.Items[3], false);
        QueueFailure(values.Items[4], true);
        NativeFailure(values.Items[5]);
        NativeFailure(values.Items[6]);
    }

    private static Term[] ErrorFrames(Term value)
    {
        Require(value is TupleTerm { Items.Count: 3 });
        var capture = (TupleTerm)value;
        Require(capture.Items[0].Equals(Term.A(ErlangExceptionClasses.Error))
            && capture.Items[1].Equals(Term.A(ErlangErrorReasons.BadArgument)));
        Term[] frames = Cons.Items(capture.Items[2]).ToArray();
        Require(frames.Length != 0 && frames.All(frame => frame is TupleTerm { Items.Count: 4 }));
        return frames;
    }

    private static void QueueFailure(Term value, bool nonTail)
    {
        Term[] frames = ErrorFrames(value);
        Term expected = Term.Tuple(Term.A(QueueModuleNames.Module), Term.A(QueueModuleNames.Get),
            Term.List(Term.A(StaticApplyRegressionNames.InvalidQueue)),
            Location(StaticApplyRegressionNames.QueueFileName, StaticApplyRegressionNames.GetError));
        Require(frames[0].Equals(expected));
        Require(!frames.Any(frame => ((TupleTerm)frame).Items[0].Equals(Term.A(ErlangModuleNames.Module))
            && ((TupleTerm)frame).Items[1].Equals(Term.A(StaticApplyRegressionNames.Apply))));
        Require(!frames.Any(frame => ((TupleTerm)frame).Items[0].Equals(Term.A(StaticApplyRegressionNames.Module))
            && ((TupleTerm)frame).Items[1].Equals(Term.A(StaticApplyRegressionNames.Tail))));
        Term continuation = Term.Tuple(Term.A(StaticApplyRegressionNames.Module), Term.A(StaticApplyRegressionNames.NonTail), Term.I(0),
            Location(StaticApplyRegressionSources.ExecutionFile, StaticApplyRegressionNames.NonTailCall));
        Require(frames.Contains(continuation) == nonTail);
    }

    private static void NativeFailure(Term value)
    {
        Term[] frames = ErrorFrames(value);
        Term expected = Term.Tuple(Term.A(ErlangModuleNames.Module), Term.A(StaticApplyRegressionNames.Apply),
            Term.List(Term.A(QueueModuleNames.Module), Term.A(QueueModuleNames.New),
                new Cons(Term.A(StaticApplyRegressionNames.First), Term.A(StaticApplyRegressionNames.ImproperTail))),
            Term.List(Term.Tuple(Term.A(StaticApplyRegressionNames.ErrorInfo), new MapTerm(new Dictionary<Term, Term>
            {
                [Term.A(StaticApplyRegressionNames.FormatterModule)] = Term.A(StaticApplyRegressionNames.Formatter)
            }))));
        Require(frames[0].Equals(expected));
    }

    private static Term Location(string file, int line) => Term.List(
        Term.Tuple(Term.A(StaticApplyRegressionNames.File), Term.String(file)),
        Term.Tuple(Term.A(StaticApplyRegressionNames.Line), Term.I(line)));

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(StaticApplyRegressionNames.Failure);
    }
}
