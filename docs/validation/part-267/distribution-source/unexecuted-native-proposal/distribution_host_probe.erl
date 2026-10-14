-module(distribution_host_probe).
-export([run/2, output_loop/2]).

run(PeerName, Ebin) ->
    true = register(distribution_probe_observer, self()),
    {ok, Peer, Node} = peer:start_link(#{
        name => PeerName,
        connection => standard_io,
        args => ["-proto_dist", "gen_tcp", "-pa", Ebin,
                 "-gen_tcp_dist_output_loop", "distribution_host_probe", "output_loop"]
    }),
    try
        pong = net_adm:ping(Node),
        2 = rpc:call(Node, erlang, length, [[alpha, beta]]),
        receive
            {distribution_options, Controller, Handle, Options, InvalidOption} ->
                true = is_pid(Controller),
                {error, notsup} = classify(fun() -> erlang:dist_ctrl_get_opt(Handle, get_size) end),
                Stat = erlang:dist_get_stat(Handle),
                io:format(standard_error, "distribution_stat=~p~n", [Stat]),
                true = valid_stat(Stat),
                {ok, Options, InvalidOption, {error, notsup}, live_controller, validated_statistics}
        after 10000 ->
            erlang:error(controller_probe_timeout)
        end
    after
        peer:stop(Peer),
        unregister(distribution_probe_observer)
    end.

output_loop(Handle, Socket) ->
    A = erlang:dist_ctrl_get_opt(Handle, get_size),
    B = erlang:dist_ctrl_set_opt(Handle, get_size, true),
    C = erlang:dist_ctrl_get_opt(Handle, get_size),
    D = erlang:dist_ctrl_set_opt(Handle, get_size, false),
    E = erlang:dist_ctrl_get_opt(Handle, get_size),
    F = erlang:dist_ctrl_set_opt(Handle, get_size, true),
    G = erlang:dist_ctrl_get_opt(Handle, get_size),
    Invalid = classify(fun() -> erlang:dist_ctrl_get_opt(Handle, unknown_option) end),
    case whereis(distribution_probe_observer) of
        undefined -> ok;
        Observer -> Observer ! {distribution_options, self(), Handle, [A,B,C,D,E,F,G], Invalid}
    end,
    dispatch(Handle, Socket).

dispatch(Handle, Socket) ->
    receive
        dist_data ->
            drain(Handle, Socket);
        _ -> ok
    end,
    dispatch(Handle, Socket).

drain(Handle, Socket) ->
    case erlang:dist_ctrl_get_data(Handle) of
        none -> erlang:dist_ctrl_get_data_notification(Handle);
        {Size, Data} ->
            true = lists:all(fun erlang:is_binary/1, Data),
            Size = erlang:iolist_size(Data),
            ok = gen_tcp:send(Socket, Data),
            drain(Handle, Socket)
    end.

classify(F) ->
    try F() of Value -> {ok, Value}
    catch Class:Reason -> {Class, Reason}
    end.

valid_stat({ok, In, Out, Pending}) ->
    is_integer(In) andalso In >= 0 andalso
    is_integer(Out) andalso Out >= 0 andalso
    is_integer(Pending) andalso Pending >= 0;
valid_stat(_) -> false.
