-module(beam_on_load_fresh_success).
-on_load(init/0).
-export([run/0]).
init() ->
    Observer = whereis(beam_on_load_observer),
    true = is_pid(Observer),
    Observer ! {on_load_notice,?MODULE,self(),code:is_loaded(?MODULE),success},
    receive
        {on_load_continue,Observer} -> ok
    after 10000 ->
        error(observer_timeout)
    end.
run() -> new_image.
