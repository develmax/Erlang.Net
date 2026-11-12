# Part 1429 — Build the nil-branch return in registers

The shared list entry planner accepts a nil fallback that builds a list or tuple from the remaining argument registers. It reuses the argument construction planner for heap checks, constant tails and the final result. The nonempty list test, head extraction, and eligible calls run before any frame allocation.

The deterministic fixture matches OTP byte for byte. It exercises a tagged head, an ordinary cons tail call, the nil result, and a function-clause failure on an improper tail. The previous compiler allocated a six-slot frame and checked the input separately in each branch. The release tag_nums_ext/3 now matches after normalizing labels.

The direct-call audit splits the 192 source functions from the preceding frame census by the shape of their first and last clauses. The largest such group has 11 modules. Its leading nil clause and terminal cons call appear in httpc, httpd and global. The httpc release compares the nested tuple field in registers and omits redundant list and tuple checks based on argument types.

- [Whole-image oracle](check-list-nil-construction.ps1), [current](probe.txt) and [previous](previous-probe.txt) results
- [Release instructions](tag-nums-ext.txt), [next direct-call group](list-direct-calls.txt), and [child_name instructions](child-name-entry.txt)
- [Targeted tests](focused.json), [compiler hashes](compiler-hash.txt), and [layout](layout.txt)

All 1,366 release images generate with zero source rejections or compiler errors. There are 382 Equal and 984 Different. ELDAPv3 remains Equal with the C# ASN.1 generator. OTP prepares all 984 differing images for loading.

Six release images change, and all six changed functions reach the OTP frame size. tag_nums_ext/3 is newly equal after label normalization. The aggregate Code size gap drops by 387 bytes. No complete release image changes status.

- [Release corpus](otp-corpus.json), [frame comparison](frame-delta.txt), [instruction comparison](normalized-functions.txt), and [loader and Code sizes](loader-and-code.txt)
