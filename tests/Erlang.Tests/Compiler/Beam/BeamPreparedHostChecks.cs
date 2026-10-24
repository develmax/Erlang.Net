using System.Reflection;
using Erlang;

namespace Erlang.Tests;

public static class BeamPreparedHostChecks
{
    public static async Task Run()
    {
        await using var runtime = new ProcessRuntime();
        await using var foreign = new ProcessRuntime();
        var ready = new TaskCompletionSource<ProcessContext>(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource<Term>(TaskCreationOptions.RunContinuationsAsynchronously);
        ProcessHandle controllerOwner = runtime.Spawn(async context =>
        {
            ready.TrySetResult(context);

            return await release.Task;
        });
        ProcessContext controller = await ready.Task;
        try
        {
            PreparedBeamImage[] actual =
            [
                runtime.Modules.PrepareBeam(Resource(BeamPreparedHostNames.Constant + BeamPreparedHostNames.BeamExtension)),
                runtime.Modules.PrepareBeam(Resource(BeamPreparedHostNames.Add + BeamPreparedHostNames.BeamExtension)),
                runtime.Modules.PrepareBeam(Resource(BeamPreparedHostNames.Local + BeamPreparedHostNames.BeamExtension)),
                runtime.Modules.PrepareBeam(Resource(BeamPreparedHostNames.Receive + BeamPreparedHostNames.BeamExtension))
            ];
            string[] expectedMd5 =
            [
                BeamPreparedHostNames.ConstantNativeMd5,
                BeamPreparedHostNames.AddNativeMd5,
                BeamPreparedHostNames.LocalNativeMd5,
                BeamPreparedHostNames.ReceiveNativeMd5
            ];
            Require(runtime.Modules.PublishPreparedBeam().Status == BeamPublicationStatus.Published);
            Reject<ArgumentException>(() => foreign.Modules.PublishPreparedBeam(actual));
            Require(actual.All(image => !image.IsConsumed && !runtime.Modules.IsModuleLoaded(image.Name)));
            BeamPublicationResult duplicate = runtime.Modules.PublishPreparedBeam(
                actual[0],
                actual[1],
                actual[0],
                actual[1]
            );
            Require(duplicate.Status == BeamPublicationStatus.Duplicated
                && duplicate.Modules.SequenceEqual(new[] { BeamPreparedHostNames.Add, BeamPreparedHostNames.Constant }));
            Require(actual.All(image => !image.IsConsumed));
            int dictionaryEntries = controller.Dictionary.Count;
            PreparedBeamImage callbackImage = runtime.Modules.PrepareBeam(Resource(BeamPreparedHostNames.OnLoad + BeamPreparedHostNames.BeamExtension));
            Require(callbackImage.HasOnLoad && !callbackImage.IsConsumed && !runtime.Modules.IsModuleLoaded(callbackImage.Name));
            Require(controller.Dictionary.Count == dictionaryEntries);
            Reject<InvalidOperationException>(() => runtime.Modules.PublishPreparedBeam(callbackImage));
            Require(!callbackImage.IsConsumed && !runtime.Modules.IsModuleLoaded(callbackImage.Name));
            Require(runtime.Modules.PublishPreparedBeam(actual).Status == BeamPublicationStatus.Published);
            Reject<InvalidOperationException>(() => runtime.Modules.PublishPreparedBeam(actual));
            for (int index = 0; index < actual.Length; index++)
            {
                PreparedBeamImage image = actual[index];
                Require(image.IsConsumed && Convert.ToHexString(image.NativeMd5.ToArray()) == expectedMd5[index]);
                Term result = await runtime.Modules.Call(controller, image.Name, BeamPreparedHostNames.Run);
                Require(ExternalTermFormat.Encode(Term.Tuple(Term.A(BeamPreparedHostNames.Result), result)).AsSpan()
                    .SequenceEqual(Resource(image.Name + BeamPreparedHostNames.ExpectedExtension)));
                Term md5 = await runtime.Modules.Call(
                    controller,
                    image.Name,
                    BeamPreparedHostNames.ModuleInfo,
                    Term.A(BeamPreparedHostNames.Md5)
                );
                Require(md5.Equals(image.NativeMd5));
                Term attributes = await runtime.Modules.Call(
                    controller,
                    image.Name,
                    BeamPreparedHostNames.ModuleInfo,
                    Term.A(BeamPreparedHostNames.Attributes)
                );
                Require(attributes.Equals(image.Attributes));
                Term compilation = await runtime.Modules.Call(
                    controller,
                    image.Name,
                    BeamPreparedHostNames.ModuleInfo,
                    Term.A(BeamPreparedHostNames.Compilation)
                );
                Require(compilation.Equals(image.Compilation));
                Require((await runtime.Modules.Call(controller, image.Name, BeamPreparedHostNames.ModuleInfo)).Equals(runtime.Modules.ModuleInformation(image.Name)));
            }
            await Generations(runtime, controller);
            await MetadataPolicy(runtime, controller);
            await RawMetadata(runtime, controller);
        }
        finally
        {
            release.TrySetResult(Term.A(BeamPreparedHostNames.Result));
            _ = await controllerOwner.Completion;
        }
    }

    private static async Task RawMetadata(ProcessRuntime runtime, ProcessContext controller)
    {
        byte[] native = Resource(BeamPreparedHostNames.Constant + BeamPreparedHostNames.BeamExtension);
        Term repeated = Term.List(
            Term.Tuple(Term.A(BeamPreparedHostNames.LastAttribute), Term.List(Term.I(BeamPreparedHostNames.DuplicateFirstValue))),
            Term.Tuple(Term.A(BeamPreparedHostNames.FirstAttribute), Term.List(Term.I(BeamPreparedHostNames.DuplicateSecondValue))),
            Term.Tuple(Term.A(BeamPreparedHostNames.LastAttribute), Term.List(Term.I(BeamPreparedHostNames.DuplicateThirdValue)))
        );
        Term scalar = Term.I(BeamPreparedHostNames.AttributeProbeValue);
        var variants = new List<(byte[]? Payload, Term Expected, int Decoded)>
        {
            (null, Nil.Value, 0),
            ([], Nil.Value, 0)
        };
        foreach (Term term in new Term[]
        {
            scalar,
            Term.A(BeamPreparedHostNames.CustomAttribute),
            new MapTerm([new(Term.A(BeamPreparedHostNames.CustomAttribute), scalar)]),
            new Cons(scalar, Term.A(BeamPreparedHostNames.CustomAttribute)),
            repeated
        })
        {
            byte[] bytes = ExternalTermFormat.Encode(term);
            variants.Add((bytes, term, bytes.Length));
        }
        byte[] encoded = ExternalTermFormat.Encode(scalar);
        variants.Add((encoded.Concat(new[] { BeamPreparedHostNames.TrailingFirstByte, BeamPreparedHostNames.TrailingSecondByte }).ToArray(), scalar, encoded.Length));
        byte[] compressed = ExternalTermFormat.Encode(
            Cons.From(Enumerable.Repeat(scalar, BeamPreparedHostNames.CompressedProbeElements)),
            new EtfEncodingOptions { CompressionLevel = BeamPreparedHostNames.MetadataCompressionLevel }
        );
        Require(compressed[BeamPreparedHostNames.ExternalMetadataTagOffset] == BeamPreparedHostNames.CompressedMetadataTag);
        foreach (string chunk in new[] { BeamPreparedHostNames.AttributesChunk, BeamPreparedHostNames.CompilationChunk })
        {
            Reject<InvalidDataException>(() => runtime.Modules.PrepareBeam(BeamMetadataModeledBytes.WithChunk(native, chunk, compressed)));
            foreach (var variant in variants)
            {
                byte[] modeled = BeamMetadataModeledBytes.WithChunk(native, chunk, variant.Payload);
                PreparedBeamImage image = runtime.Modules.PrepareBeam(modeled);
                Require(runtime.Modules.PublishPreparedBeam(image).Status == BeamPublicationStatus.Published);
                bool attributes = chunk == BeamPreparedHostNames.AttributesChunk;
                string key = attributes ? BeamPreparedHostNames.Attributes : BeamPreparedHostNames.Compilation;
                Term decoded = await runtime.Modules.Call(
                    controller,
                    image.Name,
                    BeamPreparedHostNames.ModuleInfo,
                    Term.A(key)
                );
                Require(decoded.Equals(variant.Expected));
                Require((attributes ? image.Attributes : image.Compilation).Equals(variant.Expected));
                BeamMetadataProvenance provenance = attributes ? image.AttributeProvenance : image.CompilationProvenance;
                Require(provenance.ChunkPresent == (variant.Payload is not null));
                Require(provenance.PayloadBytes == (variant.Payload?.Length ?? 0) && provenance.DecodedBytes == variant.Decoded);
                Require(provenance.IgnoredTrailingBytes == provenance.PayloadBytes - variant.Decoded);
                Require(Convert.ToHexString(image.NativeMd5.ToArray()) == BeamPreparedHostNames.ConstantNativeMd5);
                _ = await runtime.Modules.Call(
                    controller,
                    BeamPreparedHostNames.Erlang,
                    BeamPreparedHostNames.PurgeModule,
                    Term.A(image.Name)
                );
            }
        }
    }

    private static async Task MetadataPolicy(ProcessRuntime runtime, ProcessContext controller)
    {
        byte[] native = Resource(BeamPreparedHostNames.Constant + BeamPreparedHostNames.BeamExtension);
        Term custom = Term.List(Term.Tuple(Term.A(BeamPreparedHostNames.CustomAttribute), Term.List(Term.I(BeamPreparedHostNames.AttributeProbeValue))));
        foreach (Term? attributes in new Term?[] { null, Nil.Value, custom })
        {
            byte[] modeled = BeamMetadataModeledBytes.WithAttributes(native, attributes);
            PreparedBeamImage image = runtime.Modules.PrepareBeam(modeled);
            Require(runtime.Modules.PublishPreparedBeam(image).Status == BeamPublicationStatus.Published);
            Term decoded = await runtime.Modules.Call(
                controller,
                image.Name,
                BeamPreparedHostNames.ModuleInfo,
                Term.A(BeamPreparedHostNames.Attributes)
            );
            Require(decoded.Equals(attributes ?? Nil.Value));
            Require(image.Attributes.Equals(decoded));
            Require(Convert.ToHexString(image.NativeMd5.ToArray()) == BeamPreparedHostNames.ConstantNativeMd5);
            _ = await runtime.Modules.Call(
                controller,
                BeamPreparedHostNames.Erlang,
                BeamPreparedHostNames.PurgeModule,
                Term.A(image.Name)
            );
        }
    }

    private static async Task Generations(ProcessRuntime runtime, ProcessContext controller)
    {
        byte[] native = Resource(BeamPreparedHostNames.Receive + BeamPreparedHostNames.BeamExtension);
        byte[] modeled = BeamReceiveModeledBytes.WithoutSelfSend(native);
        PreparedBeamImage blocking = runtime.Modules.PrepareBeam(modeled);
        Require(runtime.Modules.PublishPreparedBeam(blocking).Status == BeamPublicationStatus.Published);
        Require(runtime.Modules.HasOldCode(blocking.Name));
        _ = await runtime.Modules.Call(
            controller,
            BeamPreparedHostNames.Erlang,
            BeamPreparedHostNames.PurgeModule,
            Term.A(blocking.Name)
        );
        var observed = new TaskCompletionSource<Term>(TaskCreationOptions.RunContinuationsAsynchronously);
        ProcessHandle owner = runtime.Spawn(async context =>
        {
            try
            {
                Term result = await runtime.Modules.Call(context, blocking.Name, BeamPreparedHostNames.Run);
                observed.TrySetResult(result);

                return result;
            }
            catch (Exception error)
            {
                observed.TrySetException(error);
                throw;
            }
        });
        while (true)
        {
            Term status = await runtime.Modules.Call(
                controller,
                BeamPreparedHostNames.Erlang,
                BeamPreparedHostNames.ProcessInfo,
                owner.Pid,
                Term.A(BeamPreparedHostNames.Status)
            );
            if (status is TupleTerm tuple && tuple.Items[1].Equals(Term.A(BeamPreparedHostNames.Waiting)))
                break;
            await controller.ReduceAsync();
        }
        PreparedBeamImage next = runtime.Modules.PrepareBeam(native);
        Require(runtime.Modules.PublishPreparedBeam(next).Status == BeamPublicationStatus.Published);
        Term executing = await runtime.Modules.Call(
            controller,
            BeamPreparedHostNames.Erlang,
            BeamPreparedHostNames.CheckProcessCode,
            owner.Pid,
            Term.A(next.Name)
        );
        Require(executing.Equals(Term.A(BeamPreparedHostNames.True)));
        PreparedBeamImage third = runtime.Modules.PrepareBeam(native);
        PreparedBeamImage atomicOther = runtime.Modules.PrepareBeam(Resource(BeamPreparedHostNames.Constant + BeamPreparedHostNames.BeamExtension));
        BeamPublicationResult rejected = runtime.Modules.PublishPreparedBeam(third, atomicOther);
        Require(rejected.Status == BeamPublicationStatus.NotPurged && rejected.Modules.SequenceEqual(new[] { next.Name }));
        Require(!third.IsConsumed && !atomicOther.IsConsumed && !runtime.Modules.HasOldCode(atomicOther.Name));
        ErlangOperatorValues.Send(controller, owner.Pid, Term.A(BeamPreparedHostNames.Match));
        Require((await observed.Task).Equals(Term.Tuple(Term.A(BeamPreparedHostNames.Result), Term.A(BeamPreparedHostNames.Match))));
        _ = await owner.Completion;
        _ = await runtime.Modules.Call(
            controller,
            BeamPreparedHostNames.Erlang,
            BeamPreparedHostNames.PurgeModule,
            Term.A(next.Name)
        );
        Require(!runtime.Modules.HasOldCode(next.Name));
        _ = runtime.Modules.DeleteModule(next.Name);
        Require(!runtime.Modules.IsModuleLoaded(next.Name) && runtime.Modules.HasOldCode(next.Name));
        Require(runtime.Modules.PublishPreparedBeam(third).Status == BeamPublicationStatus.Published && runtime.Modules.HasOldCode(next.Name));
    }

    private static void Reject<T>(Action action) where T : Exception
    {
        try
        {
            action();
        }
        catch (T)
        {
            return;
        }
        throw new InvalidOperationException(BeamPreparedHostNames.Failed);
    }

    private static byte[] Resource(string file)
    {
        Assembly assembly = typeof(BeamPreparedHostChecks).Assembly;
        string name = assembly.GetManifestResourceNames().Single(value => value.EndsWith(BeamPreparedHostNames.ResourceSeparator + file, StringComparison.Ordinal));
        using Stream stream = assembly.GetManifestResourceStream(name)!;
        using var bytes = new MemoryStream();
        stream.CopyTo(bytes);

        return bytes.ToArray();
    }

    private static void Require(bool value)
    {
        if (!value)
            throw new InvalidOperationException(BeamPreparedHostNames.Failed);
    }
}
