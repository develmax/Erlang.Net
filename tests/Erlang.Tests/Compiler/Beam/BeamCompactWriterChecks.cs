using System.Globalization;
using System.Numerics;
using Erlang.Compiler;

namespace Erlang.Tests;

internal static class BeamCompactWriterChecks
{
    internal const string TestName = "compiler/beam/compact-operands-match-otp";

    internal static Task Run()
    {
        // Captured from iolist_to_binary([beam_asm:encode(Tag, Value)]) on OTP-29.1.1.
        const string vectors = """
            0 0 00
            1 15 F1
            2 16 0A10
            3 255 0BFF
            4 256 2C00
            5 2047 EDFF
            0 2048 180800
            1 32767 197FFF
            1 32768 39008000
            0 128 0880
            1 -1 19FFFF
            1 -128 19FF80
            1 -32768 198000
            1 -32769 39FF7FFF
            1 -65535 39FF0001
            1 -65536 39FF0000
            1 9223372036854775807 D97FFFFFFFFFFFFFFF
            1 9223372036854775808 F900008000000000000000
            1 -9223372036854775808 D98000000000000000
            1 -9223372036854775809 F900FF7FFFFFFFFFFFFFFF
            1 6277101735386680763835789423207666416102355444464034512896 F9081001000000000000000000000000000000000000000000000000
            1 -6277101735386680763835789423207666416102355444464034512896 F90810FF000000000000000000000000000000000000000000000000
            """;

        foreach (string row in vectors.Split('\n'))
        {
            string[] fields = row.TrimEnd('\r').Split(' ');
            int tag = int.Parse(fields[0], CultureInfo.InvariantCulture);
            BigInteger value = BigInteger.Parse(fields[1], CultureInfo.InvariantCulture);
            using var output = new MemoryStream();
            BeamCompactWriter.Write(output, tag, value);
            string actual = Convert.ToHexString(output.ToArray());
            if (actual != fields[2])
            {
                throw new InvalidOperationException(FormattableString.Invariant($"BEAM operand {tag}/{value}: expected {fields[2]}, got {actual}."));
            }
        }

        return Task.CompletedTask;
    }
}
