# Positioned binary constructor operand types

This independent follow-up requires the producer-result failure-edge patch. It uses the already checked positioned-tail plan. Integer head bounds come from the decoded unsigned segment. The measured binary range and the literal addition determine the length bounds. The extracted tail uses the proved matchable type. The compiler only adds these proved argument annotations when SSA type inference is enabled.

Pinned OTP-29.1.1 beam_ssa_type adds argument types to bs_create_bin. Pinned beam_call_types specifies the bounds of byte_size and bit_size, and beam_types encodes supported lower and upper bounds. The C# change reuses those semantic type tables and the existing argument writers. A successful terminal extractor does not guarantee an arbitrary binary producer range. No extra producer-specific proof is needed because the positioned-tail plan already verified the match and both complete return constructions.

The full S corpus disassembly census found one module with this positioned constructor recipe, ssh_bits:mpint/1, with four arms. The proof changes all of their previously untyped argument triples to match the release. The five-module uncached comparison took 8.83 seconds, starting and ending with 2 Equal and 3 Different, no unsupported or rejected source and no exact change. Only ssh_bits changed. Its Type section is 102 bytes, previously 66 and natively 143. Its image is 11,312 bytes, previously 11,252 and natively 11,328. Its Code section grew from 609 to 633 because of 12 typed operands. The native Code section is 623 bytes.

The expanded permanent test failed on the preceding producer-edge compiler and passes the candidate. The candidate and reference ERTS results agree for 27 synthetic and 34 real-module cases, including errors and locations. The native real transcript is D692C24677C099D817E83A11696805044C224F714FA8BACF61AFBCE22B949BBE, and the synthetic transcript is 3D95140E262385FB1ADEBFCFB7D9DF9E53DCB05CC558336493A6099BE0BF6A79.

- Compiler SHA-256 6AAD4CA66314591BB57B5D17B04707D6F4CE5CD5903F8F38FF3042AF527D50DB
- Uncached pilot SHA-256 8812BBD22E2A2229BAD3504676088B33430060EE1CC33D9045548D3FD7C88768
- Two-path patch preserve-positioned-binary-constructor-types.patch SHA-256 5BA247DD16C116DDCB002076E24151B1DF196DF8579F0EBDB88B156C8ED080E4
- Dependency reuse-producer-result-for-binary-failure-u.patch SHA-256 D5BF734F61F083DF0C4972326308620C40F530A4DDDC1FBD9EB91F8FBE6B024E

The raw and disassembled witnesses remain distinct. Whole ssh_bits is still Different. Its residual includes the negative guard fact, delayed negative frame allocation, transient variable-width extraction spills and ordering of match failures and labels.
