# Part 1595 — Project entry tuple fields into their destination registers

A selected tuple arm that returns a tuple can reserve heap space while the input tuple is still live, then extract the sole live field into X0. A selected arm with a protected call can write the surviving field directly to its Y slot and the first call argument to X0 after frame allocation. The existing protected-call planner verifies the argument and lifetime constraints before any projection is suppressed. Atom-dispatch fallback labels are assigned after the arms, matching the OTP 29.1.1 label order. No dependency was added.

The targeted release build of erl_abstract_code matches the installed Windows OTP 29.1.1 image byte for byte. The release image was added to the permanent comparison. All three focused tests passed. [Disassembly](debug-info.txt) · [SHA-256](sha256.txt) · [Tests](focused.txt).

The full run accepted and generated all 1,366 images, with 392 Equal and 974 Different. The only changed release image is erl_abstract_code, now Equal. ELDAPv3 and wx_misc remain Equal. OTP accepted all 974 differing images. Full runtime and canonical readiness were outside this run. [Corpus](otp-corpus.json) · [Status changes](status-changes.txt) · [Loader and Code](loader-and-code.txt).
