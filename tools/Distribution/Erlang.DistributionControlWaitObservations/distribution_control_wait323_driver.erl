-module(distribution_control_wait323_driver).
-export([start/0, save/2, capture/1, marker/1, wait_marker/2, nonce/0, join/1]).

-define(LIFETIME, 45000).
-define(JOIN, 5000).
-define(FILE_LIMIT, 4194304).

start() ->
    Role = case os:getenv("CONTROL_WAIT_ROLE") of "a" -> a; "b" -> b; "c" -> c end,
    Outcome = capture(fun() ->
        case Role of
            a ->
                save("runtime.etf", metadata(Role)),
                B = list_to_atom(os:getenv("CONTROL_WAIT_B")),
                C = list_to_atom(os:getenv("CONTROL_WAIT_C")),
                Mode = case os:getenv("CONTROL_WAIT_MODE") of "send" -> send; "link" -> link end,
                distribution_control_wait323:run(B, C, Mode);
            b ->
                true = register(control_wait323_destination, self()),
                Worker = spawn(distribution_control_wait323, worker, [self()]),
                receive
                    {worker_ready, Worker, {trap_exit, true}} = Ready -> save("worker-ready.etf", Ready)
                after ?JOIN -> erlang:error(worker_ready_timeout)
                end,
                save("runtime.etf", metadata(Role)),
                server(b, Worker, deadline());
            c ->
                true = register(control_wait323_killer, self()),
                save("runtime.etf", metadata(Role)),
                server(c, undefined, deadline())
        end
    end),
    save("role-outcome.etf", Outcome),
    case Outcome of
        {return, {probe, {return, _}, {return, _}}} -> erlang:halt(0);
        {return, {server_joined, _}} -> erlang:halt(0);
        _ -> erlang:halt(1)
    end.

server(Role, Worker, End) ->
    true = erlang:monotonic_time(millisecond) < End,
    receive
        {prepare, Owner, Ref, B, C} when is_pid(Owner), is_reference(Ref) ->
            pong = net_adm:ping(node(Owner)),
            case Role of b -> pong = net_adm:ping(C); c -> pong = net_adm:ping(B) end,
            save("connections.etf", {node(), nodes(), erlang:system_info(dist_ctrl)}),
            Owner ! {prepared, Ref, self(), Worker, os:getpid()},
            server(Role, Worker, End);
        {kill, Owner, Ref, Target} when Role =:= c, is_pid(Owner), is_reference(Ref), is_pid(Target) ->
            Result = capture(fun() -> erlang:exit(Target, kill) end),
            save("kill-call.etf", {Owner, Ref, self(), Target, Result}),
            Owner ! {kill_ack, Ref, self(), Result},
            server(Role, Worker, End)
    after 25 ->
        case marker("cleanup-go") of
            true ->
                Cleanup = case Worker of undefined -> {return, no_worker}; _ -> capture(fun() -> join(Worker) end) end,
                save("worker-cleanup.etf", Cleanup),
                case Cleanup of
                    {return, _} -> {server_joined, Role};
                    _ -> erlang:error({worker_cleanup, Cleanup})
                end;
            false -> server(Role, Worker, End)
        end
    end.

join(Pid) ->
    Ref = erlang:monitor(process, Pid),
    true = erlang:exit(Pid, kill),
    receive
        {'DOWN', Ref, process, Pid, Reason} -> {joined, Pid, Ref, Reason}
    after ?JOIN -> erlang:error({join_timeout, Pid, Ref})
    end.

metadata(Role) ->
    Root = code:root_dir(),
    {ok, Version} = file:read_file(filename:join([Root, "releases", "29", "OTP_VERSION"])),
    {control_wait323_runtime, Role, node(), erlang:system_info(otp_release),
     string:trim(binary_to_list(Version)), Root,
     [{distribution_control_wait323, beam_md5(distribution_control_wait323)}, {?MODULE, beam_md5(?MODULE)}],
     erlang:system_info(system_version), erlang:system_info(system_architecture), os:getpid(),
     erlang:system_info(dist_buf_busy_limit), net_kernel:get_net_ticktime()}.

beam_md5(Module) ->
    {Module, Bytes, _} = code:get_object_code(Module),
    erlang:md5(Bytes).

capture(Operation) ->
    try Operation() of Value -> {return, Value}
    catch Class:Reason:Stack -> {exception, Class, Reason, Stack}
    end.

nonce() -> list_to_binary(os:getenv("CONTROL_WAIT_NONCE")).

marker(Name) ->
    case file:read_file(filename:join(os:getenv("CONTROL_WAIT_OUTPUT"), Name)) of
        {ok, Bytes} -> true = Bytes =:= nonce(), true;
        {error, enoent} -> false;
        Error -> erlang:error({marker_read, Name, Error})
    end.

wait_marker(Name, Milliseconds) -> wait_marker_until(Name, erlang:monotonic_time(millisecond) + Milliseconds).

wait_marker_until(Name, End) ->
    case marker(Name) of
        true -> ok;
        false ->
            true = erlang:monotonic_time(millisecond) < End,
            receive after 25 -> wait_marker_until(Name, End) end
    end.

save(Name, Value) ->
    Bytes = term_to_binary(Value),
    true = byte_size(Bytes) =< ?FILE_LIMIT,
    Destination = filename:join(os:getenv("CONTROL_WAIT_OUTPUT"), Name),
    Temporary = Destination ++ ".tmp",
    ok = file:write_file(Temporary, Bytes),
    ok = file:rename(Temporary, Destination),
    io:format("~ts~n", [Name]).

deadline() -> erlang:monotonic_time(millisecond) + ?LIFETIME.
