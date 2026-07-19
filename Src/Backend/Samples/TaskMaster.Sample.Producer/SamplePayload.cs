using TaskMaster.Library.Producer.Attributes;

namespace TaskMaster.Sample.Producer;

[JobType("sample-message", 1)]
public sealed class SamplePayload {
    public string Message { get; set; } = string.Empty;
    public DateTime ProducedAt { get; set; }
    public int SequenceNumber { get; set; } 
    
    public const string SCHEMA = """
    {
      "type": "object",
      "properties": {
        "Message": { "type": "string" },
        "ProducedAt": { "type": "string", "format": "date-time" },
        "SequenceNumber": { "type": "integer" }
      },
      "required": ["Message", "ProducedAt", "SequenceNumber"]
    }
    """;
}
