using static Erlang.Tests.BeamNifMarkers410Access;

namespace Erlang.Tests;

internal static class BeamNifMarkers410Checks
{
    internal static Task ActualPrimFile()
    {
        return Actual();

        static async Task Actual()
        {
            await using var runtime = new ProcessRuntime(1);
            string[] dependencies = System.Text.Json.JsonSerializer.Deserialize<string[]>(
                LibraryDependencyOriginalHostChecks.ReadRetainedResource(LibraryDependencyOriginalNames.Dependencies))!;
            PreparedBeamImage image = runtime.Modules.PrepareBeam(
                LibraryDependencyOriginalHostChecks.ReadRetainedResource(BeamNifMarkers410Names.PrimImage),
                new BeamDependencySelection(dependencies.Append(BeamNifMarkers410Names.Erlang).Append(BeamNifMarkers410Names.PrimFile))
            );
            var descriptor = (ModuleCodeDescriptor)Property(image, BeamNifMarkers410Names.Descriptor);
            Require(descriptor.DeclaredNifs is { Count: BeamNifMarkers410Names.MarkerCount } declared
                && declared.ToHashSet().SetEquals(BeamNifMarkers410Signatures.Expected));
            Require(image.Name == BeamNifMarkers410Names.PrimFile && image.NativePrimitiveExports.Count == 4);
            // No publication/on_load/file operation is performed in this metadata control.
        }
    }

    internal static async Task Variants()
    {
        for (int variant = 0; variant < 6; variant++)
        {
            await using var runtime = new ProcessRuntime(1);
            PreparedBeamImage image = runtime.Modules.PrepareBeam(BeamNifMarkers410Bytes.Image(variant));
            var descriptor = (ModuleCodeDescriptor)Property(image, BeamNifMarkers410Names.Descriptor);
            string target = variant == 5 ? BeamOpcodeEdgeNames.Middle : BeamOpcodeEdgeNames.Run;
            string forbidden = variant == 5 ? BeamOpcodeEdgeNames.Run : BeamOpcodeEdgeNames.Middle;
            if (variant == 0) Require(descriptor.DeclaredNifs is null);
            else Require(descriptor.DeclaredNifs is { Count: 1 } single && single[0] == (target, 0));
            Require(runtime.Modules.PublishPreparedBeam(image).Status == BeamPublicationStatus.Published);
            ModuleCodeImage code = runtime.Modules.CurrentCodeImage(image.Name)!;
            object table = Property(code, BeamNifMarkers410Names.Table);
            if (variant != 0)
            {
                var refusedOwner = new NifSourceEntryTestOwner();
                using var pending = (IDisposable)Call(table, BeamNifMarkers410Names.Begin, descriptor)!;
                bool refused = false;
                try
                {
                    Call(
                        pending,
                        BeamNifMarkers410Names.Publish,
                        new KeyValuePair<(string Name, int Arity), ErlangFunction>[]
                        { new((forbidden, 0), (_, _) => ValueTask.FromResult<Term>(Term.I(0))) },
                        refusedOwner
                    );
                }
                catch (ArgumentException)
                {
                    refused = true;
                }
                finally
                {
                    await refusedOwner.DisposeAsync();
                }
                Require(refused && refusedOwner.Calls == 1);
            }
            var nativeOwner = new NifSourceEntryTestOwner();
            int calls = 0;
            try
            {
                using (var pending = (IDisposable)Call(table, BeamNifMarkers410Names.Begin, descriptor)!)
                    Call(
                        pending,
                        BeamNifMarkers410Names.Publish,
                        new KeyValuePair<(string Name, int Arity), ErlangFunction>[]
                        {
                            new(
                                (target, 0),
                                (context, args) =>
                            {
                                Require(args.Count == 0 && ReferenceEquals(context.ExecutingSourceImage(image), code));
                                calls++;

                                return ValueTask.FromResult<Term>(Term.I(BeamNifMarkers410Names.NativeValue));
                            }
                            )
                        },
                        nativeOwner
                    );
                await using var actor = new RuntimeNodeFoundationHostActor(runtime);
                Term value = await actor.InvokeAsync(async context => await runtime.Modules.Call(context,
                    image.Name, BeamOpcodeEdgeNames.Run));
                Require(value.Equals(Term.I(BeamNifMarkers410Names.NativeValue)) && calls == 1);
            }
            finally
            {
                await (Task)Call(table, BeamNifMarkers410Names.Retire)!;
            }
            Require(nativeOwner.Calls == 1);
        }
    }
}
