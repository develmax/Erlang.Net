-module(lambda328_payload).
-export([make/1]).
make(Value)->fun(Argument)->{real_body,Value,Argument} end.
