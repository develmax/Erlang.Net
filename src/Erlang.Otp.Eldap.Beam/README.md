# Original OTP eldap library

Pinned OTP-29.1.1 eldap 1.3.1: all 2 original BEAM images, original Erlang sources/headers and application resources. Every packaged file matches the admitted official Windows x64 release. Provenance distinguishes pinned Git matches, generated release sources and excluded native producer files. Upstream notices and OTP-LICENSE.txt are retained.

Reference Erlang.Otp.Eldap.Beam as a ProjectReference or PackageReference. OtpEldap.Modules lists module names; ReadImage, ReadApplication and ReadApplicationUpgrade return independent hash-checked bytes. OtpEldap.Beam(module, dependencies) creates an explicit original-image selection. Dependencies/native providers are selected separately. No original VM, producer native DLL or silent C# fallback is included.

Complete packaging does not certify live network I/O, full application boot or complete protocol compatibility. See docs/OTP_LIBRARY_PACKAGING.md for the exact original execution/comparison scope.

The original eldap.erl retains its complete MIT license notice. The generated ELDAPv3.hrl header remains byte-identical and is included in the package beside the ASN.1 schema.
