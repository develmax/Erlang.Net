# Run map call-result rooting after frame canonicalization

The stage builds against Part 1847 at source archive SHA-256 995a138378d1709c7ab635fb33a5b0b54c7fed61edf22db0f50270594e4da00c and baseline compiler SHA-256 e6506a169a2a55f7cc42ddca38cf20a1debbc6eed2405f36eda0513cdeee48bd. Reference is pinned Windows OTP 29.1.1, ad05823719d77c8faee87348ea39513d4e2f99c5. SDK 10.0.400. No new dependency.

The map-specific rewrite used to replace a stack store with a slot clear before frame and alias analysis. That altered the spill simplification and slot reuse proof in ct_doctest:try_parse_exprs/1. The same proof now runs after the other canonicalization passes. It still restarts the fixed-point loop after a rewrite. Collecting BIF rewrites retain their early position.

The old Part 1845 frame has four slots and both maps use saved slots, with live=0. Part 1846 and 1847 use five slots while both maps use X0 and live=1. The late-map candidate restores four slots and keeps both X0 operands and live=1. Pinned OTP has one frame slot and both maps use X0 and live=2, since the line value also occupies an X register. The four original exact normalized matches remain, edoc_layout_chunks:doc_content/2, rpc:init/1, edlin_key:key_map/0 and peer:start_link/0.

The bounded run took 38.94 seconds. Status was 2 Equal, 11 Different, 0 Unsupported and 0 SourceRejected. All 5 selected existing tests passed. Nine normalized bodies differ from the saved Part 1846 comparison, with zero new or lost normalized matches on the scoped set. The standalone focused frame regression fails the exact Part 1847 baseline at the targeted assertion and passes the candidate. SDK style passed.

Reference, baseline and candidate fixtures return identical ASTs and error maps for two valid inputs, a parser error and a scanner error. All three actual ct_doctest BEAM images load on pinned ERTS. Their public file/1 contract returned ok for a valid markdown doctest and error({1,errors}) for malformed source. Raw traces are under fixture. No full corpus was run in this helper stage.

Apply map-root-after-frame-passes.patch and map-root-frame-test.patch together. Key logs are audit.log, witness.terms, normalized.terms, focused.log, test-run.log, baseline-test.stderr.log, and fixture/probe.log.

