# Original OTP public_key library

Pinned OTP-29.1.1 public_key 1.21.7: all 42 original BEAM images, original Erlang sources/headers, ASN.1 inputs and application resources. Every packaged file matches the admitted official Windows x64 release. Provenance distinguishes pinned Git matches, generated release sources and excluded native producer files. Upstream notices and OTP-LICENSE.txt are retained.

Reference Erlang.Otp.PublicKey.Beam as a ProjectReference or PackageReference. OtpPublicKey.Modules lists module names; ReadImage, ReadApplication and ReadApplicationUpgrade return independent hash-checked bytes. OtpPublicKey.Beam(module, dependencies) creates an explicit original-image selection. Dependencies/native providers are selected separately. No original VM, producer native DLL or silent C# fallback is included.

Complete packaging does not certify native cryptographic operations, full application boot, ASN.1 codecs, certificate validation or TLS. See docs/OTP_LIBRARY_PACKAGING.md for the exact original execution/comparison scope.
