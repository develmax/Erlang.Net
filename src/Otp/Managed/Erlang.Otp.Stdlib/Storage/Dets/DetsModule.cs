// Complete module dispatch derived from OTP-29.1.1 dets.erl, Apache-2.0.
// Copyright Ericsson AB 1996-2026. Upstream source and notices are retained.
namespace Erlang;

public static partial class DetsModule
{
    public const string Name = DetsNames.Module;
    public static IReadOnlyList<(string Name, int Arity)> Exports { get; } = Array.AsReadOnly(new (string, int)[]
    {
        (DetsFunctions.All,0),
        (DetsFunctions.Bchunk,2),
        (DetsFunctions.Close,1),
        (DetsFunctions.Delete,2),
        (DetsFunctions.DeleteAllObjects,1),
        (DetsFunctions.DeleteObject,2),
        (DetsFunctions.First,1),
        (DetsFunctions.Foldl,3),
        (DetsFunctions.Foldr,3),
        (DetsFunctions.FromEts,2),
        (DetsFunctions.Info,1),
        (DetsFunctions.Info,2),
        (DetsFunctions.InitTable,2),
        (DetsFunctions.InitTable,3),
        (DetsFunctions.Insert,2),
        (DetsFunctions.InsertNew,2),
        (DetsFunctions.IsCompatibleBchunkFormat,2),
        (DetsFunctions.IsDetsFile,1),
        (DetsFunctions.Lookup,2),
        (DetsFunctions.Match,1),
        (DetsFunctions.Match,2),
        (DetsFunctions.Match,3),
        (DetsFunctions.MatchDelete,2),
        (DetsFunctions.MatchObject,1),
        (DetsFunctions.MatchObject,2),
        (DetsFunctions.MatchObject,3),
        (DetsFunctions.Member,2),
        (DetsFunctions.Next,2),
        (DetsFunctions.OpenFile,1),
        (DetsFunctions.OpenFile,2),
        (DetsFunctions.Pid2Name,1),
        (DetsFunctions.RepairContinuation,2),
        (DetsFunctions.SafeFixtable,2),
        (DetsFunctions.Select,1),
        (DetsFunctions.Select,2),
        (DetsFunctions.Select,3),
        (DetsFunctions.SelectDelete,2),
        (DetsFunctions.Slot,2),
        (DetsFunctions.Sync,1),
        (DetsFunctions.Table,1),
        (DetsFunctions.Table,2),
        (DetsFunctions.ToEts,2),
        (DetsFunctions.Traverse,2),
        (DetsFunctions.UpdateCounter,3),
        (DetsFunctions.Start,0),
        (DetsFunctions.Stop,0),
        (DetsFunctions.IstartLink,1),
        (DetsFunctions.Init,2),
        (DetsFunctions.InternalOpen,3),
        (DetsFunctions.AddUser,3),
        (DetsFunctions.InternalClose,1),
        (DetsFunctions.RemoveUser,2),
        (DetsFunctions.SystemContinue,3),
        (DetsFunctions.SystemTerminate,4),
        (DetsFunctions.SystemCodeChange,4),
        (DetsFunctions.FileInfo,1),
        (DetsFunctions.Fsck,1),
        (DetsFunctions.Fsck,2),
        (DetsFunctions.GetHeadField,2),
        (DetsFunctions.View,1),
        (DetsFunctions.Where,2),
        (DetsFunctions.Verbose,0),
        (DetsFunctions.Verbose,1),
        (DetsFunctions.LookupKeys,2)
    });

    public static void Register(ModuleRegistry registry) => registry.InstallPreloaded(target =>
    {
        foreach (var export in Exports)
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

    internal static Atom Ok => Term.A(DetsNames.Ok);
    internal static Atom Badarg => Term.A(ErlangErrorReasons.BadArgument);
    internal static Atom End => Term.A(DetsNames.EndOfTable);
    internal static Atom Undefined => Term.A(DetsNames.Undefined);

    internal static Term Bool(bool value) => Term.A(value ? DetsNames.True : DetsNames.False);

    internal static Term Error(Term reason) => Term.Tuple(Term.A(DetsNames.Error), reason);

    public static async ValueTask<Term> Invoke(ProcessContext context, string function, IReadOnlyList<Term> arguments)
    {
        if (!Exports.Contains((function, arguments.Count)))
        {
            throw new ErlangException(ErlangErrorReasons.UndefinedFunction);
        }
        using var tail = context.EnterSourceTailCall(Name, function);
        Term a = arguments.Count == 0 ? Nil.Value : arguments[0];
        Term b = arguments.Count < 2 ? Nil.Value : arguments[1];
        Term c = arguments.Count < 3 ? Nil.Value : arguments[2];
        Term result;
        switch (function)
        {
            case DetsFunctions.All:
            case DetsFunctions.Start:
            case DetsFunctions.Stop:
            case DetsFunctions.Pid2Name:
                return await Call(
                    context,
                    DetsDependencyNames.Server,
                    function,
                    arguments.ToArray()
                );
            case DetsFunctions.Close:
                result = await Call(
                    context,
                    DetsDependencyNames.Server,
                    DetsExternalFunctions.Close,
                    a
                );

                return result.Equals(Badarg) ? Error(Term.A(DetsFailureNames.NotOwner)) : result;
            case DetsFunctions.OpenFile:
                if (arguments.Count == 1)
                {
                    Term file = a is Atom atom ? Term.String(atom.Name) : a;
                    try
                    {
                        _ = Cons.Items(file).ToArray();
                    }
                    catch (ErlangException)
                    {
                        throw DetsSource.BadArgument(context, function, arguments);
                    }
                    result = await Call(
                        context,
                        DetsDependencyNames.Server,
                        function,
                        file
                    );
                }
                else
                {
                    // Erlang is_list/1 admits a cons even with an improper tail.
                    // Only a non-list argument enters the single-option clause.
                    if (b is not (Cons or Nil))
                    {
                        return await Invoke(context, function, [a, Term.List(b)]);
                    }
                    DetsOpenOptions options;
                    try
                    {
                        options = DetsOpenOptions.Parse(a, b);
                    }
                    catch (ErlangException)
                    {
                        throw DetsSource.BadArgument(context, function, arguments);
                    }
                    result = await Call(
                        context,
                        DetsDependencyNames.Server,
                        function,
                        a,
                        options.ToRecord()
                    );
                }
                if (result is TupleTerm { Items.Count: 2 } openError && openError.Items[1] is TupleTerm { Items.Count: 3 } fileError
                    && fileError.Items[0] is Atom { Name: DetsFailureNames.FileError }
                    && fileError.Items[2] is Atom { Name: DetsFailureNames.InvalidArgument or ErlangErrorReasons.BadArgument })
                {
                    throw DetsSource.BadArgument(
                        context,
                        function,
                        arguments,
                        DetsSourceLines.InvalidFileArgument
                    );
                }

                return result;
            case DetsFunctions.IstartLink:
                return Term.Tuple(
                    Ok,
                    await Call(
                        context,
                        DetsDependencyNames.ProcessLibrary,
                        DetsExternalFunctions.SpawnLink,
                        Term.A(Name),
                        Term.A(DetsRequestNames.Init),
                        Term.List(context.Self, a)
                    )
                );
            case DetsFunctions.Init:
                return await DetsTableWorker.Initialize(context, a, b);
            case DetsFunctions.SystemContinue:
                return await DetsTableWorker.Current(context).Run(context);
            case DetsFunctions.SystemTerminate:
                DetsTableWorker.Current(context).Close(false);
                throw new ErlangException(a, ErlangExceptionClasses.Exit);
            case DetsFunctions.SystemCodeChange:
                return Term.Tuple(Ok, a);
            case DetsFunctions.InternalOpen:
                return await Request(context, a, Term.Tuple(Term.A(function), b, c));
            case DetsFunctions.AddUser:
                return await Request(context, a, Term.Tuple(Term.A(function), b, c));
            case DetsFunctions.InternalClose:
                return await Request(context, a, Term.A(DetsRequestNames.Close));
            case DetsFunctions.RemoveUser:
                return await Request(context, a, Term.Tuple(Term.A(DetsRequestNames.Close), b));
            case DetsFunctions.Info:
                if (arguments.Count == 2 && b is Atom { Name: DetsInfoNames.Users })
                {
                    result = await Call(
                        context,
                        DetsDependencyNames.Server,
                        DetsExternalFunctions.Users,
                        a
                    );

                    return result is Nil ? Undefined : result;
                }
                Term pid = await GetPid(context, a);
                if (pid.Equals(Badarg))
                {
                    return Undefined;
                }
                if (arguments.Count == 2 && b is Atom { Name: DetsInfoNames.Owner })
                {
                    return pid;
                }
                result = await Request(context, pid, arguments.Count == 1 ? Term.A(DetsRequestNames.Info) : Term.Tuple(Term.A(DetsRequestNames.Info), b));

                return result.Equals(Badarg) ? Undefined : result;
            case DetsFunctions.Insert:
            case DetsFunctions.InsertNew:
                result = await TableRequest(context, a, Term.Tuple(Term.A(function), b is Cons or Nil ? b : Term.List(b)));
                break;
            case DetsFunctions.Delete:
                result = await TableRequest(context, a, Term.Tuple(Term.A(DetsRequestNames.DeleteKey), Term.List(b)));
                break;
            case DetsFunctions.DeleteObject:
                result = await TableRequest(context, a, Term.Tuple(Term.A(function), Term.List(b)));
                break;
            case DetsFunctions.Lookup:
                result = await TableRequest(context, a, Term.Tuple(Term.A(DetsRequestNames.LookupKeys), Term.List(b)));
                break;
            case DetsFunctions.LookupKeys:
                Term keys;
                try
                {
                    keys = await Call(
                        context,
                        DetsDependencyNames.Lists,
                        DetsExternalFunctions.Usort,
                        b
                    );
                }
                catch (ErlangException)
                {
                    throw DetsSource.BadArgument(
                        context,
                        function,
                        arguments,
                        DetsSourceLines.InvalidKeys
                    );
                }
                if (keys is Nil)
                {
                    throw DetsSource.BadArgument(
                        context,
                        function,
                        arguments,
                        DetsSourceLines.InvalidKeys
                    );
                }
                result = await TableRequest(context, a, Term.Tuple(Term.A(function), keys));
                break;
            case DetsFunctions.Member:
            case DetsFunctions.Slot:
            case DetsFunctions.SafeFixtable:
            case DetsFunctions.Where:
            case DetsFunctions.IsCompatibleBchunkFormat:
                result = await TableRequest(context, a, Term.Tuple(Term.A(function), b));
                break;
            case DetsFunctions.UpdateCounter:
                result = await TableRequest(context, a, Term.Tuple(Term.A(function), b, c));
                break;
            case DetsFunctions.DeleteAllObjects:
                result = await TableRequest(context, a, Term.A(function));
                if (result is Atom { Name: DetsProtocolNames.FixedTableReply })
                {
                    return await Invoke(context, DetsFunctions.MatchDelete, [a, Term.A(DetsMatchNames.Wildcard)]);
                }
                break;
            case DetsFunctions.Sync:
                result = await TableRequest(context, a, Term.A(function));
                break;
            case DetsFunctions.First:
            case DetsFunctions.Next:
                result = await TableRequest(context, a, function == DetsFunctions.First ? Term.A(DetsRequestNames.First) : Term.Tuple(Term.A(DetsRequestNames.Next), b));
                if (result is TupleTerm { Items.Count: 2 } first && first.Items[0].Equals(Ok))
                {
                    return first.Items[1];
                }
                if (!result.Equals(Badarg))
                {
                    throw new ErlangException(result, ErlangExceptionClasses.Exit);
                }
                break;
            case DetsFunctions.Match:
            case DetsFunctions.MatchObject:
            case DetsFunctions.Select:
                result = await Match(context, function, arguments);
                break;
            case DetsFunctions.MatchDelete:
            case DetsFunctions.SelectDelete:
                Term spec = function == DetsFunctions.MatchDelete ? MatchSpec(b, DetsNames.True) : b;
                result = await TableRequest(context, a, Term.Tuple(Term.A(DetsRequestNames.SelectDelete), spec));
                if (function == DetsFunctions.MatchDelete && result is Integer)
                {
                    return Ok;
                }
                break;
            case DetsFunctions.RepairContinuation:
                result = await RepairContinuation(context, a, b);
                break;
            case DetsFunctions.Foldr:
                return await Invoke(context, DetsFunctions.Foldl, arguments);
            case DetsFunctions.Foldl:
            case DetsFunctions.Traverse:
            case DetsFunctions.FromEts:
            case DetsFunctions.ToEts:
                return await Traverse(context, function, arguments);
            case DetsFunctions.InitTable:
                if (arguments.Count == 2)
                {
                    return await Invoke(context, function, [a, b, Nil.Value]);
                }

                return await InitTable(context, arguments);
            case DetsFunctions.Bchunk:
                if (b is not Atom { Name: DetsProtocolNames.StartOfBchunk }
                    && (b is not TupleTerm { Items.Count: DetsContinuationFields.Count } state || state.Items[0] is not Atom { Name: DetsNames.Continuation }
                        || state.Items[DetsContinuationFields.Kind] is not Atom { Name: DetsNames.Bchunk } || !state.Items[DetsContinuationFields.Table].Equals(a)))
                {
                    throw DetsSource.BadArgument(context, function, arguments);
                }
                result = await TableRequest(
                    context,
                    a,
                    Term.Tuple(
                        Term.A(b is Atom { Name: DetsProtocolNames.StartOfBchunk } ? DetsRequestNames.BchunkInit : DetsFunctions.Bchunk),
                        b is Atom { Name: DetsProtocolNames.StartOfBchunk } ? a : b
                    )
                );
                break;
            case DetsFunctions.Table:
                return await QueryTable(context, a, arguments.Count == 1 ? Nil.Value : b);
            case DetsFunctions.IsDetsFile:
            case DetsFunctions.FileInfo:
            case DetsFunctions.Fsck:
            case DetsFunctions.View:
                return await FileOperation(context, function, a);
            case DetsFunctions.GetHeadField:
                result = await Call(
                    context,
                    DetsDependencyNames.File,
                    DetsExternalFunctions.Pread,
                    a,
                    b,
                    Term.I(DetsV9Layout.WordBytes)
                );
                if (result is TupleTerm { Items.Count: 2 } read && read.Items[0].Equals(Ok) && read.Items[1] is BitString { IsBinary: true } bytes
                    && bytes.ByteSpan.Length == DetsV9Layout.WordBytes)
                {
                    return Term.I(System.Buffers.Binary.BinaryPrimitives.ReadUInt32BigEndian(bytes.ByteSpan));
                }
                throw new ErlangException(Term.Tuple(Term.A(ErlangErrorReasons.BadMatch), result));
            case DetsFunctions.Verbose:
                Term verbose = arguments.Count == 0 ? Bool(true) : a;
                _ = await Call(
                    context,
                    DetsDependencyNames.Server,
                    DetsExternalFunctions.Verbose,
                    verbose
                );
                Term all = await Call(context, DetsDependencyNames.Server, DetsExternalFunctions.All);
                foreach (Term table in Cons.Items(all))
                {
                    _ = await TableRequest(context, table, Term.Tuple(Term.A(DetsRequestNames.SetVerbose), verbose));
                }

                return all;
            default:
                throw new InvalidOperationException(function);
        }
        if (result.Equals(Badarg))
        {
            throw DetsSource.BadArgument(
                context,
                function,
                arguments,
                DetsSourceLines.InlineBadArgument
            );
        }

        return result;
    }

    internal static ValueTask<Term> Call(
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

    internal static async ValueTask<Term> GetPid(ProcessContext context, Term table)
    {
        try
        {
            return await Call(
                context,
                DetsDependencyNames.Server,
                DetsExternalFunctions.GetPid,
                table
            );
        }
        catch (ErlangException)
        {
            return Badarg;
        }
    }

    internal static async ValueTask<Term> TableRequest(ProcessContext context, Term table, Term operation)
    {
        Term pid = await GetPid(context, table);

        return pid is Pid ? await Request(context, pid, operation) : Badarg;
    }

    internal static async ValueTask<Term> Request(ProcessContext context, Term process, Term operation)
    {
        if (process is not Pid pid)
        {
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        }
        ReferenceTerm reference = context.Runtime.Monitor(context, pid);
        context.Runtime.Send(pid, Term.Tuple(Term.A(DetsNames.Call), Term.Tuple(context.Self, reference), operation));
        Term? reply = await context.ReceiveAsync<Term>(message => message switch
        {
            TupleTerm { Items.Count: 2 } tuple when tuple.Items[0].Equals(reference) => tuple.Items[1],
            TupleTerm { Items.Count: 5 } tuple when tuple.Items[0] is Atom { Name: DetsNames.Down }
                && tuple.Items[1].Equals(reference) => Badarg,
            _ => null
        });
        context.Runtime.Demonitor(context, reference, flush: true);

        return reply ?? Badarg;
    }

    internal static Term MatchSpec(Term pattern, string result)
        => Term.List(Term.Tuple(pattern, Nil.Value, Term.List(Term.A(result))));
}
