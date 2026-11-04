using System.Reflection;
using System.Runtime.ExceptionServices;
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

        return Task.CompletedTask;
    }
}
