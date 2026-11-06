using System.Text;
using Erlang;

internal static class GenServerCodeBoot
{
    internal static BitString[] Create(string repository, string directory)
    {
        string output = Path.GetFullPath(Path.Combine(directory, GenServerBootNames.OutputPrefix + Guid.NewGuid().ToString(GenServerBootNames.GuidFormat)));
        string root = Path.Combine(output, GenServerBootNames.RootDirectory);
        Directory.CreateDirectory(root);
        const string owner = GenServerBootNames.OwnerModule;
        File.Copy(
            Path.Combine(repository, GenServerBootNames.OwnerDirectory, owner + GenServerValidationPaths.BeamExtension),
            Path.Combine(output, owner + GenServerValidationPaths.BeamExtension)
        );

        Term A(string text) => Term.A(text);

        Term S(string text) => Term.String(text);
        Term script = Term.Tuple(
            A(GenServerBootNames.Script),
            Term.Tuple(S(GenServerBootNames.Name), S(GenServerBootNames.Version)),
            Term.List(
                Term.Tuple(A(GenServerBootNames.Preloaded), Nil.Value),
                Term.Tuple(A(GenServerBootNames.Path), Term.List(S(output))),
                Term.Tuple(A(GenServerBootNames.PrimitiveLoad), Term.List(A(owner))),
                Term.Tuple(A(GenServerBootNames.KernelLoaded)),
                Term.Tuple(
                    A(GenServerBootNames.Apply),
                    Term.Tuple(A(GenServerBootNames.LoaderModule), A(GenServerBootNames.SetPath), Term.List(Nil.Value))
                ),
                Term.Tuple(
                    A(GenServerBootNames.KernelProcess),
                    A(GenServerBootNames.OwnerProcess),
                    Term.Tuple(A(owner), A(GenServerBootNames.StartLink), Term.List(S(root)))
                ),
                Term.Tuple(A(GenServerBootNames.Progress), A(GenServerBootNames.Started))
            )
        );
        string boot = Path.Combine(output, GenServerBootNames.Name);
        File.WriteAllBytes(boot + GenServerBootNames.BootExtension, ExternalTermFormat.Encode(script));
        string[] arguments = [GenServerBootNames.RootOption, root, GenServerBootNames.BindirOption, output, GenServerBootNames.BootOption, boot, GenServerBootNames.ModeOption, GenServerBootNames.Embedded];

        return arguments.Select(value => new BitString(Encoding.UTF8.GetBytes(value))).ToArray();
    }
}
