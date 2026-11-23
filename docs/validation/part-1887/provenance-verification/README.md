# Part1887 shadow retake release scope

The exact 110-module candidate uses source archive ECB7F3B64ABE442C4C4BCC9715BD9441FC0846863F91919AB7AC1EC826CB1A63, compiler 070AE969202CE8D6EFA7641554CDC3F19C01F521B77973FAE5C68EE2B2C82BF4 and the pinned Windows OTP-29.1.1 comparison. The prior full corpus is Part1883, with exact Part1886 results available for 42 of these modules. The scopes were matched by release BEAM path. Any pruned exact image was represented through its verified whole-file equality receipt.

The uncached Part1887 scope completed in 186.13 seconds with 18 Equal and 92 Different. There were zero Unsupported or SourceRejected results, zero whole-file gains and zero losses. Eighty images differ from the exact Part1883 output. The linked focused report passed 56/56 selected cases. Pinned ERTS prepared 79 changed images in the ordinary lane. The separate asn1rt_nif on-load check loaded the exact candidate and matched all nine checked outcomes. The archived native terms show BER decode, BER encode and PER valid and rejection cases. [Candidate terms](../bootstrap-and-derived-source-annotations-1887-shadow-retake/native-asn1/candidate.terms) · [Reference terms](../bootstrap-and-derived-source-annotations-1887-shadow-retake/native-asn1/reference.terms)

| Source change | Raw Dbgi gains vs full Part1883 | Raw Dbgi gains in exact Part1886 overlap |
| --- | ---: | ---: |
| Bootstrap include-file provenance | 12 | 8 |
| Synthesized bitstring type base/unit annotations | 4 | 2 |
| First annotation for abstract match, case and if | 5 | 3 |
| Total | 21 | 13 |

Part1883 has 85/110 exact raw Dbgi payloads. Part1887 has 106/110. The exact Part1886 overlap moves from 27/42 to 40/42. Neither comparison loses a previously exact raw section. All 110 decoded debug options and raw CInf sections match both the full prior and the pinned release. The exact Part1886 overlap similarly preserves every option list and compile-info payload. Exact names, changed paths and chunk hashes are in the machine receipt.

ASN.1 and the Unix-built ERTS preloaded profiles retain the recorded absolute -I arguments, warning options, debug defaults and cwd. ASN.1 asn1ct resolves include_lib to bootstrap kernel file.hrl and stdlib erl_compile.hrl. ERTS prim_file retains in-tree kernel/src/file_int.hrl and kernel/include/file.hrl through ordinary search, while init uses bootstrap kernel/include/file.hrl through application lookup. EUnit retains its relative ../../stdlib/include/assert.hrl and its ordinary ../include option while correcting the bootstrap file.hrl alias. The Megaco measurement profiles preserve their lexical ../include recorded option. The remaining defaults change only derived annotations. Synthesized zeros keep the opening token's logical line, explicit zero operands retain their columns, and decoded form equality after ignoring annotations and file paths holds for all 110 modules.

The four residual raw Dbgi sections are beam_doc (bootstrap eep48.hrl path), ssh_transport (bootstrap inet.hrl path), observer_backend (in-tree observer_backend.hrl lexical include path), and eunit_lib (annotation differences in list tails). The first two are now exact after removing file path spellings. eunit_lib and its remaining scopes retain exact decoded options and file transitions.

All four normalized code losses against Part1883 already appear in the exact Part1886 images. The dbg_iload helper and the two cover helpers also already differ in Part1885. The inet:set_bindx_port/2 body is exact in Part1883 and has two unread X projections at the same locations in Part1886 and Part1887. No Part1885 inet image was retained. These instructions are unrelated to the bitstring type metadata that closes inet's Dbgi. The inet_tcp_dist helper gain already appears in Part1885. ssh_connection_handler:conn_info_chans/1 first matches in Part1887 within the retained chain. One frame widens from six to seven slots in the ASN.1 generator -gen_dec_set_cases/4-fun-1-/4, whose reference uses five slots. No exact Part1885 or Part1886 image was retained for that function's module. Four other frame distances improve in the linked report.

These checks read archived outputs and use pinned OTP decoding and disassembly. No release outputs were rebuilt during the audit.
