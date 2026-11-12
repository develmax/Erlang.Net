-module(wxe_util).
-export([queue_cmd/3, rec/1]).
queue_cmd(Options, Ref, Command) ->
    put(wx_probe_command, {Options, Ref, Command}),
    queued.
rec(Command) -> {received, Command}.
