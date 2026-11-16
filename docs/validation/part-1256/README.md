# Part 1256 — Plan frames after guarded list entry checks

Extend clause-local frame emission to empty lists and single cons patterns. Extract the list fields into scratch X registers and run guards before allocation. Keep the original arguments available for the next clause. Allocate only for a tuple result containing a call. Direct returns and ordinary tail calls can finish without a frame.

The probe checks the saved field, empty and non-list fallbacks, guard failure, recursive argument order and the call error. In `asn1ct_gen:traverse_SO_type_structure/5`, the generated instructions fall from 27 to 11 and the shared seven-slot frame disappears. OTP emits 10 instructions and omits the additional nonempty-list test.

In `test_server_ctrl:get_latest_dir/2`, the generated instructions fall from 28 to 19. OTP emits 15 and shares the list extraction between the guarded and unguarded cons clauses.

The final full run generated 1,366 images with 359 Equal, 1,007 Different and zero rejected sources. All 28 changed images moved closer to the OTP Code section length, by 890 bytes in aggregate. OTP accepted all 1,007 differing pairs.

- [Probe source](split_list_frame_probe.erl)
- [Probe command](check-split-list-frame.ps1)
- [Execution result](probe.txt)
- [ASN.1 instruction comparison](traverse-type-structure.txt)
- [Common Test instruction comparison](get-latest-dir.txt)
- [Full corpus](otp-corpus.json)
- [Corpus delta](delta.json)
- [OTP loader](loader.txt)
