# Part 1734

Reuse Y slots across forward branches of a function with one frame. A write kills the old value only on the success edge. A destination interferes with any live value that it could overwrite, including writes made by init_yregs. Keep the original frame layout for handlers, selective receives, trims, backward edges and unknown Y-operand roles. The allocator visits occupied slots in first-use order with reversed register numbering, using pinned OTP beam_ssa_pre_codegen as the reference for liveness and register allocation.

All 1,366 Windows OTP 29.1.1 sources were admitted and compiled. 398 images matched byte for byte, including ELDAPv3 from the C# ASN.1 generator. 968 differed and passed the OTP loader check. The change affects 794 images and reduces the total absolute Code gap by 56776 bytes. Normalized instructions gain 0 function matches and lose 0. No status changes.

9616 functions in 794 modules use fewer Y slots. Total allocated slots decrease by 33799 across those functions. The first allocation now matches OTP in 639 functions in 318 modules.

The build and six targeted tests passed. The shared-return tail call fixture expects release of one slot, as confirmed by disassembly of the OTP fixture. The branching probe reduces the initial frame from four Y slots to two, matching OTP's frame size. Both implementations return the same values for each branch and the fallback through explicit garbage collection. The full probe images still differ. The complete runtime suite and readiness regeneration were outside this run.

[Corpus](otp-corpus.json) · [Status changes](status-changes.txt) · [Loader and Code](loader-and-code.txt) · [Functions](normalized-functions.txt) · [Tests](focused.txt) · [Frame sizes](frame-sizes.txt) · [Frame shapes](frame-shapes.txt) · [Probe](branch-probe.txt)
