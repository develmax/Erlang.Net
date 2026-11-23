# Direct call result into put_map_assoc

Baseline is the frozen Part 1845 compiler (SHA256 30BA1ABC346A5DFC18E62DA098E2F649A80DB83E6D45BDEC1088AE2B234B6D95). The source overlay SHA256 is 3E436816352C333EF8DFFD3C4670B735A1B5FE8F9FE81C32EE9A0FFE4DDD6441. The reference is Windows OTP 29.1.1 commit ad05823719d77c8faee87348ea39513d4e2f99c5. The isolated build uses .NET SDK 10.0.400 and the frozen runtime dependencies.

The existing call-result rooting pass now accepts adjacent nonfailing put_map_assoc with destination X0 and original live X count zero. The map has no other X operands before the change. A complete lifetime proof admits the saved argument only when the map is its only use on all reachable paths. The old Y slot remains initialized until the compactor can remove it. No constructor other than line metadata may intervene.

Pinned beam_ssa_codegen supplies Live for put_map_assoc. beam_validator:verify_put_map validates the live prefix and Y initialization and prunes X registers before assigning the destination. The C# pass adds X0 to that prefix, retains the failure and destination, and preserves literal and typed compact suffixes. It applies the raw source/field and live-count edits together in physical order.

- The isolated compiler build completed with zero warnings and errors. The new permanent assertion fails on the frozen compiler and passes on the candidate. One compiled Erlang fixture contains positive value and dynamic-key cases plus a later-read negative. The positive instruction blocks match OTP and the negative block is unchanged.
- Eight native fixture observations match the frozen, candidate and pinned compiler, including bignum and nested-map values, compound keys and a preserved later use.
- The eight-module selected recompile remains at 4 Equal and 4 Different, zero Unsupported and SourceRejected. All four affected binaries change and all twelve baseline, candidate and pinned preparations succeed in OTP.
- socket:tables/0 shrinks from 5 to 4 Y slots, edoc_layout_chunks:doc_content/2 from 2 to 1, logger:get_config/0 from 4 to 3, and edlin_key:key_map/0 from 1 to 0. All four keep the same live X roots at allocation and reach the pinned frame widths. The public edlin key-map deterministic term digest also matches the three versions.
- The baseline cohort comparison took 18.985 seconds and the candidate 21.272 seconds with one worker on a shared host. This small run is not a throughput benchmark.
- Read-only inventory uses all 960 saved Different images from the already completed Part 1845 full comparison. The pass projection evaluates 74 selected image Code streams, removes 97 saves in 66 modules and reports no decoder failures. It does not establish whole-file equality. This candidate was not run against a new full corpus or the standard full regression.

The first archive extraction misresolved two relative staging names to D:\Work and D:\baseline. Every newly created file was checked against the intended frozen staging bytes. The new top-level entries and the duplicate baseline were moved into recovered-staging-copy without touching pre-existing files. No linked working checkout or OTP reference file was changed.

Patch SHA256 is 53F99ACA8FB31E1FD3A51DD79407B51300CF196BF5DB8BD4C8DFF32DDD0E37A9. Forward and reverse checks validate it at the intended frozen base.
