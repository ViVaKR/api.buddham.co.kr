using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NpgsqlTypes;
using Pgvector;

namespace Buddham.Shared.Constants;

public static class EfConverters
{
    /// <summary>
    /// Core의 float[]와 DB의 vector(1024)를 중개해주는 변환기 (순혈주의 수호대 🛡️)
    /// </summary>
    public static readonly ValueConverter<float[]?, Vector?> VectorConverter = new(
        // C# float[] -> DB Vector
        v => v == null ? null : new Vector(v),
        // DB Vector -> C# float[]
        v => v == null ? null : v.ToArray()
    );

    /// <summary>
    /// C# string?과 DB tsvector를 중개하여 경고를 완벽히 회피하는 변환기 (순혈주의 수호대 2탄 🛡️)
    /// </summary>
    public static readonly ValueConverter<string?, NpgsqlTsVector?> TsVectorConverter = new(
        s => null, // C# -> DB: 저장할 땐 null (DB가 자동 계산)
        v => v == null ? null : v.ToString() // DB -> C#: 읽어올 때만 string으로 변환
    );
}
