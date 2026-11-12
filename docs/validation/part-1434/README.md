# Part 1434 — Emit frame-tail calls from terminal case arms

The frame return context now reaches the register tuple case emitter. A supported call in a terminal arm emits a frame-tail call after preparing its arguments. A case with a continuation retains an ordinary call. If the last extracted field is used only as the first argument and the branch has no selector alias, the emitter extracts that field directly into x0.

The OTP oracle checks the emitted call forms and execution results in both contexts. In compile_proc/3 the single-file path uses get_tuple_element to x0 and call_last. The frame remains six slots against three in OTP, and the selector still tests each tagged arm separately.

- [OTP oracle](check-case-tail.ps1), [before](previous-probe.txt), [after](probe.txt), and [compile_proc instructions](compile-proc.txt)

The final Windows OTP 29.1.1 run generates all 1,366 images with zero rejected sources and zero compiler errors. There are 382 Equal and 984 Different. ELDAPv3 remains Equal from C#-generated Erlang. The OTP loader prepares all 984 differing images.

The change reaches 109 images and 155 functions. The aggregate Code size gap decreases by 1,031 bytes. Frame sizes and normalized instruction equality stay unchanged. All three targeted .NET cases pass.

The earlier image ranking finds 2,050 functions in 541 images with fewer select_tuple_arity operations than OTP. It identifies 917 functions in 347 images that also use multiple tagged tuple tests.

- [Corpus](otp-corpus.json), [instructions](normalized-functions.txt), [Code and loader](loader-and-code.txt), and [focused tests](focused.json)
- [Opcode ranking](case-arity-before.txt) and [audit script](rank-case-arity.escript)
