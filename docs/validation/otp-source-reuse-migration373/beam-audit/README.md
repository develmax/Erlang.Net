# Runtime-first BEAM readiness: measured queue baseline

Read-only audit of current runtime, with existing readers/reflection only. No source edits, build, CI, publication, native VM or guest execution. Raw input is unchanged pinned OTP-29.1.1 queue.beam. The earlier tentative absence assumption was corrected when the existing Part368 asset was located.

## Measured whole-image result

Input: `docs/validation/part-368/original-receipts-and-raw/artifact-12117323576/capture/queue-installed.beam`,27,544bytes, SHA256 `759B1FE208F8A0DE292147EB00FFCE98F929263ADCFCD4AABB485E3B3EB4538D`. Adjacent `inputs.json` binds it to OTP commit ad05823719d77c8faee87348ea39513d4e2f99c5 and unchanged queue.erl SHA61B322302ECF74578D72B442D2317EA200A5DEE4E0994B58C5E1E961F252B101. Root admission uses existing runtime SHA691B6DC5A350335122D18041A652556911147A1E2B893227C5BF4FCCAA34AAC5 under .NET10.0.11/PowerShell7.6.5.

Existing readers successfully decode **2,068 instructions,45 opcode names,18 imports,40 exports** (38 queue APIs plus module_info/0,1). **BeamModule.Prepare fails** at BeamLinkedCodeReader.ValidateRoles:294. First concrete unsupported instruction: **is_tuple, instruction index10, Code-byte offset25**. Whole-image opcode/import closure is now measured; which paths execute under particular calls remains unmeasured because the module was not admitted or executed.

The existing private ValidateRoles method was also invoked on all456 actual instruction objects whose opcode has no source switch case. All456 reject with the expected default NotSupportedException. Only source-default cases were probed; null function/lambda context was never used to fabricate support for a recognized case. This independently confirms20 concrete role blockers:

`bif1`, `call_last`, `case_end`, `gc_bif1`, `get_hd`, `get_list`, `get_tl`, `is_atom`, `is_function2`, `is_ge`, `is_list`, `is_lt`, `is_nil`, `is_nonempty_list`, `is_tuple`, `jump`, `select_val`, `swap`, `test_arity`, `trim`.

## Catalog versus actual execution

Current BeamOpcodeCatalog defines **191** decoder descriptors. BeamLinkedCodeReader.ValidateRoles has36 explicit role names, BeamRegisterValidation34 explicit names, and BeamRegisterInterpreter30 direct handler names. Four additional receive opcodes are consumed inside a validated receive plan. These counts are neither191 executed instructions nor an assertion that every operand/control-flow shape of a named opcode is supported.

Validator34: `allocate`, `allocate_heap`, `badmatch`, `bif0`, `call`, `call_ext`, `call_ext_last`, `call_ext_only`, `call_fun2`, `call_only`, `deallocate`, `gc_bif2`, `get_tuple_element`, `init_yregs`, `is_eq_exact`, `is_integer`, `is_pid`, `is_tagged_tuple`, `label`, `line`, `loop_rec`, `loop_rec_end`, `make_fun3`, `move`, `nif_start`, `on_load`, `put_list`, `put_tuple2`, `remove_message`, `return`, `send`, `test_heap`, `timeout`, `wait_timeout`.

Interpreter30 has the same names except `loop_rec_end`, `remove_message`, `timeout`, `wait_timeout`; those four require BeamReceivePlanner/Matcher and are not standalone interpreter cases. `func_info` and `int_code_end` are linked metadata, not ordinary executable handler cases. `source-opcodes.json` and `receipt.json` preserve exact machine-readable inventories and source bindings.

## Central capability matrix

| Layer | Current factual capability | Concrete gate / next result |
| --- | --- | --- |
| BEAM container/operands | BeamStructureReader -> BeamInstructionReader decodes the unchanged queue image completely; root receipt records2,068 instructions. Catalog is metadata only. | Preserve exact input/provenance and validate all encountered operand shapes without widening admission by label alone. |
| Linker/validator/executor | 20 encountered opcode names lack linker roles; later execution/frame checks have their own restrictions. BeamRegisterValidation.cs:36-157; BeamRegisterInterpreter.cs:168-414. | Implement only measured shared runtime needs, with instruction semantics, failure branches, registers/liveness/frame and control flow together; then unchanged whole-image Prepare must pass. No export-only waiver of unsupported functions. |
| External calls/imports | BeamExternalImports.cs:11-21 whitelists self/0; whereis/error/call_on_load_function/nif_error arity1; +/-/put/load_nif arity2; code:is_loaded/1; os:getenv/1. Registered C# APIs do not automatically cross this gateway. | Queue's87 ordinary external call sites use error/2, ++/2 and lists operations outside that whitelist. Two further sites are get_module_info calls with an existing special generated-metadata route, which must be recognized against actual bodies. Extend legitimate runtime dispatch with metadata/caller semantics and tests rather than making module-specific exceptions. |
| Inline BIF operations | bif0 accepts self/0 only (validation:164-169/interpreter:289-291); gc_bif2 accepts +/- arity2 only (validation:173-179). | Queue contains18 gc_bif1 length/1 sites, one bif1 is_list/1,15 gc_bif2 div/2 and7 existing +/- sites. Supporting erlang:length in the registry is not support for its BEAM calling form. |
| Registers/functions/callbacks | Explicit X/Y state, local calls, make_fun3/call_fun2 and source-code leases exist. Frame cap1,024slots; admitted shapes validated. | Queue callback/function-test and trim/swap/call_last paths are encountered blockers. Verify frame restoration, arity errors, live register ownership and cancellation cleanup with actual bytes after support. |
| Process/scheduling | Interpreter executes context.ReduceAsync per instruction:168; source entry/image leases and actor continuation admission are real runtime mechanisms. | This proves plumbing, not all process-BIF imports. Whole queue execution and its failure/cancellation traces still require admission. |
| Mailbox/send | send uses ErlangOperatorValues.TrySendAsync:293-298; loop_rec uses real ProcessContext.ReceiveAsync:301-316. Planner requires a bounded wait_timeout/timeout loop and supported matcher operations (BeamReceivePlanner.cs:7-99). | Keep existing real small BEAM send proof; general receive control flow is limited and must not be declared whole BEAM support. Queue's observed image contains no send/receive opcodes, so widening mailbox support is not its measured first blocker. |
| Exceptions | badmatch, function_clause branch failures, BIF guard-failure transfer and finally-based frame disposal exist (interpreter:188-198,374-396,415-424). | No general try/catch handler appears in validator/interpreter cases. Do not infer exception-stack parity or that queue requires absent exception opcodes; its actual opcode inventory is authoritative. Compare class/reason/full caller metadata after admission. |
| Publication/code lifecycle | ModuleRegistry.PreparedBeam.cs:7-22 validates exports before publication; :25-83 handles ownership, duplicate/pending/old-code checks and source registration. PreparedBeamImage.cs:16-27 routes to interpreter or exact generated metadata. | Use actual image ownership/old-code lifecycle and assert handlers belong to PreparedBeamImage. Existing C# queue remains a maintained, selectable alternative; no implementation or selection change occurred here. |

Queue imports exactly: erlang:is_list/1,error/2,length/1,+/2,++/2,div/2,-/2,get_module_info/1,2; lists:reverse/2,split/2,member/2,last/1,filtermap/2,foldl/3,foldr/3,any/2,all/2. Call-site details are in queue-imports.json; not every import is an ordinary call_ext instruction.

## Existing real execution proof and module order

`tests/Erlang.Tests/ActualMinimalBeamGuestSendHostChecks.cs:17-29` prepares the genuine source-built original guest_connected_bang322 image, checks Supports(bang/2), then invokes it against connected distribution ownership/order/pressure/register/lease controls. Image SHA `AAADAC2F0BE0422FC84C4B79F398DD6EDD5B0D35778D925A0A4BC2D0BF2FE7E5`, original provenance run38836253990, pinned source/BEAM MD5 verified on both original nodes. Permanent registration is Program.cs:171. `docs/validation/part-371/evidence/canonical-final/tests.json` records this HOST as Passed. The provenance file's older materialization-only status is historical; later actual HOST evidence is separate. This proves a real small unchanged native-produced image executes on our .NET runtime, **not** execution of complete upstream queue/proplists/ordsets/maps/binary.

| Module | BEAM migration evidence in this audit | Next measured unit |
| --- | --- | --- |
| queue | Exact original asset decoded; complete static image inventory known; actual Prepare rejected as above; no guest execution. | First unchanged whole-image admission, then new/in/out/len/badarg/callback controls and all38 API contracts, with source/image owner assertions and full stacks. |
| proplists | Prior C# API compatibility is reference evidence; no whole upstream BEAM execution admitted by this audit. Required image closure not measured here. | Next exact pinned image/provenance, inventory and Prepare; reuse shared runtime support learned from queue. |
| ordsets | Existing native-produced BEAM assets are present in Part371 evidence, but their .NET loader/executor closure was not inspected/executed here. | Exact image inventory, then all21 APIs on that image; do not substitute C# handler comparisons for migration proof. |
| maps | Source/C# API evidence does not establish whole upstream BEAM execution; image closure unmeasured here. | Separate pure wrappers from actual native primitives, admit exact image and preserve own-VM cursor/hash protocol. |
| binary | Existing native-produced BEAM assets are present in Part369 evidence; no .NET execution admission was performed here. Upstream source has native stubs and Erlang wrappers. | Mixed primitive/source crosswalk, then exact image admission and native binding of stubs while wrappers execute as Erlang. |

Source/epp/moduledoc limitations belong to the separate Erlang-source compiler route. They are **not** blockers to reading already compiled queue.beam. No new broad audit, source compiler change or BEAM implementation was made here.
