#pragma once

#include "CoreMinimal.h"
#include "Blueprint/UserWidget.h"
#include "ExhibitionHudWidget.generated.h"

/**
 * 전시 HUD 위젯 C++ 베이스 클래스.
 *
 * BP_ExhibitionHud(UMG)에서 이 클래스를 부모 클래스로 설정하고,
 * UrlText(TextBlock), ConnectionText(TextBlock) 변수를 바인딩하세요.
 *
 * ExhibitionHudManager가 초기화 시 SetPanelUrl()을 호출하여 URL을 설정합니다.
 */
UCLASS(Abstract)
class EXHIBITIONCLIENT_API UExhibitionHudWidget : public UUserWidget
{
	GENERATED_BODY()

public:
	/** 모바일 패널 접속 URL을 표시합니다. */
	UFUNCTION(BlueprintCallable, Category="Exhibition|HUD")
	void SetPanelUrl(const FString& Url);

	/** 현재 연결된 패널 수를 표시합니다. */
	UFUNCTION(BlueprintCallable, Category="Exhibition|HUD")
	void SetConnectionCount(int32 Count);

	/** 서버에서 다운로드한 QR 텍스처를 위젯 Image에 반영합니다. */
	UFUNCTION(BlueprintCallable, Category="Exhibition|HUD")
	void SetQrTexture(UTexture2D* Texture);

	/** QR 코드 이미지 표시 여부를 설정합니다. */
	UFUNCTION(BlueprintCallable, Category="Exhibition|HUD")
	void SetQrVisible(bool bVisible);

	/** URL/연결 수 텍스트 블록 표시 여부를 설정합니다. */
	UFUNCTION(BlueprintCallable, Category="Exhibition|HUD")
	void SetTextBlocksVisible(bool bVisible);

	/** AI 채팅 답변을 HUD에 표시합니다. */
	UFUNCTION(BlueprintCallable, Category="Exhibition|HUD")
	void ShowChatReply(const FString& Reply);

	/** AI 채팅 답변 텍스트를 숨깁니다. */
	UFUNCTION(BlueprintCallable, Category="Exhibition|HUD")
	void HideChatReply();

protected:
	/** UMG에서 바인딩: URL 텍스트 블록 */
	UPROPERTY(BlueprintReadWrite, meta=(BindWidgetOptional))
	class UTextBlock* UrlText;

	/** UMG에서 바인딩: 연결 상태 텍스트 블록 */
	UPROPERTY(BlueprintReadWrite, meta=(BindWidgetOptional))
	class UTextBlock* ConnectionText;

	/** UMG에서 바인딩: QR 코드 이미지 */
	UPROPERTY(BlueprintReadWrite, meta=(BindWidgetOptional))
	class UImage* QrImage;

	/** UMG에서 바인딩: AI 채팅 답변 텍스트 (BP_ExhibitionHud에 TextBlock 이름 ChatReplyText 추가 필요) */
	UPROPERTY(BlueprintReadWrite, meta=(BindWidgetOptional))
	class UTextBlock* ChatReplyText;
};
