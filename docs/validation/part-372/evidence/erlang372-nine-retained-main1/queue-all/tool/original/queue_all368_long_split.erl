-module(queue_all368_long_split).
-export([cases/0,run/0,run_stacks/0,probe/1]).
cases() -> [split_front256017,
            split_rear256017,
            split_zero_lazy64017].
run() -> queue_all368_support:run(queue_all368_long_split,cases()).
run_stacks() -> queue_all368_support:profiles(queue_all368_long_split,cases()).
probe(split_front256017) -> queue:split(1,{[],queue_all368_support:bad_list(256017)});
probe(split_rear256017) -> queue:split(2,{queue_all368_support:bad_list(256017),[front]});
probe(split_zero_lazy64017) -> queue:split(0,{queue_all368_support:bad_list(64017),[]}).
