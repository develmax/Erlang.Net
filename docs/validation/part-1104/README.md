# Part 1104 — construct the tuple in its tail-call argument register

The general emitter handles an unconditional clause that binds a tuple containing one ordinary call and passes it to a local tail call. It saves parameters used after the first call, keeps the call result in X0, and writes the tuple to its final argument register. The local tail call then uses that register directly.

The pinned OTP 29.1.1 probe verified argument order, tuple contents and propagation of an exception from the inner call. The standalone probe and OTP use the same three-slot frame and tuple destination but assign two saved parameters to different Y registers. In the release closure in asn1ct_check:check_imports_1/3, the new instructions match the reference except for global label numbers. The closure frame decreased from four slots to three.

- [Probe source](beam_tuple_tail_argument_probe.erl)
- [Execution assertions](run.eval)
- [Probe disassembly](disassembly.txt)
- [Release closure, previous emission and current emission](asn1ct-check-closure.txt)

The pinned OTP pre-codegen dumps show that entry copies are ordered by SSA variable identity before register numbers are reversed. The standalone inputs are _0, _1, _2. The release closure inputs are _18, _0, Module. This explains the different Y assignments despite the same tuple and tail-call shape.

- [OTP pre-codegen excerpts](otp-precodegen.txt)

The full corpus generated 1,363 images. There were 348 Equal, 1,015 Different and three SourceRejected. ELDAPv3 remains Equal. Only asn1ct_check changed. Its Code length gap narrowed. OTP accepted all 1,015 differing images during loader preparation.

- [Full corpus](otp-corpus.json)
- [Corpus delta](delta.json)
- [OTP loader preparation](loader.txt)
- [Focused compiler tests](focused.json)
