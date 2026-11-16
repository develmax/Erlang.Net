# Part 1328

A candidate retained only the closure capture across a call and extracted the tuple argument directly into X0. The focused closure matched all OTP instructions. The full Windows run generated all 1,366 images and returned 363 Equal, 1,003 Different and zero SourceRejected. Hash comparison found no changed differing images, so the candidate was removed. OTP accepted all 1,003 differing images.

The allocation audit compares each release function's first allocate with the generated counterpart and groups the gaps by entry test and first non-tail call arity. The largest group contains 2,362 functions in 547 modules with a nonempty-list test and a two-argument first call. The group measures candidate coverage and includes different source shapes.

In xmerl_xsd_type:collapse_ws/2, OTP shares the first cons extraction across adjacent clauses, tests the second head without extracting the second tail, and reaches both recursive tail calls before the fallback frame. The generated code allocates four slots at entry and tests and extracts the first cons twice.

- [Allocation audit](checked-entry-frame-ranking.txt)
- [Audit script](rank-checked-entry-frames.eval)
- [Release comparison](xmerl-collapse-ws.txt)
- [Full candidate corpus](otp-corpus.json)
- [Focused candidate output](probe.txt)
- [OTP loader](loader.txt)
