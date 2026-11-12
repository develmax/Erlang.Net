# Part 1366 — Plan terminal cases after tagged matches

The entry case planner accepts a tagged call match followed by a case with a call as its selector. It tests the original return value in X0 before preparing the selector. An unused payload needs no extraction, a payload consumed by the selector stays in a register, and a payload used by a case arm is projected into a stack slot. The failure path retains the original unmatched value and source line. When a successful arm returns the literal that it just matched, it can return the selector directly from X0.

The compiler built without warnings. The focused fixture matches three functions after label normalization and exercises the different badmatch and case_clause paths, including the skipped selector call on a failed match. Six earlier probes passed. The release comparison shows that disk_log_1:truncate_at_close2/3 now matches normalized OTP instructions and uses the release frame size of two slots.

The complete Windows OTP 29.1.1 run generated 1,366 images with 369 Equal, 997 Different and zero SourceRejected. ELDAPv3 remains Equal and OTP accepted all 997 differing images for loading. Only disk_log_1 changed. Its Code size gap narrowed by 36 bytes and the function uses two slots instead of four. All 997 differing images have Code differences. The 995 differing export tables contain the same exported symbols, and the 970 differing Attr chunks agree after removing vsn. The opcode audit finds a move versus init_yregs difference after unequal allocation operands in 792 modules. Full runtime and readiness checks were not run.

- [Full corpus](otp-corpus.json)
- [Normalized function comparison](normalized-functions.txt)
- [Peak stack slots](peak-slots.txt)
- [Code size changes](code-delta.json)
- [OTP loader check](loader.txt)
- [Chunk payload audit](chunk-ranking.txt)
- [Opcode audit](opcode-ranking.txt)
- [Focused check](check-tagged-case.ps1)
- [Focused output](probe.txt)
- [Tagged case source audit](tagged-case-audit.txt)
- [Audit script](rank-tagged-cases.eval)
- [disk_log_1 release instructions](disk-log-case.txt)
- [Tagged lifetime regression](tagged-lifetime.txt)
- [Tagged sequence regression](tagged-sequence.txt)
- [Tagged call match regression](tagged-call-match.txt)
- [wx text command regression](wx-text-command.txt)
- [Private constants regression](private-case-constants.txt)
- [Entry case field regression](entry-case-field.txt)
