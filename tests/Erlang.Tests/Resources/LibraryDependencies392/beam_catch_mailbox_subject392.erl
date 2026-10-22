-module(beam_catch_mailbox_subject392).
-export([worker/2,probe/1]).

worker(Parent,Case) ->
    Outcome=try probe(Case) of Value->{returned,Value}
            catch Class:Reason:Stack->{caught,Class,Reason,Stack} end,
    Parent ! {Case,Outcome},
    ok.

probe(lower) -> mailbox(10);
probe(upper_inside) -> mailbox(19);
probe(float_lower) -> mailbox(10.0);
probe(float_inside) -> mailbox(19.5);
probe(reject_all) ->
    Bad={lock,not_a_pid,12}, self() ! Bad,
    Result=select(), {Result,drain()};
probe(below_then_inside) -> queued([9,10]);
probe(upper_then_inside) -> queued([20,19]);
probe(mixed_order) -> queued([9,20,10,11]);
probe(catch_mailbox_error) ->
    Result=catch begin Value=mailbox(10),erlang:error({after_selected,Value}) end,
    {Result,drain()}.

mailbox(Value) -> queued([Value]).

queued(Values) ->
    Pid=fixed_pid(),
    Bad={lock,not_a_pid,12}, self() ! Bad,
    enqueue(Pid,Values),
    Result=select(),
    {Result,drain()}.

enqueue(_,[]) -> ok;
enqueue(Pid,[Value|Rest]) -> self() ! {lock,Pid,Value},enqueue(Pid,Rest).

select() ->
    receive
        {lock,Pid,Value} when node(Pid)=:='range@fixture', Value>=10, Value<20 ->
            {selected,Value,node(Pid)}
    after 0 -> no_match
    end.

drain() -> receive Message -> [Message|drain()] after 0 -> [] end.

fixed_pid() ->
    binary_to_term(<<131,88,100,0,13,"range@fixture",42:32,0:32,7:32>>).
