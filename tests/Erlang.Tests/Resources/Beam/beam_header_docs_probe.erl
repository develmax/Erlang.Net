-module(beam_header_docs_probe).
-moduledoc "Header types".
-include("include/beam_docs_types.hrl").
-export([id/1]).
-spec id(shared()) -> shared().
id(X) -> X.
