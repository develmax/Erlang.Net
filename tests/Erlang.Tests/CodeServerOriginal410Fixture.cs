using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Erlang.Tests;

internal static class CodeServerOriginal410Fixture
{
    internal static byte[] Resource(string name)
    {
        byte[] Read(string file)
        {
            using var input = typeof(CodeServerOriginal410Fixture).Assembly.GetManifestResourceStream(CodeServerOriginal410Names.Prefix + file)!;
            using var output = new MemoryStream();
            input.CopyTo(output);

            return output.ToArray();
        }
        var hashes = JsonSerializer.Deserialize<Dictionary<string, string>>(Read(CodeServerOriginal410Names.Manifest))!;
        byte[] bytes = Read(name);
        Require(hashes[name] == Convert.ToHexString(SHA256.HashData(bytes)));

        return bytes;
    }

    internal static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException(CodeServerOriginal410Names.Missing);
    }

    internal static Term Text(string value) => Cons.From(value.Select(c => (Term)Term.I(c)));

    internal static BitString[] Create(string output)
    {
        string fixtureRoot = System.IO.Path.Combine(output, CodeServerOriginal410Names.FixtureRoot);
        Directory.CreateDirectory(fixtureRoot);
        string[] workers = [CodeServerOriginal410Names.Subject, CodeServerOriginal410Names.Observer];
        foreach (string worker in workers)
            File.WriteAllBytes(System.IO.Path.Combine(output, worker + CodeServerOriginal410Names.Beam), Resource(worker + CodeServerOriginal410Names.Beam));

        Term A(string value) => Term.A(value);
        Term script = Term.Tuple(
            A(CodeServerOriginal410Names.Script),
            Term.Tuple(Text(CodeServerOriginal410Names.BootBase), Text(CodeServerOriginal410Names.Version)),
            Cons.From([
                Term.Tuple(A(CodeServerOriginal410Names.PreLoaded), Nil.Value),
                Term.Tuple(A(CodeServerOriginal410Names.Path), Cons.From([Text(output)])),
                Term.Tuple(A(CodeServerOriginal410Names.PrimLoad), Cons.From(workers.Select(A))),
                Term.Tuple(A(CodeServerOriginal410Names.LoadCompleted)),
                Term.Tuple(
                    A(CodeServerOriginal410Names.Apply),
                    Term.Tuple(A(CodeServerOriginal410Names.Loader),
                    A(CodeServerOriginal410Names.SetPath), Cons.From([Nil.Value]))
                ),
                Term.Tuple(
                    A(CodeServerOriginal410Names.Kernel),
                    A(CodeServerOriginal410Names.Owner),
                    Term.Tuple(A(CodeServerOriginal410Names.Subject), A(CodeServerOriginal410Names.StartLink), Cons.From([Text(fixtureRoot)]))
                ),
                Term.Tuple(A(CodeServerOriginal410Names.Progress), A(CodeServerOriginal410Names.Started))
            ])
        );
        string bootBase = System.IO.Path.Combine(output, CodeServerOriginal410Names.BootBase);
        File.WriteAllBytes(bootBase + CodeServerOriginal410Names.BootExtension, ExternalTermFormat.Encode(script));
        string[] arguments = [CodeServerOriginal410Names.RootOption, fixtureRoot,
            CodeServerOriginal410Names.BindirOption, output, CodeServerOriginal410Names.BootOption, bootBase,
            CodeServerOriginal410Names.ModeOption, CodeServerOriginal410Names.Embedded];

        return arguments.Select(value => new BitString(Encoding.UTF8.GetBytes(value))).ToArray();
    }
}
