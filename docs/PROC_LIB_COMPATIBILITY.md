# proc_lib local process foundation

Reference: [OTP-29.1.1 proc_lib.erl](https://github.com/erlang/otp/blob/ad05823719d77c8faee87348ea39513d4e2f99c5/lib/stdlib/src/proc_lib.erl).

Part331 registers six of the39 declarations in the current inventory. Status:
**Partially compatible**. The local implementation passes Main6775 tests and
eleven integration/package checks; original331 observations remain pending.

| API | Implemented contract |
|---|---|
| spawn/1, spawn_link/1 | Child fun invocation, ancestry and fun MFA metadata; nonzero fun arity fails in the child |
| spawn/3, spawn_link/3 | Local MFA invocation, ancestry and translated initial-call metadata; proper caller/child failure boundaries |
| init_ack/1 | Parent comes from the head of '$ancestors'; malformed/absent ancestry retains badmatch |
| init_ack/2 | Sends {ack,self(),Return} to the supplied destination and returns ok; Return is arbitrary |

The child inherits only the ancestry entry, using the creator's registered name
when present and its PID otherwise. A list-shaped ancestry value retains its
tail, including an improper tail; nonlist ancestry becomes empty. MFA argument
guards likewise accept Cons/Nil, so an improper argument list fails inside the
child rather than becoming a caller function_clause. Normal malformed module,
function or nonlist arguments fail in the caller.

The implementation reuses the existing runtime's atomic link and spawn-trace
path. Native process_info(initial_call) describes the proc_lib:init_p wrapper;
the process dictionary's '$initial_call' describes the user call or OTP behaviour
translation. Nine HOST controls check the source-defined gen translations.
These are separate identities and must not be substituted for one another.

Permanent added coverage consists of six direct registered-MFA contracts,
21 compiled Erlang scenarios and one HOST group containing nine translation
controls. Existing6747 ordered tests and509 registered rows remain intact.

Not implemented in this slice: synchronous start/start_link/start_monitor,
init_fail, option-bearing and remote overloads, crash logger integration and
complete error-stack parity. New proc_lib-specific tracing has source-route
review but no new original tracing observation. API registration and local test
success do not establish the complete module or a performance improvement.

The next slice follows [dependency priorities](DEPENDENCY_PRIORITIES.md): real
ack/nack/DOWN startup with timeout and mailbox preservation, then gen/sys.
