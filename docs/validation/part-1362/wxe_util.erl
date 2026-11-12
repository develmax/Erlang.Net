-module(wxe_util).
-export([queue_cmd/4, queue_cmd/5, rec/1]).

queue_cmd(First, Second, Ref, Command) ->
    put(wx_probe_command, {First, Second, Ref, Command}),
    queued.

queue_cmd(First, Second, Third, Ref, Command) ->
    put(wx_probe_command, {First, Second, Third, Ref, Command}),
    queued.

rec(Command) -> {received, Command}.
