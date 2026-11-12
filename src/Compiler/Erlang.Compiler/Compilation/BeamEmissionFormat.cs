namespace Erlang.Compiler;

internal static class BeamEmissionFormat
{
    internal const byte Label = 1;
    internal const byte FunctionInfo = 2;
    internal const byte EndCode = 3;
    internal const byte LocalCall = 4;
    internal const byte TailLocalCallWithFrame = 5;
    internal const byte TailLocalCall = 6;
    internal const byte ExternalCall = 7;
    internal const byte TailExternalCallWithFrame = 8;
    internal const byte BifZero = 9;
    internal const byte BifOne = 10;
    internal const byte BifTwo = 11;
    internal const byte Allocate = 12;
    internal const byte AllocateHeap = 13;
    internal const byte TestHeap = 16;
    internal const byte Deallocate = 18;
    internal const byte Return = 19;
    internal const byte Send = 20;
    internal const byte RemoveMessage = 21;
    internal const byte ReceiveTimeout = 22;
    internal const byte LoopReceive = 23;
    internal const byte LoopReceiveEnd = 24;
    internal const byte Wait = 25;
    internal const byte WaitTimeout = 26;
    internal const byte IsLess = 39;
    internal const byte IsGreaterOrEqual = 40;
    internal const byte NumericEqualTest = 41;
    internal const byte NumericNotEqualTest = 42;
    internal const byte ExactEqualTest = 43;
    internal const byte ExactNotEqualTest = 44;
    internal const byte IsInteger = 45;
    internal const byte IsAtom = 48;
    internal const byte IsFloat = 46;
    internal const byte IsNumber = 47;
    internal const byte IsPid = 49;
    internal const byte IsReference = 50;
    internal const byte IsPort = 51;
    internal const byte IsNil = 52;
    internal const byte IsBinary = 53;
    internal const byte IsList = 55;
    internal const byte IsNonemptyList = 56;
    internal const byte IsTuple = 57;
    internal const byte TestArity = 58;
    internal const byte SelectValue = 59;
    internal const byte SelectTupleArity = 60;
    internal const byte Jump = 61;
    internal const byte CatchEnter = 62;
    internal const byte CatchEnd = 63;
    internal const byte PutList = 69;
    internal const byte BadMatch = 72;
    internal const byte CaseEnd = 74;
    internal const byte CallFun = 75;
    internal const byte IsFunction = 77;
    internal const byte Move = 64;
    internal const byte GetList = 65;
    internal const byte GetTupleElement = 66;
    internal const byte TailExternalCall = 78;
    internal const byte FloatMove = 96;
    internal const byte FloatConvert = 97;
    internal const byte FloatDivide = 101;
    internal const byte TryEnter = 104;
    internal const byte TryEnd = 105;
    internal const byte TryCase = 106;
    internal const byte TryCaseEnd = 107;
    internal const byte Raise = 108;
    internal const byte DynamicApply = 112;
    internal const byte TailDynamicApply = 113;
    internal const byte IsBoolean = 114;
    internal const byte IsFunctionWithArity = 115;
    internal const byte GetBinaryInteger = 117;
    internal const byte GetBinaryFloat = 118;
    internal const byte GetBinarySegment = 119;
    internal const byte GcBifOne = 124;
    internal const byte GcBifTwo = 125;
    internal const byte IsBitString = 129;
    internal const byte MatchBinaryString = 132;
    internal const byte InitializeWritableBinary = 133;
    internal const byte TrimFrame = 136;
    internal const byte GetBinaryUtf8 = 138;
    internal const byte GetBinaryUtf16 = 140;
    internal const byte GetBinaryUtf32 = 142;
    internal const byte Line = 153;
    internal const byte PutMapAssoc = 154;
    internal const byte PutMapExact = 155;
    internal const byte IsMap = 156;
    internal const byte HasMapFields = 157;
    internal const byte GetMapElements = 158;
    internal const byte IsTaggedTuple = 159;
    internal const byte BuildStacktrace = 160;
    internal const byte GetHead = 162;
    internal const byte GetTail = 163;
    internal const byte PutTuple = 164;
    internal const byte GetBinaryTail = 165;
    internal const byte StartBinaryMatch = 166;
    internal const byte GetBinaryPosition = 167;
    internal const byte SetBinaryPosition = 168;
    internal const byte Swap = 169;
    internal const byte StartBinaryMatchKnown = 170;
    internal const byte MakeFun = 171;
    internal const byte InitYRegisters = 172;
    internal const byte CreateBinary = 177;
    internal const byte CallFunTyped = 178;
    internal const byte BadRecord = 180;
    internal const byte UpdateRecord = 181;
    internal const byte BinaryMatch = 182;
    internal const byte GcBifThree = 152;
    internal const byte BifThree = 185;
    internal const byte IsAnyNativeRecord = 186;
    internal const byte OnLoad = 149;
    internal const byte NifStart = 179;
    internal const int MinimumHighestOpcode = 181;
    internal const int CodeHeaderSize = 16;
    internal const int AtomTag = 2;
    internal const int XRegisterTag = 3;
    internal const int FirstReservedXRegister = 1023;
    internal const int YRegisterTag = 4;
    internal const int LabelTag = 5;
    internal const int NilAtomIndex = 0;
    internal const int ConsHeapWords = 2;
    internal const byte ExtendedListOperand = 0x17;
    internal const byte ExtendedFloatRegisterOperand = 0x27;
    internal const int ExtendedTag = 7;
    internal const int AllocationList = 3;
    internal const int AllocationWords = 0;
    internal const int AllocationFloats = 1;
    internal const int AllocationFuns = 2;
    internal const int AllocationKinds = 3;
    internal const byte ExtendedLiteralOperand = 0x47;
    internal const byte ExtendedTypedRegisterOperand = 0x57;
    internal const string LiteralsChunk = "LitT";
    internal const string ClosureChunk = "FunT";
    internal const string CompilerVersion = "10.0.6";
    internal const string DisplayFunction = "display";
    internal const string Unsupported = "The function or module uses a form outside the current BEAM emission profile.";
    internal static string UnsupportedExpression(Expr expression) => expression switch
    {
        Expr.Binary binary => $"Unsupported BEAM binary operator: {binary.Operator}.",
        Expr.Unary unary => $"Unsupported BEAM unary operator: {unary.Operator}.",
        Expr.Call => "Unsupported BEAM dynamic remote call.",
        _ => $"Unsupported BEAM expression: {expression.GetType().Name}."
    };

    internal static string UnsupportedReceivePattern(Pattern pattern) =>
        $"Unsupported BEAM receive pattern: {pattern.GetType().Name}.";

    internal static string UnsupportedReceiveGuard(Expr expression) => expression switch
    {
        Expr.Call call => $"Unsupported BEAM receive guard: {call.Module ?? ErlangModuleNames.Module}:{call.Function}/{call.Arguments.Count}.",
        Expr.Binary binary => $"Unsupported BEAM receive guard: {binary.Operator} ({binary.Left.GetType().Name}, {binary.Right.GetType().Name}).",
        _ => $"Unsupported BEAM receive guard: {expression.GetType().Name}."
    };

    internal static string UnsupportedGuardCall(Expr.Call call) =>
        $"Unsupported BEAM guard call: {call.Module ?? ErlangModuleNames.Module}:{call.Function}/{call.Arguments.Count}.";

    internal static string UnsupportedSourceAttribute(string name) =>
        $"BEAM source mapping does not support the -{name} attribute.";
    internal static string UnsupportedInlineTarget(string name, int arity) =>
        $"Targeted inline lowering is not supported for {name}/{arity}.";
    internal const string ModuleFileMismatch = "BEAM emission requires a source basename that matches the module name.";
}
