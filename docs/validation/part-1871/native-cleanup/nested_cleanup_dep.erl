-module(nested_cleanup_dep).
-export([read/2, write/2]).
read(Result, _) -> Result.
write(_, Result) -> Result.
