# Part 1054 frame diagnosis and dictionary send

The frame comparison examines paired functions beginning with ordinary allocate followed by move in OTP and init_yregs in generated code. On the previous corpus, 776 modules contain a matching function with a larger generated frame and a nonzero OTP frame. Another 357 modules contain a matching function with a zero-slot OTP frame. Eight contain a matching function with equal positive frame sizes. Categories overlap. The report covers this specific entry sequence.

For a unary send of the incoming message to a process dictionary lookup with a constant key, the emitter uses the get/1 BIF and keeps the message in X registers. The asn1_db:cast/1 instructions and lines now match OTP after label normalization. The execution probe verifies delivery and badarg when the recipient is absent. The asn1_db image shrank by 16 bytes and still differs from the release.

The full corpus admitted and generated 1363 of 1366 files. All statuses remained unchanged at 344 Equal, 1019 Different and 3 SourceRejected. ELDAPv3 remains Equal. All 1019 differing images prepared successfully in OTP.

- [Full corpus](otp-corpus.json)
- [Focused checks](focused.json)
- [Loader results](loader.txt)
- [Frame groups](frame-groups.txt)
- [Cast instruction comparison](asn1-cast.txt)
- [Executed probe](after.txt)
- [Remaining get_table example](get-table.txt)
