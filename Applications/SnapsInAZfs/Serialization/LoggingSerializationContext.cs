using System.Text.Json;
using System.Text.Json.Serialization;

namespace SnapsInAZfs.Serialization;

[JsonSerializable ( typeof (string[]) )]
[JsonSerializable(typeof(SiazService.CheckZfsPropertiesSchemaResult))]
[JsonSerializable(typeof(SiazService.TemplateRecursionPair))]
[JsonSourceGenerationOptions ( JsonSerializerDefaults.General, UseStringEnumConverter = true, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
internal sealed partial class LoggingSerializationContext : JsonSerializerContext;
