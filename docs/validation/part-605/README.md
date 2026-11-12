# Part605 literal tables, attributes, and first installed OTP release matches

The backend writes the OTP 29 uncompressed LitT table in encounter order, deduplicates literal ETF representations, and includes the table in the module fingerprint. It keeps signed floating zero distinct and emits arbitrary integer operands using the compact encoder. Attr emission preserves declaration order, repeated entries and explicit vsn. Selected tagged tuples and cons patterns can extract and return an element with a constant fallback.

Three new deterministic fixtures cover literals, attributes, literal call operands, and deconstruction. The generated modules matched the OTP fixture images byte for byte and ran in the original VM. A runtime control for repeated attributes uses all matching values because OTP preserves repeated Attr entries.

The Windows release ships compiler 10.0.6, while the selected images record bootstrap compiler 10.0.4. The corpus has explicit input profiles for eight modules, including original build paths, warning and include options, and attribute insertion when present. The C# compiler reconstructs CInf, Dbgi, Attr and Docs from these inputs and source. The output path does not read the original BEAM. The corpus runner opens the original image only for comparison. Fixture sources and original images were hash-checked against the admitted Windows release inventory.

| Application | Module | Complete size |
| --- | --- | ---: |
| kernel | pg2 | 1,224 |
| mnesia | mnesia_registry | 1,100 |
| os_mon | os_mon_mib | 1,144 |
| runtime_tools | erts_alloc_config | 1,420 |
| ftp | ftp_app | 1,504 |
| inets | inets_app | 1,576 |
| snmp | snmpa_discovery_handler_default | 1,956 |
| ssl | inet6_tls_dist | 2,656 |

The profile supports the tested attribute forms and single-clause function annotations, including simple remote arguments and multiple export forms. Recorded build defines in these profiles affect CInf only, and the supported sources do not reference them. Profiles for other release modules and general debug form emission remain open. The pinned compression asset and whole-image release tests target Windows x64.

Pinned references are [beam_asm.erl](https://github.com/erlang/otp/blob/ad05823719d77c8faee87348ea39513d4e2f99c5/lib/compiler/src/beam_asm.erl), [beam_dict.erl](https://github.com/erlang/otp/blob/ad05823719d77c8faee87348ea39513d4e2f99c5/lib/compiler/src/beam_dict.erl), and [compile.erl](https://github.com/erlang/otp/blob/ad05823719d77c8faee87348ea39513d4e2f99c5/lib/compiler/src/compile.erl).

`dotnet build tests/Erlang.Tests -m:1 --no-restore -v:q` passed with zero warnings or errors. Eight selected tests passed, covering 25 deterministic fixtures and eight installed release modules. All eight regenerated release modules loaded in the original OTP VM. [Focused report](focused.json).

`dotnet run --project tools/Validation/Erlang.BeamCorpus --no-build -- <admitted-OTP-root> artifacts/beam-backend/otp-corpus-part605-complete.json` exited 1 after checking 1,366 installed Windows images. It recorded eight exact matches, 856 unsupported modules after parsing, 501 source rejections and one missing source. Full regression, canonical readiness regeneration, and publication remain open. [Per-image report](otp-corpus.json).
