# Part541 — rootless shutdown publication correction

Windows CI93/94 stopped after the Megaco/receive block. Exact-head local Megaco and its original helper settle in 4.187/4.328 seconds. Unmodified TestAnalysis produces all 2,463 original-matching outcomes but does not finish runtime disposal within 90 seconds. Temporary diagnostic instrumentation identifies empty EXIT graph registration after mailbox admission closes. The resulting ObjectDisposedException interrupts termination, leaving four uncancelled receive actors and their real payload641 records alive. Those timeouts and diagnostic failures are retained.

The correction omits the empty593 composite when both source graphs are null. It retains the stable message, payload641 ownership, publication and actual retirement. The nonempty graph branch and closed/foreign-owner checks are unchanged. This does not establish resource-bearing derived publication during shutdown; its separate anchored-interest protocol remains open.

The permanent regression closes mailbox admission, publishes a rootless payload and joins its real completion and VM disposal. Against the original production code it fails with the observed ObjectDisposedException; against the correction it passes. The diagnostic source files and temporary native-driver modes are absent from final production sources.

Final unchanged sources pass Debug371/371 and Release371/371 as explicit disjoint composites140+140+19+38+33+1, with unchanged DLL inventories within each profile. Each final uninstrumented TestAnalysis process exits normally (8.156 seconds Debug,7.937 seconds Release). A separate fresh execution of pinned OTP29.1.1 matches all2,463 complete outcomes/stacks in each profile. Whole deterministic ETF SHA256: C4ED56E9C3C32D0831FD1F785DDA1D730654EC6AB9DE10A5F7FDCA024263B2B7. No projection, identity rewriting or frame removal.

The initial preformat sandbox371 attempt is incomplete after child-process/internal-port failures and a timeout; it is not accepted as a passing regression or silently combined with the final outside runs. Its raw failures remain archived. Final four-file SourceLayout reports0; worktree/index bytes equal the freeze. All8 metadata gates and12 early gates pass, with3,989 exact members and2,220 source guards. The new [current76 freeze](../part-541/current-source-freeze.json) preserves the historical part541 freeze and rebinds the focused645 collector.

[Summary](summary.json), [archive receipt](archive.json), [raw evidence](evidence.zip). The archive contains272 byte-verified members (3,206,361 bytes), SHA256 F044866DDCFF8EF4B7B94753D4AE5ACB1018178B16DFBE7D824CC07596BEF3A9.

Expected catalog9,429; no local full-suite claim. API/full-contract deltas0/0; registered708, verified180, pending504 and24 known differences remain unchanged. Exact publication-head full CI, current review, merge and latest-main CI are separate pending gates.
