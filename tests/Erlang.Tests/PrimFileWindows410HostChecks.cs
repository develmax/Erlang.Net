using System.Runtime.ExceptionServices;

namespace Erlang.Tests;

internal static class PrimFileWindows410HostChecks
{
    private static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException(PrimFileWindows410HostNames.Failure);
    }

    private static Term Call(string name, params object[] arguments)
    {
        var type = typeof(ProcessRuntime).Assembly.GetType(PrimFileWindows410HostNames.Type, true)!;
        try
        {
            return (Term)type.GetMethod(name, PrimFileWindows410HostNames.Methods)!.Invoke(null, arguments)!;
        }
        catch (System.Reflection.TargetInvocationException error) when (error.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(error.InnerException).Throw();
            throw;
        }
    }

    private static BitString Native(string text, int trailingNulls = 1)
    {
        var bytes = new byte[(text.Length + trailingNulls) * 2];
        for (int index = 0; index < text.Length; index++)
        {
            bytes[index * 2] = (byte)text[index];
            bytes[index * 2 + 1] = (byte)(text[index] >> 8);
        }

        return new BitString(bytes);
    }

    private static Term Error(string reason) => Term.Tuple(Term.A(PrimFileWindows410HostNames.Error), Term.A(reason));

    private static Term Value(Term term)
    {
        Require(term is TupleTerm { Items.Count: 2 } tuple && tuple.Items[0].Equals(Term.A(PrimFileWindows410HostNames.Ok)));

        return ((TupleTerm)term).Items[1];
    }

    internal static async Task Run()
    {
        if (!OperatingSystem.IsWindows()) return;
        string root = Path.GetFullPath(Path.Combine(PrimFileWindows410HostNames.Root, Guid.NewGuid().ToString(PrimFileWindows410HostNames.Format)));
        Directory.CreateDirectory(root);
        string path = Path.Combine(root, PrimFileWindows410HostNames.Name);
        byte[] content = [0, 4, 1, 0, 255];
        File.WriteAllBytes(path, content);
        try
        {
            await using var runtime = new ProcessRuntime(1);
            await using var actor = new RuntimeNodeFoundationHostActor(runtime);
            await actor.Invoke(context =>
            {
                CancellationToken token = context.Cancellation;
                Require(Value(Call(PrimFileWindows410HostNames.Read, Native(path), token)).Equals(new BitString(content)));
                Require(Value(Call(PrimFileWindows410HostNames.Read, Native(path, 3), token)).Equals(new BitString(content)));
                var info = (TupleTerm)Call(
                    PrimFileWindows410HostNames.Info,
                    Native(path),
                    Term.I(1),
                    token
                );
                Require(info.Items.Count == 14 && info.Items[1].Equals(Term.I(content.Length))
                    && info.Items[2].Equals(Term.A(PrimFileWindows410HostNames.Regular))
                    && info.Items[3].Equals(Term.A(PrimFileWindows410HostNames.ReadWrite))
                    && info.Items[7].Equals(Term.I(0x81B6)) && ((Integer)info.Items[8]).Value >= 1
                    && info.Items[10].Equals(Term.I(0)) && info.Items[11].Equals(Term.I(0))
                    && info.Items[12].Equals(Term.I(0)) && info.Items[13].Equals(Term.I(0)));
                var directory = (TupleTerm)Call(
                    PrimFileWindows410HostNames.Info,
                    Native(root),
                    Term.I(0),
                    token
                );
                Require(directory.Items[2].Equals(Term.A(PrimFileWindows410HostNames.Directory)));
                Term listed = Value(Call(PrimFileWindows410HostNames.List, Native(root), token));
                Require(Cons.Items(listed).Single().Equals(Native(PrimFileWindows410HostNames.Name, 0)));
                Require(Value(Call(PrimFileWindows410HostNames.Cwd, token)) is BitString { IsBinary: true });
                Require(Call(PrimFileWindows410HostNames.Read, Native(root), token).Equals(Error(PrimFileWindows410HostNames.DirectoryError)));
                Require(Call(PrimFileWindows410HostNames.Read, Native(Path.Combine(root, PrimFileWindows410HostNames.MissingName)), token)
                    .Equals(Error(PrimFileWindows410HostNames.Missing)));
                Term[] invalid = [Nil.Value, new BitString([1]), Native(path, 0), Native(path + '\0' + PrimFileWindows410HostNames.Name)];
                foreach (Term bad in invalid)
                {
                    Require(Call(PrimFileWindows410HostNames.Read, bad, token).Equals(Error(PrimFileWindows410HostNames.Invalid)));
                    Require(Call(
                        PrimFileWindows410HostNames.Info,
                        bad,
                        Term.I(1),
                        token
                    ).Equals(Error(PrimFileWindows410HostNames.Invalid)));
                    Require(Call(PrimFileWindows410HostNames.List, bad, token).Equals(Error(PrimFileWindows410HostNames.Invalid)));
                }
                bool badFollow = false;
                try
                {
                    _ = Call(
                        PrimFileWindows410HostNames.Info,
                        Native(path),
                        Nil.Value,
                        token
                    );
                }
                catch (ErlangException error)
                {
                    badFollow = error.Reason.Equals(Term.A(ErlangErrorReasons.BadArgument));
                }
                Require(badFollow);
                File.SetAttributes(path, File.GetAttributes(path) | FileAttributes.ReadOnly);
                var readOnly = (TupleTerm)Call(
                    PrimFileWindows410HostNames.Info,
                    Native(path),
                    Term.I(-1),
                    token
                );
                Require(readOnly.Items[3].Equals(Term.A(PrimFileWindows410HostNames.ReadOnly)) && readOnly.Items[7].Equals(Term.I(0x8124)));
                using var canceled = new CancellationTokenSource();
                canceled.Cancel();
                bool stopped = false;
                try
                {
                    _ = Call(PrimFileWindows410HostNames.Read, Native(path), canceled.Token);
                }
                catch (OperationCanceledException)
                {
                    stopped = true;
                }
                Require(stopped);

                return true;
            });
        }
        finally
        {
            File.SetAttributes(path, FileAttributes.Normal);
            Directory.Delete(root, true);
        }
    }
}
