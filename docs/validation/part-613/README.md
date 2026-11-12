# Part613 literal dispatch without a catch-all

The BEAM backend emits one or more unguarded clauses with distinct atom or integer patterns and constant results, without a catch-all clause. A single clause uses an exact test. Multiple clauses use select_val and share equal return blocks. Failed matching goes to the function info label, preserving function_clause. Entry label planning includes only the emitted return blocks.

The deterministic fixture contains the exact generated ELDAPv3 absent/1 clauses, integer dispatch with shared compound results, a single atom pattern, and a tail call to the integer dispatcher. The retained reference was compiled by OTP with deterministic and its Docs chunk removed to match the fixture profile. The C# BEAM matched all bytes. A reference VM loaded the generated fixture and verified five successful calls and the three unmatched-input paths. The full ELDAPv3 module still needs further BEAM lowering.

The same lowering enables the Mnesia behaviour_info/1 callback list. With the existing Mnesia release options, the generated mnesia_backend_type.beam matched all 2,732 reference bytes. The release fixture hashes match the admitted inventory. A reference OTP VM returned the callback list and raised function_clause for an unknown input.

All eight selected tests passed. [Focused report](focused.json).

The final corpus run checked all 1,366 images. It recorded 18 exact matches, 488 source rejections and 860 modules outside the BEAM emission profile. ELDAPv3 remains in the latter group. Full regression and canonical readiness remain open. [Per-image report](otp-corpus.json).
