using System.Text;

namespace Erlang.Tests;

internal static class IoCharacterHostChecks
{
    public static async Task Run()
    {
        using var output = new StringWriter();
        using var unusedOutput = new MemoryStream();
        using var error = new MemoryStream();
        using var streams = new RuntimeStandardStreams(unusedOutput, error);
        await using var runtime = new ProcessRuntime(
            "nonode@nohost",
            output,
            100,
            streams
        );
        Erlang.Otp.OtpModules.Register(runtime.Modules);
        if (runtime.Modules.Exports.Count != HaltSubprocessRegistrationCounts.DefaultCoreAndOtp)
            throw new InvalidOperationException(HaltSubprocessHostNames.Failure);
        IoCharacterRequests.Register(runtime);
        if (runtime.Modules.Exports.Count != HaltSubprocessRegistrationCounts.WithOptionalIo)
            throw new InvalidOperationException(HaltSubprocessHostNames.Failure);
        ProcessHandle custom = runtime.Spawn(async context =>
        {
            TupleTerm request = (await context.ReceiveAsync(term => term as TupleTerm))!;
            if (request.Items.Count != 4 || request.Items[0] is not Atom { Name: "io_request" } || request.Items[1] is not Pid || request.Items[2] is not ReferenceTerm || request.Items[3] is not TupleTerm { Items.Count: 3 } chars || chars.Items[0] is not Atom { Name: "put_chars" } || chars.Items[1] is not Atom { Name: "unicode" } || chars.Items[2] is not Atom { Name: "device_decides" })
                throw new InvalidOperationException(HaltSubprocessHostNames.Failure);
            runtime.Send((Pid)request.Items[1], Term.Tuple(Term.A("io_reply"), request.Items[2], Term.A("ok")));

            return Term.A("ok");
        });
        runtime.Register("halt_probe_named_io", custom.Pid);
        ProcessHandle caller = runtime.Spawn(async context =>
        {
            Term unicode = Term.List(Term.I(233), Term.List(new BitString([0xe2]), new BitString([0x82, 0xac])), Term.I(10));
            Term ok = await runtime.Modules.Call(
                context,
                "io",
                "put_chars",
                unicode
            );
            if (!ok.Equals(Term.A("ok")))
                throw new InvalidOperationException(HaltSubprocessHostNames.Failure);
            await runtime.Modules.Call(
                context,
                "io",
                "put_chars",
                Term.A("standard_error"),
                Term.List(Term.I(0x1f600), Term.I(10))
            );
            await runtime.Modules.Call(
                context,
                "io",
                "put_chars",
                Term.A("halt_probe_named_io"),
                Term.A("device_decides")
            );
            try
            {
                await runtime.Modules.Call(
                    context,
                    "io",
                    "put_chars",
                    context.Self,
                    Nil.Value
                );
                throw new InvalidOperationException(HaltSubprocessHostNames.Failure);
            }
            catch (ErlangException exception) when (exception.Reason is Atom { Name: "calling_self" })
            {
                if (exception.StackTraceTerm is not Cons { Head: TupleTerm { Items.Count: 4 } frame } || !frame.Items[0].Equals(Term.A("io")) || !frame.Items[1].Equals(Term.A("put_chars")))
                    throw new InvalidOperationException(HaltSubprocessHostNames.Failure);
            }
            try
            {
                await runtime.Modules.Call(
                    context,
                    "io",
                    "put_chars",
                    Term.A("standard_error"),
                    new BitString([0xc3])
                );
                throw new InvalidOperationException(HaltSubprocessHostNames.Failure);
            }
            catch (ErlangException exception) when (exception.Reason is Atom { Name: "badarg" })
            {
                if (exception.StackTraceTerm is not Cons { Head: TupleTerm { Items.Count: 4 } frame } || frame.Items[3] is not Cons { Head: TupleTerm { Items.Count: 2 } info } || info.Items[1] is not MapTerm map || !map.Get(Term.A("cause")).Equals(Term.Tuple(Term.A("device"), Term.A("put_chars"))))
                    throw new InvalidOperationException(HaltSubprocessHostNames.Failure);
            }

            return Term.A("ok");
        });
        Term reason = await caller.Completion.WaitAsync(TimeSpan.FromSeconds(5));
        if (!reason.Equals(Term.A("normal")) || output.ToString() != "é€\n" || !error.ToArray().SequenceEqual(Encoding.UTF8.GetBytes("😀\n")))
            throw new InvalidOperationException(HaltSubprocessHostNames.Failure + ":unicode IO protocol:" + reason);
        if (!((await custom.Completion).Equals(Term.A("normal"))))
            throw new InvalidOperationException(HaltSubprocessHostNames.Failure);
    }
}
