-module(integer_contract493_list_to_integer1_p1).
-export([run/0,run_stacks/0,probe/1]).
run() -> ordsets_core_support:run(?MODULE,cases()).
run_stacks() -> ordsets_core_support:profiles(?MODULE,cases()).
cases() -> [c80,c81,c82,c83,c84,c85,c86,c87].
probe(c80) -> erlang:apply(erlang,opaque(list_to_integer),[[18446744073709551616]]);
probe(c81) -> erlang:apply(erlang,opaque(list_to_integer),[[18446744073709551616,49]]);
probe(c82) -> erlang:apply(erlang,opaque(list_to_integer),[[49,18446744073709551616]]);
probe(c83) -> erlang:apply(erlang,opaque(list_to_integer),[lists:duplicate(20,49)++[305]]);
probe(c84) -> erlang:apply(erlang,opaque(list_to_integer),[[304|lists:duplicate(20,49)]]);
probe(c85) -> erlang:apply(erlang,opaque(list_to_integer),[lists:duplicate(20,304)]);
probe(c86) -> erlang:apply(erlang,opaque(list_to_integer),[[43,304]]);
probe(c87) -> erlang:apply(erlang,opaque(list_to_integer),[[45,304]]).
opaque(Value) -> put(integer_contract493_input,Value),erase(integer_contract493_input).
