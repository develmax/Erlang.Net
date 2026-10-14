using System.Reflection;
using Erlang.Compiler;
using Erlang.Differential;

namespace Erlang.Tests;

public static class BeamLoaderDirectChecks
{
    public static async Task Run()
    {
        await Observe(false, false, false);
        await Observe(true, false, false);
        await Observe(false, true, false);
        await Observe(true, true, false);
    }

    public static async Task RunNative()
    {
        await Observe(false, false, true);
        await Observe(false, true, true);
    }

    internal static async Task RunSource(string source)
    {
        await Observe(
            false,
            false,
            false,
            source
        );
        await Observe(
            true,
            false,
            false,
            source
        );
        await Observe(
            false,
            true,
            false,
            source
        );
        await Observe(
            true,
            true,
            false,
            source
        );
    }

    private static async Task Observe(
        bool generated,
        bool old,
        bool nativeCaller,
        string? source = null
    )
    {
        await using var runtime = new ProcessRuntime();
        using GeneratedModuleArtifact? artifact = generated ? GeneratedModuleCompiler.Compile(source ?? BeamLoaderDirectNames.Source) : null;
        ModuleDefinition caller = artifact?.Definition ?? new Parser(source ?? BeamLoaderDirectNames.Source).ParseModule();
        caller.Register(runtime.Modules);
        string module = old ? BeamOnLoadHostNames.ReplacementFailureModule : BeamOnLoadHostNames.FreshSuccessModule;
        if (old)
            Require(runtime.Modules.PublishPreparedBeam(runtime.Modules.PrepareBeam(Resource(BeamLoaderDirectNames.ReplacementOldResource))).Status == BeamPublicationStatus.Published);
        var ready = new TaskCompletionSource<ProcessContext>(TaskCreationOptions.RunContinuationsAsynchronously);
        var finish = new TaskCompletionSource<Term>(TaskCreationOptions.RunContinuationsAsynchronously);
        ProcessHandle observer = runtime.Spawn(context =>
        {
            ready.TrySetResult(context);

            return new ValueTask<Term>(finish.Task);
        });
        ProcessContext controller = await ready.Task;
        runtime.Register(BeamOnLoadHostNames.Observer, observer.Pid);
        Term reference = await runtime.Modules.Call(
            controller,
            ErlangModuleNames.Module,
            BeamLoaderAdmissionNames.Prepare,
            Term.A(module),
            new BitString(Resource(old ? BeamLoaderDirectNames.ReplacementNewResource : BeamLoaderDirectNames.FreshResource))
        );
        await runtime.Modules.Call(
            controller,
            ErlangModuleNames.Module,
            BeamLoaderAdmissionNames.Finish,
            Cons.From([reference])
        );
        var callbackReady = new TaskCompletionSource<ProcessContext>(TaskCreationOptions.RunContinuationsAsynchronously);
        object pendingTable = typeof(ModuleRegistry).GetField(BeamLoaderDirectNames.PendingTable, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(runtime.Modules)!;
        var pending = (System.Collections.IDictionary)pendingTable;
        object ticket = pending[module]!;
        var callbackImage = (ModuleCodeImage)ticket.GetType().GetProperty(BeamLoaderDirectNames.ImageProperty, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(ticket)!;
        MethodInfo executing = typeof(ProcessContext).GetMethod(BeamLoaderDirectNames.ExecutingMethod, BindingFlags.Instance | BindingFlags.NonPublic)!;
        var observed = new TaskCompletionSource<Term>(TaskCreationOptions.RunContinuationsAsynchronously);
        ProcessHandle callback = runtime.Spawn(async context =>
        {
            callbackReady.TrySetResult(context);
            try
            {
                Term value = nativeCaller
                    ? await BeamModule.Prepare(Resource(BeamLoaderDirectNames.NativeWrapperResource)).Invoke(context, BeamLoaderDirectNames.Direct, Term.A(module))
                    : await runtime.Modules.Call(
                        context,
                        BeamLoaderDirectNames.Module,
                        BeamLoaderDirectNames.Direct,
                        Term.A(module)
                    );
                Require(!(bool)executing.Invoke(context, [callbackImage])!);
                observed.TrySetResult(value);

                return value;
            }
            catch (Exception exception)
            {
                observed.TrySetException(exception);
                throw;
            }
        });
        try
        {
            TupleTerm notice = (TupleTerm)(await controller.ReceiveAsync(
                term => term is TupleTerm tuple && tuple.Items.Count == 5 && tuple.Items[0].Equals(Term.A(BeamOnLoadHostNames.Notice)) ? term : null,
                TimeSpan.FromSeconds(BeamLoaderDirectNames.WatchdogSeconds)
            ))!;
            if (notice is null && observed.Task.IsCompleted)
                await observed.Task;
            if (notice is null)
                throw new InvalidOperationException(BeamLoaderDirectNames.Failure);
            Require(notice.Items[1].Equals(Term.A(module)) && notice.Items[2].Equals(callback.Pid));
            ProcessContext callbackContext = await callbackReady.Task;
            Require((bool)executing.Invoke(callbackContext, [callbackImage])!);
            Require((await runtime.Modules.Call(
                controller,
                ErlangModuleNames.Module,
                BeamLoaderAdmissionNames.FinishOnLoad,
                Term.A(module),
                Term.A(ErlangBooleanAtoms.False)
            )).Equals(Term.A(ErlangBooleanAtoms.True)));
            Require((bool)executing.Invoke(callbackContext, [callbackImage])!);
            Require(runtime.Modules.IsModuleLoaded(module) == old);
            if (old)
                Require((await runtime.Modules.Call(controller, module, BeamOnLoadHostNames.Run)).Equals(Term.A(BeamOnLoadHostNames.OldImage)));
            await DynamicReject(controller, module);
            Require(await controller.ReceiveAsync(
                term => term is TupleTerm tuple && tuple.Items.Count == 5 && tuple.Items[0].Equals(Term.A(BeamOnLoadHostNames.Notice)) ? term : null,
                TimeSpan.Zero
            ) is null);
            runtime.Send(callback.Pid, Term.Tuple(Term.A(BeamOnLoadHostNames.Continue), observer.Pid));
            Require((await observed.Task.WaitAsync(TimeSpan.FromSeconds(BeamLoaderDirectNames.WatchdogSeconds)))
                .Equals(Term.A(old ? BeamOnLoadHostNames.NotOk : BeamOnLoadHostNames.Ok)));
            Require((await callback.Completion.WaitAsync(TimeSpan.FromSeconds(BeamLoaderDirectNames.WatchdogSeconds)))
                .Equals(Term.A(ProcessExitReasons.Normal)));
            Require((await runtime.Modules.Call(
                controller,
                ErlangModuleNames.Module,
                BeamLoaderAdmissionNames.FinishOnLoad,
                Term.A(module),
                Term.A(ErlangBooleanAtoms.False)
            )).Equals(Term.A(ErlangBooleanAtoms.True)));
            if (old)
                Require((await runtime.Modules.Call(controller, module, BeamOnLoadHostNames.Run)).Equals(Term.A(BeamOnLoadHostNames.OldImage)));
            Require((await runtime.Modules.Call(
                controller,
                ErlangModuleNames.Module,
                BeamLoaderAdmissionNames.FinishOnLoad,
                Term.A(module),
                Term.A(ErlangBooleanAtoms.True)
            )).Equals(Term.A(ErlangBooleanAtoms.True)));
            Require((await runtime.Modules.Call(controller, module, BeamOnLoadHostNames.Run)).Equals(Term.A(BeamOnLoadHostNames.NewImage)));
            Require((await runtime.Modules.Call(
                controller,
                ErlangModuleNames.Module,
                BeamLoaderAdmissionNames.Query,
                reference
            )).Equals(Term.A(ErlangBooleanAtoms.True)));
        }
        finally
        {
            finish.TrySetResult(Term.A(ErlangBooleanAtoms.True));
        }
    }

    private static async Task DynamicReject(ProcessContext context, string module)
    {
        try
        {
            await context.Runtime.Modules.Call(
                context,
                BeamLoaderDirectNames.Module,
                BeamLoaderDirectNames.Dynamic,
                Term.A(ErlangModuleNames.Module),
                Term.A(BeamLoaderDirectNames.Callback),
                Cons.From([Term.A(module)])
            );
        }
        catch (ErlangException exception)
        {
            Require(exception.ExceptionClass == ErlangExceptionClasses.Error && exception.Reason.Equals(Term.A(ErlangErrorReasons.BadArgument)));

            return;
        }

        throw new InvalidOperationException(BeamLoaderDirectNames.Failure);
    }

    private static byte[] Resource(string name)
    {
        Assembly assembly = typeof(BeamLoaderDirectChecks).Assembly;
        string resource = assembly.GetManifestResourceNames().Single(value => value.EndsWith(BeamLoaderAdmissionNames.Separator + name, StringComparison.Ordinal));
        using Stream stream = assembly.GetManifestResourceStream(resource)!;
        using var output = new MemoryStream();
        stream.CopyTo(output);

        return output.ToArray();
    }

    private static void Require(bool value)
    {
        if (!value)
            throw new InvalidOperationException(BeamLoaderDirectNames.Failure);
    }
}
