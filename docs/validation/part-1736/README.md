# Part 1736

Keep a call result in X0 through a branch on a separate operand. Stop tracking the original stack value when it is overwritten, cleared or released. A changed read must be reached only from paths where X0 still contains the original result. Account for call clobbers and the live count in heap tests. Preserve Y initialization separately from the removed move. The whole-image maybe fixture confirms that the optimizer retains the saves where OTP dispatches directly on the call result.

All 1,366 Windows OTP 29.1.1 sources were admitted and compiled. 398 images matched byte for byte, including ELDAPv3 from the C# ASN.1 generator. 968 differed and passed the OTP loader check. The change affects 691 images and reduces the total absolute Code gap by 36443 bytes. Normalized instructions gain 4 function matches and lose 0. No status changes.

7307 result saves before branches disappear in 4683 functions across 687 modules with the same previous and current sites. 1731 also omit the save in OTP functions with the same call and branch sequence. 1235 functions in 440 modules use fewer Y slots. Total allocated slots decrease by 1273 across those functions. The first allocation now matches OTP in 210 functions in 139 modules.

The build and all six targeted tests passed. In the first probe, OTP also keeps the result in X0 and allocates a one-slot frame while the C# frame uses two slots. In the [second probe](../part-1734/beam_branch_frame_3.erl), OTP maintains a second slot for a result copied before the merge and the C# emitter copies the merged value once, with a one-slot frame. Both probes match OTP values through garbage collection. The complete runtime suite and readiness regeneration were outside this run.

[Corpus](otp-corpus.json) · [Status changes](status-changes.txt) · [Loader and Code](loader-and-code.txt) · [Functions](normalized-functions.txt) · [Tests](focused.txt) · [Forwarded results](forwarded-results.txt) · [Frame sizes](frame-sizes.txt) · [Probe](branch-result-probe.txt) · [Remaining heap spills](remaining-heap-spills.txt)
