# proc_lib local process foundation

Reference: [OTP29.1.1 proc_lib.erl](https://github.com/erlang/otp/blob/ad05823719d77c8faee87348ea39513d4e2f99c5/lib/stdlib/src/proc_lib.erl).

Part332 registers12 of39 declarations. Status: **Partially compatible**.
There are521 registered APIs across eight modules; erlang remains351.
API presence does not establish complete semantic compatibility.

| APIs | Implemented local contract |
|---|---|
| spawn/1,3; spawn_link/1,3 | Existing atomic process creation, creator ancestry, caller/child guard boundaries and initial-call metadata |
| init_ack/1,2 | Parent from ancestry or explicit destination; arbitrary acknowledgement return term |
| start/3,4; start_link/3,4 | Real monitor, optional atomic link, arbitrary ACK return, real DOWN after NACK, unlink-before-timeout kill and selective EXIT cleanup |
| init_fail/2,3 | Send NACK before error/exit/throw or explicit-stack raise; preserve parent lookup and malformed-exception failure boundaries |

Creator ancestry uses its registered name or PID and preserves list-shaped tails,
including improper tails. Native process_info(initial_call) describes the
proc_lib:init_p wrapper; '$initial_call' describes the user call or source-defined
gen behaviour translation. These identities remain separate.

Shared receive now evaluates a timeout expression once before selection but
converts its value only when a genuine no-match wait is required. Matching queued
ACK/NACK/DOWN therefore precede invalid timeout-value rejection. No dummy receive
or false timeout event is added. Exact integer zero retains the SDK snapshot
policy; identical VM scheduling under continuous concurrent arrivals is unclaimed.

Actual tests:6822/6822, retaining all6775 previous ordered names. This slice adds
31 compiled startup scenarios, eight compiled receive scenarios, six direct API
contracts and two HOST groups containing three real queued startup controls and
ten interpreted controls. Prior21 proc_lib scenarios and nine initial-call
translation controls remain. All515 previous registry rows/custom Limits are exact.

All eleven integration/package outcomes pass cumulatively. The first isolated
tools/validate.ps1 stopped at first pack with NU5019 because README.md was absent.
Six unchanged tracked package resources were supplied and the exact uncompleted
package suffix passed; tests were not rerun. The initial failure is retained.
All2431 tested source/resource bytes match their promoted Main counterparts.
Original332 and current-head CI remain pending publication.

Three fresh candidate Windows native roles pass legacy, live owner/query and
reallocation checks. These SDK observations remain separate from original OTP
execution and do not establish physical-resource equality.

Published331 original observations now pass5962/5962 plus descriptor8, including
all21 first-slice proc_lib scenarios and all5941 previous contracts. Ordinary
strict raw variance is zero. Its other four terminal typed scopes also pass;
the distribution strict text-representation refusal remains separate from its
six successful semantic groups and is retained without normalization.

Not implemented: start_monitor, option-bearing and remote overloads, crash logger
integration, complete stack parity and gen/sys system messages. No performance
improvement or whole-module completion is inferred. See [priorities](DEPENDENCY_PRIORITIES.md)
and [actual cumulative validation](validation/part-332/final-candidate/full-guard.json).
