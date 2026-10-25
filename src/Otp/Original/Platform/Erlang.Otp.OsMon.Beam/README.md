# Original OTP os_mon library

Pinned OTP-29.1.1 os_mon 2.12: all 8 original application BEAM images, Erlang sources, headers and application resources. Every packaged release file retains its original bytes and notices; provenance records pinned Git matches and generated release inputs.

Reference Erlang.Otp.OsMon.Beam in an ordinary .NET project. OtpOsMon.Modules enumerates the packaged images. ReadImage, ReadApplication and ReadApplicationUpgrade return independently hash-checked copies. OtpOsMon.Beam(module, dependencies) explicitly selects original BEAM execution on our .NET runtime; C# alternatives and native providers are selected separately. No backend fallback or automatic application startup is introduced.

The library contains no original VM executable, producer driver or monitoring executable. Excluded native files and hashes are retained in provenance. No support data is installed automatically. Finite parameter/parser/callback and invalid-argument comparisons are separate from live OS collection, application startup, database access, driver compatibility and complete API contracts.
