using Erlang;

await HelloHybrid.CSharpFeatures.Verify();
await using var runtime = new ProcessRuntime();
Erlang.Generated.ErlangModule_arithmetic.Register(runtime.Modules);
Erlang.Generated.ErlangModule_map_source.Register(runtime.Modules);
var mapCheck = runtime.Spawn(async erlangProcess =>
{
    var value = await runtime.Modules.Call(erlangProcess, "map_source", "run");
    if (!value.Equals(Term.I(42))) throw new InvalidOperationException("Map module generation failed");
    var picked = await runtime.Modules.Call(erlangProcess, "map_source", "pick", new MapTerm([new(Term.A("value"), Term.Tuple(Term.I(21), Term.I(21)))]));
    if (!picked.Equals(Term.I(21))) throw new InvalidOperationException("Map function pattern generation failed");
    var keysValue = await runtime.Modules.Call(erlangProcess, "map_source", "keys_check");
    if (!keysValue.Equals(Term.Tuple(Term.Tuple(Term.A("value"),Term.Tuple(Term.A("a"),Term.A("found"))),Term.A("true"),new Cons(Term.A("b"),new Cons(Term.A("a"),Term.A("tail")))))) throw new InvalidOperationException(HelloHybrid.ExampleDiagnostics.ListKeys);
    var listValue = await runtime.Modules.Call(erlangProcess, "map_source", "lists_check");
    if (!listValue.Equals(Term.Tuple(Term.A("b"),Term.A("tail"),Term.List(Term.I(5),Term.I(3),Term.I(1))))) throw new InvalidOperationException(HelloHybrid.ExampleDiagnostics.ListModule);
    var stringValue = await runtime.Modules.Call(erlangProcess, "map_source", "string_check");
    if (!stringValue.Equals(new BitString([97,0,98,0,42]))) throw new InvalidOperationException(HelloHybrid.ExampleDiagnostics.StringModule);
    var hybridString = case ok of ok -> <<"AB":16/float>> end.
    if (!hybridString.Equals(new BitString([84,16,84,32]))) throw new InvalidOperationException(HelloHybrid.ExampleDiagnostics.StringHybrid);
    var bitValue = await runtime.Modules.Call(erlangProcess, "map_source", "bits");
    if (!bitValue.Equals(new BitString([52,18,177,128],25))) throw new InvalidOperationException("Bit segment module generation failed");
    var guarded = await runtime.Modules.Call(erlangProcess, "map_source", "bit_guard", bitValue);
    if (!guarded.Equals(Term.A("ok"))) throw new InvalidOperationException("Bit guard module generation failed");
    var hybridBits = case <<1:3,2:5>> of <<A:3,B:5>> -> <<A:3,B:5>> end.
    if (!hybridBits.Equals(new BitString([34]))) throw new InvalidOperationException("Hybrid bit construction failed");
    var packed = case <<3,-257:16/little,5:3,2:2>> of P -> P end.
    var unpacked = await runtime.Modules.Call(erlangProcess, "map_source", "unpack", packed);
    if (!unpacked.Equals(Term.Tuple(Term.I(-257),new BitString([160],3),new BitString([128],2)))) throw new InvalidOperationException("Bit pattern module generation failed");
    var floatCheck = await runtime.Modules.Call(erlangProcess, "map_source", "float_check");
    if (!floatCheck.Equals(Term.Tuple(new FloatTerm(1.5),Term.I(5)))) throw new InvalidOperationException("Float segment module generation failed");
    var floatZero = case <<-0.0:32/float>> of <<F:32/float>> -> F end.
    if (!floatZero.Equals(new FloatTerm(-0.0))) throw new InvalidOperationException("Hybrid float zero changed sign");
    var utfCheck = await runtime.Modules.Call(erlangProcess, "map_source", "utf_check");
    if (!utfCheck.Equals(Term.Tuple(Term.I(128512),Term.I(5)))) throw new InvalidOperationException("UTF segment module generation failed");
    var utfString = case <<"A😀"/utf8,42>> of <<"A😀"/utf8,X>> -> X end.
    if (!utfString.Equals(Term.I(42))) throw new InvalidOperationException("Hybrid UTF string matching failed");
    var ifCheck = if hd(atom) =:= 1; false -> no; true -> X=40, if X > 0 -> X+2 end end.
    if (!ifCheck.Equals(Term.I(42))) throw new InvalidOperationException("Hybrid if generation failed");
    var compiledIf = await runtime.Modules.Call(erlangProcess, "map_source", "if_check");
    if (!compiledIf.Equals(Term.I(42))) throw new InvalidOperationException("Compiled if generation failed");
    var beginCheck = begin X=40,Y=2,{X+Y,bnot 0,13 band 6,8 bor 1,7 bxor 3,5 bsl 3,-5 bsr 1,true and false,true or false,true xor true} end.
    if (!beginCheck.Equals(Term.Tuple(Term.I(42),Term.I(-1),Term.I(4),Term.I(9),Term.I(4),Term.I(40),Term.I(-3),Term.A("false"),Term.A("true"),Term.A("false")))) throw new InvalidOperationException("Hybrid begin/operators failed");
    var catchCheck = catch throw(done).
    if (!catchCheck.Equals(Term.A("done"))) throw new InvalidOperationException("Hybrid catch failed");
    var bindingCheck = begin {X=1,Y=2},T=element(I=1,V={42}),[H=3|Tail=tail],{X,Y,T,I,V,H,Tail} end.
    if (!bindingCheck.Equals(Term.Tuple(Term.I(1),Term.I(2),Term.I(42),Term.I(1),Term.Tuple(Term.I(42)),Term.I(3),Term.A("tail")))) throw new InvalidOperationException("Hybrid expression bindings failed");
    var hybridMap = case #{1 => int, 1.0 => float, value => 41}#{value := 42} of
        #{1 := int, 1.0 := float, value := X} -> X;
        _ -> no
    end.
    if (!hybridMap.Equals(Term.I(42))) throw new InvalidOperationException("Hybrid map generation failed");
    return Term.A("ok");
});
if (!(await mapCheck.Completion).Equals(Term.A("normal"))) throw new InvalidOperationException("Map integration failed");
var helloPrinted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
var process = runtime.Spawn(async erlangProcess =>
{
    while (true)
    {
        var result = receive
            {hello, Name} when is_list(Name) ->
                io:format("Hello ~s~n", [Name]),
                ok;
            {stop} ->
                stop
        end.
        if (result.Equals(Term.A("stop"))) return Term.A("ok");
        helloPrinted.TrySetResult();
    }
});
runtime.Send(process.Pid, Term.Tuple(Term.A("hello"), Term.String("World")));
await helloPrinted.Task.WaitAsync(TimeSpan.FromSeconds(5));
runtime.Send(process.Pid, Term.Tuple(Term.A("stop")));
var reason = await process.Completion.WaitAsync(TimeSpan.FromSeconds(5));
if (!reason.Equals(Term.A("normal"))) throw new InvalidOperationException(reason.ToString());
var arithmetic = runtime.Spawn(async context =>
{
    var value = await runtime.Modules.Call(context, "arithmetic", "double", Term.I(21));
    if (!value.Equals(Term.I(42))) throw new InvalidOperationException("Erlang module compilation failed");
    var zero = await runtime.Modules.Call(context, "arithmetic", "zero");
    if (!zero.Equals(new FloatTerm(-0.0))) throw new InvalidOperationException("Signed zero changed during compilation");
    var sign = await runtime.Modules.Call(context, "arithmetic", "classify", zero);
    if (!sign.Equals(Term.A("negative"))) throw new InvalidOperationException("Signed-zero pattern emission failed");
    return Term.A("ok");
});
if (! (await arithmetic.Completion).Equals(Term.A("normal"))) throw new InvalidOperationException("Module failed");
