-module(queue_all368_long_reverse_ends).
-export([cases/0,run/0,run_stacks/0,probe/1]).
cases() -> [out_r_front256017,
            drop_r_front256017].
run() -> queue_all368_support:run(queue_all368_long_reverse_ends,cases()).
run_stacks() -> queue_all368_support:profiles(queue_all368_long_reverse_ends,cases()).
probe(out_r_front256017) -> queue:out_r({[],[head|queue_all368_support:bad_list(256017)]});
probe(drop_r_front256017) -> queue:drop_r({[],[head|queue_all368_support:bad_list(256017)]}).
