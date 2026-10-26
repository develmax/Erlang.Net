namespace Erlang;

internal static class IoCharacterDevice
{
    public static async ValueTask<Term> Run(ProcessContext context)
    {
        while (true)
        {
            Term request = (await context.ReceiveAsync(term => IoDefaultDevice.IsRequest(term) ? term : null))!;
            var tuple = (TupleTerm)request;
            Term reply;
            try
            {
                if (tuple.Items[3] is not TupleTerm { Items.Count: IoCharacterNames.CharacterRequestFields } chars || chars.Items[0] is not Atom { Name: IoProtocolNames.PutCharacters } || chars.Items[1] is not Atom { Name: IoProtocolNames.Unicode })
                    throw new ErlangException(ErlangErrorReasons.BadArgument);
                context.Runtime.StandardStreams.Write(ErlangStandardStreamNames.StandardError, IoUnicodeCharacters.Encode(chars.Items[2]));
                reply = Term.A(IoResultAtoms.Ok);
            }
            catch (ErlangException)
            {
                reply = Term.Tuple(IoProtocolAtoms.Failure, IoProtocolAtoms.PutCharacters);
            }
            catch (IOException)
            {
                reply = Term.Tuple(IoProtocolAtoms.Failure, Term.A(ErlangErrorReasons.BadArgument));
            }
            context.Runtime.SendTraced(context, (Pid)tuple.Items[1], Term.Tuple(IoProtocolAtoms.Reply, tuple.Items[2], reply));
        }
    }
}
