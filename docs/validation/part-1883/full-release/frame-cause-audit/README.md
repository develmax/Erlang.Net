# Part1883 remaining frame-size increases

| Function | Windows OTP-29.1.1 | Part1875 | Part1878 | Exact Part1882 | Part1883 |
| --- | ---: | ---: | ---: | ---: | ---: |
| erl_bif_types:'-type/4-fun-111-'/1 | 1 | 2 | 2 | 2 | 4 |
| erl_prim_loader:'-load_prim_archive/3-fun-0-'/2 | 1 | 2 | unavailable | 2 | 3 |
| systools_make:'-check_src/4-fun-0-'/3 | 1 | 1 | 2 | 2 | 2 |

The first two widths are verified Part1883 regressions against exact Part1882. The third predates Part1883. They are separate from the parent audit's confirmed diameter_dict_util:vendor/1 and et_wx_viewer:queue_to_list/1.

In erl_bif_types:type/4, the keyreplace callback matches [IgnoredKey, IgnoredIndex, List, Tail]. Part1882 splits the first two cons cells with get_list and reuses x1 as the discarded head. Part1883 replaces both with get_tl and carries the intermediate tail and later projected operands in Y through the shape checks. It uses four Y cells instead of two.

In erl_prim_loader:load_prim_archive/3, the callback matches a tuple with unused GI and GB fields and then checks Components against a three-element list. Part1882 keeps the unused get_tuple_element operations for fields 1 and 2 in x1. Its list projections use x1 and x2 scratch with one temporary Y plus the live accumulator. Part1883 removes those two getters and puts relatively more list intermediates in Y. It uses three instead of two Y cells.

The sealed Part1882→1883 source delta contains AdjacentNumericResults, DeadStackProjections and SharedReturns. AdjacentNumericResults only annotates typed operands in EmitCode after the frame passes. DeadStackProjections therefore accounts for the frame change through its timing before the established liveness rewrites. This comparison isolates the transform ordering. It does not isolate the one downstream optimization responsible for each changed register. The root's late-pass candidate needs to preserve the prior two-slot schedules in these two release functions even if it removes some remaining projections.

In systools_make:check_src/4, the callback decomposes ModT and calls check_mod/5. Part1875's width-1 body keeps the original tuple in Y0 and shifts the two captures to X5/X4 before projecting Mod/App/Dir directly to X0/X1/X2. Part1878 and later keep the tuple in Y1, assign the Dir field to Y0 and load it into X2 on the call path. The sealed source delta includes EntryArgumentFlow's newly admitted pre-call copy route across a checked tuple projection. That is the source-level match for the reordered argument copies. The exact first affected compiler stage between Part1875 and Part1878 was not replayed.

Three Part1882 modules were compiled once on one worker in 26.07 seconds from the pinned release inputs. The compiler SHA 14634545B76CA5846FCD6E50C011CFCB81CD25EC0E5CA03F993BC99C6928D223 and corpus tool SHA 596F74E99D68E65916006324AF26CF48169E821BF9E36C26727D1FBAB8C6470D match the sealed Part1882 run. The exact same CLI DLL was copied from retained Part1881 and used with the frozen Part1882 compiler. A subsequent direct read verified that the original Part1882 corpus-bin copy carries both identical hashes. The downstream modules remained Different. See the raw paired disassembly, source diffs and proof manifests in this lane.

