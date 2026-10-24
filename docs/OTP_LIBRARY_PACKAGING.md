# Original OTP library packaging

Packaging progress counts module names in the admitted OTP29.1.1 application inventories, not compatible APIs. Original BEAM executes on our .NET runtime; existing C# alternatives remain separate libraries. No fallback or automatic selection of dependency backends is introduced.

| Application | Original modules packaged in src | Admitted application modules | Packaging progress | Validation scope |
| --- | ---: | ---: | ---: | --- |
| stdlib | 98 | 98 | 100% | 241 public-consumer checks; actual11 collection/binary modules; retained72 process observations; four lists/queue backend combinations via projects and cold NuGet/publish |
| kernel | 104 | 104 | 100% | 218 public-consumer checks; all104 image identities; eight inet_parse outcomes compared with fresh original OTP; cold NuGet/publish |
| erts | 22 | 22 | 100% | Shared104 ERTS/SASL checks; all22 image identities; original counters/atomics wrappers and native resource cleanup; complete original outcomes/stacks |
| sasl | 17 | 17 | 100% | Shared104 ERTS/SASL checks; all17 image identities; original misc_supp/systools_lib outcomes and complete errors; cold NuGet/publish |
| syntax_tools | 9 | 9 | 100% | Shared53 checks; all9 image identities; original erl_syntax/prettypr execution and complete error stacks; cold NuGet/publish |
| parsetools | 4 | 4 | 100% | Shared53 checks; all4 image identities; actual original yeccparser grammar declarations/rules/errors; cold NuGet/publish |
| compiler | 59 | 59 | 100% | 138 consumer checks; all59 image identities; actual original beam_opcodes/erl_bifs results and complete stacks; cold NuGet/publish; not compiler self-hosting |
| tools | 16 | 16 | 100% | Shared86 consumer checks; all16 image identities; actual xref_utils values/errors; cold NuGet/publish |
| runtime_tools | 13 | 13 | 100% | Shared86 checks; all13 image identities; actual msacc calculations and application/supervisor callbacks, complete errors; cold NuGet/publish |
| eunit | 13 | 13 | 100% | Shared182 checks; original eunit_lib values/errors; complete stacks and cold NuGet/publish |
| edoc | 21 | 21 | 100% | Shared182 checks; original edoc_lib helpers/errors; four verified priv assets; cold NuGet/publish |
| xmerl | 35 | 35 | 100% | Shared182 checks; original xmerl_lib helpers/errors and image identity; cold NuGet/publish |
| mnesia | 31 | 31 | 100% | 82 checks; original fragmentation/hash-state and application callback helpers;24 complete outcomes/stacks and cold NuGet/publish |
| asn1 | 22 | 22 | 100% | 65 checks; original EXTERNAL tuple/map conversion and compiler-table lifecycle;26 complete outcomes/stacks with deterministic ETF; cold NuGet/publish |
| crypto | 2 | 2 | 100% | Shared115 checks; all81 original curve tables, aliases and errors; native crypto operations remain separate |
| public_key | 42 | 42 | 100% | Shared115 checks; translation/policy helpers and original PKCS-3 DER encoder/TLV decoder;213 complete outcomes/stacks with crypto;84 combined source/input archive entries |

These sixteen applications total508/508 packaged module names (100% of these sixteen application inventories). The ERTS descriptor lists19 modules and the distribution includes three additional implementation images; all22 are packaged and the distinction is recorded in provenance. This is not a project-wide completion percentage or full compatibility claim. The official kernel producer is Windows x64; platform identity remains explicit. No newly registered runtime APIs or full contracts are added by packaging.

Projects:

- `src/Erlang.Otp.Stdlib.Beam`: original stdlib images, source and producer provenance; OtpStdlib.Select selects Beam or an established supported Managed alternative per module.
- `src/Erlang.Otp.Kernel.Beam`: complete original kernel source/image/application-resource inventory; OtpKernel.ReadImage/ReadApplication/ReadApplicationUpgrade and explicit OtpKernel.Beam startup choices.
- `src/Erlang.Otp.Erts.Beam` and `src/Erlang.Otp.Sasl.Beam`: complete original image/source/resource inventories and explicit OtpErts/OtpSasl readers/BEAM selection factories. ERTS native BIFs remain C# runtime primitives; no native ERTS executable is shipped.
- `src/Erlang.Otp.SyntaxTools.Beam` and `src/Erlang.Otp.Parsetools.Beam`: all13 original images,16 source/header files and four application/upgrade resources; OtpSyntaxTools/OtpParsetools readers and explicit BEAM selectors.
- `src/Erlang.Otp.Compiler.Beam`: all59 original images,68 source/header files and compiler.app/appup; explicit OtpCompiler readers/selectors. Source compilation/self-hosting remains separate.
- `src/Erlang.Otp.Tools.Beam` and `src/Erlang.Otp.RuntimeTools.Beam`: all29 original images,32 source/header files and four application resources. Explicit OtpTools/OtpRuntimeTools readers and selectors; live tracing/profiling/native driver compatibility remains separate.
- `src/Erlang.Otp.Eunit.Beam`, `src/Erlang.Otp.Edoc.Beam` and `src/Erlang.Otp.Xmerl.Beam`: all69 original images,85 source/header/grammar files, six application resources and four EDoc support assets. OtpEunit/OtpEdoc/OtpXmerl provide explicit readers/selectors; OtpEdoc.ReadSupportFile reads a whitelisted hash-checked priv asset. The escript launcher is source data and is never executed by the reader.
- `src/Erlang.Otp.Mnesia.Beam`: all31 original images,32 unchanged Erlang sources/headers and two application resources. OtpMnesia readers/selectors preserve explicit backend/dependency ownership.
- `src/Erlang.Otp.Asn1.Beam`: all22 original images,25 source/header files and two application resources. The descriptor names only asn1rt_nif; all21 additional compiler/implementation images are preserved. Two generated release sources are identified separately from23 Git-matched files.
- `src/Erlang.Otp.Crypto.Beam` and `src/Erlang.Otp.PublicKey.Beam`: all44 images,four application resources and84 original Erlang/header/ASN.1 inputs:132 files in total. Fifty-two inputs match pinned Git;32 are generated release sources. Native producer DLLs are inventoried as exclusions and not shipped. OtpCrypto/OtpPublicKey provide hash-checked readers and explicit BEAM selectors.
- `src/Erlang.Otp.Stdlib` and `src/Erlang.Otp.Kernel`: existing C# alternatives and their current registration/composition APIs. The strict startup selector's Managed support is narrower than every C# registration; unsupported choices fail explicitly.

Next: extend other applications according to the boot dependency inventory, preserving admitted compilation variants and explicit dependency boundaries. Shipping an application descriptor does not automatically start it; native bindings, selected dependency images and the existing boot protocol remain required. Continue actual image ownership/execution checks and original OTP comparisons separately from file inventory checks.

## Remaining official application inventories

The admitted Windows release contains 1321 BEAM files across 34 applications (36 application directories including C/Java support). This batch packages508/1321 image names (38.5%); this is packaging progress only. Source names, descriptors, generated support and native platform prerequisites remain distinct. The complete module list is in [OTP_LIBRARY_INVENTORY.json](OTP_LIBRARY_INVENTORY.json).

| Remaining application | BEAM images | Packaging state |
| --- | ---: | --- |
| common_test | 47 | Not yet packaged |
| debugger | 24 | Not yet packaged |
| dialyzer | 29 | Not yet packaged |
| diameter | 47 | Not yet packaged |
| eldap | 2 | Not yet packaged |
| erl_interface | 0 | C/Java support; no BEAM images |
| et | 6 | Not yet packaged |
| ftp | 6 | Not yet packaged |
| inets | 63 | Not yet packaged |
| jinterface | 0 | C/Java support; no BEAM images |
| megaco | 65 | Not yet packaged |
| observer | 44 | Not yet packaged |
| odbc | 3 | Not yet packaged |
| os_mon | 8 | Not yet packaged |
| reltool | 9 | Not yet packaged |
| snmp | 90 | Not yet packaged |
| ssh | 43 | Not yet packaged |
| ssl | 78 | Not yet packaged |
| tftp | 8 | Not yet packaged |
| wx | 241 | Not yet packaged |

Next priority: remaining application families and the explicit runtime/filesystem/parse-transform prerequisites for executing the original compiler pipeline. Native profiling/tracing/driver effects must be validated separately from these finite pure calculations. Packaging an image does not prove every export, I/O effect, on_load hook, native binding or application-start closure.

Part467 executes30 complete helper outcomes/stacks from unchanged eunit_lib/edoc_lib/xmerl_lib plus original lists/gb_trees. The shared loader proves a bounded concrete literal call cannot return before admitting the optimized try-case boundary. Unknown values, branching provenance, returning calls, cycles and malformed frame ownership remain refusals. This is not whole EUnit runner, EDoc rendering, XML parser or application boot certification.

Part468 executes original mnesia_frag_hash and mnesia_app with original lists and an identical compiled observer:24 whole outcomes/stacks cover state initialization, split/merge transitions, legacy state, selected hash routing, fallback match-spec fragment ranges, invalid inputs and application callbacks. Single-key match-spec analysis through mnesia:has_var, live application startup, transactions, disk recovery, replication and distributed database semantics remain separate obligations.

Part469 executes original asn1ct_eval_ext and asn1ct_table, original lists/ets and an identical observer on .NET:26 complete outcomes/stacks cover EXTERNAL1994 tuple/map conversion, omitted/extra map keys, invalid inputs and the compiler-table create/reuse/insert/lookup/size/list/match/delete lifecycle. Existing native ETS primitives are composed explicitly. Complete typed results agree; ordinary ETF map serialization order differs. The accepted raw comparison uses OTP term_to_binary/2 with the deterministic option, preserving the whole result without frame removal or identity mapping. Original ASN.1 compilation, generated encoder/decoder execution and asn1rt_nif native ABI remain separate obligations.

Part470 executes original crypto_ec_curves, pubkey_translation, pubkey_policy_tree, generated PKCS-3, lists and an identical observer.213 complete outcomes/stacks cover every81 canonical curve table through both readers, aliases/invalid names, key-structure translation, all16 policy-tree helper exports, DER encoding (signed/large INTEGER, optional fields, invalid records/types) and prepared-TLV decoding. Entire53492 deterministic ETF bytes match OTP. This is finite original execution: crypto NIF/OpenSSL operations, crypto:supports/curves enumeration, general ASN.1 native BER parsing, complete certificate validation, live native application boot and TLS are not certified.
