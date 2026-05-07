using ExhibitionServer.Realtime;

namespace ExhibitionServer.Configuration;

/// <summary>
/// 미들웨어 파이프라인 구성을 한곳에서 관리합니다.
/// 파이프라인 순서가 중요하므로 한 메서드 안에서 순서를 보장합니다.
/// </summary>
public static class MiddlewareExtensions
{
    /// <summary>Exhibition 서버 미들웨어 파이프라인을 구성합니다.</summary>
    public static WebApplication UseExhibitionPipeline(this WebApplication app)
    {
        // 1. 개발환경 전용 미들웨어
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        // 2. 모바일 패널 정적 파일 서빙 (npm run dev 없이 dist/ 직접 서빙)
        //    wwwroot/ = Mobile-Panel/dist/ 빌드 결과물 복사 위치
        app.UseDefaultFiles();   // / 요청 시 index.html 자동 반환
        app.UseStaticFiles();    // assets/, manifest.json 등 정적 파일 서빙

        // 3. CORS (SignalR보다 먼저 등록)
        app.UseCors();

        // 4. 인증/인가 (향후 확장 포인트)
        app.UseAuthorization();

        // 5. WebSocket 지원 활성화
        app.UseWebSockets(new WebSocketOptions
        {
            // Keep-alive 주기: 빠른 연결 끊김 감지
            KeepAliveInterval = TimeSpan.FromSeconds(10),
        });

        // 6. Unreal WebSocket 연결 (미들웨어 클래스)
        app.UseUnrealWebSocket();

        return app;
    }

    /// <summary>Exhibition 서버 엔드포인트를 매핑합니다.</summary>
    public static WebApplication MapExhibitionEndpoints(this WebApplication app)
    {
        // REST API (기존 HTTP 엔드포인트 유지 — 하위 호환)
        app.MapControllers();

        // SignalR Hub (모바일 패널 실시간 통신)
        app.MapHub<ExhibitionHub>("/hub/exhibition");

        return app;
    }
}
