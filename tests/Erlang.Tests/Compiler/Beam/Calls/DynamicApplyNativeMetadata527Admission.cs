using System.Security.Cryptography;
using System.Text.Json;
using Erlang;
using static Erlang.Tests.DynamicApplyNativeMetadata527Names;

namespace Erlang.Tests;

internal static class DynamicApplyNativeMetadata527Admission
{
    internal static RuntimeNativeMetadataImage Verify(string original, JsonElement admission, byte[] selected)
    {
        string identityPath = Child(original, admission.GetProperty(IdentityName).GetString()!);
        Require(Digest(identityPath) == admission.GetProperty(IdentitySha).GetString());
        using var document = JsonDocument.Parse(File.ReadAllBytes(identityPath));
        JsonElement identity = document.RootElement;
        Require(identity.GetProperty(PinField).GetString() == PinnedCommit
            && !identity.GetProperty(OutcomeRead).GetBoolean());
        string source = Child(original, identity.GetProperty(SourceCopy).GetString()!);
        Require(Digest(source) == identity.GetProperty(SourceSha).GetString());
        Require(Convert.ToHexString(SHA256.HashData(selected)) == identity.GetProperty(EffectiveSha).GetString());
        Require(identity.GetProperty(EmbeddingPolicy).GetString() == ActualEmbedded);
        Require(identity.GetProperty(GeneratorSha).GetString() == PinnedGeneratorSha);
        string binary = identity.GetProperty(BinaryPath).GetString()!;
        Require(Path.IsPathFullyQualified(binary) && File.Exists(binary)
            && Digest(binary) == identity.GetProperty(BinarySha).GetString());
        long offset = identity.GetProperty(EmbeddedOffset).GetInt64();
        Require(offset >= 0 && identity.GetProperty(EmbeddedLength).GetInt32() == selected.Length);
        using var input = File.OpenRead(binary);
        Require(offset <= input.Length - selected.Length);
        input.Position = offset;
        byte[] actual = new byte[selected.Length];
        input.ReadExactly(actual);
        Require(actual.AsSpan().SequenceEqual(selected));
        var profile = new RuntimeNativeMetadataImage(selected);
        string witnessPath = Child(original, identity.GetProperty(WitnessField).GetString()!);
        Term witness = ExternalTermFormat.Decode(File.ReadAllBytes(witnessPath));
        VerifyWitness(witness, profile.NativeMd5);

        return profile;
    }

    internal static void VerifyWitness(Term witness, BitString expectedMd5)
    {
        Require(witness is TupleTerm { Items.Count: WitnessArity });
        var tuple = (TupleTerm)witness;
        Require(tuple.Items[WitnessModuleIndex] is Atom { Name: WitnessModule });
        Require(tuple.Items[WitnessOwnerIndex] is Atom { Name: PreloadedOwner });
        Require(tuple.Items[WitnessMd5Index] is BitString md5 && md5.Equals(expectedMd5));
    }

    private static string Child(string parent, string name)
    {
        Require(Path.GetFileName(name) == name);

        return Path.Combine(parent, name);
    }

    private static string Digest(string path)
        => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));

    private static void Require(bool value)
    {
        if (!value)
            throw new InvalidDataException(Failure);
    }
}
