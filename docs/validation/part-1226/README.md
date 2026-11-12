# Part 1226 — fuse frame and heap allocation for a list argument

Use allocate_heap when an eligible entry call consumes one cons cell with a head in an incoming register and a constant tail. The existing entry call liveness check preserves the inputs used later.

In asn1_db:load_table/4 the heap request, live count and put_list operands match OTP. The generated frame has thirteen slots compared with fourteen before and four in the reference. Code size moved closer in ten modules and the first frame moved closer in eight functions. All 1,366 images were generated. The comparison produced 357 Equal and 1,009 Different with zero rejected sources. ELDAPv3 stayed Equal. OTP accepted all differing files. Three focused tests and the execution probe passed.

- [Full corpus](otp-corpus.json)
- [Corpus delta](delta.json)
- [Frame sizes](frame.txt)
- [ASN.1 disassembly](asn1-db.txt)
- [Focused tests](focused.json)
- [Loader](loader.txt)
- [Execution probe](probe.txt)
- [Opcode ranking](ranking.txt)
