using System.Text;

if (args.Length != 2 || args[0] is not ("--write" or "--check"))
{
    Console.Error.WriteLine(StyleDiagnostics.Usage);

    return 2;
}

string root = Path.GetFullPath(args[1]);
int changed = 0;
foreach (string directory in new[] { "src", "tools", "tests", "benchmarks", "examples" })
{
    foreach (string path in Directory.EnumerateFiles(Path.Combine(root, directory), "*.cs", SearchOption.AllDirectories))
    {
        string relative = Path.GetRelativePath(root, path).Replace('\\', '/');
        if (relative.Split('/').Any(part => part is "bin" or "obj") || relative == "examples/HelloHybrid/Program.cs")
            continue;

        string original = File.ReadAllText(path);
        string formatted = SourceLayout.Apply(original);
        if (original == formatted)
            continue;

        changed++;
        if (args[0] == "--write")
            File.WriteAllText(path, formatted, new UTF8Encoding(false));
        else
            Console.Error.WriteLine(relative);
    }
}

Console.WriteLine($"Source layout: {changed} files {(args[0] == "--write" ? "updated" : "need formatting")}");
return args[0] == "--write" || changed == 0 ? 0 : 1;
