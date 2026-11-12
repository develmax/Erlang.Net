-module(beam_active_socket_probe).
-export([check/2]).

-record(socket, {handle, config}).
-record(config, {options}).
-record(socket_options, {active}).

check(Options, Socket = #socket{handle = {Listener, _}, config = #config{}}) ->
    case proplists:lookup(active, Options) of
        {_, N} when is_integer(N), N < -32768 ->
            throw(einval);
        {_, N} when is_integer(N), N > 32767 ->
            throw(einval);
        {_, N} when is_integer(N) ->
            {ok, #socket_options{active = Active}, _} = beam_active_transport:get_all_opts(Listener),
            case Active of
                Atom when is_atom(Atom), N =< 0 ->
                    self() ! {ssl_passive, Socket};
                A when is_integer(A), A + N > 32767 ->
                    throw(einval);
                A when is_integer(A), A + N =< 0 ->
                    self() ! {ssl_passive, Socket};
                _ ->
                    ok
            end;
        _ ->
            ok
    end.
