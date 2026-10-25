using System.Text;
using Erlang;
using Erlang.Otp;
using static Erlang.Otp.TestAnalysis.Library.Tests.TestAnalysisLibraryBootNames;

namespace Erlang.Otp.TestAnalysis.Library.Tests;

internal static class TestAnalysisLibraryBoot
{
    internal static IEnumerable<byte[]> Images()
        => TestAnalysisLibraryBootNames.StdlibModules.Select(module => OtpStdlib.ReadImage(module))
            .Concat(TestAnalysisLibraryBootNames.KernelModules.Select(module => OtpKernel.ReadImage(module)))
            .Concat(TestAnalysisLibraryBootNames.ErtsModules.Select(module => OtpErts.ReadImage(module)));

    internal static async Task Start(ProcessRuntime runtime, Action<bool> check)
    {
        var initialized = new TaskCompletionSource<Term>(TaskCreationOptions.RunContinuationsAsynchronously);
        ProcessHandle initializer = runtime.Spawn(async context =>
        {
            try
            {
                initialized.TrySetResult(await runtime.Modules.Call(context, PrimitiveFile, OnLoad));
            }
            catch (Exception error)
            {
                initialized.TrySetException(error);
            }

            return Nil.Value;
        });
        check((await initialized.Task.WaitAsync(TimeSpan.FromSeconds(20))).Equals(Term.A(Ok)));
        await initializer.Completion.WaitAsync(TimeSpan.FromSeconds(20));
        string directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), DirectoryPrefix + Guid.NewGuid().ToString(GuidFormat));
        Directory.CreateDirectory(directory);

        Term Text(string value) => Term.String(value);
        Term script = Term.Tuple(
            Term.A(Script),
            Term.Tuple(Text(BootBase), Text(ScriptVersion)),
            Term.List(
                Term.Tuple(Term.A(Preloaded), Nil.Value),
                Term.Tuple(Term.A(ScriptPath), Term.List(Text(directory))),
                Term.Tuple(Term.A(PrimitiveLoad), Nil.Value),
                Term.Tuple(Term.A(LoadCompleted)),
                Term.Tuple(Term.A(KernelProcess), Term.A(FileServer), Term.Tuple(Term.A(FileServerModule), Term.A(StartLink), Nil.Value)),
                Term.Tuple(Term.A(Progress), Term.A(Started))
            )
        );
        string boot = System.IO.Path.Combine(directory, BootBase);
        File.WriteAllBytes(boot + BootExtension, ExternalTermFormat.Encode(script));
        try
        {
            string[] arguments = [Root, directory, Bindir, directory, Boot, boot, Mode, Embedded];
            using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            RuntimeOtpBootSession session = await runtime.StartOtpBootAsync(arguments.Select(value => new BitString(Encoding.UTF8.GetBytes(value))), cancellation.Token);
            check(!session.Completion.IsCompleted && runtime.WhereIs(Init).Equals(session.InitializationProcess));
            check(runtime.WhereIs(FileServer) is Pid server && runtime.IsAlive(server));
        }
        finally
        {
            File.Delete(boot + BootExtension);
            Directory.Delete(directory);
        }
    }
}
