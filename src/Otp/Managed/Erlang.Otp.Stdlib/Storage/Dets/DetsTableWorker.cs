// Derived from OTP-29.1.1 dets.erl server loop and file lifecycle, Apache-2.0.
// Copyright Ericsson AB 1996-2026. Upstream sources/notices remain unchanged.
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using static Erlang.DetsModule;

namespace Erlang;

internal sealed partial class DetsTableWorker
{
    private static readonly ConditionalWeakTable<ProcessRuntime, ConcurrentDictionary<Pid, DetsTableWorker>> workers = new();
    private readonly Term parent;
    private readonly Term server;
    private ProcessRuntime runtime = null!;
    private ProcessContext owner = null!;
    private Stream? stream;
    private DetsV9File file = null!;
    private DetsOpenOptions options = null!;
    private Term table = Undefined;
    private string filename = string.Empty;
    private readonly Dictionary<Pid, int> fixedBy = new();
    private Term fixedTime = Undefined;
    private Term fixedTimestamp = Undefined;
    private readonly List<(string Operation, Term Values)> pending = new();
    private long pendingBytes;
    private Term? cacheTimer;
    private int updates;
    private bool closing;
    private bool growthQueued;
    private Term? failure;

    private DetsTableWorker(Term parent, Term server)
    {
        this.parent = parent;
        this.server = server;
    }

    internal static DetsTableWorker Current(ProcessContext context)
        => workers.GetOrCreateValue(context.Runtime).TryGetValue(context.Self, out var worker)
            ? worker : throw new ErlangException(ErlangErrorReasons.BadArgument);

    internal static async ValueTask<Term> Initialize(ProcessContext context, Term parent, Term server)
    {
        var worker = new DetsTableWorker(parent, server);
        worker.runtime = context.Runtime;
        worker.owner = context;
        workers.GetOrCreateValue(context.Runtime)[context.Self] = worker;
        context.TrapExits = true;
        try
        {
            var message = await context.ReceiveAsync<TupleTerm>(term => term is TupleTerm { Items.Count: 3 } call
                && call.Items[0] is Atom { Name: DetsNames.Call } && call.Items[2] is TupleTerm { Items.Count: 3 } operation
                && operation.Items[0] is Atom { Name: DetsRequestNames.InternalOpen } ? call : null);
            var op = (TupleTerm)message!.Items[2];
            Term response;
            try
            {
                Term[] args = Cons.Items(op.Items[2]).ToArray();
                worker.table = args.Length == 2 ? op.Items[1] : args[0];
                worker.options = args.Length == 2 ? DetsOpenOptions.Parse(args[0], Nil.Value)
                    : DetsOpenOptions.FromRecord(args[0], args[1]);
                worker.OpenStorage(context, requireExisting: args.Length == 2);
                response = Ok;
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException or ErlangException or ArgumentException)
            {
                response = worker.FileFailure(error);
            }
            Reply(context, message.Items[1], response);
            if (!response.Equals(Ok))
            {
                return Ok;
            }
            if (worker.file.Dirty && worker.options.AutoSave is not Atom { Name: DetsNames.Infinity })
            {
                await Schedule(context, worker.options.AutoSave, Term.A(DetsRequestNames.AutoSave));
            }

            return await worker.Run(context);
        }
        finally
        {
            workers.GetOrCreateValue(context.Runtime).TryRemove(context.Self, out _);
            worker.stream?.Dispose();
        }
    }

    internal async ValueTask<Term> Run(ProcessContext context)
    {
        while (!closing)
        {
            if (!growthQueued && file.NeedsGrowth)
            {
                growthQueued = true;
                context.Runtime.Send(context.Self, Term.Tuple(Term.A(DetsNames.Call), context.Self, Term.A(DetsRequestNames.MayGrow)));
            }
            Term? message = await context.ReceiveAsync<Term>(value => value);
            if (message is TupleTerm { Items.Count: 3 } call && call.Items[0] is Atom { Name: DetsNames.Call })
            {
                Term response;
                bool wasDirty = file.Dirty;
                try
                {
                    response = await Operation(context, call.Items[1], call.Items[2]);
                }
                catch (ErlangException error) when (error.Reason.Equals(Badarg))
                {
                    response = Badarg;
                }
                catch (Exception error) when (error is IOException or UnauthorizedAccessException or ArgumentException or OverflowException)
                {
                    response = FileFailure(error);
                    failure = response;
                }
                Reply(context, call.Items[1], response);
                if (!closing && !wasDirty && file.Dirty && options.AutoSave is not Atom { Name: DetsNames.Infinity })
                {
                    await Schedule(context, options.AutoSave, Term.A(DetsRequestNames.AutoSave));
                }
                // A completed request is no longer an Erlang heap root. In a
                // suspended C# async loop its debug locals otherwise retain
                // compiled match programs beyond an explicit garbage_collect.
                response = Nil.Value;
                call = null!;
                message = null;
            }
            else if (message is TupleTerm { Items.Count: 3 } exit && exit.Items[0] is Atom { Name: DetsProtocolNames.ExitSignal })
            {
                if (exit.Items[1].Equals(parent) || exit.Items[1].Equals(server))
                {
                    Close(false);
                    throw new ErlangException(exit.Items[2], ErlangExceptionClasses.Exit);
                }
                if (exit.Items[1] is Pid pid)
                {
                    Unfix(context, pid, all: true);
                }
            }
            else if (message is TupleTerm { Items.Count: 3 } system && system.Items[0] is Atom { Name: DetsProtocolNames.System })
            {
                return await Call(
                    context,
                    DetsDependencyNames.System,
                    DetsExternalFunctions.HandleSystemMsg,
                    system.Items[2],
                    system.Items[1],
                    parent,
                    Term.A(DetsNames.Module),
                    Nil.Value,
                    context.Self
                );
            }
            else
            {
                _ = await Call(
                    context,
                    DetsDependencyNames.Logger,
                    DetsExternalFunctions.Error,
                    Term.String(DetsText.UnexpectedMessage),
                    Term.List(message ?? Undefined)
                );
            }
        }

        return Ok;
    }

    private async ValueTask<Term> Operation(ProcessContext context, Term from, Term request)
    {
        Term[] args = request is TupleTerm tuple ? tuple.Items.Skip(1).ToArray() : Array.Empty<Term>();
        string operation = request is Atom atom ? atom.Name : request is TupleTerm { Items.Count: > 0 } shape
            && shape.Items[0] is Atom name ? name.Name : string.Empty;
        Pid? caller = from is TupleTerm { Items.Count: 2 } client ? client.Items[0] as Pid : from as Pid;
        if (operation == DetsRequestNames.Close)
        {
            if (args.Length == 0)
            {
                Close(false);

                return failure ?? Ok;
            }
            if (args[0] is Pid pid)
            {
                Unfix(context, pid, all: true);
            }

            return failure ?? Ok;
        }
        if (operation == DetsRequestNames.AddUser)
        {
            var incoming = DetsOpenOptions.FromRecord(args[0], args[1]);

            return args[0].Equals(table) && incoming.File.Equals(options.File) && incoming.Type == options.Type
                && incoming.KeyPosition == options.KeyPosition && incoming.RamFile == options.RamFile && incoming.ReadOnly == options.ReadOnly
                ? Ok : Error(Term.A(DetsFailureNames.IncompatibleArguments));
        }
        if (operation == DetsRequestNames.SetVerbose)
        {
            return Ok;
        }
        if (operation == DetsRequestNames.MayGrow)
        {
            growthQueued = false;
            if (failure is null)
            {
                file.MayGrow();
            }

            return Ok;
        }
        if (operation == DetsRequestNames.AutoSave)
        {
            if (file.Dirty && failure is null)
            {
                if (updates == 0)
                {
                    Save(false);
                }
                else
                {
                    updates = 0;
                    await Schedule(context, options.AutoSave, Term.A(DetsRequestNames.AutoSave));
                }
            }

            return Ok;
        }
        if (operation == DetsRequestNames.DelayedWrite)
        {
            if (args.Length != 0 && args[0].Equals(cacheTimer))
            {
                Flush();
                cacheTimer = null;
            }

            return Ok;
        }
        if (operation == DetsRequestNames.Info)
        {
            bool flush = args.Length == 0 || args[0] is Atom { Name: DetsInfoNames.Size or DetsInfoNames.NoObjects or DetsInfoNames.NoKeys or DetsInfoNames.FileSize or DetsInfoNames.Memory or DetsInfoNames.BchunkFormat };
            if (flush)
            {
                if (failure is not null)
                {
                    return failure;
                }
                Flush();
            }

            return await Info(context, args.Length == 0 ? null : args[0]);
        }
        if (operation == DetsRequestNames.IsCompatibleBchunkFormat)
        {
            return Bool(Compatible(args[0]));
        }
        if (failure is not null)
        {
            return failure;
        }
        if (file.ReadOnly && operation is DetsRequestNames.InsertNew or DetsRequestNames.UpdateCounter)
        {
            return Error(Term.Tuple(Term.A(DetsNames.AccessMode), options.File));
        }
        if (operation is DetsRequestNames.Insert or DetsRequestNames.DeleteKey or DetsRequestNames.DeleteObject)
        {
            if (file.ReadOnly)
            {
                return Error(Term.Tuple(Term.A(DetsNames.AccessMode), options.File));
            }
            var values = Cons.Items(args[0]).ToArray();
            if (operation != DetsRequestNames.DeleteKey)
            {
                foreach (Term value in values)
                {
                    _ = file.Key(value);
                }
            }
            file.Touch();
            pending.Add((operation, args[0]));
            pendingBytes += values.Sum(value => ExternalTermFormat.EncodedSize(value, new EtfEncodingOptions()));
            updates++;
            var cache = (TupleTerm)options.DelayedWrite;
            if (pendingBytes >= ((Integer)cache.Items[1]).Value || ((Integer)cache.Items[0]).Value == 0)
            {
                Flush();
            }
            else if (cacheTimer is null)
            {
                cacheTimer = context.Runtime.MakeReference();
                await Schedule(context, cache.Items[0], Term.Tuple(Term.A(DetsRequestNames.DelayedWrite), cacheTimer));
            }

            return Ok;
        }
        Flush();
        switch (operation)
        {
            case DetsRequestNames.LookupKeys:
                if (!file.HasCollectionCounts && Cons.Items(args[0]).Take(2).Count() == 1)
                {
                    failure = Error(Term.Tuple(Term.Tuple(Term.A(DetsFileReasons.BadObject), Term.A(DetsProtocolNames.EvaluateWorkList)), options.File));

                    return failure;
                }

                return Cons.From(Cons.Items(args[0]).Distinct().SelectMany(file.Lookup));
            case DetsRequestNames.Member: return Bool(file.Lookup(args[0]).Count != 0);
            case DetsRequestNames.InsertNew:
                updates++;

                return Bool(file.Insert(Cons.Items(args[0]).ToArray(), onlyNew: true));
            case DetsRequestNames.UpdateCounter:
                updates++;

                return file.UpdateCounter(args[0], args[1]);
            case DetsRequestNames.Sync:
                Save(true);

                return Ok;
            case DetsRequestNames.DeleteAllObjects:
                if (file.Fixed)
                {
                    return Term.A(DetsProtocolNames.FixedTableReply);
                }
                if (file.ReadOnly)
                {
                    failure = Error(Term.Tuple(Term.Tuple(Term.A(DetsFailureNames.FileError), options.File, Error(Term.A(DetsFailureNames.AccessDenied))), options.File));

                    return failure;
                }
                Reset();
                updates++;

                return Ok;
            case DetsRequestNames.First: return Term.Tuple(Ok, First());
            case DetsRequestNames.Next: return Next(args[0]);
            case DetsRequestNames.Slot:
                if (args[0] is not Integer slot || slot.Value < 0)
                {
                    return Badarg;
                }

                return slot.Value >= file.Next ? End : Cons.From(file.ReadSlot((uint)slot.Value));
            case DetsRequestNames.Where:
                Term key = file.Key(args[0]);
                if (!file.Lookup(key).Contains(args[0]))
                {
                    return Bool(false);
                }

                return Term.Tuple(Ok, Term.I(file.Chunks().Single(chunk => chunk.Slot == file.Slot(key)).Address));
            case DetsRequestNames.SafeFixtable:
                if (args[0] is not Atom { Name: DetsNames.True or DetsNames.False } || caller is null)
                {
                    return Badarg;
                }
                if (args[0] is Atom { Name: DetsNames.True })
                {
                    await Fix(context, caller);
                }
                else
                {
                    Unfix(context, caller, all: false);
                }

                return Ok;
            case DetsRequestNames.Scan: return await Scan(context, caller!, args);
            case DetsRequestNames.SelectDelete:
                if (file.ReadOnly)
                {
                    return Error(Term.Tuple(Term.A(DetsNames.AccessMode), options.File));
                }
                var program = Compile(args[0]);
                long count = 0;
                foreach (Term value in file.Objects().ToArray())
                {
                    var match = program.Run(context, value, MatchSpecRunMode.Production);
                    if (match.Matched && match.Value is Atom { Name: DetsNames.True })
                    {
                        long before = file.ObjectCount;
                        file.Delete(file.Key(value), value);
                        count += before - file.ObjectCount;
                    }
                }
                updates++;

                return Term.I(count);
            case DetsRequestNames.BchunkInit: case DetsRequestNames.Bchunk: return Bchunk(context, operation, args[0]);
            case DetsRequestNames.Initialize:
                Term initialized = await InitializeTable(context, args);
                if (!initialized.Equals(Ok) && !initialized.Equals(Badarg)
                    && initialized is TupleTerm { Items.Count: 2 } problem
                    && !(problem.Items[1] is TupleTerm { Items.Count: 2 } reason
                        && reason.Items[0] is Atom { Name: DetsNames.AccessMode or DetsFailureNames.FixedTable }))
                {
                    failure = initialized;
                }

                return initialized;
            case DetsRequestNames.Corrupt:
                failure = Error(args[0]);

                return failure;
            default: return Badarg;
        }
    }

    private void Flush()
    {
        foreach (var operation in pending)
        {
            var values = Cons.Items(operation.Values).ToArray();
            if (operation.Operation == DetsRequestNames.Insert)
            {
                file.Insert(values, onlyNew: false);
            }
            else
            {
                foreach (Term value in values)
                {
                    file.Delete(
                        operation.Operation == DetsRequestNames.DeleteKey ? value : file.Key(value),
                        operation.Operation == DetsRequestNames.DeleteObject ? value : null
                    );
                }
            }
        }
        pending.Clear();
        pendingBytes = 0;
    }

    private void Save(bool durable)
    {
        Flush();
        file.Save(durable);
        if (options.RamFile && !options.ReadOnly)
        {
            File.WriteAllBytes(filename, ((MemoryStream)stream!).ToArray());
        }
    }

    internal void Close(bool durable)
    {
        if (closing)
        {
            return;
        }
        try
        {
            if (failure is null)
            {
                Save(durable);
            }
        }
        finally
        {
            closing = true;
            foreach (Pid fixer in fixedBy.Keys)
            {
                runtime.Unlink(owner, fixer);
            }
            fixedBy.Clear();
            stream?.Dispose();
        }
    }

    private static void Reply(ProcessContext context, Term from, Term value)
    {
        if (from is TupleTerm { Items.Count: 2 } pair && pair.Items[0] is Pid pid)
        {
            context.Runtime.Send(pid, Term.Tuple(pair.Items[1], value));
        }
    }

    private static async ValueTask Schedule(ProcessContext context, Term milliseconds, Term operation)
    {
        _ = await Call(
            context,
            DetsDependencyNames.Erlang,
            DetsExternalFunctions.SendAfter,
            milliseconds,
            context.Self,
            Term.Tuple(Term.A(DetsNames.Call), context.Self, operation)
        );
    }
}
