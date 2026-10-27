-module(contract_spawn_subject506).
-export([held/3, entry0/0, start_ack/3]).

entry0() -> held(whereis(contract_controller506), tuple_entry, []).

start_ack(Parent, Token, Return) ->
    proc_lib:init_ack(Return),
    held(Parent, Token, []).

held(Parent, Token, Payload) ->
    put(contract_payload506, Payload),
    Parent ! {held, Token, self()},
    loop(Payload).

loop(Payload) ->
    receive
        {allocate, From, Ref, list} ->
            Next = lists:seq(1, 16384),
            put(contract_payload506, Next),
            From ! {allocated, Ref, self(), erlang:phash2(Next)},
            loop(Next);
        {allocate, From, Ref, binary} ->
            Next = binary:copy(<<17>>, 65536),
            put(contract_payload506, Next),
            From ! {allocated, Ref, self(), erlang:phash2(Next)},
            loop(Next);
        {gc, From, Ref} ->
            Return = erlang:garbage_collect(),
            From ! {collected, Ref, self(), Return},
            loop(Payload)
    end.
