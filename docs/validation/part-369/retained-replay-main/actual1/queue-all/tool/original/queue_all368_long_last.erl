-module(queue_all368_long_last).
-export([cases/0,run/0,run_stacks/0,probe/1]).
cases() -> [get_last64017,
            get_r_last64017,
            peek_last64017,
            peek_r_last64017].
run() -> queue_all368_support:run(queue_all368_long_last,cases()).
run_stacks() -> queue_all368_support:profiles(queue_all368_long_last,cases()).
probe(get_last64017) -> queue:get({queue_all368_support:bad_list(64017),[]});
probe(get_r_last64017) -> queue:get_r({[],queue_all368_support:bad_list(64017)});
probe(peek_last64017) -> queue:peek({queue_all368_support:bad_list(64017),[]});
probe(peek_r_last64017) -> queue:peek_r({[],queue_all368_support:bad_list(64017)}).
