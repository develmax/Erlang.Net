# Original OTP ssl library

Pinned OTP-29.1.1 ssl 11.7.7: all 78 original BEAM images, original Erlang sources/headers and application resources. Every packaged file matches the admitted official Windows x64 release. Provenance distinguishes pinned Git matches, generated release sources and excluded native producer files. Upstream notices and OTP-LICENSE.txt are retained.

Reference Erlang.Otp.Ssl.Beam as a ProjectReference or PackageReference. OtpSsl.Modules lists module names; ReadImage, ReadApplication and ReadApplicationUpgrade return independent hash-checked bytes. OtpSsl.Beam(module, dependencies) creates an explicit original-image selection. Dependencies/native providers are selected separately. No original VM, producer native DLL or silent C# fallback is included.

Complete packaging does not certify native cryptographic operations, full application boot, certificate validation, TLS handshakes or SSH sessions/channels. See docs/OTP_LIBRARY_PACKAGING.md for the exact original execution/comparison scope.
