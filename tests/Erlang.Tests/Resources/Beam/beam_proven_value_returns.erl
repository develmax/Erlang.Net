-module(beam_proven_value_returns).
-export([keep/1,select/1]).
keep(Value) when Value =:= ready -> ready;
keep(Value) -> {other,Value}.
select(cold) -> cold;
select(warm) -> warm;
select(hot) -> warm;
select(Other) -> {other,Other}.
