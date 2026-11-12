-module(spec_probe).
-moduledoc false.
-export([id/1, other/1]).
-spec(id(Value :: T) -> Result :: T when T :: integer()).
id(Input) -> Input.
-spec other(A :: atom()) -> atom(); (B :: integer()) -> integer().
other(Input) -> Input.
