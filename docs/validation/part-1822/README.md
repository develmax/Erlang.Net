# Part 1822 — Stabilize the phash2 allocation test

On the post-merge main run, the allocation counter reported 7,712 bytes against the 4,096-byte budget. A separate local diagnostic under forced Gen0 collection measured a spike in a control interval with a spin and a previously computed result. The specific CLR mechanism is unresolved. [Main run](https://github.com/develmax/Erlang.Net/actions/runs/43856838773/job/133263175384).

Use five synchronous, same-thread measurements after warmup. Apply the unchanged 4,096-byte budget to the median. Reject a payload-sized allocation in any sample. The existing positive control copies the binary and verifies that the hash value is preserved through the same allocation measurement and payload predicate.

The Release test project built successfully. The focused resource test and copy positive control passed on .NET 10.0.11. The copy control recorded 8,389,680 bytes. The original resource test entry passed 768 invocations across two separate processes with a blocking Gen0 collection forced every two milliseconds by a background thread. An instrumented test copy rejected a sustained 8,192-byte injection on the fifth sample and a single payload copy on the second sample.

This change affects the test measurement. The hasher, registered MFA set and OTP compatibility classifications remain represented by the existing runtime and readiness evidence. The hosted suite is the next validation step. [Recorded measurements](results.json).
