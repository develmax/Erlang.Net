# Part232 allocation failure source review

Read-only review of published d2e2e034fe5d0ff618184bd45eac49cec360fd57. No production edits, rebuilds, or executions were performed for this review.

## Observed evidence

The failed Windows CI232 report has 6346/6347 checks passing. The inherited `erlang/hash-iterative-deep-terms-and-no-binary-payload-copy` host failed with the generic BinaryCopied diagnostic. Its current test measures GC.GetAllocatedBytesForCurrentThread across an awaited ModuleRegistry.Call, but captures neither allocated bytes, start/end managed thread IDs, nor ValueTask completion state. Therefore the report does not establish an actual binary payload copy or its allocation size.

Root's separate direct-DLL probe reports 100 calls over five processes, synchronously completed on stable threads, with 440 warm bytes and 1128 cold bytes per four-MiB binary hash. These local observations are below the unchanged 4096-byte budget. They are not a reproduction of the failed CI allocation, an original OTP performance comparison, or proof that a runtime bug was fixed.

## Source-confirmed paths

- ErlangHashRegistrations.phash2/2 validates the range, evaluates ErlangNativeHash.Portable2, creates the Integer result, and returns ValueTask.FromResult. Its body is synchronous.
- ErlangNativeHash.Portable2 creates a Stack<Frame> and its backing storage. The BitString branch reads binary.ByteSpan, slices ReadOnlySpan<byte>, and calls ErlangBlockHash.Hash. The block hash uses span slicing and scalar arithmetic. This binary branch contains no payload ToArray/copy allocation.
- ModuleRegistry.Call may enter the native module's code image. ProcessContext.EnterSourceCode allocates SourceCodeLease, SourceCodeScope, SourceExecutionScope, and SourceExecution; AttachSourceCode and scope cleanup update AsyncLocal state. The host also creates a params argument array and range/result Integer terms. These are real wrapper allocations, separate from binary payload storage.
- ProcessTracing.Called checks the dictionary under its gate. With no configured matching pattern, it immediately returns null before constructing Cons, MatchSpecResult work, trace output or ProcessTraceInvocation. Complete with null invocation returns the existing ValueTask. There is no source-confirmed allocation in the newly added default trace dictionary lookup itself; exact CLR lock/dictionary costs are not asserted here.

No source inspection establishes which overhead varied in CI. AsyncLocal/context storage has allocation opportunities, but attributing the failure to a particular lazy initialization, scheduling event or JIT effect would be speculation.

## Bounded test measurement repair

Retain the same four-MiB input, existing warm-up, ModuleRegistry.Call API result check, iterative deep tuple/map checks, and exact 4096-byte budget. Capture the starting managed thread ID and allocation counter. Invoke ModuleRegistry.Call into a ValueTask local; capture IsCompletedSuccessfully; require true and retrieve GetAwaiter().GetResult synchronously. Capture ending allocation counter and managed thread ID before formatting diagnostics. Require equal thread IDs, Integer result, and allocation delta <=4096. Failed completion/thread/allocation assertions must expose actual bytes, both thread IDs and completion state, with formatting outside the measured region.

This retains the full registered API path rather than substituting a pure helper hash. A preallocated argument array/range term outside the measurement is reasonable if the intended budget is specifically the invocation path, but should be explicitly described; it is not necessary for correctness and does not explain the original CI failure. Do not retry, choose a minimum sample, raise the budget, or suppress a failing sample. If a synchronous measurement still exceeds the budget, preserve the concrete diagnostic and investigate its actual allocation path.

CI232 remains failed evidence. A new test-only candidate and its fresh CI must establish the corrected measurement independently; passing local probe rounds do not promote the old failed run.
