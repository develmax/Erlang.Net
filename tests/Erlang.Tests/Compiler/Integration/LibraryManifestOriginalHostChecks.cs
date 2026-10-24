using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using Erlang;

namespace Erlang.Tests;

internal static class LibraryManifestOriginalHostChecks
{
    internal static async Task Run()
    {
        await using ProcessRuntime runtime = await ProcessRuntime.CreateAsync(LibrarySelectionHostData.Both());
        PreparedBeamImage observer = runtime.Modules.PrepareBeam(
            Read(LibraryManifestOriginalNames.BeamFile),
            new BeamDependencySelection([LibrarySelectionHostNames.Runtime, ListBeamHostNames.Module, QueueModuleNames.Module])
        );
        LibrarySelectionHostData.Require(runtime.Modules.PublishPreparedBeam(observer).Status == BeamPublicationStatus.Published);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            foreach ((string entry, string file) in new[]
            {
                (LibraryManifestOriginalNames.Run, LibraryManifestOriginalNames.Values),
                (LibraryManifestOriginalNames.StackEntry, LibraryManifestOriginalNames.Stacks)
            })
            {
                Term actual = await runtime.Modules.Call(context, LibraryManifestOriginalNames.Observer, entry);
                byte[] bytes = ExternalTermFormat.Encode(actual, new EtfEncodingOptions { MinorVersion = 2, FunctionOwner = runtime.InitializationPid });
                byte[] original = Read(file);
                LibrarySelectionHostData.Require(bytes.AsSpan().SequenceEqual(original) && actual.Equals(runtime.ImportExternalTerm(original).Term));
            }

            return true;
        });
    }

    internal static async Task StoredFunBounds()
    {
        await using ProcessRuntime runtime = await ProcessRuntime.CreateAsync(LibrarySelectionHostData.Both());
        byte[] bytes = Read(LibraryManifestOriginalNames.BeamFile);
        byte[]? code = null;
        for (int offset = BeamClosureMutationWire.FormHeaderBytes; offset < bytes.Length;)
        {
            int size = BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(offset + BeamClosureMutationWire.LengthFieldOffset));
            if (Encoding.ASCII.GetString(bytes, offset, BeamClosureMutationWire.IdentifierBytes) == LibraryManifestOriginalNames.CodeChunk)
                code = bytes.AsSpan(offset + BeamClosureMutationWire.ChunkHeaderBytes, size).ToArray();
            offset += BeamClosureMutationWire.ChunkHeaderBytes + ((size + BeamClosureMutationWire.AlignmentMask) & ~BeamClosureMutationWire.AlignmentMask);
        }
        byte[] payload = code ?? throw new InvalidDataException(LibraryManifestHostNames.Failure);
        byte[] pattern = [LibraryManifestOriginalNames.MakeFunOpcode, BeamOpcodeEdgeBytes.U(0), BeamOpcodeEdgeBytes.Y(0), LibraryManifestOriginalNames.OperandList, BeamOpcodeEdgeBytes.U(0)];
        int found = payload.AsSpan().IndexOf(pattern);
        LibrarySelectionHostData.Require(found >= 0 && payload.AsSpan(found + pattern.Length).IndexOf(pattern) < 0);
        payload[found + 2] = BeamOpcodeEdgeBytes.Y(1);
        byte[] invalid = BeamMetadataModeledBytes.WithChunk(bytes, LibraryManifestOriginalNames.CodeChunk, payload);
        bool refused = false;
        try
        {
            _ = runtime.Modules.PrepareBeam(
                invalid,
                new BeamDependencySelection([LibrarySelectionHostNames.Runtime, ListBeamHostNames.Module, QueueModuleNames.Module])
            );
        }
        catch (InvalidDataException)
        {
            refused = true;
        }
        LibrarySelectionHostData.Require(refused && !runtime.Modules.IsModuleLoaded(LibraryManifestOriginalNames.Observer));
    }

    private static byte[] Read(string name)
    {
        using Stream stream = typeof(LibraryManifestOriginalHostChecks).Assembly.GetManifestResourceStream(LibraryManifestOriginalNames.Prefix + name)!;
        using var output = new MemoryStream();
        stream.CopyTo(output);
        byte[] bytes = output.ToArray();
        LibrarySelectionHostData.Require(Convert.ToHexString(SHA256.HashData(bytes)) == LibraryManifestOriginalNames.Digests[name]);

        return bytes;
    }
}
