-module(beam_external_doc).
-moduledoc({file, "module.md"}).
-export([run/0]).
-doc {file, "function.md"}.
run() -> ok.
