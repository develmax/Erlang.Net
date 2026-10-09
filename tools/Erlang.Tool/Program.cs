using System.Diagnostics;
using System.Formats.Tar;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Erlang.Compiler;

string? sourcePath = null, sourceText = null;
try
{
    if (args.Length == 0)
        throw new ArgumentException("Usage: erlang compile <input.erl> <output.cs> | preprocess <input.cs> <output.cs> | inventory <otp-repo> <output-directory>");
    if (args[0] == "inventory")
    {
        if (args.Length != 3)
            throw new ArgumentException("inventory requires the OTP repo and output directory");
        await Inventory.Generate(args[1], args[2]);
        return 0;
    }
    if (args.Length is < 3 or > 4)
        throw new ArgumentException("Expected input and output paths, optionally a nullable context for preprocessing");
    sourcePath = Path.GetFullPath(args[1]);
    sourceText = await File.ReadAllTextAsync(sourcePath);
    string generated = args[0] switch
    {
        "compile" => CodeGeneration.CompileModule(sourceText, sourcePath),
        "preprocess" => CodeGeneration.Preprocess(sourceText, sourcePath, args.Length == 4 ? args[3] : "enable"),
        _ => throw new ArgumentException("Unknown command")
    };
    string outputPath = Path.GetFullPath(args[2]);
    Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
    if (!File.Exists(outputPath) || await File.ReadAllTextAsync(outputPath) != generated)
        await File.WriteAllTextAsync(outputPath, generated, new UTF8Encoding(false));
    else
        File.SetLastWriteTimeUtc(outputPath, DateTime.UtcNow);
    return 0;
}
catch (CompileException ex)
{
    int offset = Math.Clamp(ex.Offset, 0, sourceText?.Length ?? 0);
    int line = 1 + (sourceText?[..offset].Count(c => c == '\n') ?? 0);
    int column = offset - (sourceText?.LastIndexOf('\n', Math.Max(0, offset - 1), offset) ?? -1);
    Console.Error.WriteLine($"{sourcePath}({line},{column}): error {ex.Code}: {ex.Message}");
    return 1;
}
catch (Exception ex) { Console.Error.WriteLine("erlang: error ERL999: " + ex.Message); return 1; }
