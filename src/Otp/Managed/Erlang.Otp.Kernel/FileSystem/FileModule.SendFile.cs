// Ported from OTP-29.1.1 file.erl (Apache-2.0), Ericsson AB 1996-2026.
using System.Numerics;

namespace Erlang;

public static partial class FileModule
{
    private static async ValueTask<Term> SendFile(ProcessContext context, IReadOnlyList<Term> arguments)
    {
        if (arguments.Count == FileApiArity.Binary)
        {
            Term opened = await Open(context, arguments[0], Term.List(A(FileProtocol.Read), A(FileProtocol.Raw), A(FileProtocol.Binary)));
            if (!IsSuccess(opened))
            {
                return opened;
            }
            Term handle = ((TupleTerm)opened).Items[1];
            Term result = await SendFile(context, [handle, arguments[1], Term.I(0), Term.I(0), Nil.Value]);
            _ = await Device(context, FileFunctionNames.Close, [handle]);

            return result;
        }
        if (arguments[0] is Pid)
        {
            return BadArgument;
        }
        Term options = arguments[FileSendfileLayout.OptionsArgumentIndex];
        if (options is Nil)
        {
            return await SendFileCore(context, arguments, FileTransferLimits.MaximumSendfileChunkSize);
        }
        try
        {
            Term chunk = await Call(
                context,
                FileDependencyNames.ProplistsModule,
                FileDependencyNames.GetOptionFunction,
                A(FileProtocol.ChunkSize),
                options,
                Term.I(FileTransferLimits.MaximumSendfileChunkSize)
            );
            if (chunk is not Integer integer)
            {
                return BadArgument;
            }

            return await SendFileCore(context, arguments, BigInteger.Min(integer.Value, FileTransferLimits.MaximumSendfileChunkSize));
        }
        catch (ErlangException error) when (error.ExceptionClass == ErlangExceptionClasses.Error)
        {
            return BadArgument;
        }
    }

    private static async ValueTask<Term> SendFileCore(ProcessContext context, IReadOnlyList<Term> arguments, BigInteger chunk)
    {
        Term handle = arguments[0];
        Term socket = arguments[1];
        Term offset = arguments[2];
        Term bytes = arguments[3];
        if (!Descriptor(handle, out Term module) || offset is not Integer || bytes is not Integer)
        {
            return BadArgument;
        }
        Func<Term, ValueTask<Term>> send;
        if (socket is TupleTerm { Items.Count: FileTupleLayout.SocketArity } rawSocket && rawSocket.Items[0].Equals(A(FileProtocol.SocketTag)))
        {
            try
            {
                return await Call(
                    context,
                    FileDependencyNames.SocketModule,
                    FileDependencyNames.SocketSendfileFunction,
                    socket,
                    handle,
                    offset,
                    bytes,
                    A(FileProtocol.Infinity)
                );
            }
            catch (ErlangException error) when (error.ExceptionClass == ErlangExceptionClasses.Error && error.Reason.Equals(A(FileProtocol.Notsup)))
            {
                send = data => Call(
                    context,
                    FileDependencyNames.SocketModule,
                    FileDependencyNames.SocketSendFunction,
                    socket,
                    data
                );
            }
        }
        else if (socket is TupleTerm { Items.Count: FileTupleLayout.InetSocketArity } inetSocket && inetSocket.Items[0].Equals(A(FileProtocol.InetTag)))
        {
            Term result = await CallModule(
                context,
                inetSocket.Items[1],
                FileFunctionNames.Sendfile,
                socket,
                handle,
                offset,
                bytes
            );
            if (!result.Equals(Error(A(FileProtocol.Enotsup))))
            {
                return result;
            }
            send = data => Call(
                context,
                FileDependencyNames.GenTcpModule,
                FileDependencyNames.GenTcpSendFunction,
                socket,
                data
            );
        }
        else if (socket is PortTerm)
        {
            Term result = await CallModule(
                context,
                module,
                FileFunctionNames.Sendfile,
                handle,
                socket,
                offset,
                bytes,
                new Integer(chunk),
                Nil.Value,
                Nil.Value,
                arguments[FileSendfileLayout.OptionsArgumentIndex]
            );
            if (!result.Equals(Error(A(FileProtocol.Enotsup))))
            {
                return result;
            }
            send = data => Call(
                context,
                FileDependencyNames.GenTcpModule,
                FileDependencyNames.GenTcpSendFunction,
                socket,
                data
            );
        }
        else if (socket is FunctionTerm { Arity: FileSendfileLayout.SendCallbackArity } callback)
        {
            send = data => callback.Invoke(context, [data]);
        }
        else
        {
            throw FileSource.Error(
                context,
                FileFunctionNames.Sendfile,
                arguments,
                Term.Tuple(A(ErlangErrorReasons.CaseClause), socket),
                FileSourceLocations.SendfileSocketDispatch,
                FileSourceArities.SendfileCore
            );
        }
        if (((Integer)bytes).Value < 0)
        {
            return Error(A(FileProtocol.Invalid));
        }
        Term previous = await Device(context, FileFunctionNames.Position, [handle, Term.Tuple(A(FileProtocol.Cur), Term.I(0))]);
        if (!IsSuccess(previous))
        {
            throw new ErlangException(Term.Tuple(A(ErlangErrorReasons.BadMatch), previous));
        }
        Term positioned = await Device(context, FileFunctionNames.Position, [handle, Term.Tuple(A(FileProtocol.Bof), offset)]);
        if (!IsSuccess(positioned))
        {
            return positioned;
        }
        Term sent = await SendChunks(
            context,
            handle,
            send,
            ((Integer)bytes).Value,
            chunk
        );
        _ = await Device(context, FileFunctionNames.Position, [handle, Term.Tuple(A(FileProtocol.Bof), ((TupleTerm)previous).Items[1])]);

        return sent;
    }

    private static async ValueTask<Term> SendChunks(
        ProcessContext context,
        Term handle,
        Func<Term, ValueTask<Term>> send,
        BigInteger length,
        BigInteger chunk
    )
    {
        BigInteger sent = BigInteger.Zero;
        while (length == FileTransferLimits.SendfileUntilEof || sent < length)
        {
            BigInteger count = length == FileTransferLimits.SendfileUntilEof ? chunk : BigInteger.Min(length - sent, chunk);
            Term result = await Device(context, FileFunctionNames.Read, [handle, new Integer(count)]);
            if (result.Equals(A(FileProtocol.Eof)))
            {
                return Success(new Integer(sent));
            }
            if (!IsSuccess(result))
            {
                return result;
            }
            Term data = ((TupleTerm)result).Items[1];
            Term size = await Call(
                context,
                FileProtocol.Erlang,
                FileProtocol.IolistSize,
                data
            );
            Term response = await send(data);
            if (!response.Equals(Ok))
            {
                return response;
            }
            sent += ((Integer)size).Value;
        }

        return Success(new Integer(sent));
    }
}
