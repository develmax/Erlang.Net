# Part 1574 — Build tuple messages in registers before sending

The opcode audit found 781 of 976 differing modules with at least one function whose first opcode mismatch is a move versus init_yregs after different frame allocations. A separate group of 549 modules has a function starting with allocate_heap in OTP and allocate in C#. The counts overlap. [Opcode audit](baseline-opcodes.txt) · [Closest images](nearest-images.txt).

For an unguarded tuple entry followed by a send and a constant return, the compiler can build the message directly in X registers. It reserves heap once and retains the tuple until the message is ready. The existing argument-construction planner chooses the registers and emits the constructors. The reference is OTP 29.1.1 beam_ssa_codegen.erl. In asn1_db:reply/2 the frame drops from four slots to zero, and the instructions and registers match after normalizing labels. [Disassembly](asn1_db-reply.txt).

The OTP VM agreed on the message contents, the path where the message also contains the recipient, function_clause for a bad pattern, and badarg for an invalid recipient. Three focused compiler tests passed. [Fixture](beam_register_send.erl) · [Instructions](reply.txt) · [Outcomes](fixture.txt) · [Tests](focused.txt).

All 1,366 sources were accepted and generated. The byte comparison found 390 Equal and 976 Different. ELDAPv3 matched from C# ASN.1 generation. The only changed image was asn1_db, whose Code section moved 21 bytes closer to OTP. OTP accepted all 976 differing images for loading. [Corpus](otp-corpus.json) · [Functions](normalized-functions.txt) · [Loader](loader-and-code.txt).
