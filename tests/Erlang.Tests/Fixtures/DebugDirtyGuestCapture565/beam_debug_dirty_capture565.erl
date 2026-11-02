-module(beam_debug_dirty_capture565).
-export([values/0,stacks/0,own/0]).

values() -> erlang:term_to_binary(beam_debug_dirty424:run(),[{minor_version,2}]).
stacks() -> erlang:term_to_binary(beam_debug_dirty424:run_stacks(),[{minor_version,2}]).
own() -> erlang:term_to_binary(beam_debug_dirty424:run_own(),[{minor_version,2}]).
