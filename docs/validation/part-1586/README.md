# Part 1586 — Carry binary guard proofs into case and if branches

The general C# emitter carries the proof from a successful is_binary guard into nested case and if bodies. Each branch gets its own additional guard facts. A fact from one branch cannot affect its siblings. The traversal stops at nested funs and comprehensions, which can introduce shadowing. No dependencies were added. The type fact agrees with OTP 29.1.1 beam_ssa_type.erl, which infers a bitstring with size unit 8 from is_binary.

The regression fixture exercises a match inside a case arm, including successful capture and badmatch for an invalid skip or short fixed suffix. Three focused tests passed. The release function ct_netconfc:frame/2 now uses the OTP match instruction sequence, with remaining differences in live registers, frame size and surrounding instructions. [Focused tests](focused.txt) · [Release disassembly](ct-netconfc-frame.txt).

Across the previous 976 differing images, 883 modules had at least one function whose first normalized instruction difference was between two allocate instructions. This ranks a broad register and frame allocation gap. The module counts overlap, and each class includes differences in any operand. [Instruction comparison](prior-corpus-first-opcode.txt).

All 1,366 release sources were accepted and generated. There were 390 Equal and 976 Different. ELDAPv3 was Equal from the C# ASN.1 generator. Six release images changed and their Code size gaps fell by a combined 190 bytes. OTP accepted all 976 differing images for loading. The full runtime suite and canonical readiness were outside this run. [Corpus](otp-corpus.json) · [Loader and Code](loader-and-code.txt).
