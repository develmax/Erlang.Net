-module(beam_certificate_name_probe).
-export([check/2, accept/4]).
-record(address, {family}).
-define(LOG(Ip), case logger:allow(error, ?MODULE) of true -> erlang:apply(logger, macro_log, [#{mfa => {?MODULE, check, 2}, line => ?LINE, file => ?FILE}, error | ["Rejected ~p", [Ip]]]); false -> ok end).
-define(LOG_OPTIONS(Error), case logger:allow(error, ?MODULE) of true -> erlang:apply(logger, macro_log, [#{mfa => {?MODULE, accept, 4}, line => ?LINE, file => ?FILE}, error | ["Options ~s", [beam_certificate_transport:format(Error)]]]); false -> ok end).

check(Socket, Ip) ->
    {ok, Allowed} = beam_certificate_transport:allowed(),
    case Allowed of
        [] -> ok;
        _ ->
            case beam_certificate_transport:peercert(Socket) of
                {ok, Der} ->
                    Cert = beam_certificate_transport:decode(Der, otp),
                    Hosts = beam_certificate_transport:hosts(Allowed),
                    case
                        beam_certificate_transport:verify(
                            Cert, [{ip, Ip} | [{dns_id, Host} || Host <- Hosts]])
                    of
                        true -> ok;
                        false ->
                            ?LOG(Ip),
                            {error, denied}
                    end;
                {error, no_peercert} -> ok;
                {error, _} = Error -> Error
            end
    end.

accept(#address{family = Family}, Socket, Ip, Port) ->
    Options = beam_certificate_transport:defaults(Ip),
    case
        beam_certificate_transport:handshake(
            Socket,
            beam_certificate_transport:merge(Options, [Family, binary, {active, false}, {packet, 4}], []),
            beam_certificate_transport:timeout())
    of
        {ok, Ssl} ->
            case check(Ssl, Ip) of
                ok -> {Ssl, {Ip, Port}};
                {error, Reason} ->
                    beam_certificate_transport:close_ssl(Ssl),
                    exit({accept, Reason})
            end;
        {error, {options, _} = Reason} = Error ->
            ?LOG_OPTIONS(Error),
            beam_certificate_transport:close_socket(Socket),
            exit({accept, Reason});
        {error, Reason} ->
            beam_certificate_transport:close_socket(Socket),
            exit({accept, Reason})
    end.
