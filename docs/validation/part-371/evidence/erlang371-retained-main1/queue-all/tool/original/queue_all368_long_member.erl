-module(queue_all368_long_member).
-export([cases/0,run/0,run_stacks/0,probe/1]).
cases() -> [member_miss_256017,
            member_first_256017,
            member_last_256017,
            member_front_miss_256017].
run() -> queue_all368_support:run(queue_all368_long_member,cases()).
run_stacks() -> queue_all368_support:profiles(queue_all368_long_member,cases()).
probe(member_miss_256017) -> queue:member(missing,{queue_all368_support:bad_list(256017),[]});
probe(member_first_256017) -> queue:member(1,{queue_all368_support:bad_list(256017),[]});
probe(member_last_256017) -> queue:member(256017,{queue_all368_support:bad_list(256017),[]});
probe(member_front_miss_256017) -> queue:member(missing,{[],queue_all368_support:bad_list(256017)}).
