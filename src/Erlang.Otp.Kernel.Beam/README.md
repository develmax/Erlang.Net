# Original OTP kernel for .NET

This project/package owns all104 original kernel BEAM images and the unchanged kernel.app/kernel.appup resources from the admitted official OTP29.1.1 Windows x64 release (kernel11.0.4). The package also retains the original source/include directories and a SHA256 inventory tied to the admitted archive and source commit. Existing C# alternatives remain in Erlang.Otp.Kernel.

Reference Erlang.Otp.Kernel.Beam as a project or NuGet package. Repository builds reference the runtime composition; packing with UseInstalledPackage=true depends on Erlang.Net.CSharp0.1.0. Set ErlangPreprocessEnabled=false in ordinary C# package consumers. The original images/application resources are embedded, so deployment needs no OTP installation or repository path.

```csharp
byte[] image = OtpKernel.ReadImage("net_kernel");
byte[] application = OtpKernel.ReadApplication();
byte[] upgrade = OtpKernel.ReadApplicationUpgrade();
```

Use these images with the existing ordinary ModuleRegistry.PrepareBeam/atomic publication route and an explicit BeamDependencySelection. OtpKernel.Beam(module,dependencies) creates an explicit startup selection; it does not choose dependency backends or fabricate the missing full startup closure. Native bindings, on_load order, native drivers and process/application boot retain their existing runtime configuration. The strict startup API currently supports only its established subset of C# alternatives; kernel C# alternatives retain their existing registration/composition API rather than gaining a new implicit switch here.

The producer platform is part of provenance. Shipping104 images is packaging completeness, not104 fully compatible modules, cross-platform certification, complete application boot or a performance claim. Tests separately identify inspection, actual execution and cold deployment scopes. No handwritten replacement of pure original Erlang bodies is introduced.
