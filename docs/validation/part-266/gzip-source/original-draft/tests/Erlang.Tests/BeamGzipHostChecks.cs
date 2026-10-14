using System.Reflection;
using System.IO.Compression;

namespace Erlang.Tests;

public static class BeamGzipHostChecks
{
    public static void Run()
    {
        byte[] raw = [70, 79, 82, 49, 1, 2, 3];
        byte[] gzip = Compress(raw);
        Term Binary(byte[] value) => new BitString(value);
        Require(Inflate(Binary(gzip)).Equals(Binary(raw)));
        Term nested = Term.List(Term.List(gzip.Take(3).Select(value => Term.I(value)).ToArray()), Binary(gzip[3..]));
        Require(Inflate(nested).Equals(Binary(raw)));
        Term improper = new Cons(Term.I(gzip[0]), Binary(gzip[1..]));
        Require(Inflate(improper).Equals(Binary(raw)));
        Term rawBinary = Binary(raw);
        Require(ReferenceEquals(Inflate(rawBinary), rawBinary));
        Term rawList = Cons.From(raw.Select(value => Term.I(value)));
        Require(ReferenceEquals(Inflate(rawList), rawList));
        Require(Inflate(Binary(gzip.Concat(Compress([])).ToArray())).Equals(Binary(raw)));
        Require(Inflate(Binary(Compress([]).Concat(gzip).ToArray())).Equals(Binary(raw)));
        foreach (byte[] truncated in new[] { gzip[..BeamGzipHostNames.HeaderBytes], gzip[..^1], gzip[..^BeamGzipHostNames.TrailerBytes] })
            Fallback(Binary(truncated));
        Fallback(Binary(gzip.Concat(new byte[] { 0 }).ToArray()));
        byte[] crc = gzip.ToArray();
        crc[^BeamGzipHostNames.TrailerBytes] ^= BeamGzipHostNames.BadChecksumDelta;
        Fallback(Binary(crc));
        byte[] size = gzip.ToArray();
        size[^BeamGzipHostNames.IsizeOffsetFromEnd] ^= BeamGzipHostNames.BadChecksumDelta;
        Fallback(Binary(size));
        byte[] flags = gzip.ToArray();
        flags[BeamGzipHostNames.HeaderFlagsOffset] |= BeamGzipHostNames.ReservedFlag;
        Fallback(Binary(flags));
        Fallback(Nil.Value);
        Fallback(new Cons(Term.I(256), Binary(gzip)));
        Fallback(new BitString([0], 1));
    }

    private static Term Inflate(Term input)
    {
        MethodInfo method = typeof(ModuleRegistry).Assembly.GetType(BeamGzipHostNames.Type)!.GetMethod(BeamGzipHostNames.Method, BindingFlags.Static | BindingFlags.NonPublic)!;

        return (Term)method.Invoke(null, [input])!;
    }

    private static void Fallback(Term input)
        => Require(ReferenceEquals(Inflate(input), input));

    private static byte[] Compress(byte[] raw)
    {
        using var output = new MemoryStream();
        using (var gzip = new GZipStream(output, CompressionMode.Compress, true))
            gzip.Write(raw);

        return output.ToArray();
    }

    private static void Require(bool value)
    {
        if (!value)
            throw new InvalidOperationException(BeamGzipHostNames.Failure);
    }
}
