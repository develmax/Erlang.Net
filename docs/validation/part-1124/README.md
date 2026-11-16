# Part 1124 — emit the list head into the tail argument register

The checked-list entry path writes the head into its final tail-call argument register when the old value in that register has no other argument use. The temporary register remains available when the move plan needs the old value.

The OTP 29.1.1 probe executes the generated and reference images. Both versions emit the same instructions for the two dead-register examples, covering a failing hd BIF and get_hd on a checked list. The regression test also covers forwarding a live argument, improper lists, badarg and badmatch.

- [Probe source](beam_nested_inline_list_probe.erl)
- [Execution checks](run.eval)
- [Probe disassembly](disassembly.txt)
- [Release disassembly](yeccpars2-3.txt)

The Code gap narrowed in 13 parser modules. In yeccpars2_3/7, the hd BIF now writes directly to X0. The semantic-stack test and the failing state-stack BIF still differ from OTP, which emits only get_hd and the tail call.

The full corpus generated 1,363 images. The statuses remain 348 Equal, 1,015 Different and three SourceRejected. ELDAPv3 remains Equal. OTP accepted all differing images during loader preparation.

- [Focused compiler tests](focused.json)
- [Full corpus](otp-corpus.json)
- [Corpus delta](delta.json)
- [OTP loader preparation](loader.txt)
