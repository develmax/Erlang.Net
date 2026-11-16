# Part 1733

Use the independent move order from pinned OTP 29.1.1 as implemented in beam_ssa_codegen:gen_moves and order_moves, then apply beam_block:sort_moves to the contiguous stack loads. Restrict the rewrite to complete argument setups with unique destinations and no reads of those destinations. Keep runs of only stack loads and runs adjacent to a preceding stack save in their existing order.

The full Windows corpus admits and compiles all 1,366 sources. 398 images match byte for byte, including ELDAPv3 from the C# ASN.1 generator. 968 differ. The OTP VM accepted all differing images. The change affects 691 images with unchanged Code section sizes. It gains 2 normalized function instruction matches with 0 lost. 162 functions in 114 images first diverge in the order of independent moves before the same call.

The same-call and same-operands comparison identifies 14 newly matching schedules and one lost schedule in progress_map/5 in crashdump_viewer. OTP sorts the original Y registers before beam_trim remaps them. Our postpass sees the resulting Y numbers. The frame audit identifies an oversized allocation with unchanged X liveness and ordinary branching in 6,477 functions across 761 modules.

The six focused tests passed. The new deterministic fixture matches the complete OTP image and the previous build emits a different image. The standalone OTP probe checks two return values and records the old and new instructions of run/1. The complete runtime suite and readiness regeneration were outside this run.

[Corpus](otp-corpus.json) · [Status changes](status-changes.txt) · [Loader and Code](loader-and-code.txt) · [Functions](normalized-functions.txt) · [Tests](focused.txt) · [Probe](independent-moves.txt) · [Move differences](argument-move-order.txt) · [First opcodes](first-opcodes.txt) · [Frame shapes](frame-shapes.txt) · [Call schedules](call-schedules.txt) · [Trimmed move order](trimmed-move-order.txt)
