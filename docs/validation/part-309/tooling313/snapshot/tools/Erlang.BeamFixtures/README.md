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

The fixture set also includes a captured closure with an independently declared result. Export additionally queries file metadata with beam_lib and beam_disasm: original module MD5, attributes, compilation information, exports, locals, raw FunT and function keys. These queries read the BEAM file without loading it; on_load remains compilation/disassembly/metadata only. Metadata ETF/text and the exact evaluation, exit status and output digests are retained. Native observations require a fresh successful pinned workflow; merely building this exporter establishes none of these metadata results.

Each successful export also writes a separate `load-nif-opcodes` directory for one new module with three literal `erlang:load_nif/2` caller forms. This does not change the retained six-fixture/five-body-execution manifest. The new module is compiled, disassembled and queried for file metadata only; it is never loaded or invoked in the original runtime. The requested opcode shapes are hypotheses until the actual pinned compiler output passes `verify-load-nif-opcodes.py`. The verifier checks `call_ext`, `call_ext_last` with a positive Y frame retaining Path/Info across observable `put/2`, and a no-frame `call_ext_only` control. It reads actual original ETF disassembly with the existing bounded metadata ETF decoder, binds raw BEAM/chunks, source, compiler/build provenance and head/run/attempt, and retains all compilation/disassembly failures. No native library or loader ABI execution is claimed. The new exporter records `OpcodeShapesVerified=false`; only the separate successful verifier result establishes the three observed shapes.
