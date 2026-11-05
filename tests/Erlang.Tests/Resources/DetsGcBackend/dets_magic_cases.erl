%% Observable magic-reference lifetime required by dets:repair_continuation/2.
-module(dets_magic_cases).
-export([run_worker/3,error_worker/2,worker/1]).

run_worker(Parent,Tag,_) ->
    Parent ! {Tag,{dead(),live(),remote(false),remote(true)}}.

error_worker(Parent,Tag) -> Parent ! {Tag,[]}.

spec() -> [{'_',[],['$_']}].

dead() ->
    Bin = term_to_binary(ets:match_spec_compile(spec())),
    true = garbage_collect(),
    ets:is_compiled_ms(binary_to_term(Bin)).

live() ->
    Program = ets:match_spec_compile(spec()),
    Bin = term_to_binary(Program),
    true = garbage_collect(),
    {ets:is_compiled_ms(binary_to_term(Bin)),ets:is_compiled_ms(Program)}.

remote(Keep) ->
    Pid = spawn(?MODULE,worker,[self()]),
    Pid ! {make,Keep},
    Bin = receive {Pid,B} -> B end,
    true = garbage_collect(Pid),
    Result = ets:is_compiled_ms(binary_to_term(Bin)),
    Pid ! stop,
    Result.

worker(Parent) ->
    receive
        {make,Keep} -> make(Parent,Keep)
    end.

make(Parent,Keep) ->
    Program = ets:match_spec_compile(spec()),
    Parent ! {self(),term_to_binary(Program)},
    case Keep of true -> parked(Program); false -> parked(none) end.

parked(State) ->
    receive
        stop -> State
    end.
