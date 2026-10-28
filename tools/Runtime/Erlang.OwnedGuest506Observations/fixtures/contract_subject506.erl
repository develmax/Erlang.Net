-module(contract_subject506).
-export([held/3, entry0/0, start_ack/3, start_fail/3, crash/2, hibernate/3, wake/3]).

entry0() ->
    Parent = whereis(contract_controller506),
    held(Parent, tuple_entry, []).

held(Parent, Token, Payload) ->
    put(contract_payload506, Payload),
    Parent ! {held, Token, self()},
    loop(Payload).

loop(Payload) ->
    receive
        {replace, From, Ref, Next} ->
            put(contract_payload506, Next),
            From ! {reply, Ref, self(), replaced},
            loop(Next);
        {drop, From, Ref} ->
            erase(contract_payload506),
            From ! {reply, Ref, self(), dropped},
            loop([]);
        {gc, From, Ref, Type} ->
            Result = erlang:garbage_collect(self(), [{type, Type}]),
            From ! {reply, Ref, self(), Result},
            loop(Payload);
        {self_info, From, Ref, Items} ->
            Value = erlang:process_info(self(), Items),
            From ! {reply, Ref, self(), Value},
            loop(Payload);
        {stop, From, Ref} ->
            From ! {reply, Ref, self(), stopped},
            ok
    end.

start_ack(Parent, Token, Return) ->
    proc_lib:init_ack(Return),
    held(Parent, Token, []).

start_fail(Return, Class, Reason) ->
    proc_lib:init_fail(Return, {Class, Reason}).

crash(Class, Reason) ->
    erlang:raise(Class, Reason, []).

hibernate(Parent, Token, Payload) ->
    Parent ! {hibernating, Token, self()},
    proc_lib:hibernate(?MODULE, wake, [Parent, Token, Payload]).

wake(Parent, Token, Payload) ->
    Parent ! {woke, Token, self(), get('$ancestors'), get('$initial_call')},
    held(Parent, Token, Payload).
