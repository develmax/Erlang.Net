using System.Text;
using Erlang;

internal static class GenEventBoot
{
    internal static async Task Start(ProcessRuntime runtime, string directory)
    {
        string root = Path.Combine(directory, GenEventBootNames.DirectoryPrefix + Guid.NewGuid().ToString(GenEventBootNames.GuidFormat));
        Directory.CreateDirectory(root);
        string boot = Path.Combine(root, GenEventBootNames.ScriptName);

        Term A(string text) => Term.A(text);
        Term script = Term.Tuple(
            A(GenEventValidationAtoms.Script),
            Term.Tuple(Term.String(GenEventBootNames.ScriptName), Term.String(GenEventBootNames.ScriptVersion)),
            Term.List(
                Term.Tuple(A(GenEventValidationAtoms.PreLoaded), Nil.Value),
                Term.Tuple(A(GenEventValidationAtoms.Path), Term.List(Term.String(root))),
                Term.Tuple(A(GenEventValidationAtoms.PrimLoad), Nil.Value),
                Term.Tuple(A(GenEventValidationAtoms.KernelLoadCompleted)),
                Term.Tuple(
                    A(GenEventValidationAtoms.Apply),
                    Term.Tuple(A(GenEventValidationModules.ErlPrimLoader), A(GenEventValidationFunctions.SetPath), Term.List(Nil.Value))
                ),
                Term.Tuple(
                    A(GenEventValidationAtoms.KernelProcess),
                    A(GenEventValidationModules.CodeServer),
                    Term.Tuple(
                        A(GenEventValidationModules.CodeServer),
                        A(GenEventValidationFunctions.StartLink),
                        Term.List(Term.List(Term.String(root), A(GenEventValidationAtoms.Embedded)))
                    )
                ),
                Term.Tuple(A(GenEventValidationAtoms.Progress), A(GenEventValidationAtoms.Started))
            )
        );
        File.WriteAllBytes(boot + GenEventBootNames.BootExtension, ExternalTermFormat.Encode(script));
        var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        ProcessHandle process = runtime.Spawn(async context =>
        {
            try
            {
                await runtime.Modules.Call(context, GenEventValidationModules.PrimFile, GenEventValidationFunctions.OnLoad);
                ready.SetResult();
            }
            catch (Exception error)
            {
                ready.SetException(error);
            }

            return Nil.Value;
        });
        await ready.Task.WaitAsync(GenEventValidationBudgets.Initialization);
        await process.Completion.WaitAsync(GenEventValidationBudgets.StartupWorkerExit);
        string[] options = [GenEventBootNames.RootOption, root, GenEventBootNames.BinaryDirectoryOption, root, GenEventBootNames.BootOption, boot, GenEventBootNames.ModeOption, GenEventValidationAtoms.Embedded];
        using var cancellation = new CancellationTokenSource(GenEventValidationBudgets.OriginalBoot);
        RuntimeOtpBootSession session = await runtime.StartOtpBootAsync(
            options.Select(value => new BitString(Encoding.UTF8.GetBytes(value))), cancellation.Token);
        if (session.Completion.IsCompleted)
        {
            throw new InvalidOperationException(GenEventValidationDiagnostics.BootEnded);
        }
    }
}
