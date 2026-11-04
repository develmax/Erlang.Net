using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Erlang;

namespace Erlang.Tests;

internal static class PrimFileDeletePlatformChecks
{
    private static object? Call(string type, string method, params object[] arguments)
    {
        var owner = typeof(ProcessRuntime).Assembly.GetType(PrimFileDeleteContractNames.Prefix + type, true)!;
        try
        {
            return owner.GetMethod(method, BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, arguments);
        }
        catch (TargetInvocationException error) when (error.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(error.InnerException).Throw();
            throw;
        }
    }

    private static void Require(bool value, string operation)
    {
        if (!value)
        {
            throw new InvalidOperationException(PrimFileDeleteContractNames.Failure + operation);
        }
    }

    private static BitString Native(string path, int zeros = 1)
    {
        byte[] bytes = (OperatingSystem.IsWindows() ? Encoding.Unicode : Encoding.UTF8).GetBytes(path);
        int terminator = OperatingSystem.IsWindows() ? sizeof(char) : sizeof(byte);

        return new BitString(bytes.Concat(new byte[zeros * terminator]).ToArray());
    }

    private static Term Delete(Term path, CancellationToken cancellation = default)
        => (Term)Call(
            PrimFileDeleteContractNames.DeleteType,
            PrimFileDeleteContractNames.Delete,
            path,
            cancellation
        )!;

    private static Term Error(string reason) => Term.Tuple(Term.A("error"), Term.A(reason));

    internal static Task Mapping()
    {
        foreach (var (number, linux, darwin) in new (int, string, string)[]
        {
            (1,"eperm","eperm"), (2,"enoent","enoent"), (11,"eagain","edeadlk"),
            (21,"eperm","eperm"), (35,"edeadlk","eagain"), (36,"enametoolong","einprogress"),
            (40,"eloop","emsgsize"), (62,"etime","eloop"), (63,"enosr","enametoolong"),
            (69,"esrmnt","edquot"), (70,"ecomm","estale"), (84,"eilseq","eoverflow"),
            (95,"enotsup","emultihop"), (0,"errno_0","errno_0"),
            (PrimFileDeleteContractNames.UnknownErrno,"errno_10000","errno_10000")
        })
        {
            Require(
                (string)Call(
                    PrimFileDeleteContractNames.ErrorType,
                    PrimFileDeleteContractNames.Error,
                    number,
                    false
                )! == linux,
                "Linux errno " + number
            );
            Require(
                (string)Call(
                    PrimFileDeleteContractNames.ErrorType,
                    PrimFileDeleteContractNames.Error,
                    number,
                    true
                )! == darwin,
                "Darwin errno " + number
            );
        }
        byte[] raw = [0xff, 0xfe, (byte)'x', 0, 0];
        Require(
            ((byte[])Call(PrimFileDeleteContractNames.UnixPathType, PrimFileDeleteContractNames.Marshal, new BitString(raw))!).SequenceEqual(raw),
            "raw Unix byte preservation"
        );
        foreach (Term malformed in new Term[] { Nil.Value, Term.A("path"), new BitString([]), new BitString([1]), new BitString([1, 0, 2]), new BitString([0], 3) })
        {
            Require(
                Call(PrimFileDeleteContractNames.UnixPathType, PrimFileDeleteContractNames.Marshal, malformed) is null,
                "invalid Unix native path"
            );
        }
        Require(
            ((byte[])Call(PrimFileDeleteContractNames.UnixPathType, PrimFileDeleteContractNames.Marshal, new BitString([0, 0]))!).Length == 2,
            "empty terminated path"
        );
        Require(Delete(Term.A("not_binary")).Equals(Error("einval")), "NIF invalid argument");
        using var canceled = new CancellationTokenSource();
        canceled.Cancel();
        try
        {
            _ = Delete(Term.A("not_binary"), canceled.Token);
            Require(false, "cancellation admitted");
        }
        catch (OperationCanceledException) { }

        return Task.CompletedTask;
    }

    private static string Repository()
    {
        for (DirectoryInfo? directory = new(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "reference-baseline.json")))
            {
                return directory.FullName;
            }
        }
        throw new DirectoryNotFoundException(PrimFileDeleteContractNames.Failure);
    }

    internal static async Task Host()
    {
        string root = Repository();
        string output = Path.Combine(root, PrimFileDeleteContractNames.Output, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(output);
        var rows = new List<Term>();

        void Observe(string label, Term result) => rows.Add(Term.Tuple(Term.A(label), result));
        string existing = Path.Combine(output, "existing");
        string folder = Path.Combine(output, "folder");
        string unicode = Path.Combine(output, "Ж日");
        string readOnly = Path.Combine(output, "readonly");
        File.WriteAllBytes(existing, [1, 2, 3]);
        Directory.CreateDirectory(folder);
        File.WriteAllBytes(unicode, [4]);
        File.WriteAllBytes(readOnly, [5]);
        if (OperatingSystem.IsWindows())
        {
            File.SetAttributes(readOnly, FileAttributes.ReadOnly);
        }
        else
        {
            File.SetUnixFileMode(readOnly, UnixFileMode.UserRead | UnixFileMode.GroupRead | UnixFileMode.OtherRead);
        }
        Observe("existing", Delete(Native(existing)));
        Observe("second_delete", Delete(Native(existing)));
        Observe("missing", Delete(Native(Path.Combine(output, "missing"))));
        Observe("directory", Delete(Native(folder)));
        Observe("unicode", Delete(Native(unicode)));
        Observe("readonly", Delete(Native(readOnly)));
        Require(!File.Exists(existing) && !File.Exists(unicode) && Directory.Exists(folder), "deletion effects");
        if (OperatingSystem.IsWindows())
        {
            File.SetAttributes(readOnly, FileAttributes.Normal);
            File.Delete(readOnly);
        }
        Directory.Delete(folder);
        Term actual = Cons.From(rows);
        string capture = Path.Combine(root, PrimFileDeleteContractNames.Capture);
        using JsonDocument provenance = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(capture, "provenance.json")));
        foreach (var (property, name) in new[] { ("ObserverSourceSha256", "prim_file_delete_cases.erl"), ("CorpusSha256", "original.etf"), ("DeleteRacesSha256", "deletion-races.etf") })
        {
            Require(
                Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(capture, name)))) == provenance.RootElement.GetProperty(property).GetString(),
                "fixture hash " + name
            );
        }
        Term expected = OperatingSystem.IsWindows()
            ? ExternalTermFormat.Decode(File.ReadAllBytes(Path.Combine(capture, "original.etf")))
            : Cons.From(new Term[]
            {
                Term.Tuple(Term.A("existing"),Term.A("ok")), Term.Tuple(Term.A("second_delete"),Error("enoent")),
                Term.Tuple(Term.A("missing"),Error("enoent")), Term.Tuple(Term.A("directory"),Error("eperm")),
                Term.Tuple(Term.A("unicode"),Term.A("ok")), Term.Tuple(Term.A("readonly"),Term.A("ok"))
            });
        File.WriteAllBytes(Path.Combine(output, "actual.etf"), ExternalTermFormat.Encode(actual));
        Require(actual.Equals(expected), "native deletion outcomes " + output);

        Term CanonicalPair(IEnumerable<Term> values) => Cons.From(values.OrderBy(value => value, Comparer<Term>.Create((left, right) => TermOrder.Compare(left, right))));
        var oraclePairs = Cons.Items(ExternalTermFormat.Decode(File.ReadAllBytes(Path.Combine(capture, "deletion-races.etf"))))
            .Select(pair => CanonicalPair(Cons.Items(pair))).ToHashSet();
        if (!OperatingSystem.IsWindows()) oraclePairs = await PrimFileDeleteNativeOracle.Capture(root, output);
        var races = new List<Term>();
        for (int round = 0; round < PrimFileDeleteContractNames.RaceRounds; round++)
        {
            string path = Path.Combine(output, "race-" + round);
            File.WriteAllBytes(path, [1]);
            Term[] results = await Task.WhenAll(Task.Run(() => Delete(Native(path))), Task.Run(() => Delete(Native(path))));
            races.Add(Cons.From(results));
            File.WriteAllBytes(Path.Combine(output, "race-actual.etf"), ExternalTermFormat.Encode(Cons.From(races)));
            bool admitted = oraclePairs.Contains(CanonicalPair(results));
            Require(admitted && !File.Exists(path), "native concurrent deletion outcomes: " + Cons.From(results));
        }
        string open = Path.Combine(output, "open");
        File.WriteAllBytes(open, [7]);
        using (var stream = new FileStream(
            open,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite | FileShare.Delete
        ))
        {
            Require(Delete(Native(open, 2)).Equals(Term.A("ok")), "open file and redundant native NULs");
        }
        string blocker = Path.Combine(output, "blocker");
        File.WriteAllBytes(blocker, [8]);
        Require(
            Delete(Native(Path.Combine(blocker, "child"))).Equals(Error(OperatingSystem.IsWindows() ? "enoent" : "enotdir")),
            "non-directory ancestor"
        );
        File.Delete(blocker);
        if (!OperatingSystem.IsWindows())
        {
            string target = Path.Combine(output, "target");
            string link = Path.Combine(output, "link");
            File.WriteAllBytes(target, [9]);
            File.CreateSymbolicLink(link, target);
            Require(Delete(Native(link)).Equals(Term.A("ok")) && File.Exists(target), "unlink preserves symlink target");
            File.CreateSymbolicLink(link, Path.Combine(output, "absent-target"));
            Require(Delete(Native(link)).Equals(Term.A("ok")), "unlink dangling symlink");
            File.Delete(target);
        }
        File.WriteAllText(
            Path.Combine(output, "receipt.json"),
            JsonSerializer.Serialize(
                new
                {
                    OperatingSystem = System.Runtime.InteropServices.RuntimeInformation.OSDescription,
                    Equal = true,
                    Oracle = OperatingSystem.IsWindows() ? "Pinned OTP Windows capture" : "Pinned Unix source contracts and same-host native C unlink races; whole native OTP execution not claimed",
                    RaceRounds = PrimFileDeleteContractNames.RaceRounds,
                    RuntimeSha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(typeof(ProcessRuntime).Assembly.Location)))
                },
                new JsonSerializerOptions { WriteIndented = true }
            )
        );
    }
}
