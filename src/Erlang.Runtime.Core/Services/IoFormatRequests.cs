namespace Erlang;

internal static class IoFormatRequests
{
    public static void Register(ModuleRegistry registry) => registry.Register(
        IoModuleNames.Module,
        IoModuleNames.Format,
        2,
        (context, arguments) => Format(context, arguments[0], arguments[1])
    );

    private static async ValueTask<Term> Format(ProcessContext context, Term format, Term arguments)
    {
        Pid device = context.Runtime.GetGroupLeader(context);
        if (device.Equals(context.Self))
            throw new ErlangException(IoDeviceFailureReasons.CallingSelf);
        ReferenceTerm reference = context.Runtime.Monitor(context, device);
        context.Runtime.Send(
            device,
            Term.Tuple(
                IoProtocolAtoms.Request,
                context.Self,
                reference,
                Term.Tuple(
                    IoProtocolAtoms.PutCharacters,
                    IoProtocolAtoms.Unicode,
                    IoProtocolAtoms.FormatterModule,
                    IoProtocolAtoms.FormatterFunction,
                    Cons.From([format, arguments])
                )
            )
        );
        TupleTerm? result = await context.ReceiveAsync(term => term is TupleTerm tuple &&
            (tuple.Items.Count == IoProtocolLayout.ReplyFields && tuple.Items[0] is Atom { Name: IoProtocolNames.Reply } && tuple.Items[1].Equals(reference) ||
             tuple.Items.Count == IoProtocolLayout.MonitorDownFields && tuple.Items[0] is Atom { Name: ProcessMessageTags.MonitorDown } && tuple.Items[1].Equals(reference)) ? tuple : null);
        if (result!.Items.Count == IoProtocolLayout.MonitorDownFields)
        {
            await context.ReceiveAsync(
                term => term is TupleTerm { Items.Count: 3 } exit && exit.Items[0] is Atom { Name: ProcessMessageTags.LinkedExit } && exit.Items[1].Equals(device) ? exit : null,
                TimeSpan.Zero
            );

            throw new ErlangException(IoDeviceFailureReasons.Terminated);
        }
        context.Runtime.Demonitor(context, reference, true);
        Term reply = result.Items[2];
        if (reply is TupleTerm { Items.Count: IoProtocolLayout.DeviceErrorFields } error && error.Items[0] is Atom { Name: IoProtocolNames.Failure })
        {
            Term reason = error.Items[1];
            if (reason is Atom { Name: IoDeviceFailureReasons.Terminated or IoDeviceFailureReasons.CallingSelf })
                throw new ErlangException(reason);
            if (reason is TupleTerm { Items.Count: IoProtocolLayout.TranslationErrorFields } translation && translation.Items[0] is Atom { Name: IoDeviceFailureReasons.NoTranslation })
                throw new ErlangException(IoDeviceFailureReasons.NoTranslation);

            throw new ErlangException(ErlangErrorReasons.BadArgument);
        }

        return reply;
    }
}
