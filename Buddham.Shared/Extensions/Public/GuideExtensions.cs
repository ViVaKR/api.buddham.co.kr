using Buddham.Core.Entities.Public;
using Buddham.Core.Schemas;

namespace Buddham.Shared.Extensions.Public;

public static class GuideExtensions
{
    extension(IQueryable<Guide> query)
    {
        /// <summary>
        /// 목록 프로젝션 (Embedding / SearchVector 등 무거운 컬럼은 읽지 않는다)
        /// </summary>
        public IQueryable<GuideSchema.Response> AsSummary()
        {
            return query.Select(x => new GuideSchema.Response
            {
                Id = x.Id,
                GuideCategory = x.GuideCategory,
                Title = x.Title,
                Content = x.Content,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt,
                UserId = x.UserId
            });
        }
    }

    extension(Guide entity)
    {
        /// <summary>
        /// 단건 변환
        /// </summary>
        public GuideSchema.Response AsDetail()
        {
            return new GuideSchema.Response
            {
                Id = entity.Id,
                GuideCategory = entity.GuideCategory,
                Title = entity.Title,
                Content = entity.Content,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt,
                UserId = entity.UserId
            };
        }

        /// <summary>
        /// 전체 덮어쓰기(Full Replace). 부분 패치가 아니다.
        /// embedding 이 null 이면 기존 Embedding 을 유지한다.
        /// </summary>
        public void ApplyUpdate(GuideSchema.Request request, DateTime now, float[]? embedding = null)
        {
            entity.GuideCategory = request.GuideCategory;
            entity.Title = request.Title.Trim();
            entity.Content = request.Content.Trim();
            entity.UpdatedAt = now;

            if (embedding is not null)
                entity.Embedding = embedding;
        }
    }

    extension(GuideSchema.Request request)
    {
        /// <summary>
        /// 생성용 엔티티 변환. UserId 누락(Guid.Empty 저장)을 막기 위해 필수 인자로 받는다.
        /// </summary>
        public Guide ToEntity(Guid userId, DateTime now)
        {
            if (userId == Guid.Empty)
                throw new ArgumentException("UserId 가 필요합니다.", nameof(userId));

            return new Guide
            {
                UserId = userId,
                GuideCategory = request.GuideCategory,
                Title = request.Title.Trim(),
                Content = request.Content.Trim(),
                CreatedAt = now,
                UpdatedAt = null,
                Embedding = null
            };
        }
    }
}
