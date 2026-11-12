# Part 1053 Event Tracer debug option order

The Windows Event Tracer release profile now records debug_info after warn_unused_vars and warn_obsolete_bool_op, matching the installed OTP 29.1.1 metadata. The previous et.beam had identical abstract forms and differed only in that option order. The release image and source are now permanent fixtures.

The generated et_collector, et_selector and et_viewer debug option lists also match. Both et_wx modules still lack regenerated abstract forms and use fallback debug metadata.

The corpus admitted and generated 1363 of 1366 files. Et is the only change to the previous status set. The new totals are 344 Equal, 1019 Different and 3 SourceRejected. ELDAPv3 remains Equal. All 1019 differing images prepared successfully in OTP.

A fresh opcode ranking on the preceding corpus found the largest first-difference group at 834 modules, with OTP move versus generated init_yregs. Nonempty-list tests versus allocation followed at 707 modules. These counts rank opcode sequences and do not attribute a unique root cause to each module.

- [Full corpus](otp-corpus.json)
- [Focused checks](focused.json)
- [Loader results](loader.txt)
- [Event Tracer option comparison](et-options.txt)
- [Prior corpus opcode ranking](prior-opcodes.txt)
