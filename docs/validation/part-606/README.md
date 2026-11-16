# Part606 additional OTP release profiles

The backend emits an empty-list argument check with the function-info failure label. It folds supported sequences of fresh bindings to closed values and constructs a single-cell list in a fresh call argument register. Independent constant moves follow OTP's alternating selection order. Debug info generation serializes supported nested tuples, cons cells, maps, variables, and match expressions with original token annotations. Docs signatures for non-variable patterns use name/arity.

The C# compiler regenerated four additional installed Windows OTP 29.1.1 images from the distributed sources and explicit release build parameters. The reference images and sources were independently hash-checked against the admitted release inventory. The original BEAM is read by the comparison runner after generation.

| Module | Image size |
| --- | ---: |
| odbc_app | 1,700 |
| runtime_tools_sup | 1,884 |
| disk_log_sup | 1,904 |
| tls_connection_sup | 2,612 |

All four generated images loaded in the original OTP VM. The runtime checks exercised supervisor data results, successful matching of an empty list, and function_clause on a nonempty list. The five-argument deterministic fixture compared independent call moves for both local and external calls.

Pinned references are [beam_ssa_codegen.erl](https://github.com/erlang/otp/blob/ad05823719d77c8faee87348ea39513d4e2f99c5/lib/compiler/src/beam_ssa_codegen.erl), [genop.tab](https://github.com/erlang/otp/blob/ad05823719d77c8faee87348ea39513d4e2f99c5/lib/compiler/src/genop.tab), and [erl_parse.yrl](https://github.com/erlang/otp/blob/ad05823719d77c8faee87348ea39513d4e2f99c5/lib/stdlib/src/erl_parse.yrl).

`dotnet build tests/Erlang.Tests -m:1 --no-restore -v:q` passed with zero warnings or errors. Eight selected tests passed, covering 26 deterministic fixture modules and 12 installed release images on Windows x64. [Focused report](focused.json).

`dotnet run --project tools/Validation/Erlang.BeamCorpus --no-build -- <admitted-OTP-root> artifacts/beam-backend/otp-corpus-part606-final.json` exited 1 after checking all 1,366 installed Windows images. It recorded 12 byte matches, 852 unsupported modules after parsing, 501 source rejections, and one missing source. An intermediate run generated tls_connection_sup with different release metadata. The final profile and map annotations resolved that difference. Full regression, canonical readiness regeneration, and publication remain open. [Per-image report](otp-corpus.json).
