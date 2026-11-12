// Derived from OTP-29.1.1 dets public query/callback control flow, Apache-2.0.
// Copyright Ericsson AB 1996-2026. Retained original source contains full notices.
using static Erlang.DetsNames;

namespace Erlang;

public static partial class DetsModule
{
    private static async ValueTask<Term> Match(ProcessContext context, string function, IReadOnlyList<Term> arguments)
    {
        string what = function == DetsFunctions.Match ? Bindings : function == DetsFunctions.MatchObject ? DetsNames.Object : Select;
        if (arguments.Count == 1)
        {
            if (arguments[0] is not TupleTerm { Items.Count: DetsContinuationFields.Count } continuation || continuation.Items[0] is not Atom { Name: Continuation }
                || continuation.Items[DetsContinuationFields.Kind] is not Atom kind || kind.Name != what)
            {
                return Badarg;
            }

            return await Request(
                context,
                continuation.Items[DetsContinuationFields.Process],
                Term.Tuple(Term.A(DetsRequestNames.Scan), continuation, Bool(false))
            );
        }
        bool safe = arguments.Count == 2;
        Term specification = what == Select ? arguments[1] : MatchSpec(arguments[1], what == Bindings ? DetsMatchNames.Bindings : DetsMatchNames.WholeObject);
        Term compiled;
        try
        {
            compiled = await Call(
                context,
                DetsDependencyNames.Ets,
                DetsExternalFunctions.MatchSpecCompile,
                specification
            );
        }
        catch (ErlangException)
        {
            return Badarg;
        }
        Term count = safe ? Term.A(Default) : arguments[2];
        if (count is not Atom { Name: Default } && (count is not Integer limit || limit.Value < 0))
        {
            return Badarg;
        }
        Term result = await TableRequest(
            context,
            arguments[0],
            Term.Tuple(
                Term.A(DetsRequestNames.Scan),
                Term.A(what),
                count,
                Term.Tuple(Term.A(DetsMatchNames.Program), compiled),
                specification,
                Bool(safe)
            )
        );
        if (!safe)
        {
            return result;
        }
        Term accumulated = Nil.Value;
        while (result is TupleTerm { Items.Count: 2 } chunk && chunk.Items[0] is Cons or Nil)
        {
            accumulated = Cons.From(Cons.Items(chunk.Items[0]), accumulated);
            var continuation = (TupleTerm)chunk.Items[1];
            result = await Request(
                context,
                continuation.Items[DetsContinuationFields.Process],
                Term.Tuple(Term.A(DetsRequestNames.Scan), continuation, Bool(true))
            );
        }

        return result.Equals(End) ? accumulated : result;
    }

    private static async ValueTask<Term> RepairContinuation(ProcessContext context, Term term, Term specification)
    {
        if (term is not TupleTerm { Items.Count: DetsContinuationFields.Count } state || state.Items[0] is not Atom { Name: Continuation })
        {
            return Badarg;
        }
        if (state.Items[DetsContinuationFields.MatchProgram] is not TupleTerm { Items.Count: 2 } program || program.Items[0] is not Atom { Name: DetsQueryNames.MatchSpecification })
        {
            return state;
        }
        if (await Call(
            context,
            DetsDependencyNames.Ets,
            DetsExternalFunctions.IsCompiledMs,
            program.Items[1]
        ) is Atom { Name: True })
        {
            return state;
        }
        Term[] items = state.Items.ToArray();
        items[7] = Term.Tuple(
            Term.A(DetsMatchNames.Program),
            await Call(
                context,
                DetsDependencyNames.Ets,
                DetsExternalFunctions.MatchSpecCompile,
                specification
            )
        );

        return new TupleTerm(items);
    }

    private static async ValueTask<Term> Traverse(ProcessContext context, string function, IReadOnlyList<Term> arguments)
    {
        if (function == DetsFunctions.FromEts)
        {
            Term source = arguments[1] is Atom
                ? await Call(
                    context,
                    DetsDependencyNames.Ets,
                    DetsExternalFunctions.Whereis,
                    arguments[1]
                ) : arguments[1];
            if (source.Equals(Undefined))
            {
                throw DetsSource.BadArgument(context, function, arguments);
            }
            _ = await Call(
                context,
                DetsDependencyNames.Ets,
                DetsExternalFunctions.SafeFixtable,
                source,
                Bool(true)
            );
            Term initialChunk = await Call(
                context,
                DetsDependencyNames.Ets,
                DetsExternalFunctions.Select,
                source,
                MatchSpec(Term.A(DetsMatchNames.Wildcard), DetsMatchNames.WholeObject),
                Term.I(DetsNames.EtsTransferChunkObjects)
            );
            Term result = await TableRequest(
                context,
                arguments[0],
                Term.Tuple(
                    Term.A(DetsRequestNames.Initialize),
                    FromEtsInput(initialChunk),
                    Term.A(DetsProtocolNames.Term),
                    Term.A(Default)
                )
            );
            _ = await Call(
                context,
                DetsDependencyNames.Ets,
                DetsExternalFunctions.SafeFixtable,
                source,
                Bool(false)
            );
            if (result is TupleTerm { Items.Count: 2 } thrown && thrown.Items[0] is Atom { Name: DetsProtocolNames.Thrown })
            {
                throw new ErlangException(thrown.Items[1], ErlangExceptionClasses.Throw);
            }
            if (result.Equals(Badarg))
            {
                throw DetsSource.BadArgument(context, function, [arguments[0], source]);
            }

            return result;
        }
        Term table = function == DetsFunctions.Traverse || function == DetsFunctions.ToEts ? arguments[0] : arguments[2];
        Term callback;
        Term accumulator;
        if (function == DetsFunctions.ToEts)
        {
            Term target = arguments[1] is Atom ? await Call(
                context,
                DetsDependencyNames.Ets,
                DetsExternalFunctions.Whereis,
                arguments[1]
            ) : arguments[1];
            if (target.Equals(Undefined))
            {
                throw DetsSource.BadArgument(context, function, arguments);
            }
            callback = new FunctionTerm(
                2,
                async (execution, args) =>
            {
                _ = await Call(
                    (ProcessContext)execution,
                    DetsDependencyNames.Ets,
                    DetsExternalFunctions.Insert,
                    target,
                    args[0]
                );

                return arguments[1];
            }
            );
            accumulator = arguments[1];
        }
        else
        {
            Term candidate = function == DetsFunctions.Traverse ? arguments[1] : arguments[0];
            callback = candidate;
            accumulator = function == DetsFunctions.Traverse ? Nil.Value : arguments[1];
        }
        Term specification = MatchSpec(Term.A(DetsMatchNames.Wildcard), DetsMatchNames.WholeObject);
        Term compiled = await Call(
            context,
            DetsDependencyNames.Ets,
            DetsExternalFunctions.MatchSpecCompile,
            specification
        );
        Term chunk = await TableRequest(
            context,
            table,
            Term.Tuple(
                Term.A(DetsRequestNames.Scan),
                Term.A(DetsNames.Object),
                Term.A(Default),
                Term.Tuple(Term.A(DetsMatchNames.Program), compiled),
                specification,
                Bool(true)
            )
        );
        while (chunk is TupleTerm { Items.Count: 2 } batch && batch.Items[0] is Cons or Nil)
        {
            foreach (Term value in Cons.Items(batch.Items[0]))
            {
                if (function != DetsFunctions.Traverse)
                {
                    accumulator = await TraverseCallback(
                        context,
                        function,
                        callback,
                        [value, accumulator]
                    );
                    continue;
                }
                Term result = await TraverseCallback(
                    context,
                    function,
                    callback,
                    [value]
                );
                if (result is Atom { Name: DetsProtocolNames.Continue })
                {
                    continue;
                }
                if (result is TupleTerm { Items.Count: 2 } item && item.Items[0] is Atom { Name: DetsProtocolNames.Continue or DetsProtocolNames.Done } action)
                {
                    accumulator = new Cons(item.Items[1], accumulator);
                    if (action.Name == DetsProtocolNames.Done)
                    {
                        return accumulator;
                    }
                    continue;
                }
                if (result.Equals(Badarg))
                {
                    throw DetsSource.BadArgument(context, function, arguments);
                }

                return result;
            }
            var state = (TupleTerm)batch.Items[1];
            chunk = await Request(context, state.Items[DetsContinuationFields.Process], Term.Tuple(Term.A(DetsRequestNames.Scan), state, Bool(true)));
        }
        if (chunk.Equals(Badarg))
        {
            throw DetsSource.BadArgument(context, function, arguments);
        }
        if (!chunk.Equals(End))
        {
            return chunk;
        }

        return accumulator;
    }

    private static async ValueTask<Term> TraverseCallback(
        ProcessContext context,
        string function,
        Term callback,
        Term[] arguments
    )
    {
        bool traverse = function == DetsFunctions.Traverse;
        using var entry = DetsSource.Scope(
            context,
            traverse ? DetsFunctions.Traverse : DetsFunctions.Foldl,
            traverse ? 2 : 3,
            traverse ? DetsSourceLines.TraverseCall : DetsSourceLines.FoldCall
        );
        using var worker = DetsSource.Scope(
            context,
            DetsProtocolNames.TraverseWorkerFunction,
            4,
            DetsSourceLines.TraverseWorkerCall
        );
        using var binaries = DetsSource.Scope(
            context,
            DetsProtocolNames.TraverseBinaryFunction,
            5,
            DetsSourceLines.FoldCallback
        );
        using var closure = traverse ? DetsSource.Scope(
            context,
            DetsProtocolNames.TraverseCallbackFunction,
            4,
            DetsSourceLines.TraverseCallback
        )
            : function == DetsFunctions.ToEts ? DetsSource.Scope(
                context,
                DetsProtocolNames.ToEtsCallbackFunction,
                2,
                DetsSourceLines.ToEtsCallback
            ) : null;

        return callback is FunctionTerm fun ? await fun.Invoke(context, arguments)
            : await Call(
                context,
                DetsDependencyNames.Erlang,
                DetsExternalFunctions.Apply,
                callback,
                Cons.From(arguments)
            );
    }

    private static FunctionTerm FromEtsInput(Term chunk) => new(
        1,
        async (execution, arguments) =>
    {
        if (arguments[0] is Atom { Name: Close })
        {
            return Ok;
        }
        if (chunk.Equals(End))
        {
            return Term.A(EndOfInput);
        }
        var batch = (TupleTerm)chunk;
        Term next = await Call(
            (ProcessContext)execution,
            DetsDependencyNames.Ets,
            DetsExternalFunctions.Select,
            batch.Items[1]
        );

        return Term.Tuple(batch.Items[0], FromEtsInput(next));
    }
    );

    private static Dictionary<string, Term>? Options(Term term, params string[] names)
    {
        var options = new Dictionary<string, Term>(StringComparer.Ordinal);
        try
        {
            foreach (Term option in term is Cons or Nil ? Cons.Items(term) : new[] { term })
            {
                if (option is not TupleTerm { Items.Count: 2 } pair || pair.Items[0] is not Atom atom
                    || !names.Contains(atom.Name, StringComparer.Ordinal) || !options.TryAdd(atom.Name, pair.Items[1]))
                {
                    return null;
                }
            }
        }
        catch (ErlangException)
        {
            return null;
        }

        return options;
    }

    private static async ValueTask<Term> InitTable(ProcessContext context, IReadOnlyList<Term> arguments)
    {
        Term optionsTerm = arguments.Count == 2 ? Nil.Value : arguments[2];
        var settings = Options(optionsTerm, DetsQueryNames.Format, MinimumSlots);
        if (arguments[1] is not FunctionTerm)
        {
            throw DetsSource.BadArgument(
                context,
                DetsFunctions.InitTable,
                arguments,
                DetsSourceLines.InvalidInitializeFunction
            );
        }
        if (settings is null)
        {
            throw DetsSource.BadArgument(
                context,
                DetsFunctions.InitTable,
                arguments,
                DetsSourceLines.InvalidInitializeOptions
            );
        }
        Term format = settings.GetValueOrDefault(DetsQueryNames.Format, Term.A(DetsProtocolNames.Term));
        Term minimum = settings.GetValueOrDefault(MinimumSlots, Term.A(Default));
        if (format is not Atom { Name: DetsProtocolNames.Term or Bchunk } || minimum is not Atom { Name: Default }
            && (minimum is not Integer min || min.Value < 0))
        {
            throw DetsSource.BadArgument(
                context,
                DetsFunctions.InitTable,
                arguments,
                DetsSourceLines.InvalidInitializeOptions
            );
        }
        if (minimum is Integer number && number.Value < DetsV9Layout.MinimumSlots)
        {
            minimum = Term.I(DetsV9Layout.MinimumSlots);
        }
        Term result = await TableRequest(context, arguments[0], Term.Tuple(
            Term.A(DetsRequestNames.Initialize),
            arguments[1],
            format,
            minimum
        ));
        if (result is TupleTerm { Items.Count: 2 } thrown && thrown.Items[0] is Atom { Name: DetsProtocolNames.Thrown })
        {
            throw new ErlangException(thrown.Items[1], ErlangExceptionClasses.Throw);
        }
        if (result.Equals(Badarg))
        {
            throw DetsSource.BadArgument(context, DetsFunctions.InitTable, arguments);
        }

        return result;
    }

    private static async ValueTask<Term> QueryTable(ProcessContext context, Term table, Term optionsTerm)
    {
        var settings = Options(optionsTerm, DetsFunctions.Traverse, DetsQueryNames.NObjects);
        if (settings is null)
        {
            throw DetsSource.BadArgument(context, DetsFunctions.Table, [table, optionsTerm]);
        }
        Term traverse = settings.GetValueOrDefault(DetsFunctions.Traverse, Term.A(Select));
        Term number = settings.GetValueOrDefault(DetsQueryNames.NObjects, Term.A(Default));
        if (number is not Atom { Name: Default } && (number is not Integer count || count.Value <= 0))
        {
            throw DetsSource.BadArgument(context, DetsFunctions.Table, [table, optionsTerm]);
        }
        FunctionTerm traversal;
        bool lookup = true;
        if (traverse is Atom { Name: DetsQueryNames.FirstNext })
        {
            traversal = new FunctionTerm(
                0,
                async (execution, _) =>
            {
                var caller = (ProcessContext)execution;

                return await QueryNext(caller, table, await Invoke(caller, DetsFunctions.First, [table]));
            }
            );
        }
        else if (traverse is Atom { Name: Select })
        {
            traversal = new FunctionTerm(
                1,
                async (execution, args) =>
                await QuerySelect((ProcessContext)execution, await Match((ProcessContext)execution, Select, [table, args[0], number]))
            );
        }
        else if (traverse is TupleTerm { Items.Count: 2 } select && select.Items[0] is Atom { Name: Select })
        {
            lookup = false;
            traversal = new FunctionTerm(
                0,
                async (execution, _) =>
                await QuerySelect((ProcessContext)execution, await Match((ProcessContext)execution, Select, [table, select.Items[1], number]))
            );
        }
        else
        {
            throw DetsSource.BadArgument(context, DetsFunctions.Table, [table, optionsTerm]);
        }
        var before = new FunctionTerm(1, (execution, _) => Invoke((ProcessContext)execution, DetsFunctions.SafeFixtable, [table, Bool(true)]));
        var after = new FunctionTerm(0, (execution, _) => Invoke((ProcessContext)execution, DetsFunctions.SafeFixtable, [table, Bool(false)]));
        var info = new FunctionTerm(
            1,
            async (execution, args) =>
        {
            string? tag = (args[0] as Atom)?.Name;

            return tag switch
            {
                DetsQueryNames.NumOfObjects => await Invoke((ProcessContext)execution, DetsFunctions.Info, [table, Term.A(DetsInfoNames.Size)]),
                DetsInfoNames.Keypos => await Invoke((ProcessContext)execution, DetsFunctions.Info, [table, Term.A(DetsInfoNames.Keypos)]),
                DetsQueryNames.IsUniqueObjects => Bool(await Invoke((ProcessContext)execution, DetsFunctions.Info, [table, Term.A(DetsInfoNames.Type)]) is not Atom { Name: DuplicateBag }),
                _ => Undefined
            };
        }
        );
        Term lookupFun = lookup ? new FunctionTerm(2, (execution, args) => Invoke((ProcessContext)execution, DetsFunctions.LookupKeys, [table, args[1]])) : Undefined;
        var format = new FunctionTerm(
            1,
            async (execution, args) =>
        {
            var caller = (ProcessContext)execution;
            var specification = (TupleTerm)args[0];
            if (specification.Items[0] is Atom { Name: DetsQueryNames.All })
            {
                return Term.Tuple(Term.A(Name), Term.A(DetsRequestNames.Table), optionsTerm is Nil ? Term.List(table) : Term.List(table, optionsTerm));
            }
            if (specification.Items[0] is Atom { Name: DetsQueryNames.MatchSpecification })
            {
                Term options = new Cons(
                    Term.Tuple(Term.A(DetsRequestNames.Traverse), Term.Tuple(Term.A(Select), specification.Items[1])),
                    optionsTerm is Cons or Nil ? optionsTerm : Term.List(optionsTerm)
                );

                return Term.Tuple(Term.A(Name), Term.A(DetsRequestNames.Table), Term.List(table, options));
            }
            var element = (FunctionTerm)specification.Items[4];
            Term[] keys = Cons.Items(specification.Items[2]).ToArray();
            if (keys.Length == 1)
            {
                Term key = await element.Invoke(caller, [keys[0]]);

                return await Call(
                    caller,
                    DetsDependencyNames.IoLibrary,
                    DetsExternalFunctions.Format,
                    Term.String(DetsText.QueryLookup),
                    Term.List(Term.A(Name), table, key)
                );
            }
            var formatted = new List<Term>();
            foreach (Term key in keys)
            {
                formatted.Add(await element.Invoke(caller, [key]));
            }

            return await Call(
                caller,
                DetsDependencyNames.IoLibrary,
                DetsExternalFunctions.Format,
                Term.String(DetsText.QueryLookupKeys),
                Term.List(Term.A(Name), table, Cons.From(formatted))
            );
        }
        );

        return await Call(
            context,
            DetsDependencyNames.Query,
            DetsExternalFunctions.Table,
            traversal,
            Term.List(
                Term.Tuple(Term.A(DetsQueryNames.PreFun), before),
                Term.Tuple(Term.A(DetsQueryNames.PostFun), after),
                Term.Tuple(Term.A(DetsQueryNames.InfoFun), info),
                Term.Tuple(Term.A(DetsQueryNames.FormatFun), format),
                Term.Tuple(Term.A(DetsQueryNames.KeyEquality), Term.A(DetsMatchNames.ExactEquality)),
                Term.Tuple(Term.A(DetsQueryNames.LookupFun), lookupFun)
            )
        );
    }

    private static async ValueTask<Term> QueryNext(ProcessContext context, Term table, Term key)
    {
        if (key.Equals(End))
        {
            return Nil.Value;
        }
        Term values = await Invoke(context, DetsFunctions.Lookup, [table, key]);
        var tail = new FunctionTerm(
            0,
            async (execution, _) =>
        {
            var caller = (ProcessContext)execution;

            return await QueryNext(caller, table, await Invoke(caller, DetsFunctions.Next, [table, key]));
        }
        );

        return Cons.From(Cons.Items(values), tail);
    }

    private static async ValueTask<Term> QuerySelect(ProcessContext context, Term chunk)
    {
        if (chunk.Equals(End))
        {
            return Nil.Value;
        }
        if (chunk is not TupleTerm { Items.Count: 2 } batch)
        {
            return chunk;
        }
        var tail = new FunctionTerm(
            0,
            async (execution, _) =>
            await QuerySelect((ProcessContext)execution, await Match((ProcessContext)execution, Select, [batch.Items[1]]))
        );
        await ValueTask.CompletedTask;

        return Cons.From(Cons.Items(batch.Items[0]), tail);
    }
}
