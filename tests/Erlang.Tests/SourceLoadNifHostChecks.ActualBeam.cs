using System.Reflection;
using System.Security.Cryptography;

namespace Erlang.Tests;

public static partial class SourceLoadNifHostChecks
{
    private static async Task ActualBeamCaller()
    {
        Assembly assembly = typeof(SourceLoadNifHostChecks).Assembly;
        string resourceName = assembly.GetManifestResourceNames().Single(value => value.EndsWith(ActualBeamLoadNifHostNames.Resource, StringComparison.Ordinal));
        using Stream resource = assembly.GetManifestResourceStream(resourceName)!;
        using var contents = new MemoryStream();
        resource.CopyTo(contents);
        byte[] bytes = contents.ToArray();
        Require(Convert.ToHexString(SHA256.HashData(bytes)) == ActualBeamLoadNifHostNames.Sha256);
        foreach (bool registered in new[] { false, true })
        {
            await using var runtime = new ProcessRuntime();
            BeamModule standalone = BeamModule.Prepare(bytes);
            Require(standalone.Supports(ActualBeamLoadNifHostNames.Load, SourceLoadNifHostNames.Arity));
            PreparedBeamImage? prepared = null;
            if (registered)
            {
                prepared = runtime.Modules.PrepareBeam(bytes);
                Require(runtime.Modules.PublishPreparedBeam(prepared).Status == BeamPublicationStatus.Published);
            }
            object expectedOwner = (object?)prepared ?? standalone;
            ModuleCodeImage? expectedImage = registered ? runtime.Modules.CurrentCodeImage(standalone.Name) : null;
            Require(runtime.Modules.HasExport(SourceLoadNifHostNames.Erlang, SourceLoadNifHostNames.Function, SourceLoadNifHostNames.Arity));
            await NifAdmissionHostBridge.Context(
                runtime,
                async context =>
            {
                Term rejection = Term.Tuple(
                    Term.A(SourceLoadNifHostNames.Error),
                    Term.Tuple(Term.A(SourceLoadNifHostNames.BadLibrary), Term.String(SourceLoadNifHostNames.ApplyRejectedMessage))
                );
                Term fallback = registered
                    ? await runtime.Modules.Call(
                        context,
                        standalone.Name,
                        ActualBeamLoadNifHostNames.Load,
                        Term.String(ActualBeamLoadNifHostNames.Path),
                        Term.I(SourceLoadNifHostNames.Marker)
                    )
                    : await standalone.Invoke(
                        context,
                        ActualBeamLoadNifHostNames.Load,
                        Term.String(ActualBeamLoadNifHostNames.Path),
                        Term.I(SourceLoadNifHostNames.Marker)
                    );
                Require(fallback.Equals(rejection));
                if (expectedImage is not null)
                    Require(References(context, expectedImage) == 0);
            }
            );
            int calls = 0;
            object? escaped = null;
            var marker = new InvalidOperationException(ActualBeamLoadNifHostNames.Failure);
            bool fail = false;
            bool bind = false;
            var managedOwner = new NifAdmissionTestOwner();
            int boundCalls = 0;
            Install(
                runtime.Modules,
                async (context, capability, arguments) =>
            {
                calls++;
                escaped = capability;
                SourceCodeEntry caller = Consume(capability, context);
                Require(ReferenceEquals(caller.Owner, expectedOwner));
                expectedImage ??= caller.Image;
                Require(ReferenceEquals(caller.Image, expectedImage));
                Require(References(context, caller.Image) == SourceLoadNifHostNames.OwnedDirectReferences);
                Require(arguments.Length == SourceLoadNifHostNames.Arity
                    && arguments[0].Equals(Term.String(ActualBeamLoadNifHostNames.Path))
                    && arguments[1].Equals(Term.I(SourceLoadNifHostNames.Marker)));
                Refused(capability, context);
                await Task.Yield();
                Require(References(context, caller.Image) == SourceLoadNifHostNames.OwnedDirectReferences);
                if (registered)
                {
                    object reservation = await NifAdmissionHostBridge.Reserve(runtime.Modules, context);
                    try
                    {
                        if (bind)
                            await NifAdmissionHostBridge.Initialize(
                                reservation,
                                _ => ValueTask.FromResult(NifAdmissionHostBridge.Initialized(
                                    managedOwner,
                                    [new KeyValuePair<(string Name, int Arity), ErlangFunction>(
                                        (ActualBeamLoadNifHostNames.Stub, 0),
                                        (_, arguments) =>
                                    {
                                        Require(arguments.Count == 0);
                                        boundCalls++;

                                        return ValueTask.FromResult<Term>(Term.I(SourceLoadNifHostNames.Marker));
                                    }
                                    )]
                                ))
                            );
                    }
                    finally
                    {
                        await NifAdmissionHostBridge.Dispose(reservation);
                    }
                }
                if (fail)
                    throw marker;

                return arguments[1];
            }
            );
            await NifAdmissionHostBridge.Context(
                runtime,
                async context =>
            {
                async ValueTask<Term> Invoke(string function, params Term[] arguments)
                    => registered
                        ? await runtime.Modules.Call(
                            context,
                            standalone.Name,
                            function,
                            arguments
                        )
                        : await standalone.Invoke(context, function, arguments);

                Term result = await Invoke(ActualBeamLoadNifHostNames.Load, Term.String(ActualBeamLoadNifHostNames.Path), Term.I(SourceLoadNifHostNames.Marker));
                Require(result.Equals(Term.I(SourceLoadNifHostNames.Marker)) && calls == 1);
                Refused(escaped!, context);
                Require(References(context, expectedImage!) == 0);
                bool stubFailed = false;
                try
                {
                    await Invoke(ActualBeamLoadNifHostNames.Stub);
                }
                catch (ErlangException error) when (error.Reason.Equals(Term.A(ActualBeamLoadNifHostNames.NotLoaded)))
                {
                    stubFailed = true;
                }
                Require(stubFailed && calls == 1 && References(context, expectedImage!) == 0);
                fail = true;
                bool ownedFailure = false;
                try
                {
                    await Invoke(ActualBeamLoadNifHostNames.Load, Term.String(ActualBeamLoadNifHostNames.Path), Term.I(SourceLoadNifHostNames.Marker));
                }
                catch (InvalidOperationException error) when (ReferenceEquals(error, marker))
                {
                    ownedFailure = true;
                }
                Require(ownedFailure && calls == 2 && References(context, expectedImage!) == 0);
                Refused(escaped!, context);
                if (registered)
                {
                    fail = false;
                    bind = true;
                    await Invoke(ActualBeamLoadNifHostNames.Load, Term.String(ActualBeamLoadNifHostNames.Path), Term.I(SourceLoadNifHostNames.Marker));
                    Require(calls == 3 && References(context, expectedImage!) == 0);
                    Require((await Invoke(ActualBeamLoadNifHostNames.Stub)).Equals(Term.I(SourceLoadNifHostNames.Marker)));
                    Require(boundCalls == 1 && References(context, expectedImage!) == 0);
                    Require(managedOwner.Calls == 0);
                }
            }
            );
            Require(runtime.Modules.HasExport(SourceLoadNifHostNames.Erlang, SourceLoadNifHostNames.Function, SourceLoadNifHostNames.Arity));
        }
    }
}
