# Part 1229 — fuse allocation for constructed case selector operands

Calls in case selectors can reuse the entry argument construction plan for eligible tuples and lists. The OTP execution probe covers a live input in a branch and an input used by the continuation.

The Code size gap narrowed in 50 modules and grew by three bytes each in logger_config and mnesia_controller. The total absolute Code size gap fell by 697 bytes. All 1,366 release modules generated images, with 357 Equal and 1,009 Different. The OTP loader accepted all differing pairs. Three focused tests passed.

- [Full corpus](otp-corpus.json)
- [Code size delta](delta.json)
- [Focused tests](focused.json)
- [Loader](loader.txt)
- [Execution probe](probe.txt)
- [Probe source](entry_selector_heap_probe.erl)
- [Opcode ranking](ranking.txt)
