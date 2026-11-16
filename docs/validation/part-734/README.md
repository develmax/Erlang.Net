# Part734 Byte-identical HTTP connection supervisor image

The compiler handles the nested singleton and pair list pattern in X registers before allocating a zero-slot call frame. It retains the original argument for pattern failure, emits the local lookup, self BIF and registration in source order, and folds the remaining pure bindings into one return literal. Other clause shapes retain their existing lowering.

The reference is Windows OTP-29.1.1 ad05823719d77c8faee87348ea39513d4e2f99c5. The profile reproduces the HTTP server source directory, include paths, SERVER_SOFTWARE compile definition, warning options and app_vsn attribute. Existing list, call and metadata writers are reused. Dependencies and licenses are unchanged.

The generated httpd_connection_sup.beam matches all 2,964 bytes and every chunk of the installed release. The generated file loaded in OTP, started and registered a supervisor under its expected name and raised function_clause for four malformed list inputs. The permanent release comparison uses the existing vendored source and image. [OTP oracle](image-oracle.txt).

Built the corpus runner with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part734-tool`. Its DLL ran with the installed OTP root from artifacts/library376-official-compiler/official-admission.json and output artifacts/beam-backend/otp-corpus-part734.json.

The next candidate is httpd_acceptor_sup. Its release uses the same compilation options. The first instruction differences are in start_link/1, init/1 and child_spec/1.

The corpus checked 1,366 files and reported 29 Equal, 8 Different, 488 SourceRejected and 841 Unsupported. The runner exited with status 1 because differences remain. httpd_connection_sup was the only status change. ELDAPv3 remained Equal. [Corpus report](otp-corpus.json).

Built the harness with `dotnet build tests/Erlang.Tests -m:1 --no-restore -v:q -o artifacts/beam-backend/part734-tests`. All three selected compiler checks passed. The full runtime harness and canonical readiness regeneration remain pending. The readiness scope retains the immutable Part561 comparison report. [Focused report](focused.json).
