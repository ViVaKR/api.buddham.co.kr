namespace Buddham.Shared.Constants;

public class PgTypes
{
    public const string TimestampTz = "timestamptz"; // timestamp with time zone
    public const string Timestamp = "timestamp"; // timestamp without time zone
    public const string Date = "date";
    public const string Time = "time";
    public const string TimeTz = "timetz";
    public const string Interval = "interval";

    // 🔢 숫자
    public const string SmallInt = "smallint"; // int2
    public const string Integer = "integer"; // int4
    public const string BigInt = "bigint"; // int8
    public const string Decimal = "decimal(19, 4)"; // 기본값
    public const string Money = "decimal(19, 4)"; // 금액 전용
    public const string DecimalSmall = "decimal(10, 2)"; // 소규모 금액
    public const string Numeric = "numeric";
    public const string Real = "real"; // float4
    public const string DoublePrecision = "double precision"; // float8

    // 📝 문자
    public const string Text = "text";

    // 🔵 기타
    public const string Bool = "boolean";
    public const string Uuid = "uuid";
    public const string Jsonb = "jsonb";
    public const string Json = "json";
    public const string Bytea = "bytea";

    // Vector
    public const string Vector = "vector";
    public const string TsVector = "tsvector";

    // 🗺️ PostGIS (공간정보)
    public const string Geometry = "geometry";
    public const string Geography = "geography";

    // 원화 전용!
    public const string KRW = "bigint"; // 원화
    public const string Currency = "decimal(19,4)"; // 글로벌 통화
    public const string CurrencyExact = "decimal(19,0)"; // 소수점 없는 통화
    public const string Details = "text";

    // 🔥 특수 길이는 메서드로 조립!
    public static string VarcharOf(int length) => $"varchar({length})";
    public static string DecimalOf(int p, int s) => $"decimal({p},{s})";
    public static string CharOf(int length) => $"char({length})"; 
}
