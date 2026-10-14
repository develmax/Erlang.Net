namespace Erlang;

public static class IoCharacterRequests
{
    public static void Register(ProcessRuntime runtime)
    {
        if (runtime.WhereIs(IoCharacterNames.StandardError) is not Pid)
        {
            ProcessHandle device = runtime.Spawn(IoCharacterDevice.Run);
            runtime.Register(IoCharacterNames.StandardError, device.Pid);
        }
        foreach (int arity in new[] { 1, 2 })
            runtime.Modules.Register(
                IoModuleNames.Module,
                IoCharacterNames.Function,
                arity,
                Execute
            );
    }

    private static async ValueTask<Term> Execute(ProcessContext context, IReadOnlyList<Term> arguments)
    {
        Term selected = arguments.Count == 1 ? Term.A(IoCharacterNames.StandardIo) : arguments[0];
        Term data = arguments[^1];
        Pid? device = selected switch
        {
            Atom { Name: IoCharacterNames.StandardIo } => context.Runtime.GetGroupLeader(context),
            Pid pid => pid,
            Atom name => context.Runtime.WhereIs(name.Name) as Pid,
            _ => null
        };
        if (device is null)
            throw Failure(
                context,
                arguments,
                Term.A(IoCharacterNames.Arguments),
                false
            );
        if (device.Equals(context.Self))
            throw Failure(
                context,
                arguments,
                Term.A(IoDeviceFailureReasons.CallingSelf),
                false
            );
        try
        {
            data = IoUnicodeCharacters.Encode(data);
        }
        catch (ErlangException)
        {
            // Native io_request forwards invalid chardata to the selected server.
        }
        ReferenceTerm reference = context.Runtime.Monitor(context, device);
        context.Runtime.SendTraced(
            context,
            device,
            Term.Tuple(
                IoProtocolAtoms.Request,
                context.Self,
                reference,
                Term.Tuple(IoProtocolAtoms.PutCharacters, IoProtocolAtoms.Unicode, data)
            )
        );
        TupleTerm result = (await context.ReceiveAsync(term => term is TupleTerm tuple && (tuple.Items.Count == IoProtocolLayout.ReplyFields && tuple.Items[0].Equals(IoProtocolAtoms.Reply) && tuple.Items[1].Equals(reference) || tuple.Items.Count == IoProtocolLayout.MonitorDownFields && tuple.Items[0] is Atom { Name: ProcessMessageTags.MonitorDown } && tuple.Items[1].Equals(reference)) ? tuple : null))!;
        if (result.Items.Count == IoProtocolLayout.MonitorDownFields)
        {
            await context.ReceiveAsync(
                term => term is TupleTerm { Items.Count: 3 } exit && exit.Items[0] is Atom { Name: ProcessMessageTags.LinkedExit } && exit.Items[1].Equals(device) ? exit : null,
                TimeSpan.Zero
            );
            throw Failure(
                context,
                arguments,
                Term.A(IoDeviceFailureReasons.Terminated),
                false
            );
        }
        context.Runtime.Demonitor(context, reference, true);
        Term reply = result.Items[2];
        if (reply is TupleTerm { Items.Count: 2 } error && error.Items[0].Equals(IoProtocolAtoms.Failure))
            throw Failure(
                context,
                arguments,
                error.Items[1],
                true
            );

        return reply;
    }

    private static ErlangException Failure(
        ProcessContext context,
        IReadOnlyList<Term> arguments,
        Term reason,
        bool device
    )
    {
        string converted = reason switch
        {
            Atom { Name: IoDeviceFailureReasons.Terminated } => IoDeviceFailureReasons.Terminated,
            Atom { Name: IoDeviceFailureReasons.CallingSelf } => IoDeviceFailureReasons.CallingSelf,
            TupleTerm { Items.Count: 3 } tuple when tuple.Items[0] is Atom { Name: IoDeviceFailureReasons.NoTranslation } => IoDeviceFailureReasons.NoTranslation,
            _ => ErlangErrorReasons.BadArgument
        };
        var info = new MapTerm(new Dictionary<Term, Term>
        {
            [Term.A(IoCharacterNames.ErrorModule)] = Term.A(IoCharacterNames.ErrorFormatter),
            [Term.A(IoCharacterNames.Cause)] = Term.Tuple(Term.A(device ? IoCharacterNames.Device : IoModuleNames.Module), reason)
        });
        Term frame = Term.Tuple(
            Term.A(IoModuleNames.Module),
            Term.A(IoCharacterNames.Function),
            Cons.From(arguments),
            Term.List(Term.Tuple(Term.A(ErlangErrorMetadataNames.ErrorInfo), info))
        );

        return new ErlangException(converted) { ExplicitStackTrace = true, StackTraceTerm = new Cons(frame, context.CaptureErrorStack(Term.I(arguments.Count), null)) };
    }
}
