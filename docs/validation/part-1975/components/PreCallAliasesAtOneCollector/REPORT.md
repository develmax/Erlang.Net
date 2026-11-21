# Pre-call aliases across one collector

This proposal extends the Part 1974 owned-frame alias proof. A value still live in its original X register may replace the saved copy across one explicit heap check or GC BIF. Every newly rooted X register must correspond to one old Y root in the same window. Register writes, an intervening entry label, a second collector and a gap below the additional root reject the candidate. The existing frame compactor owns parameter, initialization and exit-width changes. The output path never reads the pinned bytes.

Pinned `beam_ssa_pre_codegen:find_yregs_is/3` adds values to Y when a later use crosses a register-clobbering operation. `beam_ssa_codegen:liveness_anno/3` derives the X root prefix at a collection point. The proposal preserves each ordinary call, GC BIF failure label and source line. The narrowing depends on established frame ownership and the X value read by the existing spill.

The immutable Part 1971 diagnostic scope contains 17 candidate sites in 16 modules, including 14 heap-check and three GC-BIF sites. This is a static upper bound independent of the already qualified no-GC group. The [proof and actual beam pairs](../precall-owned-slot-def-use-1974-audit/REPORT.md) include tagged tuple allocation, closure capture and arithmetic results. The permanent synthetic test covers heap, GC BIF and a two-root prefix, and rejects an X overwrite, a gap and two safepoints.

Eight native scenarios compare real `xmerl_xsd_type:modulo/3` from pinned OTP and the generated Part 1972 release. The deterministic term files match at F984FDDF499CBFFEF0A14C50386371D4B32CE9C5326C2344CC31494D34C5A317. They include signed and bignum arithmetic, a float, badarith and exact failure line sequences. The candidate still requires compilation, its focused and scoped regressions, a fresh native run and raw comparison.

