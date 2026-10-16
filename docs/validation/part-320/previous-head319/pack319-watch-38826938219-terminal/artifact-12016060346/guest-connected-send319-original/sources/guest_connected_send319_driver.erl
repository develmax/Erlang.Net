-module(guest_connected_send319_driver).
-export([server/0, client/0]).

-define(DEADLINE, 5000).
-define(SERVER_LIFETIME, 30000).

server() ->
    true = register(guest_connected_send319_server, self()),
    save("runtime.etf", metadata()),
    receive
        {stop, Client, Reference} when is_pid(Client), is_reference(Reference) ->
            Client ! {server_stopping, Reference},
            receive
                {finish, Reference} -> save("shutdown.etf", {return, server_joined_protocol})
            after ?DEADLINE -> erlang:error(server_finish_timeout)
            end
    after ?SERVER_LIFETIME -> erlang:error(server_lifetime_timeout)
    end.

client() ->
    save("runtime.etf", metadata()),
    Remote = list_to_atom(os:getenv("GUEST_CONNECTED_SEND_REMOTE")),
    Outcome = capture(fun() ->
        pong = net_adm:ping(Remote),
        save("connection.etf", {node(), Remote, nodes()}),
        guest_connected_send319:run(Remote)
    end),
    save("outcome.etf", Outcome),
    save("worker-cleanup.etf", get(guest_connected_send319_cleanup)),
    Cleanup = capture(fun() ->
        Reference = make_ref(),
        {guest_connected_send319_server, Remote} ! {stop, self(), Reference},
        receive
            {server_stopping, Reference} ->
                {guest_connected_send319_server, Remote} ! {finish, Reference},
                server_joined_protocol
        after ?DEADLINE -> erlang:error(server_stop_timeout)
        end
    end),
    save("server-cleanup.etf", Cleanup),
    case {Outcome, Cleanup} of
        {{return, _}, {return, server_joined_protocol}} -> erlang:halt(0);
        _ -> erlang:halt(1)
    end.

metadata() ->
    Root = code:root_dir(),
    {ok, Version} = file:read_file(filename:join([Root, "releases", "29", "OTP_VERSION"])),
    {guest_connected_send319_runtime, node(), erlang:system_info(otp_release),
     string:trim(binary_to_list(Version)), Root,
     beam_md5(guest_connected_send319), beam_md5(?MODULE),
     erlang:system_info(system_version), erlang:system_info(system_architecture)}.

beam_md5(Module) ->
    {Module, Binary, _} = code:get_object_code(Module),
    erlang:md5(Binary).

capture(Operation) ->
    try Operation() of
        Value -> {return, Value}
    catch
        Class:Reason:Stack -> {exception, Class, Reason, Stack}
    end.

save(Name, Value) ->
    Destination = filename:join(os:getenv("GUEST_CONNECTED_SEND_OUTPUT"), Name),
    Temporary = Destination ++ ".tmp",
    ok = file:write_file(Temporary, term_to_binary(Value)),
    ok = file:rename(Temporary, Destination),
    io:format("~ts ~tp.~n", [Name, Value]).
