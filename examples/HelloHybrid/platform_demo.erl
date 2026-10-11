-module(platform_demo).
-export([run/0, init/1, start_worker/0, handle_call/3, handle_info/2, terminate/2]).
-record(state, {values = #{}, timer = undefined}).

run() ->
    {ok, Supervisor} = supervisor:start_link(platform_demo, supervisor),
    {ok, {cache, Worker, worker, [platform_demo]}} = supervisor:which_child(Supervisor, cache),
    scheduled = gen_server:call(Worker, {refresh, self()}),
    receive {refreshed, 100} -> ok after 2000 -> error(refresh_timeout) end,
    Values = gen_server:call(Worker, snapshot),
    [2, 3] = lists:sort(fun(A, B) -> A =< B end, maps:keys(Values)),
    100 = maps:fold(fun(_, Value, Acc) -> Value + Acc end, 0, Values),
    ok = supervisor:stop(Supervisor),
    {platform_ready, 100, [2, 3]}.

init(supervisor) ->
    {ok, {#{strategy => one_for_one}, [#{id => cache, start => {platform_demo, start_worker, []}}]}};
init(worker) ->
    {ok, #state{}}.

start_worker() ->
    gen_server:start_link(platform_demo, worker, []).

handle_call({refresh, Parent}, _, State) ->
    Timer = erlang:send_after(0, self(), {refresh, Parent}),
    {reply, scheduled, State#state{timer = Timer}};
handle_call(snapshot, _, State) ->
    {reply, State#state.values, State}.

handle_info({refresh, Parent}, State) ->
    Pairs = lists:foldr(fun(Key, Acc) -> [{Key, Key * 10} | Acc] end, [], [1, 2, 3]),
    Selected = maps:filter(fun(Key, _) -> Key > 1 end, maps:from_list(Pairs)),
    Values = maps:map(fun(_, Value) -> Value * 2 end, Selected),
    Sum = maps:fold(fun(_, Value, Acc) -> Value + Acc end, 0, Values),
    Parent ! {refreshed, Sum},
    {noreply, State#state{values = Values, timer = undefined}}.

terminate(_, State) ->
    case State#state.timer of
        undefined -> ok;
        Timer -> erlang:cancel_timer(Timer), ok
    end.
