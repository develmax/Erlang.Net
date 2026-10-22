using System.Security.Cryptography;
using Erlang;

namespace Erlang.Tests;

internal static class ArtifactInspectorHostAssets
{
    internal static byte[] Read(string name, string sha)
    {
        using Stream stream = typeof(ArtifactInspectorHostAssets).Assembly.GetManifestResourceStream(name)
            ?? throw new InvalidDataException(ArtifactInspectorHostNames.Failure);
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        byte[] bytes = memory.ToArray();
        Require(Hash(bytes) == sha);

        return bytes;
    }

    internal static string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes));

    internal static byte[] Export(string module, string function, int arity) => BeamLiteralExportHostBytes.Export(
        Term.A(module),
        Term.A(function),
        Term.I(arity)
    );

    internal static void InvalidImage(Action action)
    {
        try
        {
            action();
        }
        catch (InvalidDataException)
        {
            return;
        }
        throw new InvalidOperationException(ArtifactInspectorHostNames.Failure);
    }

    internal static void InvalidEtf(Action action)
    {
        try
        {
            action();
        }
        catch (ErlangException error) when (error.Reason.Equals(Term.A(ErlangErrorReasons.BadArgument)))
        {
            return;
        }
        throw new InvalidOperationException(ArtifactInspectorHostNames.Failure);
    }

    internal static void Require(bool condition)
    {
        if (!condition) throw new InvalidDataException(ArtifactInspectorHostNames.Failure);
    }
}
