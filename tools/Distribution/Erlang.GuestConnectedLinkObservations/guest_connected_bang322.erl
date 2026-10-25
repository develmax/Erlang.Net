-module(guest_connected_bang322).
-export([bang/2]).

bang(Pid, Message) -> Pid ! Message.
