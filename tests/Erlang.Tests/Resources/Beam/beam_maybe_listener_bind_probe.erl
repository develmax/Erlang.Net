-module(beam_maybe_listener_bind_probe).
-export([listen_port/3]).

-record(address, {family}).

listen_port(#address{family = Family}, Port, {Socket, Backlog}) ->
    maybe
        Address = #{family => Family, addr => any, port => Port},
        ok ?=
            beam_bind_transport:bind(Socket, Address),
        ok ?=
            beam_bind_transport:listen(Socket, Backlog),
        {ok, #{addr := Ip, port := Assigned}} ?=
            beam_bind_transport:sockname(Socket),
        {ok, {Socket, {Ip, Assigned}}}
    else
        {error, _} = Error ->
            Error
    end.
