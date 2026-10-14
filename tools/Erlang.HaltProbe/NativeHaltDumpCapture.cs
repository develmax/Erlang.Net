using System.Security.Cryptography;
using System.Text;

namespace Erlang.HaltProbe;

internal static class NativeHaltDumpCapture
{
    public static async Task<NativeHaltDumpEvidence> Read(string directory)
    {
        string path = Path.Combine(directory, NativeHaltTrackNames.DumpFile);
        if (!File.Exists(path))
            return new(
                path,
                false,
                null,
                null,
                Array.Empty<byte>(),
                false,
                null
            );
        try
        {
            await using var stream = new FileStream(
                path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                4096,
                FileOptions.Asynchronous | FileOptions.SequentialScan
            );
            long length = stream.Length;
            byte[] prefix = new byte[(int)Math.Min(length, NativeHaltTrackNames.DumpPrefixLength)];
            await stream.ReadExactlyAsync(prefix);
            stream.Position = 0;
            string hash = Convert.ToHexString(await SHA256.HashDataAsync(stream));
            bool signature = prefix.AsSpan().StartsWith(Encoding.ASCII.GetBytes(NativeHaltTrackNames.DumpSignature));

            return new(
                path,
                true,
                length,
                hash,
                Array.AsReadOnly(prefix),
                signature,
                null
            );
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return new(
                path,
                true,
                null,
                null,
                Array.Empty<byte>(),
                false,
                exception.ToString()
            );
        }
    }
}
