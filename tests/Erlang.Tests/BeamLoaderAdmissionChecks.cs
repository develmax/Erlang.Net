using System.Reflection;
using System.IO.Compression;

namespace Erlang.Tests;

public static class BeamLoaderAdmissionChecks
{
    public static async Task Run()
    {
        await using var runtime = new ProcessRuntime();
        await using var foreign = new ProcessRuntime();
        var completed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        ProcessHandle actor = runtime.Spawn(async context =>
        {
            try
            {
                byte[] bytes = Resource(BeamPreparedHostNames.Constant + BeamLoaderAdmissionNames.Extension);
                Term module = Term.A(BeamPreparedHostNames.Constant);
                Term reference = await Call(
                    context,
                    BeamLoaderAdmissionNames.Prepare,
                    module,
                    new BitString(bytes)
                );
                Require(reference is ResourceReferenceTerm);
                Require((await Call(context, BeamLoaderAdmissionNames.Query, reference)).Equals(Term.A(ErlangBooleanAtoms.False)));
                Require((await Call(context, BeamLoaderAdmissionNames.Finish, Cons.From([reference]))).Equals(Term.A(BeamLoaderAdmissionNames.Ok)));
                Require((await Call(context, BeamLoaderAdmissionNames.Query, reference)).Equals(Term.A(ErlangBooleanAtoms.False)));
                Require(runtime.ReferenceResources.AcquirePrepared((ReferenceTerm)reference) is null);
                await Badarg(() => Call(context, BeamLoaderAdmissionNames.Finish, Cons.From([reference])));
                ResourceReferenceTerm wrongOwner = foreign.ReferenceResources.CreatePrepared(foreign.Modules.PrepareBeam(bytes));
                await Badarg(() => Call(context, BeamLoaderAdmissionNames.Query, wrongOwner));
                await Badarg(() => Call(context, BeamLoaderAdmissionNames.Finish, Cons.From([wrongOwner])));
                ResourceReferenceTerm wrongKind = runtime.ReferenceResources.CreateBinary(bytes);
                await Badarg(() => Call(context, BeamLoaderAdmissionNames.Query, wrongKind));
                Term fresh = await Call(
                    context,
                    BeamLoaderAdmissionNames.Prepare,
                    module,
                    new BitString(bytes)
                );
                await Badarg(() => Call(context, BeamLoaderAdmissionNames.Finish, new Cons(fresh, Term.I(1))));
                Require(runtime.ReferenceResources.AcquirePrepared((ReferenceTerm)fresh) is not null);
                Require((await Call(
                    context,
                    BeamLoaderAdmissionNames.Prepare,
                    Term.A(BeamLoaderAdmissionNames.ForeignModule),
                    new BitString(bytes)
                ))
                    .Equals(Term.Tuple(Term.A(BeamLoaderAdmissionNames.Error), Term.A(BeamLoaderAdmissionNames.BadFile))));
                using var compressed = new MemoryStream();
                using (var gzip = new GZipStream(compressed, CompressionMode.Compress, true))
                    gzip.Write(bytes);
                await GzipErrorArguments(context, compressed.ToArray(), bytes);
                byte[] unsupportedOnLoad = Resource(BeamPreparedHostNames.OnLoad + BeamLoaderAdmissionNames.Extension);
                try
                {
                    await Call(
                        context,
                        BeamLoaderAdmissionNames.Prepare,
                        Term.A(BeamPreparedHostNames.OnLoad),
                        new BitString(unsupportedOnLoad)
                    );
                    throw new InvalidOperationException(BeamLoaderAdmissionNames.Failure);
                }
                catch (NotSupportedException)
                {
                }
                byte[] onLoad = Resource(BeamLoaderAdmissionNames.SupportedOnLoadResource);
                Term callbackModule = Term.A(BeamOnLoadHostNames.FreshSuccessModule);
                Term callbackReference = await Call(
                    context,
                    BeamLoaderAdmissionNames.Prepare,
                    callbackModule,
                    new BitString(onLoad)
                );
                await Reject(ErlangErrorReasons.SystemLimit, () => Call(context, BeamLoaderAdmissionNames.Finish, Cons.From([fresh, callbackReference])));
                Require(runtime.ReferenceResources.AcquirePrepared((ReferenceTerm)fresh) is not null && runtime.ReferenceResources.AcquirePrepared((ReferenceTerm)callbackReference) is not null);
                Require((await Call(context, BeamLoaderAdmissionNames.Finish, Cons.From([callbackReference])))
                    .Equals(Term.Tuple(Term.A(BeamLoaderAdmissionNames.OnLoad), Cons.From([callbackModule]))));
                Require((await Call(context, BeamLoaderAdmissionNames.Query, callbackReference)).Equals(Term.A(ErlangBooleanAtoms.True)));
                for (int repeat = 0; repeat < 2; repeat++)
                    Require((await Call(
                        context,
                        BeamLoaderAdmissionNames.FinishOnLoad,
                        callbackModule,
                        Term.A(ErlangBooleanAtoms.False)
                    )).Equals(Term.A(ErlangBooleanAtoms.True)));
                Require((await Call(
                    context,
                    BeamLoaderAdmissionNames.FinishOnLoad,
                    callbackModule,
                    Term.A(ErlangBooleanAtoms.True)
                )).Equals(Term.A(ErlangBooleanAtoms.True)));
                await Badarg(() => Call(
                    context,
                    BeamLoaderAdmissionNames.FinishOnLoad,
                    callbackModule,
                    Term.A(ErlangBooleanAtoms.True)
                ));
                completed.TrySetResult();

                return Term.A(ErlangBooleanAtoms.True);
            }
            catch (Exception exception)
            {
                completed.TrySetException(exception);
                throw;
            }
        });
        await completed.Task;
        await actor.Completion;
        await ClosedResourceAdmission();
    }

    private static async Task ClosedResourceAdmission()
    {
        var runtime = new ProcessRuntime();
        PreparedBeamImage image = runtime.Modules.PrepareBeam(Resource(BeamPreparedHostNames.Constant + BeamLoaderAdmissionNames.Extension));
        ResourceReferenceTerm reference = runtime.ReferenceResources.CreatePrepared(image);
        await runtime.DisposeAsync();
        MethodInfo finish = typeof(RuntimeReferenceResources).GetMethod(BeamLoaderAdmissionNames.Admit, BindingFlags.Instance | BindingFlags.NonPublic)!;
        try
        {
            finish.Invoke(runtime.ReferenceResources, [new ReferenceTerm[] { reference }]);
        }
        catch (TargetInvocationException exception) when (exception.InnerException is ErlangException error)
        {
            Require(error.Reason.Equals(Term.A(ErlangErrorReasons.BadArgument)) && !image.IsConsumed);

            return;
        }

        throw new InvalidOperationException(BeamLoaderAdmissionNames.Failure);
    }

    private static async Task GzipErrorArguments(ProcessContext context, byte[] gzip, byte[] original)
    {
        try
        {
            await Call(
                context,
                BeamLoaderAdmissionNames.Prepare,
                Term.I(1),
                new BitString(gzip)
            );
        }
        catch (ErlangException exception)
        {
            Require(exception.Reason.Equals(Term.A(ErlangErrorReasons.BadArgument)));
            var frame = (TupleTerm)Cons.Items(exception.StackTraceTerm).First();
            Term[] arguments = Cons.Items(frame.Items[2]).ToArray();
            Require(arguments[1].Equals(new BitString(original)));
            var location = (TupleTerm)Cons.Items(frame.Items[3]).First();
            Require(location.Items[0].Equals(Term.A(BeamLoaderAdmissionNames.ErrorInfo)));
            var info = (MapTerm)location.Items[1];
            Require(info.Entries.Any(entry => entry.Key.Equals(Term.A(BeamLoaderAdmissionNames.Module)) && entry.Value.Equals(Term.A(BeamLoaderAdmissionNames.Formatter))));

            return;
        }

        throw new InvalidOperationException(BeamLoaderAdmissionNames.Failure);
    }

    private static ValueTask<Term> Call(ProcessContext context, string name, params Term[] arguments)
        => context.Runtime.Modules.Call(
            context,
            ErlangModuleNames.Module,
            name,
            arguments
        );

    private static Task Badarg(Func<ValueTask<Term>> execute) => Reject(ErlangErrorReasons.BadArgument, execute);

    private static async Task Reject(string reason, Func<ValueTask<Term>> execute)
    {
        try
        {
            await execute();
        }
        catch (ErlangException exception)
        {
            Require(exception.Reason.Equals(Term.A(reason)));

            return;
        }

        throw new InvalidOperationException(BeamLoaderAdmissionNames.Failure);
    }

    private static byte[] Resource(string name)
    {
        Assembly assembly = typeof(BeamLoaderAdmissionChecks).Assembly;
        string path = assembly.GetManifestResourceNames().Single(value => value.EndsWith(BeamLoaderAdmissionNames.Separator + name, StringComparison.Ordinal));
        using Stream stream = assembly.GetManifestResourceStream(path)!;
        using var output = new MemoryStream();
        stream.CopyTo(output);

        return output.ToArray();
    }

    private static void Require(bool value)
    {
        if (!value)
            throw new InvalidOperationException(BeamLoaderAdmissionNames.Failure);
    }
}
