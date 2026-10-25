using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Buddham.Core.Common.Options;

public class AppJsonOptions
{
    public static readonly JsonSerializerOptions Default = Configure(new JsonSerializerOptions());

    /// <summary>
    /// JsonSerializerOptions
    /// var json = JsonSerializer.Serialize(dto, AppJsonOptions.Default);
    /// var obj  = JsonSerializer.Deserialize&lt;HelpResponse&gt;(json, AppJsonOptions.Default);
    /// </summary>
    public static JsonSerializerOptions Configure(JsonSerializerOptions options)
    {
        options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.PropertyNameCaseInsensitive = true;
        options.WriteIndented = false;
        options.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.NumberHandling = JsonNumberHandling.AllowReadingFromString;
        // Enum 가독성 및 확장성 확보
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        // 한글/경전 특수문자 로그 및 캐시 가독성 확보
        options.Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
        return options;
    }
}
