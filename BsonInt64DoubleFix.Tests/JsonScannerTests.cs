namespace BsonInt64DoubleFix.Tests;

using MongoDB.Bson.Serialization;

public class JsonScannerTests
{
  [Test]
  public void Test_01_Exception()
  {
    const string json = """
                        {
                          "Size" : 12345678901234567890,
                        }
                        """;
    Assert.Throws<OverflowException>(() => BsonSerializer.Deserialize<Model>(json));
    
    JsonScannerFix.Initialize();
    var model = BsonSerializer.Deserialize<Model>(json);
    Assert.That(model.Size, Is.EqualTo(12345678901234567890d));;
  }
  
  [Test]
  public void Test_02_Normal()
  {
    const string json = """
                        {
                          "Size" : 123.456,
                        }
                        """;
    var model = BsonSerializer.Deserialize<Model>(json);
    Assert.That(model.Size, Is.EqualTo(123.456));;
  }
  
  private class Model
  {
    public double Size { get; set; }
  }
}