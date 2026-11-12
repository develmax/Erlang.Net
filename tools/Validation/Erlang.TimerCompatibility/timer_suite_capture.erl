-module(timer_suite_capture).
-export([write/1,project/2,worker/2]).

write(Directory) ->
    Tests=lists:append([Names || {_,_,Names}<-timer_simple_SUITE:groups()]) -- [timer_perf],
    Rows=[{Name,observe(Name)} || Name<-Tests],
    ok=file:write_file(filename:join(Directory,"upstream.etf"),term_to_binary(Rows)).

project(Input,Output) ->
    {ok,Bytes}=file:read_file(Input),
    Rows=binary_to_term(Bytes),
    true=lists:all(fun({_,{ok,_}})->true;(_)->false end,Rows),
    Projected=[{Name,case Result of {ok,R} when is_reference(R)->{ok,reference};_->Result end}
               || {Name,Result}<-Rows],
    ok=file:write_file(Output,term_to_binary(Projected)).

observe(Name) ->
    P=spawn(?MODULE,worker,[Name,self()]),
    receive {P,Result}->Result after 30000->error({upstream_timeout,Name}) end.

worker(Name,Parent) ->
    Result=try
        Config=timer_simple_SUITE:init_per_testcase(Name,[]),
        apply(timer_simple_SUITE,Name,[Config])
    of
        Value when is_reference(Value)->{ok,reference};
        Value->{ok,Value}
    catch C:R:S->{C,R,S}
    end,
    Parent!{self(),Result}.
