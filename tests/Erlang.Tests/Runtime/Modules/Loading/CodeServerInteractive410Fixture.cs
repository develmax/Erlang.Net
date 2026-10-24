using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Erlang.Tests;

internal static class CodeServerInteractive410Fixture
{
    internal static byte[] Resource(string name)
    {
        byte[] Read(string file)
        {
            using var input = typeof(CodeServerInteractive410Fixture).Assembly.GetManifestResourceStream(CodeServerInteractive410Names.Prefix + file)!;
            using var output = new MemoryStream();
            input.CopyTo(output);

            return output.ToArray();
        }
        var hashes = JsonSerializer.Deserialize<Dictionary<string, string>>(Read(CodeServerInteractive410Names.Manifest))!;
        byte[] bytes = Read(name);
        Require(hashes[name] == Convert.ToHexString(SHA256.HashData(bytes)));

        return bytes;
    }

    internal static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException(CodeServerInteractive410Names.Missing);
    }

    internal static Term Text(string value) => Cons.From(value.Select(c => (Term)Term.I(c)));

    internal static BitString[] Create(string output, bool app)
    {
        string fixtureRoot = System.IO.Path.Combine(output, CodeServerInteractive410Names.FixtureRoot);
        Directory.CreateDirectory(fixtureRoot);
        Directory.CreateDirectory(System.IO.Path.Combine(fixtureRoot, CodeServerInteractive410Names.Lib));
        if (app)
            Directory.CreateDirectory(System.IO.Path.Combine(
                fixtureRoot,
                CodeServerInteractive410Names.Lib,
                CodeServerInteractive410Names.AppDirectory,
                CodeServerInteractive410Names.Ebin
            ));
        string[] workers = [CodeServerInteractive410Names.Subject, CodeServerInteractive410Names.Observer];
        foreach (string worker in workers)
            File.WriteAllBytes(
                System.IO.Path.Combine(output, worker + CodeServerInteractive410Names.Beam),
                Resource(worker + CodeServerInteractive410Names.Beam)
            );

        Term A(string value) => Term.A(value);
        Term script = Term.Tuple(
            A(CodeServerInteractive410Names.Script),
            Term.Tuple(Text(CodeServerInteractive410Names.BootBase), Text(CodeServerInteractive410Names.Version)),
            Cons.From([
                Term.Tuple(A(CodeServerInteractive410Names.PreLoaded), Nil.Value),
                Term.Tuple(A(CodeServerInteractive410Names.Path), Cons.From([Text(output)])),
                Term.Tuple(A(CodeServerInteractive410Names.PrimLoad), Cons.From(workers.Select(A))),
                Term.Tuple(A(CodeServerInteractive410Names.LoadCompleted)),
                Term.Tuple(
                    A(CodeServerInteractive410Names.Apply),
                    Term.Tuple(A(CodeServerInteractive410Names.Loader),
                    A(CodeServerInteractive410Names.SetPath), Cons.From([Nil.Value]))
                ),
                Term.Tuple(
                    A(CodeServerInteractive410Names.Kernel),
                    A(CodeServerInteractive410Names.Owner),
                    Term.Tuple(A(CodeServerInteractive410Names.Subject), A(CodeServerInteractive410Names.StartLink), Cons.From([Text(fixtureRoot)]))
                ),
                Term.Tuple(A(CodeServerInteractive410Names.Progress), A(CodeServerInteractive410Names.Started))
            ])
        );
        string bootBase = System.IO.Path.Combine(output, CodeServerInteractive410Names.BootBase);
        File.WriteAllBytes(bootBase + CodeServerInteractive410Names.BootExtension, ExternalTermFormat.Encode(script));
        string[] arguments = [CodeServerInteractive410Names.RootOption, fixtureRoot,
            CodeServerInteractive410Names.BindirOption, output, CodeServerInteractive410Names.BootOption, bootBase,
            CodeServerInteractive410Names.ModeOption, CodeServerInteractive410Names.Interactive];

        return arguments.Select(value => new BitString(Encoding.UTF8.GetBytes(value))).ToArray();
    }
}
