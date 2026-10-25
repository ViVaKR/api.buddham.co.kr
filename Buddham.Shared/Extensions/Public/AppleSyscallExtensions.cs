using Buddham.Core.Entities.Public;
using Buddham.Core.Schemas;

namespace Buddham.Shared.Extensions.Public;

public static class AppleSyscallExtensions
{
    extension(IQueryable<AppleSyscall> query)
    {
        /// <summary>
        /// 목록
        /// </summary>
        public IQueryable<AppleSyscallSchema.Response> AsSummary()
        {
            return query.Select(x => new AppleSyscallSchema.Response
            {
                Id = x.Id,
                SyscallNumber = x.SyscallNumber,
                ReturnType = x.ReturnType,
                FuncName = x.FuncName,
                Arguments = x.Arguments,
                ExampleAsm = x.ExampleAsm,
                ExampleC = x.ExampleC,
                Description = x.Description,
            });
        }
    }

    extension(AppleSyscall entity)
    {
        /// <summary>
        /// 수정
        /// </summary>
        public void ApplyPatch(AppleSyscallSchema.Request request)
        {
            entity.ExampleAsm = request.ExampleAsm;
            entity.ExampleC = request.ExampleC;
            entity.Description = request.Description;
            entity.UpdatedAt = DateTime.UtcNow;
        }
    }
}