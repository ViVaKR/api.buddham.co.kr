using Buddham.Core.Entities.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Buddham.Shared.Configurations.Auth;

public class ApplicationUserRoleConfiguration
    : IEntityTypeConfiguration<ApplicationUserRole>, IAuthConfiguration
{
    public void Configure(EntityTypeBuilder<ApplicationUserRole> builder)
    {
        builder.HasOne(ur => ur.User) // ur.User
            .WithMany(u => u.UserRoles)
            .HasForeignKey(ur => ur.UserId)
            .IsRequired() // 추가 됨
            .OnDelete(DeleteBehavior.Cascade); // 추가됨

        builder.HasOne(ur => ur.Role) // ur.Role
            .WithMany(r => r.UserRoles)
            .HasForeignKey(ur => ur.RoleId)
            .IsRequired() // 추가됨 
            .OnDelete(DeleteBehavior.Cascade); // 추가됨

        builder.HasQueryFilter(ur => ur.Role.DeletedAt == null && ur.User
            .DeletedAt == null); // 추가됨

        // 복합 PK 설정을 명시
        // 보통 UserId와 RoleId를 묶어서 PK로 쓰는데, 이 순서가 성능에 영향을 줌
        builder.HasKey(ur => new { ur.UserId, ur.RoleId });

        // 만약 특정 역할(Role)을 가진 사용자 전체를 뽑는 일이 많다면 역방향 인덱스도 필요
        builder.HasIndex(ur => ur.RoleId);
    }
}
