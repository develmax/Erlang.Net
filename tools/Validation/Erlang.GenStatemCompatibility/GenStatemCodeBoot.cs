using System.Text;
using Erlang;

internal static class GenStatemCodeBoot
{
    internal static BitString[] Create(string repository, string directory)
    {
        string output = Path.GetFullPath(Path.Combine(directory, GenStatemBootNames.OutputPrefix + Guid.NewGuid().ToString(GenStatemBootNames.GuidFormat)));
        string root = Path.Combine(output, GenStatemBootNames.RootDirectory);
        Directory.CreateDirectory(root);
        const string owner = GenStatemBootNames.OwnerModule;
        File.Copy(
            Path.Combine(repository, GenStatemBootNames.OwnerDirectory, owner + GenStatemValidationPaths.BeamExtension),
            Path.Combine(output, owner + GenStatemValidationPaths.BeamExtension)
        );

        Term A(string text) => Term.A(text);

        Term S(string text) => Term.String(text);
        Term script = Term.Tuple(
            A(GenStatemBootNames.Script),
            Term.Tuple(S(GenStatemBootNames.Name), S(GenStatemBootNames.Version)),
            Term.List(
                Term.Tuple(A(GenStatemBootNames.Preloaded), Nil.Value),
                Term.Tuple(A(GenStatemBootNames.Path), Term.List(S(output))),
                Term.Tuple(A(GenStatemBootNames.PrimitiveLoad), Term.List(A(owner))),
                Term.Tuple(A(GenStatemBootNames.KernelLoaded)),
                Term.Tuple(
                    A(GenStatemBootNames.Apply),
                    Term.Tuple(A(GenStatemBootNames.LoaderModule), A(GenStatemBootNames.SetPath), Term.List(Nil.Value))
                ),
                Term.Tuple(
                    A(GenStatemBootNames.KernelProcess),
                    A(GenStatemBootNames.OwnerProcess),
                    Term.Tuple(A(owner), A(GenStatemBootNames.StartLink), Term.List(S(root)))
                ),
                Term.Tuple(A(GenStatemBootNames.Progress), A(GenStatemBootNames.Started))
            )
        );
        string boot = Path.Combine(output, GenStatemBootNames.Name);
        File.WriteAllBytes(boot + GenStatemBootNames.BootExtension, ExternalTermFormat.Encode(script));
        string[] arguments = [GenStatemBootNames.RootOption, root, GenStatemBootNames.BindirOption, output, GenStatemBootNames.BootOption, boot, GenStatemBootNames.ModeOption, GenStatemBootNames.Embedded];

        return arguments.Select(value => new BitString(Encoding.UTF8.GetBytes(value))).ToArray();
    }
}
