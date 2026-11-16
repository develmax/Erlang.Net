# Part 1125 — forward a call result beside a tuple

A tail call of the form `finish({Key, Value}, Result)` can keep the preceding call result in X0 while saving only the tuple inputs in the frame. After the heap check, the result moves to X1 and the tuple goes into X0. The emitter supports both local and external tail calls.

The release comparison shows the instructions of `asn1db_member/3` now match OTP 29.1.1 apart from numeric labels. The previous image used three Y slots and a regular external call. The new image uses two slots and `call_ext_last`. Both probe functions matched the OTP instructions after omitting label definitions and were executed on OTP.

- [Probe source](beam_tuple_tail_argument_probe.erl)
- [Oracle assertions](run.eval)
- [Probe disassembly](disassembly.txt)
- [Release comparison in reference, previous, current order](asn1db-member.txt)

The Code gap narrowed in asn1ct_check, application_controller and yecc. The full run generated 1,363 images. Results were 348 Equal, 1,015 Different and three SourceRejected. ELDAPv3 remains Equal. OTP prepared all differing images for loading.

- [Changed images](changed-images.txt)
- [Focused tests](focused.json)
- [Full corpus](otp-corpus.json)
- [Corpus delta](delta.json)
- [Loader results](loader.txt)
