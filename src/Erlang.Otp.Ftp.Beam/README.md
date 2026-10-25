# Original OTP ftp library

Pinned OTP-29.1.1 ftp 1.2.6: all 6 original BEAM images, original Erlang sources/headers and application resources. Every packaged file matches the admitted official Windows x64 release. Provenance distinguishes pinned Git matches, generated release sources and excluded native producer files. Upstream notices and OTP-LICENSE.txt are retained.

Reference Erlang.Otp.Ftp.Beam as a ProjectReference or PackageReference. OtpFtp.Modules lists module names; ReadImage, ReadApplication and ReadApplicationUpgrade return independent hash-checked bytes. OtpFtp.Beam(module, dependencies) creates an explicit original-image selection. Dependencies/native providers are selected separately. No original VM, producer native DLL or silent C# fallback is included.

Complete packaging does not certify live network I/O, full application boot or complete protocol compatibility. See docs/OTP_LIBRARY_PACKAGING.md for the exact original execution/comparison scope.
