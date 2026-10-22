-module(beam_image388).
-export([version/0, capture/2]).

version() -> generation_one.

capture(Owner, Case) ->
    Result = try
        Owner ! {held_ready, self(), version()},
        receive
            {release, Case} -> local_failure(Case)
        end
    catch
        Class:Reason:Stack ->
            {old_handler, version(), after_catch(), Class, Reason, Stack}
    end,
    Owner ! {image_report, self(), Case, Result},
    ok.

local_failure(held_error) -> erlang:error({old_image_error, generation_one});
local_failure(held_throw) -> erlang:throw({old_image_throw, generation_one}).

after_catch() -> {local_after_catch, generation_one}.
