namespace Erlang.Tests;

internal static class DebugInternal420Checks
{
    internal static async Task Source()
    {
        await using var runtime = new ProcessRuntime();
        await DebugInternal420Access.Initialize(runtime);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            await runtime.Modules.Call(
                context,
                DebugInternal420Names.Runtime,
                DebugInternal420Names.Flag,
                Term.A(DebugInternal420Names.Logger),
                Term.A(DebugInternal420Names.Undefined)
            );
            await DebugInternal420Access.Error(context, Term.A(DebugInternal420Names.Reds), ErlangErrorReasons.UndefinedFunction);
            DebugInternal420Access.Require((await DebugInternal420Access.Enabled(context, true)).Equals(Term.A(ErlangBooleanAtoms.False)));
            DebugInternal420Access.Require((await DebugInternal420Access.Enabled(context, true)).Equals(Term.A(ErlangBooleanAtoms.True)));
            DebugInternal420Access.Require((await DebugInternal420Access.Get(context, Term.A(DebugInternal420Names.Available))).Equals(Term.A(ErlangBooleanAtoms.True)));
            await DebugInternal420Access.Error(context, Term.A(DebugInternal420Names.Unknown), ErlangErrorReasons.BadArgument);
            bool capability = false;
            try
            {
                await DebugInternal420Access.Get(context, Term.A(DebugInternal420Names.Memory));
            }
            catch (NotSupportedException)
            {
                capability = true;
            }
            DebugInternal420Access.Require(capability);
            DebugInternal420Access.Require((await DebugInternal420Access.Enabled(context, false)).Equals(Term.A(ErlangBooleanAtoms.True)));
            await DebugInternal420Access.Error(context, Nil.Value, ErlangErrorReasons.UndefinedFunction);

            return true;
        });
    }

    internal static async Task Owner()
    {
        var runtime = new ProcessRuntime();
        await using var foreign = new ProcessRuntime();
        await DebugInternal420Access.Initialize(runtime);
        await foreign.InitializeModulesAsync(_ => { });
        ErlangFunction body = DebugInternal420Access.Capture(runtime, DebugInternal420Names.Set, 2);
        ProcessContext? saved = null;
        try
        {
            await using (var actor = new RuntimeNodeFoundationHostActor(foreign))
                await actor.InvokeAsync(async context =>
            {
                bool rejected = false;
                try
                {
                    await body(context, [Term.A(DebugInternal420Names.Available), Term.A(ErlangBooleanAtoms.True)]);
                }
                catch (ArgumentException)
                {
                    rejected = true;
                }
                DebugInternal420Access.Require(rejected);

                return true;
            });
            await using (var actor = new RuntimeNodeFoundationHostActor(runtime))
                await actor.InvokeAsync(async context =>
            {
                saved = context;
                DebugLocks419Access.Cancel(context);
                bool rejected = false;
                try
                {
                    await body(context, [Term.A(DebugInternal420Names.Available), Term.A(ErlangBooleanAtoms.True)]);
                }
                catch (OperationCanceledException)
                {
                    rejected = true;
                }
                DebugInternal420Access.Require(rejected);

                return true;
            });
            await using (var keeper = new RuntimeNodeFoundationHostActor(runtime))
                await keeper.InvokeAsync(async context =>
            {
                await DebugInternal420Access.Error(context, Term.A(DebugInternal420Names.Available), ErlangErrorReasons.UndefinedFunction);

                return true;
            });
            await runtime.DisposeAsync();
            bool closed = false;
            try
            {
                await body(saved!, [Term.A(DebugInternal420Names.Available), Term.A(ErlangBooleanAtoms.True)]);
            }
            catch (ObjectDisposedException)
            {
                closed = true;
            }
            DebugInternal420Access.Require(closed);
        }
        finally
        {
            await runtime.DisposeAsync();
        }
    }

    internal static async Task Warning()
    {
        foreach (var mode in new[] { RuntimeWarningMapping.Warning, RuntimeWarningMapping.Info, RuntimeWarningMapping.Error })
        {
            await using var runtime = new ProcessRuntime(
                DebugInternal420Names.DefaultNode,
                null,
                64,
                null,
                1,
                new ProcessRuntimeOptions(mode)
            );
            await DebugInternal420Access.Initialize(runtime);
            await using var actor = new RuntimeNodeFoundationHostActor(runtime);
            await actor.InvokeAsync(async context =>
            {
                await runtime.Modules.Call(
                    context,
                    DebugInternal420Names.Runtime,
                    DebugInternal420Names.Flag,
                    Term.A(DebugInternal420Names.Logger),
                    context.Self
                );
                Term pid = await runtime.Modules.Call(
                    context,
                    DebugInternal420Names.Runtime,
                    DebugInternal420Names.PidToList,
                    context.Self
                );
                string pidText = string.Concat(Cons.Items(pid).Cast<Integer>().Select(i => (char)(int)i.Value));
                Term groupLeader = await runtime.Modules.Call(context, DebugInternal420Names.Runtime, DebugInternal420Names.GroupLeader);
                Term expectedArguments = Term.List(Term.String(DebugInternal420Names.ProcessPrefix + pidText + DebugInternal420Names.Space + DebugInternal420Names.Message));
                for (int repeat = 0; repeat < 2; repeat++)
                {
                    Integer before = (Integer)await runtime.Modules.Call(
                        context,
                        DebugInternal420Names.Runtime,
                        DebugInternal420Names.SystemTime,
                        Term.A(DebugInternal420Names.Microsecond)
                    );
                    await DebugInternal420Access.Enabled(context, true);
                    Term? notice = await context.ReceiveAsync(message => message, DebugInternal420Names.Deadline);
                    Integer after = (Integer)await runtime.Modules.Call(
                        context,
                        DebugInternal420Names.Runtime,
                        DebugInternal420Names.SystemTime,
                        Term.A(DebugInternal420Names.Microsecond)
                    );
                    DebugInternal420Access.Require(notice is TupleTerm { Items.Count: 5 });
                    var tuple = (TupleTerm)notice!;
                    string level = mode == RuntimeWarningMapping.Warning ? DebugInternal420Names.Warning : mode == RuntimeWarningMapping.Info ? DebugInternal420Names.Info : DebugInternal420Names.Error;
                    string tag = mode == RuntimeWarningMapping.Warning ? DebugInternal420Names.WarningMessage : mode == RuntimeWarningMapping.Info ? DebugInternal420Names.InfoMessage : DebugInternal420Names.Error;
                    DebugInternal420Access.Require(tuple.Items[0].Equals(Term.A(DebugInternal420Names.Log)) && tuple.Items[1].Equals(Term.A(level)) && tuple.Items[2].Equals(Term.String(DebugInternal420Names.Format)) && tuple.Items[3].Equals(expectedArguments) && tuple.Items[4] is MapTerm { Entries.Count: 4 });
                    var metadata = (MapTerm)tuple.Items[4];
                    DebugInternal420Access.Require(metadata.Get(Term.A(DebugInternal420Names.Pid)).Equals(context.Self) && metadata.Get(Term.A(DebugInternal420Names.Gl)).Equals(groupLeader));
                    var origin = metadata.Get(Term.A(DebugInternal420Names.ErrorLogger)) as MapTerm;
                    DebugInternal420Access.Require(origin is { Entries.Count: 2 } && origin.Get(Term.A(DebugInternal420Names.Emulator)).Equals(Term.A(ErlangBooleanAtoms.True)) && origin.Get(Term.A(DebugInternal420Names.Tag)).Equals(Term.A(tag)));
                    var time = metadata.Get(Term.A(DebugInternal420Names.Time)) as Integer;
                    DebugInternal420Access.Require(time is not null && time.Value >= before.Value && time.Value <= after.Value);
                }

                return true;
            });
        }
    }
}
