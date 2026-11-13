# Part 1580 — Pass binary match contexts through recursive byte filters

For a recursive byte filter with an identity fallback, pass the match context through the local tail call. Save the initial position for each invocation and materialize the remaining bitstring only on return. Keep the byte alignment check before the recursive branch and preserve the non-binary fallback. The reference is OTP 29.1.1 beam_ssa_bsm.erl and beam_ssa_codegen.erl.

The deterministic 640-byte fixture matches OTP. Five outcomes agreed under the OTP VM, and the managed runtime test checks the recursive path, all-filtered input, a non-byte-aligned bitstring and a non-binary argument. Three focused tests passed. [Instructions](instructions.txt) · [Outcomes](outcomes.txt) · [Tests](focused.txt).

The release ct_netconfc:trim/1 now passes the context with call_only and avoids the three-slot frame. OTP additionally proves the entry is binary, which permits bs_start_match4 with no_fail and a weaker alignment check. The candidate still uses bs_start_match3. [Release instructions](ct_netconfc-trim.txt).

In the differing release corpus, bs_start_match4 appears with no_fail in 854 functions from 131 modules and with resume in 135 functions from 53 modules. These counts include overlapping modules and functions. [Inventory](match4-inventory.txt).

The full run accepted and generated 1,366 images, with 390 Equal and 976 Different. ELDAPv3 matched from C# ASN.1 generation. The rule changed one release image. The Code size difference in ct_netconfc decreased by 46 bytes. OTP accepted all 976 differing images for loading. The full runtime suite and canonical readiness were outside this run. [Corpus](otp-corpus.json) · [Loader and Code](loader-and-code.txt).
