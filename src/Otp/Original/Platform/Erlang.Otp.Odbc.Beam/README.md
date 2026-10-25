# Original OTP odbc library

Pinned OTP-29.1.1 odbc 2.17.1: all 3 original application BEAM images, Erlang sources, headers and application resources. Every packaged release file retains its original bytes and notices; provenance records pinned Git matches and generated release inputs.

Reference Erlang.Otp.Odbc.Beam in an ordinary .NET project. OtpOdbc.Modules enumerates the packaged images. ReadImage, ReadApplication and ReadApplicationUpgrade return independently hash-checked copies. OtpOdbc.Beam(module, dependencies) explicitly selects original BEAM execution on our .NET runtime; C# alternatives and native providers are selected separately. No backend fallback or automatic application startup is introduced.

The library contains no original VM executable, producer driver or monitoring executable. Excluded native files and hashes are retained in provenance. No support data is installed automatically. Finite parameter/parser/callback and invalid-argument comparisons are separate from live OS collection, application startup, database access, driver compatibility and complete API contracts.
