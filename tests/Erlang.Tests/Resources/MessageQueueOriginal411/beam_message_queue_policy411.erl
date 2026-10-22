-module(beam_message_queue_policy411).
-export([run/0, run_stacks/0, worker/2, child/0]).

cases() -> [system_default, self_default, flip_off, flip_on, repeated,
    default_child_after_parent_off, spawn_on, spawn_off, spawn_last_on, spawn_last_off,
    logger_vector, queued_transition, dead_info, bad_flag_atom, bad_flag_integer,
    bad_spawn_atom, bad_spawn_integer, bad_spawn_arity, bad_spawn_improper,
    bad_then_good, good_then_bad].
run() -> [{Case, capture(Case, default)} || Case <- cases()].
run_stacks() -> [{Depth, [{Case, capture(Case, Depth)} || Case <- cases()]} || Depth <- [default,0,1,2,8]].

capture(Case, Depth) ->
    {Pid, Ref} = spawn_monitor(?MODULE, worker, [Case,Depth]),
    receive {'DOWN',Ref,process,Pid,{observation,Value}} -> Value;
            {'DOWN',Ref,process,Pid,Reason} -> {unexpected_down,Reason}
    after 10000 -> error(observer_timeout)
    end.
worker(Case, Depth) ->
    case Depth of default -> ok; _ -> erlang:system_flag(backtrace_depth,Depth) end,
    true = register(beam_message_queue_policy411_owner,self()),
    Result = try probe(Case) of Value -> {ok,Value}
             catch Class:Reason:Stack -> {exception,Class,Reason,Stack}
             end,
    exit({observation,Result}).

probe(system_default) -> erlang:system_info(message_queue_data);
probe(self_default) -> process_info(self(),message_queue_data);
probe(flip_off) -> {process_flag(message_queue_data,off_heap),process_info(self(),message_queue_data)};
probe(flip_on) -> {process_flag(message_queue_data,on_heap),process_info(self(),message_queue_data)};
probe(repeated) -> {process_flag(message_queue_data,off_heap),process_flag(message_queue_data,off_heap),
                    process_flag(message_queue_data,on_heap),process_info(self(),message_queue_data)};
probe(default_child_after_parent_off) ->
    on_heap = process_flag(message_queue_data,off_heap),
    child_result([]);
probe(spawn_on) -> child_result([{message_queue_data,on_heap}]);
probe(spawn_off) -> child_result([{message_queue_data,off_heap}]);
probe(spawn_last_on) -> child_result([{message_queue_data,off_heap},{message_queue_data,on_heap}]);
probe(spawn_last_off) -> child_result([{message_queue_data,on_heap},{message_queue_data,off_heap}]);
probe(logger_vector) -> child_result([link,{message_queue_data,off_heap}]);
probe(queued_transition) ->
    self() ! first, self() ! second,
    on_heap = process_flag(message_queue_data,off_heap),
    Before = process_info(self(),[message_queue_data,message_queue_len]),
    receive second -> ok end,
    off_heap = process_flag(message_queue_data,on_heap),
    receive First -> {Before,First,process_info(self(),[message_queue_data,message_queue_len])} end;
probe(dead_info) ->
    {Pid,Ref}=spawn_monitor(fun() -> ok end),
    receive {'DOWN',Ref,process,Pid,normal} -> process_info(Pid,message_queue_data) end;
probe(bad_flag_atom) -> process_flag(message_queue_data,invalid);
probe(bad_flag_integer) -> process_flag(message_queue_data,1);
probe(bad_spawn_atom) -> spawn_opt(?MODULE,child,[],[{message_queue_data,invalid}]);
probe(bad_spawn_integer) -> spawn_opt(?MODULE,child,[],[{message_queue_data,1}]);
probe(bad_spawn_arity) -> spawn_opt(?MODULE,child,[],[{message_queue_data,on_heap,off_heap}]);
probe(bad_spawn_improper) -> spawn_opt(?MODULE,child,[],[{message_queue_data,on_heap}|invalid]);
probe(bad_then_good) -> spawn_opt(?MODULE,child,[],[{message_queue_data,invalid},{message_queue_data,off_heap}]);
probe(good_then_bad) -> spawn_opt(?MODULE,child,[],[{message_queue_data,on_heap},{message_queue_data,invalid}]).

child_result(Options) ->
    {Pid,Ref}=spawn_opt(?MODULE,child,[],[monitor|Options]),
    receive {child_mode,Mode} ->
        receive {'DOWN',Ref,process,Pid,normal} -> Mode end
    end.
child() ->
    beam_message_queue_policy411_owner ! {child_mode,process_info(self(),message_queue_data)},
    ok.
