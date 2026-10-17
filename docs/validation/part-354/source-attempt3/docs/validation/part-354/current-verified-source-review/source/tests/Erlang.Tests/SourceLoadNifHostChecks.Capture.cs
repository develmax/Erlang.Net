using System.Reflection;
using System.Security.Cryptography;
using Erlang;
using Erlang.Differential;

namespace Erlang.Tests;

public static partial class SourceLoadNifHostChecks
{
    public static async Task RunCommonFailureCapture()
    {
        foreach (bool asynchronous in new[] { false, true })
        {
            using var compiled = GeneratedModuleCompiler.Compile(SourceLoadNifCaptureHostNames.Source);
            await using var runtime = new ProcessRuntime();
            compiled.Definition.Register(runtime.Modules);
            ModuleCodeImage image = runtime.Modules.CurrentCodeImage(compiled.Definition.Name)!;
            object? escaped = null;
            int calls = 0;
            Install(
                runtime.Modules,
                async (context, capability, arguments) =>
            {
                escaped = capability;
                SourceCodeEntry caller = Consume(capability, context);
                Require(ReferenceEquals(caller.Owner, compiled.Definition));
                Require(arguments[0].Equals(Term.I(42)) && arguments[1] is Nil);
                calls++;
                if (asynchronous)
                    await Task.Yield();
                throw new ErlangException(ErlangErrorReasons.BadArgument);
            }
            );
            await NifAdmissionHostBridge.Context(
                runtime,
                async context =>
            {
                Term result = await runtime.Modules.Call(
                    context,
                    compiled.Definition.Name,
                    SourceLoadNifCaptureHostNames.Function
                );
                Require(result.Equals(Term.A(SourceLoadNifCaptureHostNames.Done)) && calls == 1);
                Require(References(context, image) == 0);
                Refused(escaped!, context);
            }
            );
        }

        Assembly assembly = typeof(SourceLoadNifHostChecks).Assembly;
        string resourceName = assembly.GetManifestResourceNames().Single(value => value.EndsWith(ActualBeamLoadNifHostNames.Resource, StringComparison.Ordinal));
        using Stream resource = assembly.GetManifestResourceStream(resourceName)!;
        using var buffer = new MemoryStream();
        resource.CopyTo(buffer);
        byte[] bytes = buffer.ToArray();
        Require(Convert.ToHexString(SHA256.HashData(bytes)) == ActualBeamLoadNifHostNames.Sha256);
        foreach (bool registered in new[] { false, true })
            foreach (bool asynchronous in new[] { false, true })
            {
                await using var runtime = new ProcessRuntime();
                BeamModule standalone = BeamModule.Prepare(bytes);
                PreparedBeamImage? prepared = null;
                if (registered)
                {
                    prepared = runtime.Modules.PrepareBeam(bytes);
                    Require(runtime.Modules.PublishPreparedBeam(prepared).Status == BeamPublicationStatus.Published);
                }
                object expectedOwner = (object?)prepared ?? standalone;
                ModuleCodeImage? image = null;
                object? escaped = null;
                int calls = 0;
                Install(
                    runtime.Modules,
                    async (context, capability, arguments) =>
                {
                    escaped = capability;
                    SourceCodeEntry caller = Consume(capability, context);
                    Require(ReferenceEquals(caller.Owner, expectedOwner));
                    image = caller.Image;
                    Require(arguments[0].Equals(Term.I(42)) && arguments[1] is Nil);
                    calls++;
                    if (asynchronous)
                        await Task.Yield();
                    throw new ErlangException(ErlangErrorReasons.BadArgument);
                }
                );
                await NifAdmissionHostBridge.Context(
                    runtime,
                    async context =>
                {
                    ErlangException? observed = null;
                    try
                    {
                        if (registered)
                            await runtime.Modules.Call(
                                context,
                                standalone.Name,
                                ActualBeamLoadNifHostNames.Load,
                                Term.I(42),
                                Nil.Value
                            );
                        else
                            await standalone.Invoke(
                                context,
                                ActualBeamLoadNifHostNames.Load,
                                Term.I(42),
                                Nil.Value
                            );
                    }
                    catch (ErlangException error)
                    {
                        observed = error;
                    }
                    Require(observed is not null && observed.Reason.Equals(Term.A(ErlangErrorReasons.BadArgument)) && calls == 1);
                    Term[] trace = Cons.Items(observed!.StackTraceTerm).ToArray();
                    Require(trace.Length >= 2 && trace[0] is TupleTerm top && top.Items.Count == 4
                        && top.Items[0].Equals(Term.A(SourceLoadNifHostNames.Erlang))
                        && top.Items[1].Equals(Term.A(SourceLoadNifHostNames.Function))
                        && top.Items[2].Equals(Term.List(Term.I(42), Nil.Value)));
                    Term info = Term.List(Term.Tuple(
                        Term.A(SourceLoadNifCaptureHostNames.ErrorInfo),
                        new MapTerm(new Dictionary<Term, Term>
                        {
                            [Term.A(SourceLoadNifCaptureHostNames.FormatterKey)] = Term.A(SourceLoadNifCaptureHostNames.Formatter)
                        })
                    ));
                    Require(((TupleTerm)trace[0]).Items[3].Equals(info));
                    Require(trace[1] is TupleTerm caller && caller.Items[0].Equals(Term.A(standalone.Name))
                        && caller.Items[1].Equals(Term.A(ActualBeamLoadNifHostNames.Load)) && caller.Items[2].Equals(Term.I(2)));
                    Require(image is not null && References(context, image) == 0);
                    Refused(escaped!, context);
                }
                );
            }
    }
}
