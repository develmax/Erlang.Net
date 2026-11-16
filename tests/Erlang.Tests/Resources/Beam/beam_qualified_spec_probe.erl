-module(beam_qualified_spec_probe).
-moduledoc false.
-export([run/1, other/1]).
-spec(beam_qualified_spec_probe:run(Value :: T) -> T when T :: integer()).
run(Value) -> Value.
-spec beam_qualified_spec_probe:other(Value :: atom()) -> atom().
other(Value) -> Value.
