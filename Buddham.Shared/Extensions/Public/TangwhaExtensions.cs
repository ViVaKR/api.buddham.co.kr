using Buddham.Core.Schemas;
using Buddham.Core.Entities.Public;
using System.Data;

namespace Buddham.Shared.Extensions.Public;

public static class TangwhaExtensions
{
    extension(IQueryable<Tangwha> query)
    {
        /// <summary>
        /// 목록
        /// </summary>
        public IQueryable<TangwhaSchema.Response> AsSummary()
        {
            return query.Select(x => new TangwhaSchema.Response
            {
                Id = x.Id,
                Title = x.Title,
                ChineseTitle = x.ChineseTitle,
                Category = x.Category,
                Tier = x.Tier,
                Era = x.Era,
                Temple = x.Temple,
                Description = x.Description,
                ThumbnailUrl = x.ThumbnailUrl,
                ImageUrl = x.ImageUrl,
                UserId = x.UserId,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            });
        }
    }

    extension(Tangwha entity)
    {
        /// <summary>
        /// 단건조회
        /// </summary>
        public TangwhaSchema.Response AsDetail()
        {
            return new TangwhaSchema.Response
            {
                Id = entity.Id,
                Title = entity.Title,
                ChineseTitle = entity.ChineseTitle,
                Category = entity.Category,
                Tier = entity.Tier,
                Era = entity.Era,
                Temple = entity.Temple,
                Description = entity.Description,
                ThumbnailUrl = entity.ThumbnailUrl,
                ImageUrl = entity.ImageUrl,
                UserId = entity.UserId,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt
            };
        }

        /// <summary>
        /// 수정
        /// </summary>
        /// <param name="request"></param>
        /// <param name="embedding"></param>
        public void ApplyPatch(TangwhaSchema.Request request, float[]? embedding = null)
        {
            entity.Title = request.Title;
            entity.ChineseTitle = request.ChineseTitle;
            entity.Category = request.Category;
            entity.Tier = request.Tier;
            entity.Era = request.Era;
            entity.Temple = request.Temple;
            entity.Description = request.Description;
            entity.ThumbnailUrl = request.ThumbnailUrl;
            entity.ImageUrl = request.ImageUrl;
            entity.UpdatedAt = DateTime.UtcNow;
            if (embedding != null)
                entity.Embedding = embedding;
        }
    }

    extension(TangwhaSchema.Request request)
    {
        public Tangwha ToEntity()
        {
            return new Tangwha
            {
                Title = request.Title,
                ChineseTitle = request.ChineseTitle,
                Category = request.Category,
                Tier = request.Tier,
                Era = request.Era,
                Temple = request.Temple,
                Description = request.Description,
                ThumbnailUrl = request.ThumbnailUrl,
                ImageUrl = request.ImageUrl,
                UpdatedAt = DateTime.UtcNow,
                Embedding = null
            };
        }
    }


}
