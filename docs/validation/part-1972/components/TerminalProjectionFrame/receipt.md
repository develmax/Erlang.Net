# Terminal tuple projection frame slots

## Scope and pinned lowering

The census uses 857 frozen Part 1969 different images, overlaying 101 actual Part 1970 pilot output files from the 113-entry report. It found 21 functions in 19 modules where a single non-tail call is followed by X0 tuple-arity dispatch, a projected result occupies Y, and the candidate allocates more frame than the pinned function. Ten functions in nine modules have exactly two such result destinations. Two have the exact 3-versus-1 width, mnesia_backup:read/1 and pubkey_cert:handle_policy_mappings/2. A second pass locates 20 post-call-only projected slots in 13 functions and 11 modules. These are observed upper bounds, not passed promotions.

The pinned mnesia_backup pre-codegen listing at line 1042 identifies one Y occupant. The checked restore record is live through disk_log:chunk/2. In the arity-three arm X0 holds the returned tuple across update_record, with the first field and then the updated record in X1. The later result field goes to X0 only after that update. The arity-two arm uses X1 for the continuation and X0 for the data or reason. The existing candidate writes the two data projections to Y0 and Y1, and the first argument request also uses Y1 before the call. Pinned beam_ssa_pre_codegen:find_yregs at 1950 marks values crossing calls, reserve_yregs at 2658 reserves the recorded occupants, reserve_xregs at 2972 and linear_scan at 3262 allocate the remaining values.

Paired raw controls explain why the full-width retake needs additional work. pubkey_cert also writes both Y targets before its local call, and must preserve pre-call argument effects. xmerl_validate:question/4 keeps a sibling error path's incoming Y1 alive while reusing that same slot in a successful tuple branch. dtls_gen_connection:decode_cipher_text/1 has multiple record carriers and nested updates. A single global last-use, a blanket stack-to-X conversion, or a wider inter-call linear scan do not prove these lifetimes.

## Source-only retake

The late inter-call frame writer now has a terminal-path plan using its existing compact operand, ownership, and remapping logic. Only a single owned frame and one ordinary call feeding X0 tuple-arity selection are considered. Each Y0 definition must be a projection from X0 after that call and all reads must be accounted for within the associated interval. Earlier uses or branch entries bypassing a definition, backwards edges, an original X1 read, live X1 at an interval exit, protected/debug/trimmed frames, and a non-tail call inside a region prevent promotion. TestHeap extends the live X prefix only along paths whose projected value will be read. The defining projection witnesses that X0 is initialized, and admitted opcodes either preserve it or write an initialized replacement. The existing inter-call rule and disabled optimizer path keep their own schedule.

Only the post-call Y0 can be removed by this intake. Analysis predicts frame 3 to 2 for mnesia_backup:read/1, with two nine-word heaps moving from live 0 to live 2 and the recursive five-word heap unchanged. The remaining Y slot crosses distinct pre-call and post-call definitions and has a separate scheduling dependency.

## Verification

The strict production/test patches applied exactly to the isolated replay files. The existing frame-ownership/flow code was invoked on the generated fixture and the actual P1970 mnesia_backup image. Both have one owned frame and pass frame-rewrite checks. The measured intervals have no bypass edge or old X1 exit use; both contain the same two live-root changes. Reproduction is in flow-proof.ps1 and flow-proof.stdout.log.

The candidate test requires a two-slot read frame, the unchanged three-slot negative frame, and the exact root counts for returning and recursive branches. The parent compiler (559E7F5052109CB87941E14A065EB36A7BAEA3F225EF85877A7AF8B239549F75) emits three slots for both functions, establishing structural red. Installed native OTP ran the pinned fixture and generated parent fixture through eight profiles each, including error identity and first stack frame. Candidate C# build, candidate execution/native preparation and corpus parity remain open. No SDK run or shared tree change occurred.

First scope before linking should include mnesia_backup, xmerl_validate, pubkey_cert and dtls_gen_connection, retain already exact modules in the associated call/case regressions, and widen to the measured 19-module screen only after the planned slot mapping has passed native preparation and actual runtime checks.
