# Part607 call flows and tftp_app release parity

The backend can pass the result of a local zero-arity call into a static external tail call. It can also lower a case over a static external call with closed arguments, a tagged two-element tuple branch, and an atom branch with a closed result. It emits the tuple type operand and the case_clause failure path. This is a scoped lowering profile. General cases, guards, and call chains remain open.

The release metadata writer now serializes local calls and case forms for the supported debug syntax. It omits undocumented private functions from the exported Docs entries. The tftp_app profile records compiler 10.0.4, build paths and warning options from the admitted Windows release. The C# compiler regenerated the 1,848-byte image using the distributed Erlang source. The retained source and reference image hashes match the admitted release inventory.

The generated tftp_app loaded in original OTP. An isolated VM started and stopped the supervisor with the services option unset and explicitly set to an empty list. The deterministic beam_call_case_probe image matched the original compiler byte for byte and exercised both success branches and case_clause in OTP. Negative controls reject flow shapes that would reuse an already bound parameter as a new binding.

Pinned sources include [beam_asm.erl](https://github.com/erlang/otp/blob/ad05823719d77c8faee87348ea39513d4e2f99c5/lib/compiler/src/beam_asm.erl) and [genop.tab](https://github.com/erlang/otp/blob/ad05823719d77c8faee87348ea39513d4e2f99c5/lib/compiler/src/genop.tab).

`dotnet build tests/Erlang.Tests -m:1 --no-restore -v:q` passed with zero warnings or errors. Eight selected tests passed, covering 27 deterministic fixture modules and 13 installed release images on Windows x64. [Focused report](focused.json).

`dotnet run --project tools/Validation/Erlang.BeamCorpus --no-build -- <admitted-OTP-root> artifacts/beam-backend/otp-corpus-part607.json` exited 1 after checking 1,366 installed images. It recorded 13 byte matches, 851 unsupported modules after parsing, 501 source rejections, and one missing source. Full regression, canonical readiness regeneration and publication remain open. [Per-image report](otp-corpus.json).
