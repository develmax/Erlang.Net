# Part 1422 — Keep list parts across a call and case selector

The case selector planner extracts the list head and tail directly into their stack slots. The first call reads those slots and returns the argument for the selector call. When that result stays in X0 during argument preparation, the planner can save it in an input slot after its last read.

The deterministic fixture matches OTP byte for byte. Both images execute the empty and nonempty list paths, both case branches, an invalid list argument, and a failure in the first call. The previous compiler differs in Code and Attr. All three selected compiler tests passed.

The Windows OTP 29.1.1 comparison generated all 1,366 images. There are 382 Equal and 984 Different, with zero source rejections or emission failures. ELDAPv3 is Equal from C#-generated Erlang. OTP prepares all 984 differing images for loading. Only mod_alias changes. Its append_index/2 instructions and registers match OTP after label normalization. Its three-slot frame keeps two live X registers at allocation, down from three, and the module's Code gap shrinks by six bytes.

The list-entry ranking still shows 217 candidate functions across 132 modules for a variable head and a call with cons arguments. A narrower opcode audit finds 19 functions across 15 modules where the release projects fields from a call result into a selector and the generated image lacks that sequence. Two have a nonempty-list entry, including asn1ct:remove_name_collisions2/4. Full runtime verification, canonical readiness regeneration and publication remain pending.

- [Corpus](otp-corpus.json), [status](status-changes.txt), [normalized instructions](normalized-functions.txt), and [loader and Code sizes](loader-and-code.txt)
- [mod_alias instructions](mod-alias-append-index.txt), [list-entry ranking](list-entry-ranking.txt), and [tuple selector candidates](tuple-selector-candidates.txt)
- [Oracle](check-list-prefix-case.ps1), [execution results](probe.txt), [previous compiler](previous-compiler.txt), [fixture hashes](fixture-hashes.txt), and [compiler tests](focused.json)
