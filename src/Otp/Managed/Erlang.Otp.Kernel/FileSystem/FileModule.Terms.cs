// Ported from OTP-29.1.1 file.erl (Apache-2.0), Ericsson AB 1996-2026.
namespace Erlang;

public static partial class FileModule
{
    private static async ValueTask<Term> FormatError(ProcessContext context, Term error)
    {
        if (error is TupleTerm { Items.Count: FileTupleLayout.LocatedErrorArity } location)
        {
            Term line = location.Items[0];
            Term module = location.Items[1];
            Term reason = location.Items[2];
            if (module.Equals(A(Name)))
            {
                if (reason.Equals(A(FileProtocol.UndefinedScript)))
                {
                    return Term.String(FileErrorMessages.UndefinedScript);
                }
                if (reason is TupleTerm { Items.Count: FileTupleLayout.ClassReasonStackArity } failure)
                {
                    return await Call(
                        context,
                        FileDependencyNames.IoLibModule,
                        FileDependencyNames.FormatFunction,
                        Term.String(FileErrorMessages.EvaluationClass),
                        Term.List(
                            line,
                            failure.Items[0],
                            failure.Items[1],
                            failure.Items[2]
                        )
                    );
                }
                if (reason is TupleTerm { Items.Count: FileTupleLayout.ReasonStackArity } pair)
                {
                    return await Call(
                        context,
                        FileDependencyNames.IoLibModule,
                        FileDependencyNames.FormatFunction,
                        Term.String(FileErrorMessages.EvaluationReason),
                        Term.List(line, pair.Items[0], pair.Items[1])
                    );
                }
            }
            Term message = await CallModule(
                context,
                module,
                FileFunctionNames.FormatError,
                reason
            );

            return await Call(
                context,
                FileDependencyNames.IoLibModule,
                FileDependencyNames.FormatFunction,
                Term.String(FileErrorMessages.Located),
                Term.List(line, message)
            );
        }
        if (error.Equals(A(ErlangErrorReasons.BadArgument)))
        {
            return Term.String(FileErrorMessages.BadArgument);
        }
        if (error.Equals(A(ErlangErrorReasons.SystemLimit)))
        {
            return Term.String(FileErrorMessages.SystemLimit);
        }
        if (error.Equals(A(FileProtocol.Terminated)))
        {
            return Term.String(FileErrorMessages.Terminated);
        }

        return await Call(
            context,
            FileDependencyNames.PosixModule,
            FileDependencyNames.PosixMessageFunction,
            error
        );
    }

    private static async ValueTask<Term> ReadTerms(ProcessContext context, string function, IReadOnlyList<Term> arguments)
    {
        bool path = function.StartsWith(FileFunctionNames.PathPrefix, StringComparison.Ordinal);
        string operation = path ? function[FileFunctionNames.PathPrefix.Length..] : function;
        Term file = arguments[path ? 1 : 0];
        Term bindings = Nil.Value;
        if (operation != FileFunctionNames.Consult)
        {
            bindings = arguments.Count > (path ? FileApiArity.Binary : FileApiArity.Unary) ? arguments[^1] : await Call(context, FileDependencyNames.EvalModule, FileDependencyNames.NewBindingsFunction);
        }
        Term opened = path
            ? await PathOpen(
                context,
                arguments[0],
                file,
                Term.List(A(FileProtocol.Read))
            )
            : await Open(context, file, Term.List(A(FileProtocol.Read)));
        if (opened is not TupleTerm tuple || tuple.Items.Count != (path ? FileTupleLayout.OpenPathResultArity : FileTupleLayout.ResultArity) || !tuple.Items[0].Equals(Ok))
        {
            return opened;
        }
        Term handle = tuple.Items[1];
        int streamLine = function switch
        {
            FileFunctionNames.Consult => FileSourceLocations.ConsultStreamCall,
            FileFunctionNames.PathConsult => FileSourceLocations.PathConsultStreamCall,
            FileFunctionNames.Eval => FileSourceLocations.EvalStreamCall,
            FileFunctionNames.PathEval => FileSourceLocations.PathEvalStreamCall,
            FileFunctionNames.Script => FileSourceLocations.ScriptStreamCall,
            _ => FileSourceLocations.PathScriptStreamCall
        };
        int sourceArity = operation == FileFunctionNames.Consult ? path ? FileApiArity.Binary : FileApiArity.Unary : path ? FileApiArity.Ternary : FileApiArity.Binary;
        using var stream = FileSource.Scope(
            context,
            function,
            sourceArity,
            streamLine
        );
        Term result = operation == FileFunctionNames.Consult ? await ConsultStream(context, handle) : await EvalStream(
            context,
            handle,
            operation == FileFunctionNames.Script,
            bindings
        );
        _ = await Device(context, FileFunctionNames.Close, [handle]);
        if (path && (result.Equals(Ok) || IsSuccess(result)))
        {
            return result.Equals(Ok) ? Success(tuple.Items[2]) : Term.Tuple(Ok, ((TupleTerm)result).Items[1], tuple.Items[2]);
        }

        return result;
    }

    private static async ValueTask<Term> ConsultStream(ProcessContext context, Term handle)
    {
        using (FileSource.Scope(
            context,
            FileFunctionNames.ConsultStream,
            FileSourceArities.ConsultEncoding,
            FileSourceLocations.ConsultEncodingCall
        ))
        {
            _ = await Call(
                context,
                FileDependencyNames.EppModule,
                FileDependencyNames.SetEncodingFunction,
                handle
            );
        }
        Term line = Term.I(1);
        var terms = new List<Term>();
        while (true)
        {
            Term response;
            using (FileSource.Scope(
                context,
                FileFunctionNames.ConsultStream,
                FileSourceArities.ConsultLoop,
                FileSourceLocations.ConsultReadCall
            ))
            {
                response = await Call(
                    context,
                    FileProtocol.Io,
                    FileDependencyNames.IoReadTermFunction,
                    handle,
                    A(string.Empty),
                    line
                );
            }
            if (response is TupleTerm { Items.Count: FileTupleLayout.LocatedIoResultArity } tuple && tuple.Items[0].Equals(Ok))
            {
                terms.Add(tuple.Items[1]);
                line = tuple.Items[2];
            }
            else if (response is TupleTerm error && error.Items[0].Equals(A(FileProtocol.Error)))
            {
                return Error(error.Items[1]);
            }
            else if (response is TupleTerm { Items.Count: FileTupleLayout.ResultArity } eof && eof.Items[0].Equals(A(FileProtocol.Eof)))
            {
                return Success(Cons.From(terms));
            }
            else
            {
                throw new ErlangException(Term.Tuple(A(ErlangErrorReasons.CaseClause), response));
            }
        }
    }

    private static async ValueTask<Term> EvalStream(
        ProcessContext context,
        Term handle,
        bool returnsValue,
        Term bindings
    )
    {
        using (FileSource.Scope(
            context,
            FileFunctionNames.EvalStream,
            FileSourceArities.EvalEncoding,
            FileSourceLocations.EvalEncodingCall
        ))
        {
            _ = await Call(
                context,
                FileDependencyNames.EppModule,
                FileDependencyNames.SetEncodingFunction,
                handle
            );
        }
        Term line = Term.I(1);
        Term? last = null;
        var errors = new List<Term>();
        while (true)
        {
            Term response;
            using (FileSource.Scope(
                context,
                FileFunctionNames.EvalStream,
                FileSourceArities.EvalLoop,
                FileSourceLocations.EvalParseCall
            ))
            {
                response = await Call(
                    context,
                    FileProtocol.Io,
                    FileDependencyNames.ParseExpressionsFunction,
                    handle,
                    A(string.Empty),
                    line
                );
            }
            if (response is TupleTerm { Items.Count: FileTupleLayout.LocatedIoResultArity } tuple && tuple.Items[0].Equals(Ok))
            {
                line = tuple.Items[2];
                Term result;
                try
                {
                    using var evaluation = FileSource.Scope(
                        context,
                        FileFunctionNames.EvalStream2,
                        FileSourceArities.EvalStep,
                        FileSourceLocations.EvalExpressionsCall
                    );
                    result = await Call(
                        context,
                        FileDependencyNames.EvalModule,
                        FileDependencyNames.EvaluateExpressionsFunction,
                        tuple.Items[1],
                        bindings
                    );
                }
                catch (ErlangException error)
                {
                    errors.Add(Term.Tuple(line, A(Name), Term.Tuple(A(error.ExceptionClass), error.Reason, error.StackTraceTerm)));
                    continue;
                }
                if (result is not TupleTerm { Items.Count: FileTupleLayout.EvaluationResultArity } value || !value.Items[0].Equals(A(FileProtocol.Value)))
                {
                    throw new ErlangException(Term.Tuple(A(FileProtocol.TryClause), result));
                }
                last = value.Items[1];
                bindings = value.Items[2];
            }
            else if (response is TupleTerm { Items.Count: FileTupleLayout.LocatedIoResultArity } failure && failure.Items[0].Equals(A(FileProtocol.Error)))
            {
                errors.Add(failure.Items[1]);
                line = failure.Items[2];
            }
            else if (response is TupleTerm { Items.Count: FileTupleLayout.ResultArity } eof && eof.Items[0].Equals(A(FileProtocol.Eof)))
            {
                if (errors.Count > 0)
                {
                    return Error(errors[0]);
                }

                return !returnsValue ? Ok : last is not null ? Success(last)
                    : Error(Term.Tuple(eof.Items[1], A(Name), A(FileProtocol.UndefinedScript)));
            }
            else
            {
                throw FileSource.Clause(
                    context,
                    FileFunctionNames.EvalStream2,
                    [response, handle, A(returnsValue ? FileProtocol.Return : FileProtocol.Ignore),
                    last is null ? A(FileProtocol.Undefined) : Term.Tuple(last), Cons.From(errors.AsEnumerable().Reverse()), bindings],
                    FileSourceLocations.EvalStreamStepClause
                );
            }
        }
    }
}
