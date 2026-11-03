using System.Buffers.Binary;
using System.Security.Cryptography;
using Erlang;
using static Erlang.Tests.NativeMetadataImage527Names;

namespace Erlang.Tests;

internal static class NativeMetadataImage527Checks
{
    internal static Task Original() => Check(lineless: false);

    internal static Task Lineless() => Check(lineless: true);

    private static async Task Check(bool lineless)
    {
        var assembly = typeof(NativeMetadataImage527Checks).Assembly;
        string resource = assembly.GetManifestResourceNames().Single(value => value.EndsWith(Fixture, StringComparison.Ordinal));
        using Stream input = assembly.GetManifestResourceStream(resource)!;
        using var bytes = new MemoryStream();
        input.CopyTo(bytes);
        byte[] original = bytes.ToArray();
        Require(Convert.ToHexString(SHA256.HashData(original)) == FixtureSha);
        byte[] selected = lineless ? WithoutLine(original) : original;
        var profile = new RuntimeNativeMetadataImage(selected);
        Require(profile.OriginalBeamSha256 == Convert.ToHexString(SHA256.HashData(selected)));
        // The profile owns a snapshot: later host buffer mutation cannot alter it.
        selected.AsSpan().Clear();
        await using var runtime = new ProcessRuntime(new ProcessRuntimeOptions { NativeMetadataImage = profile });
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        Term stack = await actor.InvokeAsync(async context =>
        {
            try
            {
                _ = await runtime.Modules.Call(
                    context,
                    ModuleName,
                    LoadModule,
                    Term.I(1),
                    Term.I(2)
                );
            }
            catch (ErlangException error)
            {
                return error.StackTraceTerm;
            }

            throw new InvalidOperationException(Error);
        });
        var first = (TupleTerm)Cons.Items(stack).First();
        Term[] location = Cons.Items(first.Items[LocationTupleIndex]).Where(value =>
            value is TupleTerm tuple && tuple.Items.Count == 2
                && tuple.Items[0] is Atom atom && atom.Name is FileKey or LineKey).ToArray();
        Term expected = lineless ? Nil.Value : Term.List(
            Term.Tuple(Term.A(FileKey), Term.String(FileName)),
            Term.Tuple(Term.A(LineKey), Term.I(HelperLine)));
        Require(Cons.From(location).Equals(expected));
    }

    private static byte[] WithoutLine(byte[] original)
    {
        using var output = new MemoryStream();
        output.Write(original, 0, FormHeader);
        bool removed = false;
        for (int offset = FormHeader; offset < original.Length;)
        {
            int length = BinaryPrimitives.ReadInt32BigEndian(original.AsSpan(offset + WordBytes, WordBytes));
            int padded = checked((length + WordBytes - 1) / WordBytes * WordBytes);
            int total = checked(ChunkHeader + padded);
            if (original.AsSpan(offset, WordBytes).SequenceEqual(System.Text.Encoding.ASCII.GetBytes(LineChunk)))
                removed = true;
            else
                output.Write(original, offset, total);
            offset = checked(offset + total);
        }
        Require(removed);
        byte[] result = output.ToArray();
        BinaryPrimitives.WriteInt32BigEndian(result.AsSpan(WordBytes, WordBytes), result.Length - ChunkHeader);

        return result;
    }

    private static void Require(bool value)
    {
        if (!value)
            throw new InvalidOperationException(Error);
    }
}
