# Part 1709

The BEAM jump pass follows OTP 29.1.1 `beam_jump:is_lt_to_is_ge/1`. A strict comparison between an integer literal and a register with an integer type proof becomes an inclusive comparison with the literal shifted by one. Both operand orders reuse the existing compact operand writer and type table. No dependency was added.

The new `beam_guarded_integer_bounds` regression compares complete images against OTP `erlc +deterministic`. The previous compiler produced SHA-256 `8bfc44853fb71db5eedac797f98c99ad10b46d3a582c51e7a2e546a366754a92`. The current and reference images both produced `1b8368da67a2984ccad6084623ea6f57bf5efabc1ed2571a23e02e368ed8f0de`. The OTP VM returned the same results for values on either side of both thresholds, threshold values, a float and an atom.

All 1,366 Windows OTP 29.1.1 sources compiled. 398 images, including ELDAPv3 generated from ASN.1, matched byte for byte. The OTP loader accepted the 968 differing images. The rule changed two public_key images with no change in Code size or normalized function match counts. The build and three targeted tests passed. The complete runtime suite and readiness regeneration were outside this run. [Corpus](otp-corpus.json) · [Loader and Code](loader-and-code.txt) · [Functions](normalized-functions.txt) · [Tests](focused.txt).
