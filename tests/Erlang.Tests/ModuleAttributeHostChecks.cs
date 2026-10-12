using Erlang.Compiler;

namespace Erlang.Tests;

public static class ModuleAttributeHostChecks
{
    public static async Task Run()
    {
        foreach (string attribute in new[]
        {
            "-custom(put(mark,changed)).", "-custom(X).", "-custom(1+2).",
            "-custom(#{run/0=>value}).", "-custom(#{a:=1}).", "-custom(<<(1+2)>>).",
            "-custom((X=1)).", "-custom(abs(-2))."
        })
        {
            bool rejected = false;
            try
            {
                new Parser("-module(attribute_host). " + attribute).ParseModule();
            }
            catch (CompileException exception) when (exception.Code == ModuleAttributeHostNames.SyntaxCode)
            {
                rejected = true;
            }
            if (!rejected)
                throw new InvalidOperationException(ModuleAttributeHostNames.Broken);
        }
        foreach (string attribute in new[]
        {
            "-on_load(run/0).", "-import(other,[run/0]).", "-compile(export_all).", "-nifs([run/0])."
        })
        {
            bool rejected = false;
            try
            {
                new Parser("-module(attribute_host). " + attribute).ParseModule();
            }
            catch (CompileException exception) when (exception.Code == ModuleAttributeHostNames.UnsupportedCode)
            {
                rejected = true;
            }
            if (!rejected)
                throw new InvalidOperationException(ModuleAttributeHostNames.Broken);
        }
        var first = new Parser("-module(attribute_host). -export([run/0]). -custom(first). run()->fun()->kept end.").ParseModule();
        var second = new Parser("-module(attribute_host). -export([run/0]). -custom(second). run()->fun()->kept end.").ParseModule();
        var left = new ModuleRegistry();
        var right = new ModuleRegistry();
        first.Register(left);
        second.Register(right);
        if (!left.ModuleInformation(ModuleAttributeHostNames.Module, ModuleAttributeHostNames.Md5).Equals(right.ModuleInformation(ModuleAttributeHostNames.Module, ModuleAttributeHostNames.Md5)) ||
            left.ModuleInformation(ModuleAttributeHostNames.Module, ModuleAttributeHostNames.Attributes).Equals(right.ModuleInformation(ModuleAttributeHostNames.Module, ModuleAttributeHostNames.Attributes)))
            throw new InvalidOperationException(ModuleAttributeHostNames.Broken);
        if (CodeGeneration.CompileModule("-module(attribute_host). -custom(first).", "attribute.erl") == CodeGeneration.CompileModule("-module(attribute_host). -custom(second).", "attribute.erl"))
            throw new InvalidOperationException(ModuleAttributeHostNames.Broken);

        await using var runtime = new ProcessRuntime();
        Exception? failure = null;
        var process = runtime.Spawn(async context =>
        {
            try
            {
                var leftFun = (FunctionTerm)await Execution.InvokeAsync(
                    first,
                    ModuleAttributeHostNames.RunFunction,
                    context,
                    []
                );
                var rightFun = (FunctionTerm)await Execution.InvokeAsync(
                    second,
                    ModuleAttributeHostNames.RunFunction,
                    context,
                    []
                );
                if (leftFun.LocalIdentity is null || rightFun.LocalIdentity is null || !leftFun.LocalIdentity.Unique.Equals(rightFun.LocalIdentity.Unique) || !leftFun.Equals(rightFun))
                    throw new InvalidOperationException(ModuleAttributeHostNames.Broken);
            }
            catch (Exception exception)
            {
                failure = exception;
                throw;
            }

            return Nil.Value;
        });
        await process.Completion.WaitAsync(ModuleAttributeHostParameters.Deadline);
        if (failure is not null)
            throw failure;
    }
}
