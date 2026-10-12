using System.Text;
using System.Text.Json;
using Erlang.Differential;

namespace Erlang.Tests;

public static class StandardStreamProtocolChecks
{
    public static Task Run()
    {
        foreach (var payload in new byte[][] { [], [13], [10], [255, 0, 195, 40] })
        {
            byte[] marker = Encoding.ASCII.GetBytes("\n" + OracleResultProtocol.Begin);
            var original = new OracleProcessOutput(Encoding.UTF8.GetString(payload), string.Empty, 0)
            {
                StandardOutputBytes = payload.Concat(marker).ToArray(),
                StandardErrorBytes = new byte[] { 255, 0 }
            };
            var restored = JsonSerializer.Deserialize<OracleProcessOutput>(JsonSerializer.Serialize(original))!;
            var bytes = OracleStandardStreamProtocol.Capture(restored);
            if (bytes.OutputHex != Convert.ToHexString(payload) || bytes.ErrorHex != "FF00")
                throw new InvalidOperationException(StandardStreamHostNames.Broken);
        }

        return Task.CompletedTask;
    }
}
