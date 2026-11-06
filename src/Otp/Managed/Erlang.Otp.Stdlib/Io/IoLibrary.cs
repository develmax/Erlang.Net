// Derived from Erlang/OTP 29.1.1 lib/stdlib/src/io.erl (Apache-2.0).
// Copyright Ericsson AB 1996-2025. See licenses and retained original source.
using static Erlang.IoLibraryNames;

namespace Erlang;

/// <summary>The explicitly selected C# implementation of the pinned io library.</summary>
public static class IoLibrary
{
    public const string Name = Module;

    public static IReadOnlyList<string> RequiredDependencies { get; } = Array.AsReadOnly(IoLibraryDependencies.Required);

    public static IReadOnlyList<(string Name, int Arity)> Exports { get; } = Array.AsReadOnly(
        new (string, int)[]
        {
            (Put, 1), (Put, 2), (Newline, 0), (Newline, 1),
            (Chars, 2), (Chars, 3), (Line, 1), (Line, 2),
            (Password, 0), (Password, 1), (SetOptions, 1), (SetOptions, 2),
            (GetOptions, 0), (GetOptions, 1), (Write, 1), (Write, 2),
            (Read, 1), (Read, 2), (Read, 3), (Read, 4),
            (Columns, 0), (Columns, 1), (Rows, 0), (Rows, 1),
            (Fwrite, 1), (Fwrite, 2), (Fwrite, 3), (Fread, 2), (Fread, 3),
            (Format, 1), (Format, 2), (Format, 3),
            (ScanExpressions, 1), (ScanExpressions, 2), (ScanExpressions, 3), (ScanExpressions, 4),
            (ScanForm, 1), (ScanForm, 2), (ScanForm, 3), (ScanForm, 4),
            (ParseExpressions, 1), (ParseExpressions, 2), (ParseExpressions, 3), (ParseExpressions, 4),
            (ParseForm, 1), (ParseForm, 2), (ParseForm, 3), (ParseForm, 4),
            (Request, 1), (Request, 2), (Requests, 1), (Requests, 2), (Printable, 0)
        });

    public static void Register(ModuleRegistry registry)
        => registry.InstallSourceWrappers(target =>
        {
            foreach (var export in Exports)
            {
                if (export.Name == Printable)
                {
                    continue;
                }
                string function = export.Name;
                target.RegisterReviewedManagedLibrary789(
                    Name,
                    function,
                    export.Arity,
                    (context, arguments) => Invoke(context, function, arguments)
                );
            }
        });

    public static async ValueTask<Term> Invoke(ProcessContext context, string function, IReadOnlyList<Term> args)
    {
        if (!Exports.Contains((function, args.Count)))
        {
            throw new ErlangException(ErlangErrorReasons.UndefinedFunction);
        }
        using var tail = context.EnterSourceTailCall(Name, function);
        Term leader = context.Runtime.GetGroupLeader(context);
        if (function == Printable)
        {
            return await Call(context, Name, Printable);
        }
        if (function is Format or Fwrite or Put or Write or Newline)
        {
            return await Output(
                context,
                function,
                args,
                leader
            );
        }
        if (function is Request or Requests)
        {
            return await IoRequest(
                context,
                args.Count == 1 ? leader : args[0],
                args[^1],
                Term.A(Error),
                function == Requests
            );
        }
        Term device = args.Count switch
        {
            0 => function == Password ? Term.A(User) : leader,
            1 => function is Columns or Rows or GetOptions or Password ? args[0] : leader,
            2 => function is Chars or Fread ? leader : args[0],
            _ => args[0]
        };
        Term request;
        switch (function)
        {
            case Columns:
            case Rows:
                Term geometry;
                using (var frame = context.EnterGeneratedFunction(
                    Name,
                    function,
                    1,
                    IoLibrarySource.Location(function == Columns ? IoLibrarySource.ColumnsRequest : IoLibrarySource.RowsRequest),
                    new object()
                ))
                {
                    geometry = await IoRequest(
                        context,
                        device,
                        T(Geometry, Term.A(function)),
                        Term.A(Error)
                    );
                }

                return geometry is Integer number && number.Value > 0
                    ? T(Ok, geometry) : T(Error, Term.A(Unsupported));
            case GetOptions:
                request = Term.A(GetOptions);
                break;
            case SetOptions:
                request = T(SetOptions, args[^1]);
                break;
            case Password:
                request = T(Password, Term.A(Unicode));
                break;
            case Line:
                request = T(Line, Term.A(Unicode), args[^1]);
                break;
            case Chars:
                if (args[^1] is not Integer count || count.Value < 0)
                {
                    throw IoLibrarySource.Clause(
                        context,
                        Chars,
                        Term.List(device, args[^2], args[^1]),
                        IoLibrarySource.GetCharsClause
                    );
                }
                request = T(
                    Chars,
                    Term.A(Unicode),
                    args[^2],
                    args[^1]
                );
                break;
            case Fread:
                request = T(Fread, args[^2], args[^1]);
                break;
            default:
                return await ReadOrParse(
                    context,
                    function,
                    args,
                    device
                );
        }

        return await IoRequest(
            context,
            device,
            request,
            Term.A(Error)
        );
    }

    private static async ValueTask<Term> ReadOrParse(
        ProcessContext context,
        string function,
        IReadOnlyList<Term> args,
        Term device
    )
    {
        Term prompt = args.Count == 1 ? args[0] : args[1];
        Term position = args.Count >= 3 ? args[2] : Term.I(1);
        Term options = args.Count == 4 ? args[3] : Nil.Value;
        bool shortRead = function == Read && args.Count <= 2;
        Term scannerArgs = shortRead ? Term.List(position) : Term.List(position, options);
        Term reply;
        using (var frame = function is ScanExpressions or ScanForm ? null : context.EnterGeneratedFunction(
            Name,
            function,
            shortRead ? 2 : 4,
            IoLibrarySource.Location(function == Read
                ? shortRead ? IoLibrarySource.ReadRequest : IoLibrarySource.ReadOptionsRequest
                : function == ParseExpressions ? IoLibrarySource.ExpressionsRequest : IoLibrarySource.FormRequest),
            new object()
        ))
        {
            reply = await IoRequest(
                context,
                device,
                T(
                    Until,
                    Term.A(Unicode),
                    prompt,
                    Term.A(Scanner),
                    Term.A(Tokens),
                    scannerArgs
                ),
                Term.A(Error)
            );
        }
        if (function is ScanExpressions or ScanForm)
        {
            return reply;
        }
        if (reply is TupleTerm { Items.Count: 3 } scanned && Is(scanned.Items[0], Ok))
        {
            string parser = function == Read ? ParseTerm : function == ParseExpressions ? ParserExpressions : ParserForm;
            Term parsed;
            using (var frame = shortRead ? null : context.EnterGeneratedFunction(
                Name,
                function,
                4,
                IoLibrarySource.Location(function == Read ? IoLibrarySource.ReadParser
                    : function == ParseExpressions ? IoLibrarySource.ExpressionsParser : IoLibrarySource.FormParser),
                new object()
            ))
            {
                parsed = await Call(
                    context,
                    Parser,
                    parser,
                    scanned.Items[1]
                );
            }
            if (shortRead)
            {
                return parsed;
            }
            if (parsed is TupleTerm { Items.Count: 2 } result && (Is(result.Items[0], Ok) || Is(result.Items[0], Error)))
            {
                return Term.Tuple(result.Items[0], result.Items[1], scanned.Items[2]);
            }

            throw new ErlangException(Term.Tuple(Term.A(ErlangErrorReasons.CaseClause), parsed));
        }
        if (shortRead && reply is TupleTerm failed)
        {
            if (failed.Items.Count == 3 && Is(failed.Items[0], Error))
            {
                return T(Error, failed.Items[1]);
            }
            if (failed.Items.Count == 2 && Is(failed.Items[0], Eof))
            {
                return Term.A(Eof);
            }
        }

        return reply;
    }

    private static async ValueTask<Term> Output(
        ProcessContext context,
        string function,
        IReadOnlyList<Term> args,
        Term leader
    )
    {
        Term device = leader;
        Term request;
        if (function is Format or Fwrite)
        {
            if (args.Count == 3)
            {
                device = args[0];
            }
            Term format = args[args.Count == 3 ? 1 : 0];
            Term data = args.Count == 1 ? Nil.Value : args[^1];
            request = T(Format, format, data);
        }
        else if (function == Newline)
        {
            device = args.Count == 0 ? leader : args[0];
            request = Term.A(Newline);
        }
        else
        {
            device = args.Count == 1 ? leader : args[0];
            request = function == Put ? T(Put, Term.A(Unicode), args[^1]) : T(Write, args[^1]);
        }
        Term errorTag = context.Runtime.MakeReference();
        Term reply;
        using (var frame = context.EnterGeneratedFunction(
            Name,
            function,
            args.Count,
            IoLibrarySource.Location(IoLibrarySource.OutputRequest),
            new object()
        ))
        {
            reply = await IoRequest(
                context,
                device,
                request,
                errorTag
            );
        }
        if (reply is TupleTerm { Items.Count: 2 } failure
            && (failure.Items[0].Equals(errorTag) || Is(failure.Items[0], Error)))
        {
            Term reason = failure.Items[1];
            string converted = reason switch
            {
                Atom { Name: Terminated } => Terminated,
                Atom { Name: CallingSelf } => CallingSelf,
                TupleTerm { Items.Count: 3 } translation when Is(translation.Items[0], NoTranslation) => NoTranslation,
                _ => ErlangErrorReasons.BadArgument
            };
            var info = new MapTerm(new Dictionary<Term, Term>
            {
                [Term.A(Cause)] = T(failure.Items[0].Equals(errorTag) ? Name : Device, reason),
                [Term.A(ErrorModule)] = Term.A(ErrorFormatter)
            });
            Term frame = Term.Tuple(
                Term.A(Name),
                Term.A(function),
                Cons.From(args),
                IoLibrarySource.Location(failure.Items[0].Equals(errorTag)
                    ? IoLibrarySource.LocalOutputError : IoLibrarySource.DeviceOutputError, T(ErrorInfo, info))
            );

            throw IoLibrarySource.Capture(context, Term.A(converted), frame);
        }

        return reply;
    }

    private static async ValueTask<Term> IoRequest(
        ProcessContext context,
        Term selected,
        Term request,
        Term errorTag,
        bool batch = false
    )
    {
        if (selected is Atom named && named.Name != Standard)
        {
            Term resolved = context.Runtime.WhereIs(named.Name);
            if (resolved is Atom)
            {
                return Term.Tuple(errorTag, Term.A(Arguments));
            }
            selected = resolved;
        }
        if (selected is not Atom && selected is not Pid)
        {
            throw IoLibrarySource.Clause(
                context,
                batch ? Requests : Request,
                batch ? Term.List(selected, request) : Term.List(selected, request, errorTag),
                batch ? IoLibrarySource.RequestsClause : IoLibrarySource.RequestClause
            );
        }
        Pid? device = selected switch
        {
            Atom { Name: Standard } => context.Runtime.GetGroupLeader(context),
            Pid pid => pid,
            Atom atom => context.Runtime.WhereIs(atom.Name) as Pid,
            _ => throw new ErlangException(ErlangErrorReasons.FunctionClause)
        };
        if (device is null)
        {
            return Term.Tuple(errorTag, Term.A(Arguments));
        }
        (bool convert, Term converted) = batch
            ? await Batch(context, device, request)
            : await ConvertRequest(context, device, request);
        if (device.Equals(context.Self))
        {
            return Term.Tuple(errorTag, Term.A(CallingSelf));
        }
        ReferenceTerm monitor = context.Runtime.Monitor(context, device);
        context.Runtime.SendTraced(context, device, T(
            WireRequest,
            context.Self,
            monitor,
            converted
        ));
        TupleTerm response = (await context.ReceiveAsync(term =>
            term is TupleTerm tuple &&
            (tuple.Items.Count == IoProtocolLayout.ReplyFields && Is(tuple.Items[0], WireReply) && tuple.Items[1].Equals(monitor)
            || tuple.Items.Count == IoProtocolLayout.MonitorDownFields && Is(tuple.Items[0], ProcessMessageTags.MonitorDown) && tuple.Items[1].Equals(monitor))
                ? tuple : null))!;
        if (response.Items.Count == IoProtocolLayout.MonitorDownFields)
        {
            await context.ReceiveAsync(
                term => term is TupleTerm { Items.Count: 3 } exit
                && Is(exit.Items[0], ProcessMessageTags.LinkedExit) && exit.Items[1].Equals(device) ? exit : null,
                TimeSpan.Zero
            );

            return Term.Tuple(errorTag, Term.A(Terminated));
        }
        context.Runtime.Demonitor(context, monitor, true);
        Term reply = response.Items[2];

        return convert && reply is BitString { IsBinary: true }
            ? await Call(
                context,
                Unicode,
                ConvertCharacters,
                reply,
                Term.A(Latin1),
                Term.A(Unicode)
            ) : reply;
    }

    private static async ValueTask<(bool, Term)> Batch(ProcessContext context, Pid device, Term requests)
    {
        var pending = new Stack<Term>();
        var converted = new List<Term>();
        Term current = requests;
        bool convert = false;
        bool recursive = false;
        while (true)
        {
            await context.ReduceAsync();
            if (current is Cons cell)
            {
                if (cell.Head is TupleTerm { Items.Count: 2 } nested && Is(nested.Items[0], Requests))
                {
                    pending.Push(cell.Tail);
                    current = nested.Items[1];
                    continue;
                }
                var next = await ConvertRequest(context, device, cell.Head);
                converted.Add(next.Item2);
                // The source uses conversion only for a final singleton with no continuation.
                convert = cell.Tail is Nil && pending.Count == 0 && next.Item1;
                recursive |= cell.Tail is not Nil || pending.Count != 0;
                current = cell.Tail;
                continue;
            }
            if (current is not Nil)
            {
                var frames = new List<Term>
                {
                    IoLibrarySource.Frame(IoLibrarySource.BatchFunction,
                        Term.List(
                            device,
                            current,
                            Cons.From(pending),
                            Nil.Value
                        ), IoLibrarySource.BatchClause)
                };
                if (recursive)
                {
                    frames.Add(IoLibrarySource.Frame(IoLibrarySource.BatchFunction, Term.I(4), IoLibrarySource.BatchRecursion));
                }
                frames.Add(IoLibrarySource.Frame(Requests, Term.I(2), IoLibrarySource.RequestsCall));

                throw IoLibrarySource.Capture(context, Term.A(ErlangErrorReasons.FunctionClause), frames.ToArray());
            }
            if (pending.Count == 0)
            {
                break;
            }
            current = pending.Pop();
        }

        return (convert, T(Requests, Cons.From(converted)));
    }

    private static async ValueTask<(bool, Term)> ConvertRequest(ProcessContext context, Pid device, Term request)
    {
        if (Is(request, Newline))
        {
            Term newline = await Call(context, Library, Newline);

            return await Compatible(
                context,
                device,
                T(Put, Term.A(Unicode), newline),
                false
            );
        }
        if (request is not TupleTerm tuple || tuple.Items.Count == 0 || tuple.Items[0] is not Atom operation)
        {
            return (false, request);
        }
        var items = tuple.Items;
        if (operation.Name == Write && items.Count == 2)
        {
            return await Compatible(
                context,
                device,
                T(
                    Put,
                    Term.A(Unicode),
                    Term.A(Library),
                    Term.A(Write),
                    Term.List(items[1])
                ),
                false
            );
        }
        if (operation.Name is Format or Fwrite && items.Count == 3)
        {
            return await Compatible(
                context,
                device,
                T(
                    Put,
                    Term.A(Unicode),
                    Term.A(Library),
                    operation,
                    Term.List(items[1], items[2])
                ),
                false
            );
        }
        if (operation.Name == Put && items.Count == 3 && IsList(items[2]))
        {
            if (device.Node == context.Self.Node)
            {
                Term chars = await TryCharacters(
                    context,
                    items[2],
                    items[1],
                    null
                );

                return (false, chars is BitString { IsBinary: true } ? T(Put, items[1], chars) : request);
            }
            bool modern = await Modern(context, device);
            Term encoded = await TryCharacters(
                context,
                items[2],
                items[1],
                Term.A(modern ? Unicode : Latin1)
            );

            return (false, modern
                ? encoded is BitString { IsBinary: true } ? T(Put, Term.A(Unicode), encoded) : request
                : T(Put, encoded is BitString { IsBinary: true } ? encoded : items[2]));
        }
        if (operation.Name == Fread && items.Count == 3)
        {
            return await Compatible(
                context,
                device,
                T(
                    Until,
                    Term.A(Unicode),
                    items[1],
                    Term.A(Library),
                    Term.A(Fread),
                    Term.List(items[2])
                ),
                true
            );
        }
        if (operation.Name == Until && items.Count == 6
            || operation.Name == Chars && items.Count == 4
            || operation.Name == Line && items.Count == 3
            || operation.Name == Password && items.Count == 2)
        {
            return await Compatible(
                context,
                device,
                tuple,
                true
            );
        }

        return (false, request);
    }

    private static async ValueTask<Term> TryCharacters(
        ProcessContext context,
        Term data,
        Term encoding,
        Term? target
    )
    {
        try
        {
            return target is null
                ? await Call(
                    context,
                    Unicode,
                    ConvertCharacters,
                    data,
                    encoding
                )
                : await Call(
                    context,
                    Unicode,
                    ConvertCharacters,
                    data,
                    encoding,
                    target
                );
        }
        catch (ErlangException)
        {
            return data;
        }
    }

    private static async ValueTask<(bool, Term)> Compatible(
        ProcessContext context,
        Pid device,
        TupleTerm request,
        bool convert
    )
    {
        if (await Modern(context, device))
        {
            return (false, request);
        }
        var items = request.Items;

        return (convert, items.Count == 2 ? items[0] : Term.Tuple(items.Take(1).Concat(items.Skip(2)).ToArray()));
    }

    private static async ValueTask<bool> Modern(ProcessContext context, Pid device)
        => Is(await Call(
            context,
            Network,
            UnicodeFlag,
            device
        ), ErlangBooleanAtoms.True);

    private static bool IsList(Term term)
        => term is Cons or Nil;

    private static bool Is(Term term, string atom) => term is Atom value && value.Name == atom;

    private static TupleTerm T(string atom, params Term[] args)
        => Term.Tuple(new Term[] { Term.A(atom) }.Concat(args).ToArray());

    private static ValueTask<Term> Call(
        ProcessContext context,
        string module,
        string function,
        params Term[] args
    )
        => context.Runtime.Modules.Call(
            context,
            module,
            function,
            args
        );
}
