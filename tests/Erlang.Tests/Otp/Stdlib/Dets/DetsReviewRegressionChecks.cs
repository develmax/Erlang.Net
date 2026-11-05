using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Security.Cryptography;
using System.Text.Json;
using Erlang;

namespace Erlang.Tests;

internal static class DetsReviewRegressionChecks
{
    internal const string Test = "runtime/dets/review-growth-and-fixed-collection";
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private const BindingFlags Static = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    private const string Failure = "DETS growth/scan review regression: ";
    private static readonly Assembly Assembly = typeof(DetsModule).Assembly;

    private static Type Type(string name) => Assembly.GetType("Erlang." + name, true)!;

    private static object? Call(object owner, string name, params object?[] args)
    {
        try
        {
            return owner.GetType().GetMethod(name, Members)!.Invoke(owner, args);
        }
        catch (TargetInvocationException error) when (error.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(error.InnerException).Throw();
            throw;
        }
    }

    private static void Require(bool condition, string what)
    {
        if (!condition) throw new InvalidOperationException(Failure + what);
    }

    private static object Create(Stream stream, string type)
    {
        object kind = Enum.Parse(Type("DetsFileType"), type);
        Type("DetsV9Writer").GetMethod("Write", Static)!.Invoke(null,
            [stream, Array.Empty<Term>(), kind, 1, (Func<Term, uint>)ErlangPortableHash.Phash2, 256, 33554432]);

        return Open(stream);
    }

    private static object Open(Stream stream) => Activator.CreateInstance(
        Type("DetsV9File"),
        Members,
        null,
        [stream, false, null, null],
        null
    )!;

    internal static Task Run()
    {
        OriginalReopenedFiles();
        foreach (string kind in new[] { "Set", "Bag", "DuplicateBag" })
        {
            using var source = new MemoryStream();
            object file = Create(source, kind);
            Term[] rows = Enumerable.Range(1, 700).Select(i => Term.Tuple(Term.I(i), new BitString(new byte[20]))).Cast<Term>().ToArray();
            _ = Call(
                file,
                "Insert",
                rows,
                false
            );
            Require((uint)file.GetType().GetProperty("Next", Members)!.GetValue(file)! > 256, "hash growth occurred");
            object header = Type("DetsV9Header").GetMethod(
                "Read",
                Static,
                null,
                [typeof(Stream)],
                null
            )!.Invoke(null, [source])!;
            Require((bool)header.GetType().GetProperty("DigestValid")!.GetValue(header)!, "dirty header checksum");
            try
            {
                Call(header, "Validate");
                Require(false, "dirty file accepted as closed");
            }
            catch (IOException error)
            {
                Require(error.Message == "not_closed", "normal repair admission");
            }
            object options = Type("DetsOpenOptions").GetMethod("Parse", Static)!.Invoke(null, [Term.A("repair"), Nil.Value])!;
            using var repaired = new MemoryStream();
            Type("DetsV9Recovery").GetMethod("Rewrite", Static)!.Invoke(null, [source, repaired, header, options, null]);
            var recovered = ((IEnumerable<Term>)Call(Open(repaired), "Objects")!).ToHashSet();
            Require(rows.All(recovered.Contains) && recovered.Count == rows.Length, "all objects survive unsaved growth repair");
        }
        using var fixedFile = new MemoryStream();
        object table = Create(fixedFile, "Set");
        Term before = Term.Tuple(Term.A("key"), new BitString(new byte[20]));
        _ = Call(
            table,
            "Insert",
            new Term[] { before },
            false
        );
        _ = Call(table, "SetFixed", true);
        var old = ((IReadOnlyList<(uint Slot, long Address, int Exponent, uint Size)>)Call(table, "Blocks")!)[0];
        Term after = Term.Tuple(Term.A("key"), new BitString(new byte[21]));
        _ = Call(
            table,
            "Insert",
            new Term[] { after },
            false
        );
        var current = ((IReadOnlyList<(uint Slot, long Address, int Exponent, uint Size)>)Call(table, "Blocks")!)[0];
        Require(old.Address == current.Address && old.Exponent == current.Exponent && old.Size < current.Size, "fixed collection grew in place");
        byte[] bytes = (byte[])Call(table, "ReadCollection", old.Address)!;
        Require(bytes.Length == current.Size, "resumed scan uses current collection length");
        object set = Enum.Parse(Type("DetsFileType"), "Set");
        var objects = (IReadOnlyList<Term>)Type("DetsV9Recovery").GetMethod("DecodeCollection", Static)!.Invoke(null, [bytes, set, 1, null, null])!;
        Require(objects.Count == 1 && objects[0].Equals(after), "fixed scan sees valid updated collection");
        _ = Call(table, "SetFixed", false);
        _ = Call(table, "Save", true);
        Require(((IEnumerable<Term>)Call(Open(fixedFile), "Objects")!).Single().Equals(after), "scan did not corrupt table");
        FreedCollectionDuringFixedScan();

        return Task.CompletedTask;
    }

    // dets_v9:scan_skip/8 accepts ?FREE blocks: an object deleted after a
    // fixed scan started is still read instead of failing the whole table.
    private static void FreedCollectionDuringFixedScan()
    {
        using var stream = new MemoryStream();
        object table = Create(stream, "Set");
        Term row = Term.Tuple(Term.A("deleted"), new BitString(new byte[20]));
        _ = Call(
            table,
            "Insert",
            new Term[] { row },
            false
        );
        _ = Call(table, "SetFixed", true);
        var block = ((IReadOnlyList<(uint Slot, long Address, int Exponent, uint Size)>)Call(table, "Blocks")!)[0];
        _ = Call(
            table,
            "Delete",
            Term.A("deleted"),
            null
        );
        byte[]? bytes = (byte[]?)Call(table, "ReadCollection", block.Address);
        Require(bytes is not null, "freed collection remains readable during a fixed scan");
        object set = Enum.Parse(Type("DetsFileType"), "Set");
        var objects = (IReadOnlyList<Term>)Type("DetsV9Recovery").GetMethod("DecodeCollection", Static)!.Invoke(null, [bytes, set, 1, null, null])!;
        Require(objects.Count == 1 && objects[0].Equals(row), "freed collection keeps the deleted object");
    }

    private static void OriginalReopenedFiles()
    {
        DirectoryInfo? root = new(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "reference-baseline.json"))) root = root.Parent;
        Require(root is not null, "repository for native fixtures");
        string directory = Path.Combine(root!.FullName, "tests/Erlang.Tests/Resources/DetsReopened");
        using JsonDocument provenance = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(directory, "provenance.json")));
        Require(
            provenance.RootElement.GetProperty("SourceCommit").GetString() == "ad05823719d77c8faee87348ea39513d4e2f99c5",
            "native fixture pin"
        );
        foreach (JsonElement item in provenance.RootElement.GetProperty("Files").EnumerateArray())
        {
            string name = item.GetProperty("Path").GetString()!;
            byte[] bytes = File.ReadAllBytes(Path.Combine(directory, name));
            Require(Convert.ToHexString(SHA256.HashData(bytes)) == item.GetProperty("Sha256").GetString(), "native fixture identity " + name);
            if (!name.EndsWith(".dets", StringComparison.Ordinal)) continue;
            using var stream = new MemoryStream();
            stream.Write(bytes);
            object table = Open(stream);
            int count = name == "set.dets" ? 1199 : 1200;
            Require(((IEnumerable<Term>)Call(table, "Objects")!).Count() == count, "native reopened free-list read " + name);
            Require(
                ((IReadOnlyList<Term>)Call(table, "Lookup", Term.I(500))!).Contains(Term.Tuple(Term.I(500), Term.I(99))),
                "native replacement " + name
            );
            Term key = Term.A("large_foreign");
            _ = Call(
                table,
                "Insert",
                new Term[] { Term.Tuple(key, new BitString(new byte[10000])) },
                false
            );
            _ = Call(table, "Save", true);
            table = Open(stream);
            Require(((IEnumerable<Term>)Call(table, "Objects")!).Count() == count + 1, "native allocation after reopen " + name);
            _ = Call(
                table,
                "Delete",
                key,
                null
            );
            _ = Call(table, "Save", true);
            Require(((IEnumerable<Term>)Call(Open(stream), "Objects")!).Count() == count, "native large-block release " + name);
        }
    }
}
