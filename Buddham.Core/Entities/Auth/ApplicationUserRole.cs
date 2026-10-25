using Microsoft.AspNetCore.Identity;

namespace Buddham.Core.Entities.Auth;

public class ApplicationUserRole: IdentityUserRole<Guid>
{
    public virtual ApplicationUser User { get; set; } = null!;
    public virtual ApplicationRole Role { get; set; } = null!; 
}

/*
 [ 차이점 ]
 1. null! - EF Core 가 채워줄게 (런타임 보장) 컴파일러만 속이는 것!
 2. ? - 진짜 null 가능
 3. required - 객체 생성할때 반드시 넣어야 함 
    new ApplicationUserRole() -> 컴파일 에러 
    new ApplicationUserRole { User = user } -> OK
 */
