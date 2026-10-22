-module(beam_filename_subject395).
-export([worker/2,probe/1]).
worker(Parent,Case) ->
    Outcome=try probe(Case) of Value->{returned,Value}
            catch Class:Reason:Stack->{caught,Class,Reason,Stack} end,
    Parent!{Case,Outcome},ok.
probe(direct) -> file:native_name_encoding();
probe(dynamic_apply) -> erlang:apply(file,native_name_encoding,[]);
probe(external_fun) -> F=fun file:native_name_encoding/0,F();
probe(repeat) -> {file:native_name_encoding(),file:native_name_encoding()};
probe(wrong_arity) -> erlang:apply(file,native_name_encoding,[unexpected]).
