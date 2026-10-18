-module(continuation_safety).
-export([terminal_fun/0,opaque_call/1,static_named/0,static_alias/0,protected_catch/0,protected_after/0,captured/0,branch/1]).
terminal_fun() -> fun terminal/0.
terminal() -> erlang:error(terminal_marker).
opaque_call(F) -> V=F(),{after_call,V}.
static_named() -> F=fun terminal/0,V=F(),{after_call,V}.
static_alias() -> F=fun terminal/0,Alias=F,V=Alias(),{after_call,V}.
protected_catch() -> F=fun terminal/0,try V=F(),{after_call,V} catch error:terminal_marker -> caught end.
protected_after() ->
    erlang:put(after_marker,missing),F=fun terminal/0,
    Result=try try V=F(),{after_call,V} after erlang:put(after_marker,executed) end
        catch error:terminal_marker -> caught end,
    {Result,erlang:erase(after_marker)}.
captured() -> F=fun terminal/0,Wrapper=fun() -> V=F(),{after_call,V} end,Wrapper().
branch(Flag) -> F=case Flag of true -> fun terminal/0; false -> fun terminal/0 end,V=F(),{after_call,V}.
