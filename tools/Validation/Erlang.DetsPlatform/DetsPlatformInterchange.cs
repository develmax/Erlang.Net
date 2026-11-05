using System.Security.Cryptography;
using System.Text.Json;
using Erlang;

internal static class DetsPlatformInterchange
{
    internal static void Verify(string output)
    {
        var files = new List<object>();
        foreach (string kind in new[] { "set", "bag", "duplicate_bag" })
        {
            string path = Path.Combine(output, "original-" + kind + ".dets");
            using var stream = File.OpenRead(path);
            var file = new ReflectedDetsFile(stream);
            if (!file.Lookup(Term.I(500)).Contains(Term.Tuple(Term.I(500), Term.I(99)))
                || file.Lookup(Term.I(1)).Length != 0
                || file.Objects().Length != (kind == "set" ? 1199 : 1200))
                throw new InvalidOperationException("C# reader differs from original Erlang DETS file: " + kind);
            stream.Position = 0;
            files.Add(new { Type = kind, Sha256 = Convert.ToHexString(SHA256.HashData(stream)), Passed = true });
        }
        File.WriteAllText(
            Path.Combine(output, "original-to-managed.json"),
            JsonSerializer.Serialize(files, new JsonSerializerOptions { WriteIndented = true })
        );
        Console.WriteLine("PASS C# reads all three original Erlang DETS file types");
    }
}
