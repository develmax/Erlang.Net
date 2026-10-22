-module(beam_warning_subject398).
-export([worker/2,probe/1]).
worker(Parent,Case) ->
    Outcome=try probe(Case) of Value->{returned,Value}
            catch Class:Reason:Stack->{caught,Class,Reason,Stack} end,
    Parent!{Case,Outcome},ok.
probe(direct) -> error_logger:warning_map();
probe(dynamic_apply) -> erlang:apply(error_logger,warning_map,[]);
probe(external_fun) -> F=fun error_logger:warning_map/0,F();
probe(repeat) -> {error_logger:warning_map(),error_logger:warning_map()};
probe(wrong_arity) -> erlang:apply(error_logger,warning_map,[unexpected]).
