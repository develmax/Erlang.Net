# Pending-tail branch evaluation

The candidate was built from frozen Part 1853 source with isolated .NET SDK 10.0.400, at BelowNormal and with 2 available managed processors. Compiler build took 64.80 seconds with zero warnings or errors. Compiler DLL SHA256 is 1790626B95341A2BA9CA76335DB038C047A5A8FAB615B066249D97AD3A515728. The cumulative patch SHA256 is 700D6C115E33213798C116DF8CA837AF7BBED0E862E862BB0CE05FA1E0905BF7. The compiled implementation files match the staged proposal hashes.

The permanent projection regression passed on the candidate and failed at its new placement assertion on the frozen compiler. All 12 pinned Windows OTP 29.1.1 fixture executions passed. The two runtime negative fixture images were byte-identical to their frozen generated images in this run.

Nine release modules ran uncached in 14.05 seconds. Status totals stayed 3 Equal, 6 Different, 0 Unsupported and 0 SourceRejected. No full-file or normalized-function equality was gained or lost. Sixteen function bodies changed in four modules. All sixteen increased the matching leading opcode sequence and no opcode prefix shortened. Every changed module had changed Code, Attr and FunT sections. The twelve changed-module image versions loaded on ERTS and had identical exports across reference, frozen generated and candidate images. The runtime execution observations cover the four source fixtures. Release functions were inspected by disassembly and admission.

The final generated ct_snmp image matches frozen Part 1853 at SHA256 859D6B05D21FCCA8B955CF66D005832918D1F2F699D032BE11F20F155417CBC4. Its two nominated method controls and the three tagged MEGACO method controls retained the precise raw opcode and operand bodies. The four positive representative methods still gain the two leading projection instructions. The three previously Equal release controls stay Equal. The normalized body changes and prefix table preserve the individual method results.

The pending-tail continuation extraction retained the old emission choices in FoldTerminalCalls. It resolves the observed pre-fold false positive without moving or globally reordering that pass. The broader Part 1855 acceptance baseline remains separate. No package-wide or full-corpus result is inferred for this staged candidate. Remaining common dependencies include tail-phi continuation distribution and multi-atom case dispatch, most directly shown by tr_MId/2.

Raw receipts include build.log, test-receipt.log, test-red.log, test-green.log, fixture/runtime-outcomes.log, cohort.json, cohort-timing.txt, cohort-delta.json, cohort-normalized.terms, cohort-method-shapes.json and cohort-loader.terms.

