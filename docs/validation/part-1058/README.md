# Part 1058 lower process dictionary and PID reads

The first experiment moved argument copies ahead of stack initialization in the general emitter. In the preliminary opcode comparison, 726 images showed the inverse order. The full experiment with the pinned documentation files restored all three baseline counts. The compiler therefore retains its original order.

In the pinned OTP compiler, frame initialization uses the intersection of registers defined along incoming paths before any operation that requires an initialized stack. The two-slot frame in asn1_db:req/1 also relies on nonallocating BIFs, short-lived X registers and receive markers. The C# emitter now uses the existing BIF encoders for process dictionary reads and self/0 in general expressions, while keeping local and imported call resolution.

- [Frame-order experiment](frame-order-experiment.json)
- [Preliminary opcode ranking of the experiment](frame-order-preliminary-ranking.txt)
- [Disassembly of the frame-order experiment](req-disassembly.txt)
- [Reply and monitor DOWN probe for the frame-order experiment](req-probe.txt)

The final corpus has 348 Equal, 1015 Different, 3 SourceRejected. ELDAPv3 is Equal. See the status delta for comparison with part 1057.

- [Final full corpus](otp-corpus.json)
- [Module status delta](status-delta.txt)
- [OTP loader results](loader.txt)
- [Focused checks](focused.json)
- [Final asn1_db:req/1 disassembly](req-with-bifs.txt)
- [Final reply and monitor DOWN probe](req-with-bifs-probe.txt)
- [Explicit BIFs and shadowed local functions](frame_bif_probe.final.txt)
- [Remote call and auto-imported BIFs](frame_remote_probe.final.txt)
- [Generated image changes relative to part 1057](generated-image-delta.txt)
- [Final opcode ranking](opcode-ranking.txt)
