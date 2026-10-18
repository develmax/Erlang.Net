using Erlang.Differential;

namespace Erlang.Tests;

internal static class QueueCallbackRegressionHostChecks
{
    internal static async Task Run()
    {
        var assembly = typeof(QueueCallbackRegressionHostChecks).Assembly;
        string resource = assembly.GetManifestResourceNames().Single(name => name.EndsWith(QueueCallbackRegressionSource.FileName, StringComparison.Ordinal));
        using Stream stream = assembly.GetManifestResourceStream(resource)!;
        using var reader = new StreamReader(stream);
        using var artifact = GeneratedModuleCompiler.Compile(await reader.ReadToEndAsync(), QueueCallbackRegressionSource.FileName);
        Term outcome = await CompiledModuleExecution.Run(artifact);
        Require(outcome is TupleTerm { Items.Count: 2 } result && result.Items[0].Equals(Term.A(QueueCallbackRegressionNames.Success)));
        Term[] rows = Cons.Items(((TupleTerm)outcome).Items[1]).ToArray();
        Require(rows.Length == QueueCallbackRegressionNames.Outcomes);
        string[] functions = [QueueModuleNames.Filter, QueueModuleNames.FilterMap, QueueModuleNames.Any,
            QueueModuleNames.All, QueueModuleNames.DeleteWith, QueueModuleNames.DeleteWithReverse];
        var failures = new List<Exception>();
        for (int index = 0; index < functions.Length; index++)
        {
            try
            {
                BadArity(rows[index], functions[index], QueueCallbackRegressionSource.FirstRunCall + index);
            }
            catch (Exception error)
            {
                failures.Add(new InvalidOperationException(functions[index], error));
            }
        }
        try
        {
            Expansion(rows[^1]);
        }
        catch (Exception error)
        {
            failures.Add(error);
        }
        if (failures.Count != 0)
            throw new AggregateException(QueueCallbackRegressionNames.Failure, failures);
    }

    private static void BadArity(Term value, string function, int runLine)
    {
        var row = ErrorRow(value, function);
        Require(row.Items[2] is TupleTerm { Items.Count: 2 } reason && reason.Items[0].Equals(Term.A(ErlangErrorReasons.BadArity)));
        var pair = (TupleTerm)((TupleTerm)row.Items[2]).Items[1];
        Require(pair.Items.Count == 2 && pair.Items[0] is FunctionTerm);
        var callback = (FunctionTerm)pair.Items[0];
        Require(callback.Arity == QueueCallbackRegressionNames.NoArgumentsArity && callback.Environment.Count == 0
            && callback.ExternalIdentity is null && callback.LocalIdentity is { } identity
            && identity.Module.Name == QueueCallbackRegressionSource.Module && identity.Name?.Name == QueueCallbackRegressionNames.Wrong);
        Term expectedArgument = Term.I(function == QueueModuleNames.DeleteWithReverse ? 3 : 1);
        Require(pair.Items[1].Equals(Term.List(expectedArgument)));
        var frames = new List<Term>
        {
            SourceFrame(
                QueueCallbackRegressionSource.Module,
                QueueCallbackRegressionNames.Invoke,
                QueueCallbackRegressionNames.QueueCallbackArity,
                QueueCallbackRegressionSource.FileName,
                QueueCallbackRegressionSource.InvokeCall
            )
        };
        frames.AddRange(QueueFrames(function));
        frames.Add(SourceFrame(
            QueueCallbackRegressionSource.Module,
            QueueCallbackRegressionNames.BadArityCapture,
            QueueCallbackRegressionNames.CaptureArity,
            QueueCallbackRegressionSource.FileName,
            QueueCallbackRegressionSource.CaptureCall
        ));
        frames.Add(SourceFrame(
            QueueCallbackRegressionSource.Module,
            QueueCallbackRegressionNames.Entry,
            QueueCallbackRegressionNames.NoArgumentsArity,
            QueueCallbackRegressionSource.FileName,
            runLine
        ));
        Require(row.Items[3].Equals(Cons.From(frames)));
    }

    private static IEnumerable<Term> QueueFrames(string function)
    {
        (string Module, string Function, int Line)[] frames = function switch
        {
            QueueModuleNames.Filter => [(QueueModuleNames.Module, QueueCallbackRegressionNames.FilterFront, QueueCallbackRegressionSource.FilterFrontCallback),
                (QueueModuleNames.Module, QueueModuleNames.Filter, QueueCallbackRegressionSource.FilterFrontCall)],
            QueueModuleNames.FilterMap => [(QueueCallbackRegressionNames.ListsModule, QueueCallbackRegressionNames.ListFilterMap, QueueCallbackRegressionSource.ListFilterMapCallback),
                (QueueModuleNames.Module, QueueModuleNames.FilterMap, QueueCallbackRegressionSource.FilterMapFrontCall)],
            QueueModuleNames.Any => [(QueueCallbackRegressionNames.ListsModule, QueueModuleNames.Any, QueueCallbackRegressionSource.ListAnyCallback),
                (QueueModuleNames.Module, QueueModuleNames.Any, QueueCallbackRegressionSource.AnyFrontCall)],
            QueueModuleNames.All => [(QueueCallbackRegressionNames.ListsModule, QueueModuleNames.All, QueueCallbackRegressionSource.ListAllCallback),
                (QueueModuleNames.Module, QueueModuleNames.All, QueueCallbackRegressionSource.AllFrontCall)],
            QueueModuleNames.DeleteWith => [(QueueModuleNames.Module, QueueCallbackRegressionNames.DeleteFront, QueueCallbackRegressionSource.DeleteFrontCallback),
                (QueueModuleNames.Module, QueueModuleNames.DeleteWith, QueueCallbackRegressionSource.DeleteFrontCall)],
            QueueModuleNames.DeleteWithReverse => [(QueueModuleNames.Module, QueueCallbackRegressionNames.DeleteFront, QueueCallbackRegressionSource.DeleteFrontCallback),
                (QueueModuleNames.Module, QueueModuleNames.DeleteWith, QueueCallbackRegressionSource.DeleteFrontCall),
                (QueueModuleNames.Module, QueueModuleNames.DeleteWithReverse, QueueCallbackRegressionSource.DeleteReverseCall)],
            _ => throw new InvalidOperationException(QueueCallbackRegressionNames.Failure)
        };

        return frames.Select(frame => SourceFrame(
            frame.Module,
            frame.Function,
            QueueCallbackRegressionNames.QueueCallbackArity,
            frame.Module == QueueModuleNames.Module ? QueueCallbackRegressionSource.QueueFileName : QueueCallbackRegressionSource.ListsFileName,
            frame.Line
        ));
    }

    private static void Expansion(Term value)
    {
        var row = ErrorRow(value, QueueCallbackRegressionNames.Expansion);
        Require(row.Items[2].Equals(Term.A(ErlangErrorReasons.BadArgument)));
        Term first = Term.Tuple(
            Term.A(QueueCallbackRegressionNames.ListsModule),
            Term.A(QueueCallbackRegressionNames.ListReverse),
            Term.List(new Cons(Term.I(1), Term.A(QueueCallbackRegressionNames.CallbackTail)), Nil.Value),
            Term.List(Term.Tuple(
                Term.A(QueueCallbackRegressionNames.ErrorInfo),
                new MapTerm(new Dictionary<Term, Term>
                {
                    [Term.A(QueueCallbackRegressionNames.FormatterModule)] = Term.A(QueueCallbackRegressionNames.StdlibFormatter)
                })
            ))
        );
        Term expected = Term.List(
            first,
            SourceFrame(
                QueueModuleNames.Module,
                QueueCallbackRegressionNames.FilterRear,
                QueueCallbackRegressionNames.QueueCallbackArity,
                QueueCallbackRegressionSource.QueueFileName,
                QueueCallbackRegressionSource.FilterRearExpand
            ),
            SourceFrame(
                QueueModuleNames.Module,
                QueueCallbackRegressionNames.FilterRear,
                QueueCallbackRegressionNames.QueueCallbackArity,
                QueueCallbackRegressionSource.QueueFileName,
                QueueCallbackRegressionSource.FilterRearRecursive
            ),
            SourceFrame(
                QueueModuleNames.Module,
                QueueModuleNames.Filter,
                QueueCallbackRegressionNames.QueueCallbackArity,
                QueueCallbackRegressionSource.QueueFileName,
                QueueCallbackRegressionSource.FilterRearCall
            ),
            SourceFrame(
                QueueCallbackRegressionSource.Module,
                QueueCallbackRegressionNames.Expansion,
                QueueCallbackRegressionNames.NoArgumentsArity,
                QueueCallbackRegressionSource.FileName,
                QueueCallbackRegressionSource.ExpansionCall
            ),
            SourceFrame(
                QueueCallbackRegressionSource.Module,
                QueueCallbackRegressionNames.Entry,
                QueueCallbackRegressionNames.NoArgumentsArity,
                QueueCallbackRegressionSource.FileName,
                QueueCallbackRegressionSource.RunExpansionCall
            )
        );
        Require(row.Items[3].Equals(expected));
    }

    private static TupleTerm ErrorRow(Term value, string operation)
    {
        Require(value is TupleTerm { Items.Count: 4 });
        var row = (TupleTerm)value;
        Require(row.Items[0].Equals(Term.A(operation)) && row.Items[1].Equals(Term.A(ErlangExceptionClasses.Error)));

        return row;
    }

    private static Term SourceFrame(
        string module,
        string function,
        int arity,
        string file,
        int line
    )
        => Term.Tuple(
            Term.A(module),
            Term.A(function),
            Term.I(arity),
            Term.List(
                Term.Tuple(Term.A(QueueCallbackRegressionNames.File), Term.String(file)),
                Term.Tuple(Term.A(QueueCallbackRegressionNames.Line), Term.I(line))
            )
        );

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(QueueCallbackRegressionNames.Failure);
    }
}
