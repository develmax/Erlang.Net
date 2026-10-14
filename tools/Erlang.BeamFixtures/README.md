# Pinned development BEAM fixtures

This standalone development tool compiles five independently authored Erlang modules with a source-built OTP-29.1.1 compiler at commit `ad05823719d77c8faee87348ea39513d4e2f99c5`. It has no production runtime project references and registers no Erlang API.

Build the project, then run its DLL with:

```text
BeamFixtureExport <installed-otp-root> <new-output-directory> <source-build-evidence.json> [--execute]
```

For this source-build layout, the runtime root is `PREFIX/lib/erlang`, containing its `bin` and `releases` directories. The workflow records and passes this root consistently.

The evidence must bind the source commit/tag, successful source build, installation root and complete build-log digest. The tool verifies the supplied fields, exact installed OTP_VERSION and digest; it retains that evidence unchanged. Independent workflow/job/artifact verification is still required before treating it as original proof. A pre-existing output directory is rejected.

The export retains source, compiler flags and captured exit/stdout/stderr, raw compiler-produced BEAM, every IFF chunk payload digest, original beam_disasm output and the installed oracle version. The chunk reader inventories this compiler output; it is not a production loader or general BEAM acceptance contract.

With `--execute`, four safe modules run in separate fresh OTP processes. Independent expected values are declared before execution: `{ok,42,[alpha,beta]}`, `42`, `{ok,[1,2,3]}` and `{ok,ping}`. Each process saves actual and independently expected ETF bytes, asserts exact term equality and records its real exit status. Failures retain partial output and cannot create a successful final manifest. The fifth on_load fixture is compilation-only; callback-process effects are not claimed as observed.

The manual `Pinned OTP BEAM fixtures` workflow builds the pinned compiler itself, records source/install markers and provenance, then runs this tool. Development oracle use does not add a production BEAM dependency. Successful original fixture export does not establish CLR loading or execution.

The harness and fixture sources are newly written. Retain the repository's consolidated third-party notices for referenced OTP implementation and development evidence. Do not include the owner's private local license or publish packages as part of this workflow.
