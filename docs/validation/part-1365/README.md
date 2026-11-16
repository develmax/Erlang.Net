# Part 1365 — Keep tagged payloads across calls

The entry call scheduler reserves a stack slot when a tagged payload is live beyond the first continuation call. When that call uses the payload, the scheduler projects it directly into the slot. When only later calls need it, the scheduler saves the tagged result and projects the field at its first use. A final use can project directly into an argument register when doing so preserves the other argument sources. The scheduler reloads X0 after trimming when the tagged payload remains live in the frame.

The focused fixture matches five functions after label normalization. It checks delayed projection, repeated uses, retention of an entry argument and a payload reloaded after trimming. A failed match reports the original term without running later calls. Six earlier probes passed.

The source audit finds nine modules with straight call syntax and a payload referenced after the first continuation call. Four use the payload later only. Five use it immediately and later. Broader sequences can also require saving several intermediate values, lowering nested arguments or entering from a guarded clause.

The full Windows OTP 29.1.1 run generated all 1,366 images, with 369 Equal, 997 Different and zero SourceRejected. ELDAPv3 remains Equal and OTP accepted all 997 differing images for loading. Two images changed. ttb:write_binary/2 gained a normalized instruction match, reached OTP's two-slot frame and narrowed the module's Code size gap by eight bytes. The other changed module, release_handler, now delays projection in prepare_restart_new_emulator/7. Its X0 load and final argument move order still differ. Full runtime and readiness checks were not run.

- [Full corpus](otp-corpus.json)
- [Normalized function comparison](normalized-functions.txt)
- [Peak stack slots](peak-slots.txt)
- [Code size changes](code-delta.json)
- [OTP loader check](loader.txt)
- [Focused check](check-tagged-lifetime.ps1)
- [Focused output](probe.txt)
- [Straight call sequence audit](payload-chain-audit.txt)
- [Audit script](rank-payload-call-chains.eval)
- [ttb release instructions](ttb-write-binary.txt)
- [Remaining release_handler instructions](release-handler.txt)
- [Tagged sequence regression](tagged-sequence.txt)
- [Tagged call match regression](tagged-call-match.txt)
- [wx text command regression](wx-text-command.txt)
- [Private constants regression](private-case-constants.txt)
- [Entry case field regression](entry-case-field.txt)
- [Deferred case field regression](deferred-case-field.txt)
