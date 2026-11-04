-module(gen_statem_port_second).
-export([callback_mode/0,idle/3,active/3,terminate/3]).
callback_mode() -> [state_functions,state_enter].
idle(T,C,D) -> gen_statem_port_cases:handle_event(T,C,idle,D).
active(T,C,D) -> gen_statem_port_cases:handle_event(T,C,active,D).
terminate(_,_,_) -> ok.
