-module(beam_on_load_replace_failure).
-on_load(init/0).
-export([run/0]).
init() ->
    Observer = whereis(beam_on_load_observer),
    true = is_pid(Observer),
    Observer ! {on_load_notice,?MODULE,self(),code:is_loaded(?MODULE),failure},
    receive
        {on_load_continue,Observer} -> not_ok
    after 10000 ->
        error(observer_timeout)
    end.
run() -> new_image.
