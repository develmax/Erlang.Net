using System.Text.Json;
using Erlang;
if(args.Length!=1)throw new ArgumentException();
var input=JsonSerializer.Deserialize<DriverParentHarnessInput423>(File.ReadAllText(args[0]),DriverParentHarnessJson423.Options)!;
DriverParentHarnessAccess423.VerifyInputs(input);
Directory.CreateDirectory(input.OutputDirectory);object? report=null;Exception? failure=null;
try {report=await DdllBoundary443Checks.Run(input);}catch(Exception error){failure=error;}
File.WriteAllText(Path.Combine(input.OutputDirectory,"completion.json"),JsonSerializer.Serialize(new{Passed=failure is null,Report=report,Failure=failure?.ToString(),GenuineNativeBoundaryControls=true},DriverParentHarnessJson423.Options));
DriverParentHarnessAccess423.VerifyInputs(input);
if(failure is not null)throw failure;
