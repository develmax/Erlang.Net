namespace Erlang;

internal sealed class ErlDriverTermServices420
{
    private readonly ErlDriverTermTokens420 tokens;
    private readonly ErlDriverTermProgram420 program;
    private readonly Func<nint, ErlDriverPortAdmission420> nativePort;
    private readonly Func<nuint, ErlDriverPortAdmission420> termPort;
    private readonly Func<ErlDriverPort420?, nuint, Term, int> deliver;
    private readonly ErlDriverServiceFault420 fault;

    internal ErlDriverTermServices420(
        ErlDriverTermTokens420 tokens,
        ErlDriverTermProgram420 program,
        Func<nint, ErlDriverPortAdmission420> nativePort,
        Func<nuint, ErlDriverPortAdmission420> termPort,
        Func<ErlDriverPort420?, nuint, Term, int> deliver,
        ErlDriverServiceFault420 fault
    )
    {
        this.tokens = tokens;
        this.program = program;
        this.nativePort = nativePort;
        this.termPort = termPort;
        this.deliver = deliver;
        this.fault = fault;
        Callbacks = new Dictionary<string, Delegate>(StringComparer.Ordinal)
        {
            [nameof(ErlDriverCallbackTypes420.driver_mk_atom)] = new ErlDriverCallbackTypes420.driver_mk_atom(Atom),
            [nameof(ErlDriverCallbackTypes420.driver_mk_port)] = new ErlDriverCallbackTypes420.driver_mk_port(Port),
            [nameof(ErlDriverCallbackTypes420.driver_connected)] = new ErlDriverCallbackTypes420.driver_connected(Connected),
            [nameof(ErlDriverCallbackTypes420.driver_caller)] = new ErlDriverCallbackTypes420.driver_caller(Caller),
            [nameof(ErlDriverCallbackTypes420.driver_mk_term_nil)] = new ErlDriverCallbackTypes420.driver_mk_term_nil(() => tokens.NilValue),
            [nameof(ErlDriverCallbackTypes420.driver_output_term)] = new ErlDriverCallbackTypes420.driver_output_term(Output),
            [nameof(ErlDriverCallbackTypes420.erl_drv_output_term)] = new ErlDriverCallbackTypes420.erl_drv_output_term(OutputTerm),
            [nameof(ErlDriverCallbackTypes420.driver_send_term)] = new ErlDriverCallbackTypes420.driver_send_term(Send),
            [nameof(ErlDriverCallbackTypes420.erl_drv_send_term)] = new ErlDriverCallbackTypes420.erl_drv_send_term(SendTerm)
        };
    }

    internal IReadOnlyDictionary<string, Delegate> Callbacks { get; }

    private nuint Atom(nint text)
    {
        try
        {
            return tokens.Atom(text);
        }
        catch (Exception error)
        {
            fault.Record(error);

            return 0;
        }
    }

    private nuint Port(nint port) => ReadIdentity(port, value => value.PortTerm);

    private nuint Connected(nint port) => ReadIdentity(port, value => value.Connected);

    private nuint Caller(nint port) => ReadIdentity(port, _ => ErlDriverPortTurn420.CallerFor(port, tokens.NilValue));

    private nuint ReadIdentity(nint port, Func<ErlDriverPort420, nuint> read)
    {
        try
        {
            var admission = nativePort(port);
            using var lease = admission.Lease;
            if (lease is null) return tokens.NilValue;

            return read(lease.Port);
        }
        catch (Exception error)
        {
            fault.Record(error);

            return tokens.NilValue;
        }
    }

    private int Deliver(
        ErlDriverPortAdmission420 admission,
        nuint? destination,
        nint data,
        int length
    )
    {
        using var lease = admission.Lease;
        if (admission.Status <= 0 || lease is null) return admission.Status;
        Term message = program.Read(data, length);
        nuint recipient = destination ?? lease.Port.Connected;

        return deliver(lease.Port, recipient, message);
    }

    private int Output(nint port, nint data, int length)
    {
        try
        {
            return Deliver(
                nativePort(port),
                null,
                data,
                length
            );
        }
        catch (InvalidDataException)
        {
            return -1;
        }
        catch (ErlangException)
        {
            return -1;
        }
        catch (Exception error)
        {
            fault.Record(error);

            return -1;
        }
    }

    private int OutputTerm(nuint port, nint data, int length)
    {
        try
        {
            return Deliver(
                termPort(port),
                null,
                data,
                length
            );
        }
        catch (InvalidDataException)
        {
            return -1;
        }
        catch (ErlangException)
        {
            return -1;
        }
        catch (Exception error)
        {
            fault.Record(error);

            return -1;
        }
    }

    private int Send(
        nint port,
        nuint to,
        nint data,
        int length
    )
    {
        try
        {
            if (ErlDriverThreadRole420.IsManagedThread)
                return Deliver(
                    nativePort(port),
                    to,
                    data,
                    length
                );
            // Pinned legacy unmanaged send deliberately never dereferences the
            // deprecated drvport; its actual sender is undefined.
            Term message = program.Read(data, length);

            return deliver(null, to, message);
        }
        catch (InvalidDataException)
        {
            return -1;
        }
        catch (ErlangException)
        {
            return -1;
        }
        catch (Exception error)
        {
            fault.Record(error);

            return -1;
        }
    }

    private int SendTerm(
        nuint port,
        nuint to,
        nint data,
        int length
    )
    {
        try
        {
            return Deliver(
                termPort(port),
                to,
                data,
                length
            );
        }
        catch (InvalidDataException)
        {
            return -1;
        }
        catch (ErlangException)
        {
            return -1;
        }
        catch (Exception error)
        {
            fault.Record(error);

            return -1;
        }
    }
}
