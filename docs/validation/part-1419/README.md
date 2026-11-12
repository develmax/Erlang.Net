# Part 1419 — Match the Windows Base64 scanner image

The register list entry planner now extracts head and tail once, preserves a nonempty-tail check before forwarding a literal prefix, and merges adjacent disjunctive guards that produce the same constructed value. It preserves numeric and exact comparison opcodes. The tuple-result path uses the existing exact return-shape proof and saves only the accumulator across the producer call.

The deterministic test image matches OTP. Both images execute the scan loop, numeric equality with a floating head, an improper-list tail, a lone skipped character, and the empty input function-clause failure. The previous compiler differs on both changed functions. The existing release assets are also registered in the permanent release-image test. All three selected compiler tests pass.

All 1,366 Windows OTP 29.1.1 release images were generated with zero source rejections. There are 382 Equal and 984 Different. xmerl_b64Bin_scan is the sole transition to Equal. Both of its changed functions match the release instructions and stack frame sizes exactly. Its Code chunk drops from 1,226 to 585 bytes, the reference size. ELDAPv3 remains Equal from C#-generated Erlang. OTP prepares all 984 differing images for loading.

The remaining list-entry audit prioritizes variable list heads with cons arguments in calls. Start with check_object_set_objset_list_1/3 in asn1ct_check, where OTP shares the list extraction between the literal and variable branches and uses a frame-free record update and cons construction. The C# image opens a five-slot frame and extracts the list twice. Full runtime verification, canonical readiness regeneration and publication remain pending.

- [Corpus](otp-corpus.json), [section audit](section-audit.txt), [frames](frame-delta.txt) and [loader and Code size](loader-and-code.txt)
- [Exact release comparison](xmerl-release.txt) and [hashes](xmerl-release-hashes.txt)
- [Oracle](check-guarded-list-scan.ps1), [probe](probe.txt), [previous compiler](previous-compiler.txt), [fixture hashes](fixture-hashes.txt) and [focused tests](focused.json)
- [Remaining list-entry ranking](list-entry-ranking.txt) and [record continuation](list-record-continuation.txt)
