using Buddham.Core.Entities.Public;
using Buddham.Core.Schemas;
using Pgvector;

namespace Buddham.Shared.Extensions.Public;

public static class GuideExtensions
{
    extension(IQueryable<Guide> query)
    {
        /// <summary>
        /// 목록
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
        /// 단건 조회
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
                UserId = entity.UserId,
            };
        }

        public void ApplyPatch(GuideSchema.Request request, float[]? embedding = null)
        {
            entity.GuideCategory = request.GuideCategory;
            entity.Title = request.Title;
            entity.Content = request.Content;
            entity.UpdatedAt = DateTime.UtcNow;
            // 🆕 임베딩이 새로 생성된 경우에만 업데이트
            // null 이면 기존 Embedding 그대로 유지
            if (embedding != null)
                entity.Embedding = embedding;
        }
    }

    extension(GuideSchema.Request request)
    {
        public Guide ToEntity()
        {
            return new Guide
            {
                GuideCategory = request.GuideCategory,
                Title = request.Title,
                Content = request.Content,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = null,
                Embedding = null
            };
        }
    }
}
