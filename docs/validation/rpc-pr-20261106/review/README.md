# rpc review validation

The initial C# backend put a healthy node in BadNodes when the called function threw either {erpc, timeout} or {erpc, noconnection}. Both cases reproduced against the OTP reference. The fix classifies only the error result envelope as a node failure.

Both backends pass 243 observations and 18 scenarios at backtrace depths 0, 8 and 32. The depth 8 run also passes all 13 connected checks per backend. The focused build and source admission checks pass. See [the review finding](https://github.com/develmax/Erlang.Net/pull/132#discussion_r4497034659).
