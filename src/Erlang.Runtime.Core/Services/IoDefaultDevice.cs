namespace Erlang;

internal static class IoDefaultDevice
{
    public static bool IsRequest([System.Diagnostics.CodeAnalysis.NotNullWhen(true)] Term? term) => term is TupleTerm { Items.Count: IoProtocolLayout.RequestFields } tuple && tuple.Items[0] is Atom { Name: IoProtocolNames.Request } && tuple.Items[1] is Pid;

    public static async ValueTask Execute(ProcessContext context, Term term)
    {
        var request = (TupleTerm)term;
        Term reply;
        bool characters = request.Items[3] is TupleTerm { Items.Count: IoCharacterNames.CharacterRequestFields };
        try
        {
            if (characters)
            {
                var chars = (TupleTerm)request.Items[3];
                if (chars.Items[0] is not Atom { Name: IoProtocolNames.PutCharacters } || chars.Items[1] is not Atom { Name: IoProtocolNames.Unicode })
                    throw new ErlangException(ErlangErrorReasons.BadArgument);
                await context.Runtime.Output.WriteAsync(IoUnicodeCharacters.Text(chars.Items[2]));
            }
            else
            {
                if (request.Items[3] is not TupleTerm { Items.Count: IoProtocolLayout.FormatterRequestFields } format || format.Items[0] is not Atom { Name: IoProtocolNames.PutCharacters } || format.Items[1] is not Atom { Name: IoProtocolNames.Unicode } || format.Items[2] is not Atom { Name: IoProtocolNames.FormatterModule } || format.Items[3] is not Atom { Name: IoProtocolNames.FormatterFunction })
                    throw new ErlangException(ErlangErrorReasons.BadArgument);
                Term[] arguments = Cons.Items(format.Items[4]).ToArray();
                if (arguments.Length != IoProtocolLayout.FormatterArguments)
                    throw new ErlangException(ErlangErrorReasons.BadArgument);
                await context.Runtime.Output.WriteAsync(IoFormatText.Render(arguments[0], arguments[1]));
            }
            reply = Term.A(IoResultAtoms.Ok);
        }
        catch (ErlangException exception)
        {
            reply = Term.Tuple(IoProtocolAtoms.Failure, exception.Reason);
        }
        catch (Exception exception) when (exception is IOException or ObjectDisposedException or ArgumentException)
        {
            reply = Term.Tuple(IoProtocolAtoms.Failure, Term.A(ErlangErrorReasons.BadArgument));
        }
        Term message = Term.Tuple(IoProtocolAtoms.Reply, request.Items[2], reply);
        if (characters)
            context.Runtime.SendTraced(context, (Pid)request.Items[1], message);
        else
            context.Runtime.Send((Pid)request.Items[1], message);
    }
}
