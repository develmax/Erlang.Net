# Terminal literal selectors in the Part 1973 endpoint cohort

Comparison uses the frozen Part 1973 actual Windows OTP 29.1.1 corpus, compiler f685472e21eddb969dc244fa620539ff5d145c8d267b0f8e9486a7f30dad2476, source 3a834f1bd054df421f7d563178599b2bd4cf868dfd0c475ee1cb810e42e20cfa, pinned OTP ad05823719d77c8faee87348ea39513d4e2f99c5. The prior identity-selector artifact remains independent.

| Confirmed source family | Functions | Modules | Reconstructed whole normalized exact bodies |
| --- | ---: | ---: | ---: |
| Literal-return first-argument function heads and simple local or constant-error fallback | 17 | 11 | 17 |
| Nonexhaustive terminal atom case with constant returns and case failure | 9 | 6 | 9 |
| Combined | 26 | 11 | 26 |

The head family consists of all six compact/pretty Megaco `enc_auditItem/2` functions, eight generated Diameter `msg2rec/1` and `rec2msg/1` functions across four modules, and three zlib argument dispatchers. The case family consists of `enc_immAckRequired/2` and `enc_notifyCompletionItem/2` across the six Megaco text encoder modules. See `release-mappings.terms` for pinned abstract source forms and both disassemblies.

`mapped-constant-model.eval` admits one ordinary atom discriminant in X0, distinct keys, entirely literal results that differ from the key, and at least two different results. It requires actual generated bodies to contain blocks with exactly the corresponding tests, immediate returns and private failure continuations. A source-derived model sorts the keys, visits distinct success arms in reverse order of their smallest key, preserves original fallbacks and compares the entire derived function to normalized pinned instructions. Reference terms are used only for comparison. Both the public error and the local effectful fallback keep their existing arguments and location.

The source scan across all 1,366 images found 50 function-head shapes and nine case shapes. Zero were in the 509 prior byte-equal images. Of the 50 head shapes, 32 already begin with a selection and bypass the current linear path, 17 take the measured literal-return plus simple terminal fallback form, and one additional theoretical exact model is `json:encode_atom/2`. The JSON fallback invokes a lexical callback around `atom_to_binary/2` and is excluded by the production gate. Its separate raw pair is retained in `json-control.stdout.log`.

Pinned `beam_core_to_ssa:match_con/4` and `select_val_cg/7` create a switch for distinct keys. `beam_ssa_codegen:cg_switch/3` emits the sorted table with SSA reverse postorder. `beam_ssa_share:share_switch/2` shares equivalent successor blocks, which allows repeated output literals to use the same label. The pinned Core, SSA and precodegen fixture dumps show the success returns and distinct function fallback or synthetic case failure. The explicit case fixture retains the case keyword annotation.

The two proposals reuse `EmitLiteralDispatch`. In `TryEmitLiteralTailClauses`, the head hook uses the already-validated clause and fallback argument rules before their generic sequential tests. In `TryEmitLiteralResultDispatch`, the wider case hook admits an unconditional source entry with a nonexhaustive case over the first input and constant branches. Both bypass known typed ingress and disabled optimizations. The constant-mapping draft does not absorb the earlier identity-table scope.

The fixture compiled via pinned OTP and the exact frozen C# compiler. Both images returned the same 16 native values/errors, and four recorded stack top frames agreed after normalizing paths. The C# frozen image gives positional red evidence on all three positive functions. Tests cover numeric and nested literal returns, shared result labels, local fallback effects, constant raised errors, guard failure and a second pattern column. Source-only patches pass strict whitespace replay against the exact archived P1973 input hashes. Candidate compilation, tests, natively executing candidate output, normalized gain and raw corpus gain remain pending.

## Lower-priority guard partition

The pinned `snmpa_mib_storage_ets:write_ets_file/3` groups two exact unguarded-input Boolean equality guards on X2. Their terminal calls have different literal write options, so `is_boolean` alone would discard the required distinction. A source-shape-preserving model keeps both branch argument changes and exact function_clause fallback and matches the whole normalized function. The frozen module has nine additional normalized function differences. This receipt stages no SNMP compiler change.

Production head patch SHA256 38be2adeb6b5061293429d977661bdebe172ee76ecada5c3be51d294cd99680f.
Production case patch SHA256 a6da475552112203633281fe55d524978e700d2de3f92d1eb4183dc890611010.
Combined permanent test patch SHA256 4d4a57ecf76c190d0d752c395c6f7b14c9d587abc92fafb15d90ff45fba070c2.
