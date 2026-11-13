# Part 1585 — Share a binary match context across multiple fields

The general BEAM emitter matches successive byte-aligned fields directly when a guard proves the input is binary. It skips an unused variable-length field with bs_skip_bits2 and groups a fixed-length suffix and an optional tail into one bs_match. The original input remains available for badmatch. The instruction and command encoding follows OTP 29.1.1. No dependencies were added.

The control fixture agrees with OTP on five results, including short input, negative length and an invalid skip size. Its extraction and skip sequence now uses the same instructions as OTP, with remaining register and frame differences. Three focused compiler tests passed. [Fixture](beam_binary_multiple_fields.erl) · [Disassembly](disassembly.txt) · [Outcomes](outcomes.txt) · [Tests](focused.txt).

The guarded match in ct_netconfc:frame/2 occurs inside a case arm. The current guard proof collector only checks the immediate body, so this function still follows the previous emission path. [Release function](ct-netconfc-frame.txt).

All 1,366 release sources were accepted and generated. There were 390 Equal and 976 Different. ELDAPv3 was Equal from the C# ASN.1 generator. The rule changed two release images, erl_tar and string. Their Code size gaps decreased by 18 and 72 bytes respectively. OTP accepted all 976 differing images for loading. The full runtime suite and canonical readiness were outside this run. [Corpus](otp-corpus.json) · [Loader and Code](loader-and-code.txt).
