-module(beam_deprecated_docs_probe).
-moduledoc "Deprecated API".
-export([old/0, fallback/1, custom/1]).
-export_type([legacy/0]).

-deprecated([{old,0,next_version},{'_','_',"Use the replacement"}]).
-deprecated_type({legacy,0}).
-type legacy() :: atom().

-spec old() -> legacy().
old() -> old.
fallback(Value) -> Value.
-doc(#{deprecated => <<"Explicit note">>}).
custom(Value) -> Value.
