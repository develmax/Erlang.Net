using System.Buffers.Binary;
using System.Runtime.InteropServices;

namespace Erlang.Compiler;

internal static class BeamOtpEtfCompression
{
    private const string Library = "Erlang.Beam.Zlib";
    private const int DocumentationLevel = 6;
    private const int CompressedHeaderSize = 6;
    private const int VersionSize = 1;
    private const byte CompressedTag = 80;
    private const int BufferError = -5;
    private const string Failed = "Pinned OTP zlib could not compress the BEAM documentation payload.";

    internal static bool HasPinnedCompressor => OperatingSystem.IsWindows() && RuntimeInformation.ProcessArchitecture == Architecture.X64;

    internal static byte[] Encode(Term document, bool debugInfo = false)
    {
        var options = new EtfEncodingOptions
        {
            Deterministic = true,
            MaximumNestingDepth = debugInfo ? EtfResourceLimits.CompilerDebugNestingDepth : EtfResourceLimits.MaximumNestingDepth
        };
        if (!HasPinnedCompressor)
        {
            return ExternalTermFormat.Encode(document, options with { CompressionLevel = DocumentationLevel });
        }

        byte[] plain = ExternalTermFormat.Encode(document, options);
        if (plain.Length <= CompressedHeaderSize)
        {
            return plain;
        }

        byte[] body = plain.AsSpan(VersionSize).ToArray();
        uint compressedSize = checked((uint)(plain.Length - CompressedHeaderSize));
        byte[] compressed = new byte[compressedSize];
        int status = Compress(body, (uint)body.Length, compressed, ref compressedSize);
        if (status == BufferError)
        {
            return plain;
        }
        if (status != 0 || compressedSize > compressed.Length)
        {
            throw new InvalidOperationException(Failed);
        }

        byte[] result = new byte[CompressedHeaderSize + compressedSize];
        result[0] = plain[0];
        result[1] = CompressedTag;
        BinaryPrimitives.WriteInt32BigEndian(result.AsSpan(2), body.Length);
        compressed.AsSpan(0, (int)compressedSize).CopyTo(result.AsSpan(CompressedHeaderSize));

        return result;
    }

    [DllImport(Library, EntryPoint = "erlang_beam_zlib_compress", CallingConvention = CallingConvention.Cdecl)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.AssemblyDirectory)]
    private static extern int Compress(
        [In] byte[] source,
        uint sourceSize,
        [Out] byte[] destination,
        ref uint destinationSize
    );
}
