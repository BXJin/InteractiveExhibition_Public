#pragma once

#include "CoreMinimal.h"
#include "Subsystems/GameInstanceSubsystem.h"
#include "Interfaces/IHttpRequest.h"
#include "Interfaces/IHttpResponse.h"
#include "UI/ExhibitionHudWidget.h"
#include "ExhibitionHudManager.generated.h"

/**
 * HUD 위젯의 생성·표시·업데이트를 담당하는 GameInstanceSubsystem.
 *
 * 사용 방법:
 *   1. Content Browser에서 BP_ExhibitionHud(UMG) 생성 → 부모 클래스를 UExhibitionHudWidget으로 설정
 *   2. DefaultGame.ini에 HudWidgetClass 경로 설정 (또는 에디터 CDO에서 설정)
 *   3. GameMode BeginPlay(또는 LevelBlueprint)에서 ShowHud() 호출
 *
 * Config(DefaultGame.ini):
 *   [/Script/ExhibitionClient.ExhibitionHudManager]
 *   HudWidgetClass=/Game/UI/BP_ExhibitionHud.BP_ExhibitionHud_C
 *   MobilePanelPort=3001
 */
UCLASS(Config=Game, DefaultConfig)
class EXHIBITIONCLIENT_API UExhibitionHudManager : public UGameInstanceSubsystem
{
	GENERATED_BODY()

public:
	virtual void Initialize(FSubsystemCollectionBase& Collection) override;
	virtual void Deinitialize() override;

	/** HUD를 뷰포트에 표시합니다. GameMode BeginPlay에서 호출하세요. */
	UFUNCTION(BlueprintCallable, Category="Exhibition|HUD")
	void ShowHud();

	/** HUD를 뷰포트에서 제거합니다. */
	UFUNCTION(BlueprintCallable, Category="Exhibition|HUD")
	void HideHud();

	/** 현재 표시 중인 위젯 인스턴스를 반환합니다. */
	UFUNCTION(BlueprintPure, Category="Exhibition|HUD")
	UExhibitionHudWidget* GetHudWidget() const { return HudWidget; }

private:
	UFUNCTION()
	void OnPanelConnected(const FString& ConnectionId);

	UFUNCTION()
	void OnPanelDisconnected(const FString& ConnectionId);

	UFUNCTION()
	void OnChatReply(const FString& ReplyText);

	/** 서버 /api/panel/qr.png 에서 QR PNG를 비동기 다운로드합니다. */
	void FetchQrCodeAsync();

	/** HTTP 응답 수신 후 PNG → UTexture2D 변환 → 위젯에 반영합니다. */
	void OnQrPngReceived(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bSuccess);

	/** 로컬 IPv4 주소를 반환합니다. 실패 시 "127.0.0.1"을 반환합니다. */
	static FString GetLocalIpAddress();

	bool IsLocalHttpsAvailable() const;
	FString GetPanelScheme() const;
	int32 GetPanelPort() const;
	FString GetPanelUrl(const FString& Host) const;
	FString GetQrEndpointUrl(const FString& Host) const;

private:
	UPROPERTY(Config)
	TSoftClassPtr<UExhibitionHudWidget> HudWidgetClass;

	/** ASP.NET 서버 포트 (패널 서빙 + QR 엔드포인트 공용) */
	UPROPERTY(Config)
	int32 MobilePanelPort = 5225;

	UPROPERTY(Config)
	int32 SecureMobilePanelPort = 7225;

	UPROPERTY(Config)
	FString LocalHttpsCertificatePath = TEXT("ExhibitionServer/server.pfx");

	UPROPERTY()
	UExhibitionHudWidget* HudWidget = nullptr;

	int32 ConnectedPanelCount = 0;

	/** 채팅 답변 자동 숨김 타이머 */
	FTimerHandle ChatReplyTimerHandle;

	/** 채팅 답변 표시 시간 (초) */
	UPROPERTY(Config)
	float ChatReplyDisplaySeconds = 8.0f;
};
