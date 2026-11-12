using System.Text;
using Erlang;

internal static class GenStatemCodeBoot
{
    internal static BitString[] Create(string repository, string directory)
    {
        string output = Path.GetFullPath(Path.Combine(directory, "code-boot-" + Guid.NewGuid().ToString("N")));
        string root = Path.Combine(output, "root");
        Directory.CreateDirectory(root);
        const string owner = "beam_code_server_subject410";
        File.Copy(
            Path.Combine(repository, "tests/Erlang.Tests/Resources/CodeServerOriginal410", owner + ".beam"),
            Path.Combine(output, owner + ".beam")
        );

        Term A(string text) => Term.A(text);

        Term S(string text) => Term.String(text);
        Term script = Term.Tuple(
            A("script"),
            Term.Tuple(S("gen-statem-code-server"), S("1")),
            Term.List(
                Term.Tuple(A("preLoaded"), Nil.Value),
                Term.Tuple(A("path"), Term.List(S(output))),
                Term.Tuple(A("primLoad"), Term.List(A(owner))),
                Term.Tuple(A("kernel_load_completed")),
                Term.Tuple(A("apply"), Term.Tuple(A("erl_prim_loader"), A("set_path"), Term.List(Nil.Value))),
                Term.Tuple(A("kernelProcess"), A("statem_code_owner"), Term.Tuple(A(owner), A("start_link"), Term.List(S(root)))),
                Term.Tuple(A("progress"), A("started"))
            )
        );
        string boot = Path.Combine(output, "gen-statem-code-server");
        File.WriteAllBytes(boot + ".boot", ExternalTermFormat.Encode(script));
        string[] arguments = ["-root", root, "-bindir", output, "-boot", boot, "-mode", "embedded"];

        return arguments.Select(value => new BitString(Encoding.UTF8.GetBytes(value))).ToArray();
    }
}
