using System.Text;

namespace Erlang.Differential;

public static class OracleStandardStreamProtocol
{
    public static (string OutputHex, string ErrorHex) Capture(OracleProcessOutput captured)
    {
        byte[] marker = Encoding.ASCII.GetBytes(OracleResultProtocol.Begin);
        int markerOffset = captured.StandardOutputBytes.Span.IndexOf(marker);
        if (markerOffset < 1 || captured.StandardOutputBytes.Span[markerOffset - 1] != (byte)'\n')
            throw new InvalidOperationException(OracleDiagnostics.InvalidResultFrame);

        return (Convert.ToHexString(captured.StandardOutputBytes.Span[..(markerOffset - 1)]), Convert.ToHexString(captured.StandardErrorBytes.Span));
    }
}
