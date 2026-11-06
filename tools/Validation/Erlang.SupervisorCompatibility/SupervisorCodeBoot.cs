using System.Text;
using Erlang;

internal static class SupervisorCodeBoot
{
    internal static BitString[] Create(string repository, string directory)
    {
        string output = Path.GetFullPath(Path.Combine(directory, SupervisorBootNames.OutputPrefix + Guid.NewGuid().ToString(SupervisorBootNames.GuidFormat)));
        string root = Path.Combine(output, SupervisorBootNames.RootDirectory);
        Directory.CreateDirectory(root);
        const string owner = SupervisorBootNames.OwnerModule;
        File.Copy(
            Path.Combine(repository, SupervisorBootNames.OwnerDirectory, owner + SupervisorValidationPaths.BeamExtension),
            Path.Combine(output, owner + SupervisorValidationPaths.BeamExtension)
        );

        Term A(string text) => Term.A(text);

        Term S(string text) => Term.String(text);
        Term script = Term.Tuple(
            A(SupervisorBootNames.Script),
            Term.Tuple(S(SupervisorBootNames.Name), S(SupervisorBootNames.Version)),
            Term.List(
                Term.Tuple(A(SupervisorBootNames.Preloaded), Nil.Value),
                Term.Tuple(A(SupervisorBootNames.Path), Term.List(S(output))),
                Term.Tuple(A(SupervisorBootNames.PrimitiveLoad), Term.List(A(owner))),
                Term.Tuple(A(SupervisorBootNames.KernelLoaded)),
                Term.Tuple(
                    A(SupervisorBootNames.Apply),
                    Term.Tuple(A(SupervisorBootNames.LoaderModule), A(SupervisorBootNames.SetPath), Term.List(Nil.Value))
                ),
                Term.Tuple(
                    A(SupervisorBootNames.KernelProcess),
                    A(SupervisorBootNames.OwnerProcess),
                    Term.Tuple(A(owner), A(SupervisorBootNames.StartLink), Term.List(S(root)))
                ),
                Term.Tuple(A(SupervisorBootNames.Progress), A(SupervisorBootNames.Started))
            )
        );
        string boot = Path.Combine(output, SupervisorBootNames.Name);
        File.WriteAllBytes(boot + SupervisorBootNames.BootExtension, ExternalTermFormat.Encode(script));
        string[] arguments = [SupervisorBootNames.RootOption, root, SupervisorBootNames.BindirOption, output, SupervisorBootNames.BootOption, boot, SupervisorBootNames.ModeOption, SupervisorBootNames.Embedded];

        return arguments.Select(value => new BitString(Encoding.UTF8.GetBytes(value))).ToArray();
    }
}
