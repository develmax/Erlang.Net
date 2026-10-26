-module(distribution_link_receiver325).
-export([worker/1]).

-define(ARM_MS, 45000).
-define(EXIT_MS, 45000).

worker(Owner) ->
    _ = process_flag(trap_exit, true),
    Owner ! {worker_ready, self(), process_info(self(), trap_exit)},
    receive
        {expect_exit, Observer, Nonce, Target} when is_pid(Observer), is_binary(Nonce), is_pid(Target) ->
            true = Nonce =:= distribution_link_retention325_driver:nonce(),
            Armed = {receiver_armed, Nonce, self(), Target},
            distribution_link_retention325_driver:save("receiver-arm.etf", Armed),
            Observer ! Armed,
            receive
                {'EXIT', Target, killed} ->
                    Proof = {linked_exit, Nonce, self(), Target, killed},
                    distribution_link_retention325_driver:save("receiver-exit.etf", Proof),
                    Observer ! Proof,
                    hold()
            after ?EXIT_MS -> erlang:error(linked_exit_timeout)
            end
    after ?ARM_MS -> erlang:error(receiver_configuration_timeout)
    end.

hold() -> receive _ -> hold() end.
