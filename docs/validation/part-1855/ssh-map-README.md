# Map-field call continuation

The C# compiler now admits a single effectful field producer in an associative map used once by an ordinary selector call. The earlier ConstructedCallContinuation prefix, pure argument construction planner, and case emitter supply the replacement. The field result stays in X0 through the heap check, and the map is created in the selector argument register. A later case use of the map or entry variables keeps the previous path.

The pinned Windows OTP release is OTP 29.1.1 at ad05823719d77c8faee87348ea39513d4e2f99c5. The official +dcg listing for ssh_channel_sup shows the producer, frame, roots and map operands before BEAM normalization. The C# implementation uses the AST and compile options. No upstream source or dependency was added.

The 115-byte raw prefix for ssh_channel_sup:start_the_channel/7, through supervisor:start_child/2, now matches the OTP release. Its SHA-256 is 6031B4D2E1A62106E2F6E5A04AC2F1BF6A20FACF44F2396AA6F00D4560F55A3F. The complete candidate and release are 3,108 bytes each with 369-byte Code chunks. The first different file offset is 532. The remaining differences are post-call selection label numbering, direct and nested terminal projections, and the derived Attr digest. The full SSH image remains Different.

| Eight-module comparison | Safe-line baseline | Candidate |
|---|---|---|
| Equal | 5 | 5 |
| Different | 3 | 3 |
| Unsupported | 0 | 0 |
| SourceRejected | 0 | 0 |
| Comparison time | 8.34 s | 7.63 s |

No whole-file matches were gained or lost. Only ssh_channel_sup changed in the scoped set. Native ERTS preparation and execution cover both client and server channel starts and all four supervisor response forms. The independent fixture checks preserved map values on both success and a failure branch that still reads the map. Its native output is identical with the previous and candidate compiler. The permanent test fails with the expected baseline assertion and passes with the candidate and managed BEAM preparation.

The single-worker build ran with BelowNormal priority and took 70.10 seconds. The standalone compiler, focused regression, source layout check, patch forward/reverse checks and native tests passed. The patch SHA-256 is 4D59FC4CAAF665521FAD2369E2DAC876F89BFC7DCFE9ECE5F257A72435E56C6D. Raw data, exact input and output identities, and the OTP stage listing are retained here. The full corpus for the candidate belongs to root integration.
