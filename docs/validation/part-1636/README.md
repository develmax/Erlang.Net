# Part 1636

The append-chain emitter preserves the operand needed across the inner append in one Y slot. It constructs a short constant prefix in X1 for the outer append. A direct tail append releases the frame in call_ext_last. A tuple result or external consumer releases the slot before the outer append. Pinned OTP release disassembly and the existing call and tuple emitters provide the reference. No dependency was added.

The permanent fixture matches OTP byte for byte for the direct return, tuple return, tail consumer and repeated-input cases. OTP VM checks verified values, improper tails and the reported badarg location. All three focused tests passed. [Disassembly](disassembly.txt) · [Hashes](fixture-sha256.txt) · [Tests](focused.txt) · [VM outcome](outcomes.txt).

All 1,366 Windows OTP release sources were accepted and compiled. The full corpus recorded 394 Equal including ELDAPv3 and Observer, and 972 Different. Every generated release image matched the previous run. The OTP loader accepted all differing images. The full suite and readiness regeneration were outside this run. [Corpus](otp-corpus.json) · [Loader](loader-and-code.txt).

The larger release case in httpd_file still requires per-branch live inputs and a deferred record field read.
