-module(beam_local_record_checks).
-export([read/1, read_mixed/1, read_checked_mixed/1, escape/0, exposed/1, read_escaped/1, read_exposed/1, constructed/1]).

-record(entry, {value}).

read(Record = #entry{}) ->
    put(last_record, Record),
    read_private(Record).

read_private(#entry{value=Value}) -> Value.

read_mixed(Value) -> mixed(Value).
read_checked_mixed(Record = #entry{}) -> mixed(Record).
mixed(#entry{value=Value}) -> Value.

escape() -> fun escaped/1.
read_escaped(Record = #entry{}) -> escaped(Record).
read_exposed(Record = #entry{}) -> exposed(Record).
escaped(#entry{value=Value}) -> Value.
exposed(#entry{value=Value}) -> Value.
constructed(Value) -> from_constructed(#entry{value=Value}).
from_constructed(#entry{value=Value}) -> Value.
