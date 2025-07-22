namespace BsonInt64DoubleFix;

using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using static System.Reflection.BindingFlags;
using static System.Reflection.Emit.OpCodes;

public partial class JsonScannerFix
{
  private class Impl
  {
    private static bool IsBadInt64String(string s) => !long.TryParse(s, out _);

    public static JsonToken GetNumberToken()
    {
      var type = JsonTokenType.Int64;
      var lexeme = new string(['d']);
      if (type == JsonTokenType.Double || IsBadInt64String(lexeme))
      {
        var value = JsonConvert.ToDouble(lexeme);
        return new DoubleJsonToken(lexeme, value);
      }
      
      return new Int64JsonToken(lexeme, 0);
    }
    
    public static IEnumerable<CodeInstruction> GetNumberTokenIfx(IEnumerable<CodeInstruction> instructions,
      ILGenerator il)
    {
      var codeMatcher = new CodeMatcher(instructions, il);
      
      var subString = GetSubStringVariable(codeMatcher);
      var int64Label = GetInt64Label(codeMatcher);

      codeMatcher.RemoveInstruction();  //Remove [396] = {CodeInstruction} bne.un.s Label130
      AddIsBadInt64StringCheck(codeMatcher, subString, int64Label, il);
      
      return codeMatcher.InstructionEnumeration();
    }

    private static void AddIsBadInt64StringCheck(CodeMatcher codeMatcher, object subString, Label int64Label, ILGenerator il)
    {
/*
      IL_0014: ldloc.0      // 'type'
      IL_0015: ldc.i4.s     10 // 0x0a  //JsonTokenType.Double
      IL_0017: beq.s        IL_0021     //isNotDoubleLabel
      IL_0019: ldloc.s, 7   // substring
      IL_001a: call         bool BsonInt64DoubleFix.JsonScannerFix/Impl::IsBadInt64String(string)
      IL_001f: br.s         IL_0022     //isBadInt64Label
      IL_0021: ldc.i4.1                 //isNotDoubleLabel
      IL_0022: stloc.2      // V_2      //isBadInt64Label

      IL_0023: ldloc.2      // V_2
      IL_0024: brfalse.s    IL_0039     //int64Label
*/      
      var isNotDoubleLabel = il.DefineLabel();
      var isBadInt64Label = il.DefineLabel();

      codeMatcher.InsertAndAdvance(
        Code(Beq_S, isNotDoubleLabel),
        Code(Ldloc_S, subString),
        Code(Call, typeof(Impl).GetMethod(nameof(IsBadInt64String), Static | NonPublic)),
        Code(Br_S, isBadInt64Label),
        Code(Ldc_I4_1).WithLabels(isNotDoubleLabel),
        Code(Stloc_2).WithLabels(isBadInt64Label),
        Code(Ldloc_2),
        Code(Brfalse_S, int64Label)
      );
    }

    private static CodeInstruction Code(OpCode code, object? operand = null) => new(code, operand);
    
    private static object GetSubStringVariable(CodeMatcher codeMatcher)
    {
/*
[392] = {CodeInstruction} callvirt System.String MongoDB.Bson.IO.JsonBuffer::GetSubstring(System.Int32 start, System.Int32 count)
[393] = {CodeInstruction} stloc.s System.String (7) 
 */      
      var match = codeMatcher.MatchStartForward(new CodeMatch(c => c.opcode == Stloc_S && c.operand is LocalBuilder { LocalIndex: 7 }));//[393]
      return match.Instruction.operand;
    }

    private static Label GetInt64Label(CodeMatcher codeMatcher)
    {
/*
[394] = {CodeInstruction} ldloc.3 NULL
[395] = {CodeInstruction} ldc.i4.s 10
[396] = {CodeInstruction} bne.un.s Label130
 */      
      var match = codeMatcher.MatchStartForward(new CodeMatch(Bne_Un_S)); //[396]
      return (Label)match.Instruction.operand; //Label130
    }
  }
}