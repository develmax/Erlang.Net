# Keep an ordinary-call result live for a following GC BIF

The prototype uses the frozen Part 1837 compiler sources and reads the immutable uncached saved images beside that freeze. The archive SHA256 is 9BE82DBC068947060880683C3AD65196078F67DFB46223D7465275B90F5D68C6. The baseline compiler SHA256 is 1B466FC163258DA86706E378E8D055E0084D284B96B67410EF1D4B33C264184F. The reference is release OTP 29.1.1 commit ad05823719d77c8faee87348ea39513d4e2f99c5. The pinned .NET SDK is 10.0.400.

The initial candidate worktree failed to materialize old validation blobs because C: ran out of space. The source build instead used an isolated D: artifact directory. It exports the compiler source project at 3ea4bc0f4, overlays the frozen source archive and references the exact deployed runtime dependencies. The test runs the permanent assertion in a small separate executable. Standard repository validation is left to the integration checkpoint.

Pinned beam_ssa:clobbers_xregs identifies ordinary calls as clobbers and preserves X values through a GC BIF. beam_ssa_codegen emits the live-prefix count for that BIF. On C# output, a call result sometimes moves from X0 to Y immediately before the next BIF reads it. The new pass reuses X0 when the saved term has exactly one use in its proven variable lifetime. It retains the caller's live X prefix and raises zero to one, preserves typed operand suffixes and initializes the old Y slot until ordinary compaction can remove it. The admitted region has no trims or exception handlers, and the BIF failure exits the function. A separate negative control keeps the Y save when the original input is needed after the BIF.

- scan.json covers all 962 previously differing images and records 720 structural candidates across 198 modules and zero decode errors.
- proof.json applies the exact C# pass to the selected saved Code streams without recompiling any source. It removes 178 moves across 84 modules and every transformed stream decodes.
- before.json and after.json are ten-module recompile reports with 4 Equal, 6 Different and no Unsupported or SourceRejected. Five outputs change. The baseline comparison takes 32.590 seconds and the candidate 29.539 seconds on the shared host with one worker.
- ct_repeat:remaining_time/1 matches the pinned arithmetic sequence, live roots and frame width. Its subsequent alias moves remain. asn1ct_gen:pgen_hrltypes/4 reaches the expected frame size as well and still differs in type and scheduling.
- All 18 baseline, candidate and pinned images from the differing cohort pass ERTS code:prepare_loading.
- Native fixture before, after and OTP execution logs match over ten cases including bignum allocation, floats and arithmetic exceptions. The localized permanent assertion is red on the frozen compiler and green on the candidate. The wider harness is deferred.
- The patch SHA256 is 6F0DAADA63BF78D7AA45DED850189CE13E1F6B4EC3B085238611A9F97E149FB6.
