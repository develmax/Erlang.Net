-module(beam_local_record_checks).
-export([read/1, read_mixed/1, read_checked_mixed/1, escape/0, exposed/1, read_escaped/1, read_exposed/1, constructed/1, literal_shape/0, literal_wrong_arity/0, updated/2]).

-record(entry, {value}).
-record(wide, {a, b, c, value}).

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
literal_shape() -> from_literal({entry, literal}).
from_literal(#entry{value=Value}) -> Value.
literal_wrong_arity() -> from_wrong_arity({entry, wrong, arity}).
from_wrong_arity(#entry{value=Value}) -> Value.
updated(Record = #wide{}, Value) ->
    Updated = Record#wide{value=Value},
    put(updated, Updated),
    from_updated(Updated).
from_updated(#wide{value=Value}) -> Value.
