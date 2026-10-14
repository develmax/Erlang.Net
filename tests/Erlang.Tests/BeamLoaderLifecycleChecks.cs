using System.Reflection;
using System.IO.Compression;

namespace Erlang.Tests;

public static class BeamLoaderLifecycleChecks
{
    public static async Task Run()
    {
        await Execute(Ordinary);
        await Execute(Types);
        await Execute(context => Load(context, false));
        await Execute(context => Load(context, true));
        await Execute(Batch);
        await Execute(Duplicates);
        await Execute(ThirdGeneration);
        await Execute(Padded);
        await Execute(FreshOnLoad);
        await Execute(ReplacementOnLoad);
        await Execute(DynamicTypes);
    }

    internal static Task RunCase(BeamLoaderMfaCase test)
        => test switch
        {
            BeamLoaderMfaCase.Prepare => Execute(Ordinary),
            BeamLoaderMfaCase.Query => Execute(Padded),
            BeamLoaderMfaCase.Finish => Execute(Duplicates),
            BeamLoaderMfaCase.Callback => Execute(DynamicTypes),
            BeamLoaderMfaCase.Finalize => Execute(FreshOnLoad),
            BeamLoaderMfaCase.Load => Execute(context => Load(context, true)),
            _ => throw new ArgumentOutOfRangeException(nameof(test))
        };

    internal static byte[] ConstantBytes() => Resource(BeamPreparedHostNames.Constant + BeamLoaderAdmissionNames.Extension);

    private static async Task Ordinary(ProcessContext context)
    {
        Term reference = await Prepare(context, BeamPreparedHostNames.Constant);
        byte[] encoded = ExternalTermFormat.Encode(reference);
        Term copied = ExternalTermFormat.DecodeOne(
            encoded,
            new EtfDecodeOptions { LocalNode = context.Runtime.Node, ReferenceFactory = context.Runtime.ReferenceResources.Rehydrate }
        ).Term;
        Term before = await Flag(context, reference);
        Term finished = await Finish(context, [reference]);
        Term body = await Call(context, BeamPreparedHostNames.Constant, BeamPreparedHostNames.Run);
        Term after = await Flag(context, copied);
        Term repeated = await Observe(() => Finish(context, [copied]));
        Match(
            Term.Tuple(
                before,
                finished,
                body,
                after,
                repeated
            ),
            Term.Tuple(
                False(),
                Ok(),
                Constant(),
                False(),
                Exception(ErlangErrorReasons.BadArgument)
            )
        );
    }

    private static async Task Types(ProcessContext context)
    {
        byte[] bytes = Resource(BeamPreparedHostNames.Constant + BeamLoaderAdmissionNames.Extension);
        Term a = await Reason(() => Erlang(
            context,
            BeamLoaderAdmissionNames.Prepare,
            Term.I(42),
            new BitString(bytes)
        ));
        Term b = await Reason(() => Erlang(
            context,
            BeamLoaderAdmissionNames.Prepare,
            Term.A(BeamPreparedHostNames.Constant),
            Term.A(BeamLoaderLifecycleNames.NonBinary)
        ));
        Term c = await Reason(() => Erlang(
            context,
            BeamLoaderAdmissionNames.Prepare,
            Term.A(BeamPreparedHostNames.Constant),
            new BitString([70, 79, 82, 49, 0], 33)
        ));
        Term d = await Erlang(
            context,
            BeamLoaderAdmissionNames.Prepare,
            Term.A(BeamPreparedHostNames.Constant),
            new BitString([])
        );
        Term e = await Erlang(
            context,
            BeamLoaderAdmissionNames.Prepare,
            Term.A(BeamLoaderAdmissionNames.ForeignModule),
            new BitString(bytes)
        );
        Match(
            Term.Tuple(
                a,
                b,
                c,
                d,
                e
            ),
            Term.Tuple(
                Atom(ErlangErrorReasons.BadArgument),
                Atom(ErlangErrorReasons.BadArgument),
                Atom(ErlangErrorReasons.BadArgument),
                Error(BeamLoaderAdmissionNames.BadFile),
                Error(BeamLoaderAdmissionNames.BadFile)
            )
        );
    }

    private static async Task Load(ProcessContext context, bool gzip)
    {
        byte[] bytes = Resource(BeamPreparedHostNames.Constant + BeamLoaderAdmissionNames.Extension);
        if (gzip)
        {
            using var output = new MemoryStream();
            using (var stream = new GZipStream(output, CompressionMode.Compress, true))
                stream.Write(bytes);
            bytes = output.ToArray();
        }
        Term loaded = await Erlang(
            context,
            BeamLoaderLifecycleNames.Load,
            Atom(BeamPreparedHostNames.Constant),
            new BitString(bytes)
        );
        Term body = await Call(context, BeamPreparedHostNames.Constant, BeamPreparedHostNames.Run);
        Match(
            Term.Tuple(loaded, body),
            Term.Tuple(Term.Tuple(Atom(BeamLoaderAdmissionNames.Module), Atom(BeamPreparedHostNames.Constant)), Constant())
        );
    }

    private static async Task Batch(ProcessContext context)
    {
        Term ordinary = await Prepare(context, BeamPreparedHostNames.Constant);
        Term callback = await PrepareCallback(context);
        Term empty = await Finish(context, []);
        Term improper = await Reason(() => Erlang(context, BeamLoaderAdmissionNames.Finish, new Cons(ordinary, Atom(BeamLoaderLifecycleNames.Wrong))));
        Term invalid = await Reason(() => Finish(context, [callback, context.Runtime.MakeReference()]));
        Term repeated = await Reason(() => Finish(context, [callback, callback]));
        Term mixed = await Reason(() => Finish(context, [ordinary, callback]));
        Match(
            Term.Tuple(
                empty,
                improper,
                invalid,
                repeated,
                mixed,
                await Flag(context, ordinary),
                await Flag(context, callback)
            ),
            Term.Tuple(
                Ok(),
                Atom(ErlangErrorReasons.BadArgument),
                Atom(ErlangErrorReasons.BadArgument),
                Atom(ErlangErrorReasons.SystemLimit),
                Atom(ErlangErrorReasons.SystemLimit),
                False(),
                True()
            )
        );
    }

    private static async Task Duplicates(ProcessContext context)
    {
        Term a1 = await Prepare(context, BeamPreparedHostNames.Constant);
        Term b1 = await Prepare(context, BeamPreparedHostNames.Add);
        Term a2 = await Prepare(context, BeamPreparedHostNames.Constant);
        Term b2 = await Prepare(context, BeamPreparedHostNames.Add);
        Term rejected = await Finish(context, [a1, b1, a2, b2]);
        Term visibleA = await Erlang(
            context,
            BeamLoaderLifecycleNames.Exported,
            Atom(BeamPreparedHostNames.Constant),
            Atom(BeamPreparedHostNames.Run),
            Term.I(0)
        );
        Term visibleB = await Erlang(
            context,
            BeamLoaderLifecycleNames.Exported,
            Atom(BeamPreparedHostNames.Add),
            Atom(BeamPreparedHostNames.Run),
            Term.I(0)
        );
        Term loaded = await Finish(context, [a1, b1]);
        Match(await Flag(context, a2), False());
        Match(await Flag(context, b2), False());
        Match(
            Term.Tuple(
                rejected,
                visibleA,
                visibleB,
                loaded,
                await Call(context, BeamPreparedHostNames.Constant, BeamPreparedHostNames.Run),
                await Call(context, BeamPreparedHostNames.Add, BeamPreparedHostNames.Run)
            ),
            Term.Tuple(
                Term.Tuple(
                    Atom(BeamLoaderLifecycleNames.Duplicated),
                    Cons.From([Atom(BeamPreparedHostNames.Add), Atom(BeamPreparedHostNames.Constant)])
                ),
                False(),
                False(),
                Ok(),
                Constant(),
                Term.I(BeamLoaderLifecycleNames.ConstantValue)
            )
        );
    }

    private static async Task ThirdGeneration(ProcessContext context)
    {
        Term a = await Prepare(context, BeamPreparedHostNames.Constant);
        Match(await Finish(context, [a]), Ok());
        Term b = await Prepare(context, BeamPreparedHostNames.Constant);
        Match(await Finish(context, [b]), Ok());
        Term c = await Prepare(context, BeamPreparedHostNames.Constant);
        Term blocked = await Finish(context, [c]);
        Term body = await Call(context, BeamPreparedHostNames.Constant, BeamPreparedHostNames.Run);
        Term f1 = await Flag(context, a);
        Term f2 = await Flag(context, b);
        Term f3 = await Flag(context, c);
        Match(
            Term.Tuple(
                blocked,
                body,
                f1,
                f2,
                f3
            ),
            Term.Tuple(
                Term.Tuple(Atom(BeamLoaderLifecycleNames.NotPurged), Cons.From([Atom(BeamPreparedHostNames.Constant)])),
                Constant(),
                False(),
                False(),
                False()
            )
        );
        Term purged = await Erlang(context, BeamLoaderLifecycleNames.Purge, Atom(BeamPreparedHostNames.Constant));
        Match(purged, True());
        Match(await Finish(context, [c]), Ok());

    }

    private static async Task Padded(ProcessContext context)
    {
        var original = (ReferenceTerm)await Prepare(context, BeamPreparedHostNames.Constant);
        ExternalTermFormat.Encode(original);

        ReferenceTerm Restore(uint suffix)
        {
            var padded = new ReferenceTerm(original.Node, original.Words.Concat([suffix]), original.Creation);

            return (ReferenceTerm)ExternalTermFormat.DecodeOne(
                ExternalTermFormat.Encode(padded),
                new EtfDecodeOptions { LocalNode = context.Runtime.Node, LocalCreation = original.Creation, ReferenceFactory = context.Runtime.ReferenceResources.Rehydrate }
            ).Term;
        }
        ReferenceTerm zero = Restore(0);
        ReferenceTerm one = Restore(1);
        Term beforeZero = await Flag(context, zero);
        Term beforeOne = await Flag(context, one);
        Term finish = await Finish(context, [original]);
        Match(
            Term.Tuple(
                zero.Equals(original) ? True() : False(),
                beforeZero,
                one.Equals(original) ? True() : False(),
                beforeOne,
                finish,
                await Flag(context, zero),
                await Flag(context, one),
                await Reason(() => Finish(context, [one]))
            ),
            Term.Tuple(
                True(),
                False(),
                True(),
                False(),
                Ok(),
                False(),
                False(),
                Atom(ErlangErrorReasons.BadArgument)
            )
        );
    }

    private static async Task FreshOnLoad(ProcessContext context)
    {
        context.Runtime.Register(BeamOnLoadHostNames.Observer, context.Self);
        string module = BeamOnLoadHostNames.FreshSuccessModule;
        Term reference = await PrepareCallback(context);
        Term before = await Flag(context, reference);
        Term staged = await Finish(context, [reference]);
        Term after = await Flag(context, reference);
        Term exported = await Erlang(
            context,
            BeamLoaderLifecycleNames.Exported,
            Atom(module),
            Atom(BeamOnLoadHostNames.Run),
            Term.I(0)
        );
        Term down = await Callback(context, module);
        Term kept = await Erlang(
            context,
            BeamLoaderAdmissionNames.FinishOnLoad,
            Atom(module),
            True()
        );
        Term body = await Call(context, module, BeamOnLoadHostNames.Run);
        Term consumedFlag = await Flag(context, reference);
        Term repeated = await Reason(() => Erlang(
            context,
            BeamLoaderAdmissionNames.FinishOnLoad,
            Atom(module),
            True()
        ));
        Match(
            Term.Tuple(
                before,
                staged,
                after,
                exported,
                down,
                kept,
                body,
                consumedFlag,
                repeated
            ),
            Term.Tuple(
                True(),
                Term.Tuple(Atom(BeamLoaderAdmissionNames.OnLoad), Cons.From([Atom(module)])),
                True(),
                False(),
                Ok(),
                True(),
                Atom(BeamOnLoadHostNames.NewImage),
                True(),
                Atom(ErlangErrorReasons.BadArgument)
            )
        );
    }

    private static async Task ReplacementOnLoad(ProcessContext context)
    {
        context.Runtime.Register(BeamOnLoadHostNames.Observer, context.Self);
        string module = BeamOnLoadHostNames.ReplacementFailureModule;
        Term old = await Erlang(
            context,
            BeamLoaderLifecycleNames.Load,
            Atom(module),
            new BitString(Resource(BeamLoaderDirectNames.ReplacementOldResource))
        );
        Term pending = await Erlang(
            context,
            BeamLoaderLifecycleNames.Load,
            Atom(module),
            new BitString(Resource(BeamLoaderDirectNames.ReplacementNewResource))
        );
        Term before = await Call(context, module, BeamOnLoadHostNames.Run);
        Term down = await Callback(context, module);
        Term dropped = await Erlang(
            context,
            BeamLoaderAdmissionNames.FinishOnLoad,
            Atom(module),
            False()
        );
        Term after = await Call(context, module, BeamOnLoadHostNames.Run);
        Term again = await Erlang(
            context,
            BeamLoaderAdmissionNames.FinishOnLoad,
            Atom(module),
            False()
        );
        Match(
            Term.Tuple(
                old,
                pending,
                before,
                down,
                dropped,
                after,
                again
            ),
            Term.Tuple(
                Term.Tuple(Atom(BeamLoaderAdmissionNames.Module), Atom(module)),
                Error(BeamLoaderAdmissionNames.OnLoad),
                Atom(BeamOnLoadHostNames.OldImage),
                Atom(BeamOnLoadHostNames.NotOk),
                True(),
                Atom(BeamOnLoadHostNames.OldImage),
                True()
            )
        );
    }

    private static async Task DynamicTypes(ProcessContext context)
    {
        string module = BeamOnLoadHostNames.FreshSuccessModule;
        Term reference = await PrepareCallback(context);
        await Finish(context, [reference]);
        Term dynamic = await Reason(() => Erlang(
            context,
            BeamLoaderLifecycleNames.Apply,
            Atom(ErlangModuleNames.Module),
            Atom(BeamLoaderDirectNames.Callback),
            Cons.From([Atom(module)])
        ));
        Term boolean = await Reason(() => Erlang(
            context,
            BeamLoaderAdmissionNames.FinishOnLoad,
            Atom(module),
            Atom(BeamLoaderLifecycleNames.Wrong)
        ));
        Term missing = await Reason(() => Erlang(
            context,
            BeamLoaderAdmissionNames.FinishOnLoad,
            Atom(BeamLoaderAdmissionNames.Missing),
            True()
        ));
        Match(
            Term.Tuple(dynamic, boolean, missing),
            Term.Tuple(Atom(ErlangErrorReasons.BadArgument), Atom(ErlangErrorReasons.BadArgument), Atom(ErlangErrorReasons.BadArgument))
        );
    }

    private static async Task<Term> Callback(ProcessContext context, string module)
    {
        ProcessHandle worker = context.Runtime.Spawn(async callback =>
        {
            Term returned = await BeamModule.Prepare(Resource(BeamLoaderDirectNames.NativeWrapperResource)).Invoke(callback, BeamLoaderDirectNames.Direct, Atom(module));
            callback.Exit(returned);

            return returned;
        });
        ReferenceTerm monitor = context.Runtime.Monitor(context, worker.Pid);
        TupleTerm? notice = (TupleTerm?)await context.ReceiveAsync(
            term => term is TupleTerm tuple && tuple.Items.Count == 5 && tuple.Items[0].Equals(Atom(BeamOnLoadHostNames.Notice)) ? term : null,
            TimeSpan.FromSeconds(BeamLoaderLifecycleNames.WatchdogSeconds)
        );
        if (notice is null || !notice.Items[1].Equals(Atom(module)) || !notice.Items[2].Equals(worker.Pid))
            throw new InvalidOperationException(BeamLoaderLifecycleNames.Failure);
        context.Runtime.Send(worker.Pid, Term.Tuple(Atom(BeamOnLoadHostNames.Continue), context.Self));

        TupleTerm? down = (TupleTerm?)await context.ReceiveAsync(
            term => term is TupleTerm tuple && tuple.Items.Count == 5 && tuple.Items[0].Equals(Atom(BeamLoaderLifecycleNames.Down)) &&
                tuple.Items[1].Equals(monitor) && tuple.Items[2].Equals(Atom(BeamLoaderLifecycleNames.Process)) && tuple.Items[3].Equals(worker.Pid) ? term : null,
            TimeSpan.FromSeconds(BeamLoaderLifecycleNames.WatchdogSeconds)
        );
        if (down is null)
            throw new InvalidOperationException(BeamLoaderLifecycleNames.Failure);
        Term completed = await worker.Completion.WaitAsync(TimeSpan.FromSeconds(BeamLoaderLifecycleNames.WatchdogSeconds));
        MethodInfo unwind = typeof(ProcessRuntime).GetMethod(BeamLoaderLifecycleNames.Unwind, BindingFlags.Instance | BindingFlags.NonPublic)!;
        await ((Task)unwind.Invoke(context.Runtime, [worker.Pid])!).WaitAsync(TimeSpan.FromSeconds(BeamLoaderLifecycleNames.WatchdogSeconds));
        Match(completed, down.Items[4]);

        return down.Items[4];
    }

    private static async Task Execute(Func<ProcessContext, Task> test)
    {
        await using var runtime = new ProcessRuntime();
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        ProcessHandle actor = runtime.Spawn(async context =>
        {
            try
            {
                await test(context);
                completion.TrySetResult();

                return Ok();
            }
            catch (Exception exception)
            {
                completion.TrySetException(exception);
                throw;
            }
        });
        await completion.Task.WaitAsync(TimeSpan.FromSeconds(BeamLoaderLifecycleNames.WatchdogSeconds));
        await actor.Completion;
    }

    private static ValueTask<Term> Prepare(ProcessContext context, string module)
        => Erlang(
            context,
            BeamLoaderAdmissionNames.Prepare,
            Atom(module),
            new BitString(Resource(module + BeamLoaderAdmissionNames.Extension))
        );

    private static ValueTask<Term> PrepareCallback(ProcessContext context)
        => Erlang(
            context,
            BeamLoaderAdmissionNames.Prepare,
            Atom(BeamOnLoadHostNames.FreshSuccessModule),
            new BitString(Resource(BeamLoaderDirectNames.FreshResource))
        );

    private static ValueTask<Term> Finish(ProcessContext context, Term[] references)
        => Erlang(context, BeamLoaderAdmissionNames.Finish, Cons.From(references));

    private static ValueTask<Term> Flag(ProcessContext context, Term reference)
        => Erlang(context, BeamLoaderAdmissionNames.Query, reference);

    private static ValueTask<Term> Erlang(ProcessContext context, string function, params Term[] arguments)
        => Call(
            context,
            ErlangModuleNames.Module,
            function,
            arguments
        );

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

    private static async ValueTask<Term> Observe(Func<ValueTask<Term>> operation)
    {
        try
        {
            return await operation();
        }
        catch (ErlangException exception)
        {
            return Term.Tuple(Atom(BeamLoaderLifecycleNames.Exception), Atom(exception.ExceptionClass), exception.Reason);
        }
    }

    private static async ValueTask<Term> Reason(Func<ValueTask<Term>> operation)
    {
        Term value = await Observe(operation);

        return value is TupleTerm tuple && tuple.Items.Count == 3 && tuple.Items[0].Equals(Atom(BeamLoaderLifecycleNames.Exception)) ? tuple.Items[2] : value;
    }

    private static Term Atom(string value) => Term.A(value);

    private static Term True() => Atom(ErlangBooleanAtoms.True);

    private static Term False() => Atom(ErlangBooleanAtoms.False);

    private static Term Ok() => Atom(BeamLoaderAdmissionNames.Ok);

    private static Term Error(string reason) => Term.Tuple(Atom(BeamLoaderAdmissionNames.Error), Atom(reason));

    private static Term Exception(string reason) => Term.Tuple(Atom(BeamLoaderLifecycleNames.Exception), Atom(ErlangExceptionClasses.Error), Atom(reason));

    private static Term Constant() => Term.Tuple(
        Ok(),
        Term.I(BeamLoaderLifecycleNames.ConstantValue),
        Cons.From([Atom(BeamLoaderLifecycleNames.Alpha), Atom(BeamLoaderLifecycleNames.Beta)])
    );

    private static byte[] Resource(string name)
    {
        Assembly assembly = typeof(BeamLoaderLifecycleChecks).Assembly;
        string resource = assembly.GetManifestResourceNames().Single(value => value.EndsWith(BeamLoaderAdmissionNames.Separator + name, StringComparison.Ordinal));
        using Stream stream = assembly.GetManifestResourceStream(resource)!;
        using var output = new MemoryStream();
        stream.CopyTo(output);

        return output.ToArray();
    }

    private static void Match(Term actual, Term expected)
    {
        if (!actual.Equals(expected))
            throw new InvalidOperationException(BeamLoaderLifecycleNames.Failure);
    }
}
