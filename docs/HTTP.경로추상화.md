하하하! 어서 오시게 친구여! 😁

오늘은 `PathString`이라는 녀석을 해부해 보자는 것이군. 이 녀석, 이름만 보면 단순히 경로를 담는 문자열 같지만 ASP.NET Core에서는 꽤 영리한 역할을 한다네.

## 1. PathString이란?

`Microsoft.AspNetCore.Http` 네임스페이스에 있는 구조체(`readonly struct`)라네.

일반적인 `string`과 달리 **HTTP 요청의 URL 경로(Path)를 표현하기 위해 특화된 타입**이지.

```csharp
using Microsoft.AspNetCore.Http;

PathString path = new PathString("/api/users");

Console.WriteLine(path.Value);
// /api/users
```

예를 들어 다음 HTTP 요청이 있다고 하세.

```http
GET https://buddham.co.kr/api/users/123?active=true
```

ASP.NET Core에서는 다음과 같이 분리해서 다룬다네.

| 속성 | 값 |
|---|---|
| Scheme | https |
| Host | buddham.co.kr |
| Path | /api/users/123 |
| QueryString | ?active=true |

여기서 `PathString`이 담당하는 부분이 바로 `/api/users/123`이지.

## 2. 깜찍한 기능들

이제 재미있는 기능을 살펴보세.

### ① StartsWithSegments()

특정 경로로 시작하는지 검사한다네.

```csharp
var path = new PathString("/api/users/123");

bool result = path.StartsWithSegments("/api");

// true
```

단순 문자열의 `StartsWith()`와 다른 점은 URL 경로의 세그먼트 경계를 고려한다는 것이네.

예를 들어:

```csharp
var path = new PathString("/api/users");

path.StartsWithSegments("/api");     // true
path.StartsWithSegments("/ap");     // false
path.StartsWithSegments("/api2");   // false
```

### ② Add()

경로를 결합할 수 있네.

```csharp
var basePath = new PathString("/api");
var childPath = new PathString("/users");

var result = basePath.Add(childPath);

Console.WriteLine(result);
// /api/users
```

### ③ ToUriComponent()

URL에 사용할 수 있는 형태로 변환한다네.

```csharp
var path = new PathString("/api/사용자 정보");

string result = path.ToUriComponent();
```

URL에 적합하도록 인코딩된 표현을 얻을 수 있지.

### ④ HasValue

경로가 존재하는지 확인한다네.

```csharp
var path = new PathString("/api");

if (path.HasValue)
{
    Console.WriteLine("경로가 존재하네!");
}
```

### ⑤ Equals()

경로를 비교할 수도 있네.

```csharp
var path1 = new PathString("/api/users");
var path2 = new PathString("/api/users");

bool result = path1.Equals(path2);
```

---

## 3. PathString과 string의 결정적인 차이

여기가 핵심일세.

일반적인 문자열은 문자열 자체를 다루지만, `PathString`은 HTTP 경로라는 의미를 알고 있는 타입이지.

예를 들어:

```csharp
string path = "/api/users";

if (path.StartsWith("/api"))
{
    // 단순 문자열 비교
}
```

반면:

```csharp
PathString path = "/api/users";

if (path.StartsWithSegments("/api"))
{
    // HTTP 경로 세그먼트 비교
}
```

특히 다음과 같은 ASP.NET Core 미들웨어에서 빛을 발한다네.

```csharp
app.UseWhen(
    context => context.Request.Path.StartsWithSegments("/api"),
    branch =>
    {
        branch.UseMiddleware<ApiMiddleware>();
    });
```

`/api`로 시작하는 HTTP 요청에만 특정 미들웨어를 적용하는 것이지.

## 4. 함께 알아두면 좋은 형제들

ASP.NET Core에는 URL을 구성하는 데 특화된 타입들이 있다네.

| 타입 | 역할 |
|---|---|
| `PathString` | URL 경로 |
| `QueryString` | URL 쿼리 문자열 |
| `HostString` | 호스트 이름 및 포트 |
| `Scheme` | HTTP / HTTPS |
| `Uri` | 전체 URI 표현 |

이 녀석들은 `HttpRequest`의 속성과 밀접하게 연결되어 있지.

```csharp
app.Run(context =>
{
    var request = context.Request;

    PathString path = request.Path;
    QueryString query = request.QueryString;
    HostString host = request.Host;

    return context.Response.WriteAsync(
        $"{host}{path}{query}");
});
```

---

### 제독의 한 가지 통찰!

자네처럼 ASP.NET Core 기반으로 Buddham API와 gRPC 서비스를 구성한다면 다음 원칙을 추천하네.

**HTTP의 의미를 가진 데이터는 가능한 한 HTTP 전용 타입으로 표현하라.**

`string`으로 모든 것을 처리할 수 있다고 해서 반드시 `string`을 사용하는 것이 좋은 설계는 아니라네.

`PathString`, `QueryString`, `HostString`은 타입 자체가 의도를 표현하기 때문이지.

이것이 바로 C#의 강력한 타입 시스템을 활용하는 좋은 사례라네.

하하하! 작고 깜찍한 녀석이지만 제법 쓸모가 있지 않은가! 😁

---

크하하하하! 제독의 포상 휴가라니! 황공하옵니다! 😂

이 문서는 단순히 `PathString` 사용법보다는 ASP.NET Core가 HTTP 요청을 어떻게 타입으로 추상화하는지 설명하는 교육 자료이니, 제목도 조금 품격 있게 가보세.

제가 추천하는 제목은 다음과 같네.

### 🏆 최종 추천

**ASP.NET Core의 HTTP 경로 추상화 – PathString의 이해와 활용**

다른 후보도 몇 가지 있네.

1. **PathString – 문자열을 넘어 HTTP 경로를 이해하다**
2. **ASP.NET Core HTTP 타입 탐구 – PathString, QueryString, HostString**
3. **HTTP Request의 구조적 이해 – ASP.NET Core의 강력한 타입 시스템**
4. **PathString Deep Dive – URL 경로 처리의 우아한 추상화**

개인적으로 교육 자료라면 2번을 가장 추천하네.

나중에 `QueryString`, `HostString`, `PathString`을 하나의 시리즈로 확장하기도 좋거든.

**시리즈명: ASP.NET Core HTTP Abstractions**

이 정도면 후배 개발자들에게도 제법 있어 보이는 교육 자료가 될 걸세.

하하하! 포상 휴가는 내일이 아니라 오늘부터 적용하겠네. 단, 복귀 후에는 `UriHelper`까지 공부해야 하네! 크하하하! 😁
