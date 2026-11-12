# Part 1346 — Keep constructor operands in registers

The existing constructor planner now accepts tuple and record arguments written directly inside a tail call. It keeps the embedded call result in X0 while constructing the argument and saves inputs that survive an earlier call in the frame. The ordinary call resolver also recognizes monitor/2. The previous bound constructor path still uses the same planner.

The focused OTP comparison exercised a record with a monitor and an ETS table, a tuple with an integer conversion, and a badarg from that conversion. Both probe functions match decoded instructions after label normalization. The bound constructor, entry call chain, conversion call and wx queue regressions passed.

The full Windows OTP 29.1.1 corpus generated all 1,366 images with 365 Equal, 1,001 Different and zero SourceRejected. Forty generated images changed. Twenty functions gained decoded instruction matches after label and closure ID normalization, including asn1_db:init/2 and both digraph:add_edge arities. No prior instruction matches were lost in the changed images. The Code size gap narrowed in 20 images and widened by one byte in diameter_config, for a net reduction of 163 bytes. ELDAPv3, error_handler and wxGridCellBoolEditor remain Equal. OTP accepted all 1,001 differing images for loading.

The entry audit compares the first instruction after ignoring labels and source information. The largest group on the prior corpus has 3,867 functions across 611 modules where OTP tests for a nonempty list before allocating. Generated code allocates immediately. Inspect shared cons dispatch in that group next. The audit measures instruction order and groups may overlap by module.

- [Focused check](check-constructor-tail.ps1)
- [Focused output](probe.txt)
- [Bound constructor regression](legacy-constructor-regression.txt)
- [Entry call chain regression](entry-call-chain-regression.txt)
- [Conversion call regression](conversion-call-regression.txt)
- [Wx queue regression](wx-option-queue-regression.txt)
- [asn1_db:init/2 instructions](asn1-db-init.txt)
- [Full corpus](otp-corpus.json)
- [Instruction matches](normalized-functions.txt)
- [Code size delta](code-delta.json)
- [OTP loader](loader.txt)
- [Entry frame audit](delayed-frames-baseline.txt)
- [Entry frame audit script](rank-delayed-frames.eval)
