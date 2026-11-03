using System.Text;
using System.Text.Json;
using static Erlang.Tests.DynamicApplyBoundary520Names;

namespace Erlang.Tests;

internal static class DynamicApplySourceAdmission520Checks
{
    internal static Task DeclaredLineEndings()
    {
        string path = Path.GetTempFileName();
        try
        {
            JsonElement text = JsonSerializer.SerializeToElement(new { HashMode = TextLfMode });
            JsonElement raw = JsonSerializer.SerializeToElement(new { HashMode = RawMode });
            File.WriteAllBytes(path, Encoding.UTF8.GetBytes(SourceFixtureLf));
            string textLf = DynamicApplyBoundary520Checks.SourceDigest(path, text);
            string rawLf = DynamicApplyBoundary520Checks.SourceDigest(path, raw);
            File.WriteAllBytes(path, Encoding.UTF8.GetBytes(SourceFixtureLf.Replace("\n", "\r\n", StringComparison.Ordinal)));
            if (textLf != DynamicApplyBoundary520Checks.SourceDigest(path, text)
                || rawLf == DynamicApplyBoundary520Checks.SourceDigest(path, raw))
                throw new InvalidOperationException(Failure);
            bool refused = false;
            try
            {
                _ = DynamicApplyBoundary520Checks.SourceDigest(path, JsonSerializer.SerializeToElement(new { HashMode = UnknownHashMode }));
            }
            catch (InvalidOperationException)
            {
                refused = true;
            }
            if (!refused)
                throw new InvalidOperationException(Failure);

            return Task.CompletedTask;
        }
        finally
        {
            File.Delete(path);
        }
    }
}
