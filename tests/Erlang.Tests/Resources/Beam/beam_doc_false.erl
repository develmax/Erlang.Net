-module(beam_doc_false).
-moduledoc false.
-export([a/0]).
-type private() :: atom().
a() -> ok.
