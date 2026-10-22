-module(beam_catch_subject392).
-export([worker/2,probe/1,dispatch/1,only_integer/1,deep/2,tail/1]).

worker(Parent,Case) ->
    Outcome=try probe(Case) of Value->{returned,Value}
            catch Class:Reason:Stack->{caught,Class,Reason,Stack} end,
    Parent ! {Case,Outcome},
    ok.

probe(normal) -> catch ?MODULE:dispatch(normal);
probe(throw_value) -> catch ?MODULE:dispatch(throw);
probe(exit_value) -> catch ?MODULE:dispatch(exit);
probe(error_value) -> catch ?MODULE:dispatch(error);
probe(badarg) -> catch erlang:length(not_list);
probe(function_clause) -> catch ?MODULE:only_integer(not_integer);
probe(nested_throw) -> catch {before_inner,(catch ?MODULE:dispatch(throw)),after_inner};
probe(nested_error) -> catch begin Inner=catch ?MODULE:dispatch(error), erlang:error({outer,Inner}) end;
probe(catch_in_try) ->
    try (catch ?MODULE:dispatch(error)) of Value->{try_value,Value}
    catch C:R:S->{outer_caught,C,R,S} end;
probe(try_in_catch) ->
    catch try ?MODULE:dispatch(throw)
          catch throw:Reason->erlang:error({converted,Reason}) end;
probe(after_in_catch) ->
    Result=catch try ?MODULE:dispatch(error)
                 after erlang:put(after_ran,{after_value,[1,2]}) end,
    {Result,erlang:get(after_ran)};
probe(raw_rethrow) ->
    try ?MODULE:dispatch(error)
    catch Class:Reason:Stack -> catch erlang:raise(Class,{replaced,Reason},Stack) end;
probe(tail_error) -> catch ?MODULE:tail(64);
probe(deep_success) -> catch ?MODULE:deep(8192,normal);
probe(deep_error) -> catch ?MODULE:deep(8192,error);
probe(trim_normal) -> trim_candidate(normal,{first,[1,2]},{second,#{a=>1}});
probe(trim_error) -> trim_candidate(error,{first,[1,2]},{second,#{a=>1}});
probe(normal_then_error) ->
    First=catch ?MODULE:dispatch(normal), Second=catch ?MODULE:dispatch(error), {First,Second}.

dispatch(normal) -> {value,[1,2],#{a=>one,b=>two}};
dispatch(throw) -> erlang:throw({thrown,[1,2],#{a=>one}});
dispatch(exit) -> erlang:exit({exited,[1,2],#{a=>one}});
dispatch(error) -> erlang:error({failed,[1,2],#{a=>one}}).
only_integer(Value) when is_integer(Value) -> Value.
tail(0) -> ?MODULE:dispatch(error);
tail(N) -> tail(N-1).
deep(0,Mode) -> case Mode of normal->0; error->erlang:error(deep_reason) end;
deep(N,Mode) -> Value=deep(N-1,Mode), Value+1.
trim_candidate(Mode,First,Second) ->
    Result=catch begin
        erlang:put(trim_first,First), erlang:put(trim_second,Second),
        ?MODULE:dispatch(Mode)
    end,
    {Result,erlang:get(trim_first),erlang:get(trim_second)}.
