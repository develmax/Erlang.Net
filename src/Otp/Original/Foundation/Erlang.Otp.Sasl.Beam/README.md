# Original OTP SASL modules for .NET

This project/package contains all 17 original SASL BEAM images from the admitted OTP 29.1.1 Windows x64 release (sasl 4.4), original source/header support, `sasl.app` and `sasl.appup`. The descriptor module list and complete image catalog agree. Every file is bound to the admitted release inventory and pinned source commit.

Reference `Erlang.Otp.Sasl.Beam` as a project or NuGet package. Repository builds use the runtime composition; package production with `UseInstalledPackage=true` depends on `Erlang.Net.CSharp` 0.1.0. Ordinary C# package consumers set `ErlangPreprocessEnabled=false`. Images and application resources are embedded, so deployment requires no original OTP installation.

`OtpSasl.ReadImage(name)`, `ReadApplication()` and `ReadApplicationUpgrade()` verify hashes and return independent bytes. `OtpSasl.Beam(name, dependencies)` requires explicit dependencies and uses the existing startup selection API. Ordinary `ModuleRegistry.PrepareBeam` and atomic publication remain available with an explicit import selection. No silent managed fallback or automatic application startup is added.

Packaging all SASL images does not certify release handling, service control, filesystem effects, every API or complete SASL/application boot. Tests separately inspect all images and execute selected unchanged SASL bodies using the same compiled observer on our runtime and original OTP, retaining whole results/stacks. Existing C# integration and native primitives remain runtime responsibilities. The original OTP license and source notices are retained.
