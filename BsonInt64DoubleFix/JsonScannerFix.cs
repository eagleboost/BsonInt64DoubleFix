namespace BsonInt64DoubleFix;

using HarmonyLib;
using MongoDB.Bson.Serialization;
using static System.Reflection.BindingFlags;

public partial class JsonScannerFix
{
  public static void Initialize()
  {
    var harmony = new Harmony(typeof(JsonScannerFix).FullName!);
    var assembly = typeof(BsonSerializer).Assembly;
    var jsonScannerType = assembly.GetType("MongoDB.Bson.IO.JsonScanner");
    var getNumberToken = jsonScannerType!.GetMethod("GetNumberToken", Static | NonPublic);
    var getNumberTokenFix = typeof(Impl).GetMethod(nameof(Impl.GetNumberTokenIfx), Static | Public);
    harmony.Patch(getNumberToken, transpiler: new HarmonyMethod(getNumberTokenFix));
  }
}