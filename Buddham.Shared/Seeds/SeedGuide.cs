using Buddham.Core.Entities.Public;
using Buddham.Core.EnumTypes;
using Buddham.Core.Interfaces.Ollama;
using Buddham.Shared.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Buddham.Shared.Seeds;

public static class SeedGuide
{
    // 관리자 계정 실제 아이디
    private static readonly Guid AdminUserId = Guid.Parse("01a19457-1cd4-705f-a5bf-11a2ea4749b4");

    public static async Task SeedGuideASync(this WebApplication app, CancellationToken ct = default)
    {
        using var scope = app.Services.CreateScope();
        var sp = scope.ServiceProvider;
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<WebApplication>>();
        var db = scope.ServiceProvider.GetRequiredService<PublicDbContext>();
        var ollama = sp.GetRequiredService<IOllamaService>();   // ✅ 인터페이스로 요청
        var seeds = BuildSeeds();

        int added = 0, updated = 0, embedded = 0, embedFailed = 0;
        var ollamaAvailable = true;

        // 제목 기준 upsert (제목 중복이 있어도 첫 행만 사용)
        var existing = (await db.Guides.ToListAsync(ct))
            .GroupBy(g => g.Title)
            .ToDictionary(g => g.Key, g => g.First());

        foreach (var seed in seeds)
        {
            Guide guide;
            if (existing.TryGetValue(seed.Title, out var found))
            {
                guide = found;
                if (guide.Content != seed.Content)
                {
                    guide.Content = seed.Content;
                    guide.Embedding = null;          // 내용이 바뀌었으니 재생성 대상
                    updated++;
                }
            }
            else
            {
                guide = seed;
                db.Guides.Add(guide);
                added++;
            }

            // 임베딩이 없는 행만 생성 (Ollama 장애 시 앱 시작은 막지 않음)
            if (guide.Embedding is null && ollamaAvailable)
            {
                try
                {
                    var source = $"[가이드] {guide.Title}\n{guide.Content}";
                    guide.Embedding = await ollama.GetEmbeddingAsync(source, ct);
                    embedded++;
                }
                catch (Exception ex)
                {
                    ollamaAvailable = false;         // 연속 타임아웃 방지
                    embedFailed++;
                    logger.LogWarning("[Guide-AI] 벡터 생성 실패, 나머지 임베딩은 다음 시작 때 채웁니다: {Message}", ex.Message);
                }
            }
        }
        await db.SaveChangesAsync(ct);

        logger.LogInformation(
          "Guide 시드 완료: 추가 {Added}, 갱신 {Updated}, 임베딩 {Embedded}, 임베딩 실패 {Failed}",
          added, updated, embedded, embedFailed);
    }

    private static List<Guide> BuildSeeds() =>
    [
         // ── 1. 시작하기 ─────────────────────────────
            Create("처음 오셨나요? 둘러 보기",
                """
                부다함에 오신 것을 환영합니다.
                경전과 불교 용어를 찾아 읽고, AI 도우미에게 질문하며, 다른 수행자들과 댓글로 소통할 수 있는 공간입니다.
                먼저 상단 메뉴를 차례로 눌러 보시고, 궁금한 점은 이 가이드에서 찾아보세요.
                """),
            Create("가명(Pseudonym)과 아바타 설정하기",
                """
                부다함에서는 실명 대신 가명으로 활동합니다.
                내 정보 화면에서 가명과 아바타를 정하면 댓글과 채팅에 그대로 표시됩니다.
                가명은 다른 사용자가 부르는 이름이 되니, 부담 없고 기억하기 쉬운 이름을 권해 드립니다.
                """),
            Create("로그인과 계정 관리",
                """
                로그인하면 댓글 작성, 리액션, 채팅 등 모든 기능을 이용할 수 있습니다.
                둘러보기만 하실 때는 로그인 없이도 경전과 용어를 볼 수 있습니다.
                비밀번호 변경이나 로그아웃은 내 정보 메뉴에서 하실 수 있습니다.
                """),
            Create("화면 구성과 메뉴 한눈에 보기",
                """
                상단에는 주요 메뉴, 본문 영역에는 선택한 경전이나 용어, 한쪽에는 AI 도우미와 채팅이 배치되어 있습니다.
                모바일에서는 메뉴가 접혀 있으니 왼쪽 위 버튼으로 펼쳐서 이용하세요.
                """),

            // ── 2. 경전·용어 보기 ───────────────────────
            Create("경전 찾아 읽기",
                """
                경전 메뉴에서 분류를 따라 들어가면 원하는 경전을 찾을 수 있습니다.
                목록에서 제목을 누르면 본문이 열리고, 긴 경전은 장·품 단위로 나뉘어 있어 이어 읽기 편합니다.
                제목을 정확히 모를 때는 검색 기능을 함께 활용해 보세요.
                """),
            Create("불교 용어 사전 활용하기",
                """
                낯선 용어는 불교 용어 사전에서 바로 찾아볼 수 있습니다.
                한자, 산스크리트·팔리어 표기와 쉬운 풀이를 함께 확인하세요.
                본문을 읽다 모르는 단어가 나오면 사전에서 검색하는 것을 습관으로 들이면 이해가 훨씬 빨라집니다.
                """),
            Create("원문과 번역 함께 보기",
                """
                일부 경전은 원문과 번역을 나란히 볼 수 있습니다.
                번역만 읽다가 뜻이 애매할 때 원문을 대조해 보면 표현의 뉘앙스를 파악하는 데 도움이 됩니다.
                화면 설정에서 원문 표시를 켜고 끌 수 있습니다.
                """),
            Create("읽던 곳 이어서 보기",
                """
                로그인한 상태에서 읽은 경전은 기록되어, 다음에 다시 들어왔을 때 이어서 읽을 수 있습니다.
                여러 경전을 번갈아 읽어도 각각의 진행 위치가 따로 보관됩니다.
                """),

            // ── 3. 검색 ─────────────────────────────────
            Create("키워드 검색 잘하는 법",
                """
                검색창에 찾고 싶은 단어를 입력하면 제목과 본문에서 일치하는 내용을 찾아 줍니다.
                너무 긴 문장보다는 핵심 단어 한두 개가 더 정확한 결과를 줍니다.
                결과가 너무 많으면 단어를 하나 더 추가해 범위를 좁혀 보세요.
                """),
            Create("의미 기반(AI) 검색이란?",
                """
                의미 기반 검색은 단어가 똑같지 않아도 뜻이 비슷한 내용을 찾아 주는 방식입니다.
                예를 들어 "마음이 불안할 때"라고 입력하면 불안, 번뇌, 평온과 관련된 내용이 함께 검색됩니다.
                정확한 용어를 모를 때 특히 유용합니다.
                """),
            Create("검색 결과 좁히기와 정렬하기",
                """
                검색 결과는 분류별로 필터링하고, 관련도순이나 최신순으로 정렬할 수 있습니다.
                원하는 결과가 나오지 않으면 필터를 해제하고 다시 검색해 보세요.
                """),

            // ── 4. AI 도우미 ────────────────────────────
            Create("AI에게 질문하기",
                """
                AI 도우미는 경전과 불교 용어에 관한 궁금증을 대화 형식으로 풀어 드립니다.
                입력창에 평소 말하듯 질문을 적고 전송하면 됩니다.
                이어서 꼬리 질문을 하면 앞선 대화를 바탕으로 더 깊이 답해 줍니다.
                """),
            Create("좋은 질문을 만드는 요령",
                """
                질문이 구체적일수록 답변이 좋아집니다.
                "공이 뭐예요?"보다 "반야심경에서 말하는 공은 허무와 어떻게 다른가요?"처럼 맥락을 함께 적어 보세요.
                한 번에 하나의 주제를 묻고, 부족하면 이어서 질문하는 방식을 권합니다.
                """),
            Create("AI 답변을 읽을 때 유의할 점",
                """
                AI 답변은 학습을 돕는 참고 자료이며 항상 정확하다고 보장할 수 없습니다.
                중요한 내용은 반드시 경전 본문이나 스승의 가르침으로 다시 확인해 주세요.
                잘못된 내용을 발견하면 댓글이나 문의로 알려 주시면 개선에 큰 도움이 됩니다.
                """),

            // ── 5. 소통 ─────────────────────────────────
            Create("댓글과 대댓글 쓰기",
                """
                글 하단의 입력창에서 댓글을 작성하고, 다른 분의 댓글에는 답글 버튼으로 대댓글을 남길 수 있습니다.
                대댓글은 원래 댓글 아래에 묶여 대화의 흐름을 따라 읽기 쉽습니다.
                """),
            Create("합장·연꽃·공덕, 세 가지 리액션 알아보기",
                """
                댓글에는 합장, 연꽃, 공덕 세 가지 마음을 표현할 수 있습니다.
                한 댓글에는 하나의 리액션만 남길 수 있고, 같은 것을 다시 누르면 취소되며, 다른 것을 누르면 바뀝니다.
                짧은 한마디가 어려울 때 마음을 전하는 간편한 방법입니다.
                """),
            Create("멘션으로 대화 이어가기",
                """
                대댓글을 달면 답하려는 상대의 가명이 자동으로 표시되어 누구에게 하는 말인지 분명해집니다.
                여러 사람이 이어 말하는 긴 대화에서도 흐름을 놓치지 않을 수 있습니다.
                """),
            Create("내 댓글 수정·삭제하기",
                """
                내가 쓴 댓글은 언제든 수정하거나 삭제할 수 있습니다.
                단, 원댓글을 삭제하면 그에 달린 대댓글도 함께 삭제되니 신중하게 결정하세요.
                """),
            Create("채팅 이용하기",
                """
                채팅에서는 다른 수행자들과 실시간으로 이야기를 나눌 수 있습니다.
                공부하다 막힌 부분을 묻거나 가벼운 안부를 나누는 공간으로 활용하세요.
                """),
            Create("함께 지키는 소통 예절",
                """
                서로를 존중하는 말씨를 사용해 주세요.
                비방, 광고, 개인정보 노출, 특정 종교나 사람에 대한 혐오 표현은 삼가 주시고,
                서로 다른 견해는 차분하게 설명해 주세요.
                모두가 편안하게 공부하는 도량이 될 수 있도록 함께 노력해 주시기 바랍니다.
                """),
    ];

    private static Guide Create(string title, string content) => new()
    {
        UserId = AdminUserId,
        Title = title,
        Content = content.ReplaceLineEndings("\n").Trim(),   // 줄바꿈 차이로 매번 '변경'으로 오인되는 것 방지
        GuideCategory = GuideCategory.Help,
        CreatedAt = DateTime.UtcNow
    };
}
