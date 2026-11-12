// Derived from lib/stdlib/src/ets.erl, OTP-29.1.1, Apache-2.0.
// Copyright Ericsson AB 1996-2026. Original source and notices remain unchanged.
namespace Erlang;

/// <summary>The explicitly selected C# ETS library over the shared runtime ETS primitives.</summary>
public static partial class EtsModule
{
    public const string Name = "ets";

    public static IReadOnlyList<(string Name, int Arity)> LibraryExports { get; } = Array.AsReadOnly(new (string, int)[]
    {
        (EtsFunctionNames.All, 0), (EtsFunctionNames.DeleteAllObjects, 1), (EtsFunctionNames.SelectDelete, 2),
        (EtsFunctionNames.File2tab, 1), (EtsFunctionNames.File2tab, 2), (EtsFunctionNames.Foldl, 3), (EtsFunctionNames.Foldr, 3),
        (EtsFunctionNames.MatchDelete, 2), (EtsFunctionNames.Tab2file, 2), (EtsFunctionNames.Tab2file, 3), (EtsFunctionNames.TabfileInfo, 1),
        (EtsFunctionNames.FromDets, 2), (EtsFunctionNames.ToDets, 2), (EtsFunctionNames.InitTable, 2), (EtsFunctionNames.TestMs, 2),
        (EtsFunctionNames.Tab2list, 1), (EtsFunctionNames.Table, 1), (EtsFunctionNames.Table, 2), (EtsFunctionNames.Fun2ms, 1),
        (EtsFunctionNames.MatchSpecRun, 2), (EtsFunctionNames.RepairContinuation, 2),
        (EtsFunctionNames.Inspect, 0), (EtsFunctionNames.Inspect, 1), (EtsFunctionNames.Inspect, 2), (EtsFunctionNames.Inspect, 3)
    });

    public static void Register(ModuleRegistry registry)
        => registry.InstallSourceWrappers(target =>
        {
            foreach (var export in LibraryExports)
            {
                string function = export.Name;
                target.RegisterReviewedManagedLibrary789(
                    Name,
                    function,
                    export.Arity,
                    (context, arguments) => Invoke(context, function, arguments)
                );
            }
        });

    public static async ValueTask<Term> Invoke(ProcessContext context, string function, IReadOnlyList<Term> arguments)
    {
        if (NativeExports.Contains((function, arguments.Count)))
        {
            return await context.Runtime.Modules.Call(
                context,
                Name,
                function,
                arguments.ToArray()
            );
        }
        if (!LibraryExports.Contains((function, arguments.Count)))
        {
            throw new ErlangException(ErlangErrorReasons.UndefinedFunction);
        }
        using var tail = context.EnterSourceTailCall(Name, function);
        int line = function switch
        {
            EtsFunctionNames.All => EtsSourceLines.AllRequest,
            EtsFunctionNames.DeleteAllObjects => EtsSourceLines.DeleteAllObjects,
            EtsFunctionNames.SelectDelete => arguments[1].Equals(Term.List(Term.Tuple(A(EtsAtomNames.Wildcard), Nil.Value, Term.List(A(EtsAtomNames.True))))) ? EtsSourceLines.SelectDeleteAll : EtsSourceLines.SelectDeleteMatch,
            EtsFunctionNames.Tab2list => EtsSourceLines.TableToList,
            EtsFunctionNames.MatchDelete => EtsSourceLines.MatchDelete,
            EtsFunctionNames.MatchSpecRun => EtsSourceLines.MatchSpecRun,
            EtsFunctionNames.RepairContinuation => EtsSourceLines.RepairContinuation,
            EtsFunctionNames.Foldl => EtsSourceLines.FoldLeftResolve,
            EtsFunctionNames.Foldr => EtsSourceLines.FoldRightResolve,
            EtsFunctionNames.InitTable => EtsSourceLines.InitializeResolve,
            EtsFunctionNames.TestMs => EtsSourceLines.TestMatchSpec,
            EtsFunctionNames.Table => EtsSourceLines.QueryResolve,
            EtsFunctionNames.Fun2ms => EtsSourceLines.FunToMatchSpec,
            EtsFunctionNames.Tab2file => EtsSourceLines.SaveTable,
            EtsFunctionNames.File2tab => EtsSourceLines.LoadTable,
            EtsFunctionNames.TabfileInfo => EtsSourceLines.FileInformation,
            EtsFunctionNames.Inspect => arguments.Count == 0 ? EtsSourceLines.InspectTables : EtsSourceLines.BrowseTable,
            _ => 1
        };
        using var source = EtsSource.Frame(
            context,
            function,
            function is EtsFunctionNames.Table or EtsFunctionNames.File2tab ? 2 : function == EtsFunctionNames.Tab2file || function == EtsFunctionNames.Inspect && arguments.Count > 0 ? 3 : arguments.Count,
            line
        );
        switch (function)
        {
            case EtsFunctionNames.All:
                Term reference = await Call(context, Name, EtsFunctionNames.InternalRequestAll);
                var schedulers = (Integer)await Call(
                    context,
                    EtsDependencyModules.Erlang,
                    EtsFunctionNames.SystemInfo,
                    A(EtsAtomNames.Schedulers)
                );
                Term tables = Nil.Value;
                for (var lane = schedulers.Value; lane > 0; lane--)
                {
                    TupleTerm reply = (await context.ReceiveAsync(value => value is TupleTerm { Items.Count: 2 } pair
                        && pair.Items[0].Equals(reference) ? pair : null))!;
                    tables = Cons.From(Cons.Items(reply.Items[1]), tables);
                }

                return tables;
            case EtsFunctionNames.DeleteAllObjects:
                await Call(
                    context,
                    Name,
                    EtsFunctionNames.InternalDeleteAll,
                    arguments[0],
                    A(EtsAtomNames.Undefined)
                );

                return A(EtsAtomNames.True);
            case EtsFunctionNames.SelectDelete:
                if (arguments[1].Equals(Term.List(Term.Tuple(A(EtsAtomNames.Wildcard), Nil.Value, Term.List(A(EtsAtomNames.True))))))
                {
                    return await Call(
                        context,
                        Name,
                        EtsFunctionNames.InternalDeleteAll,
                        arguments[0],
                        A(EtsAtomNames.Undefined)
                    );
                }

                return await Call(
                    context,
                    Name,
                    EtsFunctionNames.InternalSelectDelete,
                    arguments[0],
                    arguments[1]
                );
            case EtsFunctionNames.Tab2list:
                return await Call(
                    context,
                    Name,
                    EtsFunctionNames.MatchObject,
                    arguments[0],
                    A(EtsAtomNames.Wildcard)
                );
            case EtsFunctionNames.MatchDelete:
                await Invoke(
                    context,
                    EtsFunctionNames.SelectDelete,
                    [arguments[0], Term.List(Term.Tuple(arguments[1], Nil.Value, Term.List(A(EtsAtomNames.True))))]
                );

                return A(EtsAtomNames.True);
            case EtsFunctionNames.MatchSpecRun:
                return await Call(
                    context,
                    EtsDependencyModules.Lists,
                    EtsFunctionNames.Reverse,
                    await Call(
                        context,
                        Name,
                        EtsFunctionNames.MatchSpecRunR,
                        arguments[0],
                        arguments[1],
                        Nil.Value
                    )
                );
            case EtsFunctionNames.RepairContinuation:
                return await Repair(context, arguments[0], arguments[1]);
            case EtsFunctionNames.Foldl:
            case EtsFunctionNames.Foldr:
                return await Fold(
                    context,
                    function == EtsFunctionNames.Foldr,
                    arguments[0],
                    arguments[1],
                    arguments[2]
                );
            case EtsFunctionNames.InitTable:
                return await Initialize(context, arguments[0], arguments[1]);
            case EtsFunctionNames.TestMs:
                Term tested = await Call(
                    context,
                    EtsDependencyModules.Erlang,
                    EtsFunctionNames.MatchSpecTest,
                    arguments[0],
                    arguments[1],
                    A(EtsAtomNames.Table)
                );

                return tested is TupleTerm { Items.Count: 4 } result && Is(result.Items[0], EtsAtomNames.Ok)
                    ? Term.Tuple(A(EtsAtomNames.Ok), result.Items[1]) : tested;
            case EtsFunctionNames.FromDets:
            case EtsFunctionNames.ToDets:
                return await Dets(
                    context,
                    function,
                    arguments[0],
                    arguments[1]
                );
            case EtsFunctionNames.Fun2ms:
                return await FunToMatchSpec(context, arguments[0]);
            case EtsFunctionNames.Tab2file:
                return await Save(
                    context,
                    arguments[0],
                    arguments[1],
                    arguments.Count == 3 ? arguments[2] : Nil.Value
                );
            case EtsFunctionNames.File2tab:
                return await Load(context, arguments[0], arguments.Count == 2 ? arguments[1] : Nil.Value);
            case EtsFunctionNames.TabfileInfo:
                return await FileInfo(context, arguments[0]);
            case EtsFunctionNames.Table:
                return await QueryTable(context, arguments[0], arguments.Count == 2 ? arguments[1] : Nil.Value);
            case EtsFunctionNames.Inspect:
                return arguments.Count == 0 ? await DisplayTables(context)
                    : await Browse(
                        context,
                        arguments[0],
                        arguments.Count >= 2 ? arguments[1] : Term.I(EtsDisplayLayout.DefaultHeight),
                        arguments.Count == 3 ? arguments[2] : Term.I(EtsDisplayLayout.DefaultWidth)
                    );
            default:
                throw new ErlangException(ErlangErrorReasons.UndefinedFunction);
        }
    }

    private static Atom A(string value) => Term.A(value);

    private static bool Is(Term value, string atom) => value is Atom a && a.Name == atom;

    private static bool IsList(Term value) => value is Nil or Cons;

    private static ValueTask<Term> Call(
        ProcessContext context,
        string module,
        string function,
        params Term[] arguments
    )
        => context.Runtime.Modules.Call(
            context,
            module,
            function,
            arguments
        );

    private static async ValueTask<Term> SoftWhereis(ProcessContext context, Term table)
    {
        if (table is not Atom)
        {
            return table;
        }
        Term reference = await Call(
            context,
            Name,
            EtsFunctionNames.Whereis,
            table
        );
        if (Is(reference, EtsAtomNames.Undefined))
        {
            throw EtsSource.Error(
                context,
                EtsFunctionNames.SoftWhereis,
                [table],
                A(EtsErrorNames.Badarg),
                EtsSourceLines.ResolveTableError,
                Term.Tuple(
                    A(EtsAtomNames.ErrorInfo),
                    new MapTerm([
                    new KeyValuePair<Term, Term>(A(EtsAtomNames.Cause), A(EtsAtomNames.Id)),
                    new KeyValuePair<Term, Term>(A(EtsAtomNames.Module), A(EtsAtomNames.ErlStdlibErrors))
                ])
                )
            );
        }

        return reference;
    }

    private static async ValueTask<Term> Apply(ProcessContext context, Term function, params Term[] arguments)
    {
        if (function is not FunctionTerm callback)
        {
            throw ErlangFunctionCallErrors.Capture(context, Term.Tuple(A(EtsErrorNames.Badfun), function));
        }
        if (callback.Arity != arguments.Length)
        {
            throw ErlangFunctionCallErrors.Capture(context,
                Term.Tuple(A(EtsErrorNames.Badarity), Term.Tuple(function, Term.List(arguments))));
        }

        try
        {
            return await callback.Invoke(context, arguments);
        }
        catch (ErlangException error)
        {
            throw context.CaptureSourceFailure(error);
        }
    }

    private static async ValueTask<Term> Repair(ProcessContext context, Term continuation, Term spec)
    {
        if (Is(continuation, EtsAtomNames.EndOfTable))
        {
            return continuation;
        }
        if (continuation is TupleTerm tuple && (tuple.Items.Count == EtsContinuationLayout.OrderedArity
            && tuple.Items[EtsContinuationLayout.OrderedChunkSizeIndex] is Integer
            && IsList(tuple.Items[EtsContinuationLayout.OrderedObjectsIndex])
            && tuple.Items[EtsContinuationLayout.OrderedRemainingIndex] is Integer
            && tuple.Items[EtsContinuationLayout.OrderedReturnedIndex] is Integer
            || tuple.Items.Count == EtsContinuationLayout.HashArity
            && tuple.Items[EtsContinuationLayout.HashBucketIndex] is Integer
            && tuple.Items[EtsContinuationLayout.HashChunkSizeIndex] is Integer
            && IsList(tuple.Items[EtsContinuationLayout.HashObjectsIndex])
            && tuple.Items[EtsContinuationLayout.HashRemainingIndex] is Integer))
        {
            int position = tuple.Items.Count == EtsContinuationLayout.OrderedArity ? EtsContinuationLayout.OrderedMatchSpecIndex : EtsContinuationLayout.HashMatchSpecIndex;
            if (Is(await Call(
                context,
                Name,
                EtsFunctionNames.IsCompiledMs,
                tuple.Items[position]
            ), EtsAtomNames.True))
            {
                return continuation;
            }
            Term[] values = tuple.Items.ToArray();
            using var location = context.EnterSourceLocation(EtsSource.Location(tuple.Items.Count == EtsContinuationLayout.OrderedArity ? EtsSourceLines.RepairOrderedContinuationCompile : EtsSourceLines.RepairHashContinuationCompile));
            values[position] = await Call(
                context,
                Name,
                EtsFunctionNames.MatchSpecCompile,
                spec
            );

            return Term.Tuple(values);
        }

        throw EtsSource.Error(
            context,
            EtsFunctionNames.RepairContinuation,
            [continuation, spec],
            A(EtsErrorNames.FunctionClause),
            EtsSourceLines.RepairContinuation
        );
    }

    private static async ValueTask<Term> Fold(
        ProcessContext context,
        bool reverse,
        Term function,
        Term accumulator,
        Term tableArg
    )
    {
        Term table = await SoftWhereis(context, tableArg);
        Term cursor;
        using (context.EnterSourceLocation(EtsSource.Location(reverse ? EtsSourceLines.FoldRightFix : EtsSourceLines.FoldLeftFix)))
        {
            await Call(
                context,
                Name,
                EtsFunctionNames.SafeFixtable,
                table,
                A(EtsAtomNames.True)
            );
        }
        using (context.EnterSourceLocation(EtsSource.Location(reverse ? EtsSourceLines.FoldRightLast : EtsSourceLines.FoldLeftFirst)))
        {
            cursor = await Call(
                context,
                Name,
                reverse ? EtsFunctionNames.LastLookup : EtsFunctionNames.FirstLookup,
                table
            );
        }
        using var body = context.EnterSourceLocation(EtsSource.Location(reverse ? EtsSourceLines.FoldRightBody : EtsSourceLines.FoldLeftBody));
        try
        {
            while (!Is(cursor, EtsAtomNames.EndOfTable))
            {
                var pair = (TupleTerm)cursor;
                using var helper = EtsSource.Frame(
                    context,
                    reverse ? EtsFunctionNames.DoFoldr : EtsFunctionNames.DoFoldl,
                    4,
                    reverse ? EtsSourceLines.FoldRightApply : EtsSourceLines.FoldLeftApply
                );
                accumulator = await Call(
                    context,
                    EtsDependencyModules.Lists,
                    reverse ? EtsFunctionNames.Foldr : EtsFunctionNames.Foldl,
                    function,
                    accumulator,
                    pair.Items[1]
                );
                using var next = context.EnterSourceLocation(EtsSource.Location(reverse ? EtsSourceLines.FoldRightPrevious : EtsSourceLines.FoldLeftNext));
                cursor = await Call(
                    context,
                    Name,
                    reverse ? EtsFunctionNames.PrevLookup : EtsFunctionNames.NextLookup,
                    table,
                    pair.Items[0]
                );
            }

            return accumulator;
        }
        finally
        {
            using var release = context.EnterSourceLocation(EtsSource.Location(reverse ? EtsSourceLines.FoldRightRelease : EtsSourceLines.FoldLeftRelease));
            await Call(
                context,
                Name,
                EtsFunctionNames.SafeFixtable,
                table,
                A(EtsAtomNames.False)
            );
        }
    }

    private static async ValueTask<Term> Initialize(ProcessContext context, Term tableArg, Term function)
    {
        Term table = await SoftWhereis(context, tableArg);
        using (context.EnterSourceLocation(EtsSource.Location(EtsSourceLines.InitializeClear)))
        {
            await Invoke(context, EtsFunctionNames.DeleteAllObjects, [table]);
        }
        Term chunk;
        using (context.EnterSourceLocation(EtsSource.Location(EtsSourceLines.InitializeRead)))
        {
            chunk = await Apply(context, function, A(EtsInitializationCallbacks.Read));
        }
        using var continuation = EtsSource.Frame(
            context,
            EtsFunctionNames.InitTableContinue,
            2,
            EtsSourceLines.InitializeContinue,
            tail: true
        );
        while (!Is(chunk, EtsAtomNames.EndOfInput))
        {
            if (chunk is not TupleTerm { Items.Count: 2 } pair || !IsList(pair.Items[0]) || pair.Items[1] is not FunctionTerm)
            {
                using var failed = context.EnterSourceLocation(EtsSource.Location(EtsSourceLines.InitializeCallbackExit));
                throw context.CaptureSourceFailure(new ErlangException(chunk, EtsExceptionClasses.Exit));
            }
            function = pair.Items[1];
            try
            {
                using var insert = EtsSource.Frame(
                    context,
                    EtsFunctionNames.InitTableSub,
                    2,
                    EtsSourceLines.InitializeInsert
                );
                Term input = pair.Items[0];
                while (input is Cons cell)
                {
                    await Call(
                        context,
                        Name,
                        EtsFunctionNames.Insert,
                        table,
                        cell.Head
                    );
                    input = cell.Tail;
                }
                if (input is not Nil)
                {
                    throw EtsSource.Error(
                        context,
                        EtsFunctionNames.InitTableSub,
                        [table, input],
                        A(EtsErrorNames.FunctionClause),
                        EtsSourceLines.InitializeBatchGuard
                    );
                }
            }
            catch (ErlangException error)
            {
                await Ignore(() => Apply(context, function, A(EtsInitializationCallbacks.Close)));
                Term reason = error.ExceptionClass == EtsExceptionClasses.Error ? Term.Tuple(error.Reason, error.StackTraceTerm) : error.Reason;
                using var failed = context.EnterSourceLocation(EtsSource.Location(EtsSourceLines.InitializeBatchExit));
                throw context.CaptureSourceFailure(new ErlangException(reason, EtsExceptionClasses.Exit));
            }
            using var read = context.EnterSourceLocation(EtsSource.Location(EtsSourceLines.InitializeReadNext));
            chunk = await Apply(context, function, A(EtsInitializationCallbacks.Read));
        }

        return A(EtsAtomNames.True);
    }

    private static async ValueTask<Term> Dets(
        ProcessContext context,
        string function,
        Term table,
        Term disk
    )
    {
        Term result;
        try
        {
            result = await Call(
                context,
                EtsDependencyModules.Dets,
                function == EtsFunctionNames.FromDets ? EtsFunctionNames.ToEts : EtsFunctionNames.FromEts,
                disk,
                table
            );
        }
        catch (ErlangException error)
        {
            result = error.ExceptionClass == EtsExceptionClasses.Throw ? error.Reason
                : Term.Tuple(
                    A(EtsAtomNames.ExitSignal),
                    error.ExceptionClass == EtsExceptionClasses.Error ? Term.Tuple(error.Reason, error.StackTraceTerm) : error.Reason
                );
        }
        if (result is TupleTerm { Items.Count: 2 } pair && (Is(pair.Items[0], EtsAtomNames.Error) || Is(pair.Items[0], EtsAtomNames.ExitSignal)))
        {
            Term reason = Is(pair.Items[0], EtsAtomNames.ExitSignal) && pair.Items[1] is TupleTerm { Items.Count: 2 } exit ? exit.Items[0] : pair.Items[1];
            int line = function == EtsFunctionNames.FromDets ? Is(pair.Items[0], EtsAtomNames.Error) ? EtsSourceLines.FromDetsError : pair.Items[1] is TupleTerm { Items.Count: 2 } ? EtsSourceLines.FromDetsStackError : EtsSourceLines.FromDetsExit
                : Is(pair.Items[0], EtsAtomNames.Error) ? EtsSourceLines.ToDetsError : pair.Items[1] is TupleTerm { Items.Count: 2 } ? EtsSourceLines.ToDetsStackError : EtsSourceLines.ToDetsExit;
            throw EtsSource.Error(
                context,
                function,
                [table, disk],
                reason,
                line
            );
        }
        if (result.Equals(function == EtsFunctionNames.FromDets ? table : A(EtsAtomNames.Ok)))
        {
            return function == EtsFunctionNames.FromDets ? A(EtsAtomNames.True) : disk;
        }

        throw EtsSource.Error(
            context,
            function,
            [table, disk],
            result,
            function == EtsFunctionNames.FromDets ? EtsSourceLines.FromDetsUnexpected : EtsSourceLines.ToDetsUnexpected
        );
    }

    private static async ValueTask<Term> FunToMatchSpec(ProcessContext context, Term function)
    {
        if (function is not FunctionTerm)
        {
            throw EtsSource.Error(
                context,
                EtsFunctionNames.Fun2ms,
                [function],
                A(EtsErrorNames.FunctionClause),
                EtsSourceLines.FunToMatchSpecGuard
            );
        }
        Term data = await Call(
            context,
            EtsDependencyModules.ErlEval,
            EtsFunctionNames.FunData,
            function
        );
        if (data is TupleTerm { Items.Count: 3 } tuple && Is(tuple.Items[0], EtsAtomNames.FunData))
        {
            Term transformed = await Call(
                context,
                EtsDependencyModules.MsTransform,
                EtsFunctionNames.TransformFromShell,
                A(Name),
                tuple.Items[2],
                tuple.Items[1]
            );

            return transformed is TupleTerm { Items.Count: 3 } error && Is(error.Items[0], EtsAtomNames.Error) && error.Items[1] is Cons
                ? Term.Tuple(A(EtsAtomNames.Error), A(EtsErrorNames.TransformError)) : transformed;
        }

        using var failed = context.EnterSourceLocation(EtsSource.Location(EtsSourceLines.FunToMatchSpecRealFunction));
        throw context.CaptureSourceFailure(new ErlangException(
            Term.Tuple(
                A(EtsErrorNames.Badarg),
                Term.Tuple(
                    A(Name),
                    A(EtsAtomNames.Fun2ms),
                    Term.List(EtsDiagnostics.UntransformedFunction.Split(' ').Select(value => (Term)A(value)).ToArray())
                )
            ),
            EtsExceptionClasses.Exit
        ));
    }

    private static async ValueTask Ignore(Func<ValueTask<Term>> action)
    {
        try
        {
            await action();
        }
        catch (ErlangException)
        {
        }
    }
}
