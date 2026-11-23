# Delay an independent call-result spill across heap construction

This isolated candidate uses the existing late BEAM pass pipeline. A returning ordinary call verifies frame initialization. Keep its result in the heap check's live X prefix, use the independent constructor's outgoing X1 for a heap-neutral nullary BIF and its straight-line list or tuple consumers, then write the original frame slot. The rewrite proves the constructors and argument moves do not use the pending result or its slot. It retains the original layout when a constructor reads the slot.

Reference is OTP-29.1.1 at ad05823719d77c8faee87348ea39513d4e2f99c5. The original compiler's beam_ssa_codegen treats self and node as non-GC BIFs. beam_validator requires valid stack roots at ordinary calls and test_heap and prunes X registers beyond the declared live prefix. The change reuses the C# nullary BIF classification and compact operand codec without importing upstream source or adding dependencies. Git source excerpts are saved alongside the report.

The selected witness was megaco_tcp_sup:init/1 after a three-module comparison confirmed that mnesia_frag_hash had independent branch, frame, type and line blockers. The new 112-byte Code range for init/1, including its labels and line references, is byte-identical to the release image. Its SHA-256 is 704968DD140CFB055748B5611A26D7384C66633AC29311DFEA569B3BFE428A8E. The original Part1850 candidate had a different range digest. Raw disassembly found exactly one changed function in megaco_tcp_sup. Its remaining which_child/2 differences are argument ordering, initialization of a newly dead frame slot and X register allocation across terminal tuple construction. Attr still differs because it includes the code-derived version.

| Evidence | Frozen Part1850 | Candidate |
|---|---|---|
| Three-module diagnostic cohort | 0 Equal, 3 Different | 0 Equal, 3 Different |
| Six prior-equal modules | 6 Equal, 0 Different | 6 Equal, 0 Different |
| Unsupported / SourceRejected across both cohorts | 0 / 0 | 0 / 0 |
| Three-module comparison, one worker | 3.41 s | 3.02 s |
| Six-module comparison, one worker | 4.16 s | 4.13 s |

The repository-style regression compiles one fixture with independent and dependent constructor paths and calls the managed BEAM loader. The isolated test fails with the expected assertion on frozen Part1850 and passes with the candidate. Native preparation and execution separately validate both cases. Native tests on megaco_tcp_sup verify supervisor flags and children, self PID, and positive and missing child lookups. OTP release and both C# outputs produced identical observations. Source formatting and forward/reverse patch application checks pass.

The exact base archive digest is AE9AF63F43D2874A7120045251EB1A9BE230A7EAFE55E3A323F462326FCD6B71. Additional hashes, raw compiler and runtime outputs, tool timing, and the module lists are in this directory. The full corpus and repository regression were left to the integration lane. This scoped candidate has no whole-file gains or losses.
