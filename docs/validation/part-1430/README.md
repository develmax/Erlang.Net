# Part 1430 — Share list entry after a leading empty-list clause

When a leading nil clause is disjoint from the subsequent shared cons clauses, the entry planner can emit the nonempty path first. It retains the explicit nil test for inputs whose proper-list shape has not been proved. A tuple-field return after exact field tests now extracts its result directly into x0 after those tests succeed.

The deterministic fixture matches pinned OTP byte for byte. Both compiled images return the first matching value and preserve function_clause for nonlists and improper tails. The previous compiler allocated a five-slot frame. Reordering alone removed the frame, and scheduling the field tests before projection aligned the remaining instructions.

The first corpus run exposed a normalized instruction regression in test_server_sup:from/2. The register-tail path had already emitted its proven integer range. The final planner retains the existing path when broadening shared list entry would bypass this proof.

The generated httpc and httpd child_name/2 functions now dispatch without a frame. They still test the tuple shape and the empty list, checks OTP omits using caller types. The global find_node_tag2/2 path needs register construction of a tuple in its matching cons arm.

- [Fixture oracle](check-leading-nil.ps1), [before](previous-probe.txt), [reordering only](first-probe.txt), and [final result](probe.txt)
- [httpc instructions](httpc-entry.txt), [httpd instructions](httpd-entry.txt), and [global instructions](global-entry.txt)

The final run generated all 1,366 release images with zero source rejections and compiler errors. There are 382 Equal and 984 Different. ELDAPv3 remains Equal from C#-generated Erlang. OTP prepares all 984 differing images for loading.

The change reaches 84 images and 108 functions. Seven functions gain normalized equality and none lose it. Thirty-seven frame sizes move closer, including 32 that reach the OTP size. The aggregate Code size gap shrinks by 2,807 bytes. No whole release image changes status.

- [Corpus](otp-corpus.json), [instruction comparison](normalized-functions.txt), [frame comparison](frame-delta.txt), and [loader and Code sizes](loader-and-code.txt)
- [Initial instruction comparison](initial-normalized-functions.txt), [initial from/2](from-entry.txt), and [restored operand](from-final.txt)
- [Focused tests](focused.json), [compiler hashes](compiler-hash.txt), [direct-call ranking](list-direct-calls.txt), and [sequence ranking](sequence-endings-before.txt)
