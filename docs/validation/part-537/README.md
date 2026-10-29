# Part537 — Dictionary projection and timer payload custody

Local implementation and selected validation are complete. Publication, exact-head full CI, PR review, merge and main CI remain separate gates.

Dictionary get_keys/0,1 uses captured real guest borrows and result-frame list construction; exact value comparison and allocation happen outside the dictionary monitor. Public replacement does not gain canonical authority. Timers retain the complete reference/destination/message graph and detach source wrappers before publication. Callback ExecutionContext capture is suppressed. Fire, cancellation, recipient exit, rollback and normal VM disposal retain one deterministic retirement owner; actual Timer.DisposeAsync completes before backing release, outside runtime/mailbox monitors. The original start deadline is preserved.

The new selector set contains29 groups: seven dictionary custody/identity groups, one exact original dictionary observer, twenty separate timer family/lifecycle groups and one opaque alias control. Existing181 resource groups and55 timer cases form a265-group union. Final LF-pinned sources pass Debug265/265 and Release265/265, each as one owned outside-sandbox run, with unchanged per-case limits. The earlier inside-sandbox263/265 EACCES result is retained. Source freeze binds24 C# files, including the inherited536 review formatting delta; no source mutation occurred between final profiles.

A corrected original-storage native Cancel test fails the live-custody invariant and settles cleanup. The same corrected control passes after integration. Initial reflection-name/schema errors and an aggregate timeout are preparation failures, not compatibility RED evidence. The first integrated25/28 exposed three fixture mistakes: atom-key output legitimately exports through an opaque replacement; atomics/counters fixtures held their own global invocation during VM disposal. The final test uses a real resource-bearing key and ends only the fixture invocation before shutdown after genuine custody assertions.

The portable tool compiles and executes exact original dictionary source using admitted OTP29.1.1, then independently replays one actual managed observer. Both final profiles equal the original complete108-byte Outcome ETF, with protected DLL hashes, unique output windows and real actor/runtime cleanup. Exceptional stack compatibility is not claimed by this successful observer. Existing55 timer cases include error predicates, not dedicated whole-stack observers. The added required CI step reuses the existing Windows provider/job and does not repeat the full suite; independent original/managed failures remain visible.

Remaining: forced callback claim/cancel/publication/close races, broader producer/family combinations, resource-bearing canonical keys across all families, cancellation during projection, registration/name/suspension corner cases, arbitrary FunctionTerm/image-purge lifetime and general monitor/reason roots. No complete API or whole-contract count changes:708 registered /180 verified /504 pending /24 known differences, deltas0/0.

Archive SHA256: `97FF7F1825167611BF79C5A7DE2662D7E1CB91C2867033B89157DB9480D953C4`.

[evidence.zip](evidence.zip) retains actual reports, raw results, preparation failures, independent reviews and final source hashes.
