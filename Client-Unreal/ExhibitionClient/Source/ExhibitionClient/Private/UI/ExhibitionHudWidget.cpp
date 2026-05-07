#include "UI/ExhibitionHudWidget.h"

#include "Components/TextBlock.h"
#include "Components/Image.h"

void UExhibitionHudWidget::SetPanelUrl(const FString& Url)
{
	if (UrlText)
	{
		UrlText->SetText(FText::FromString(Url));
	}
}

void UExhibitionHudWidget::SetConnectionCount(int32 Count)
{
	if (ConnectionText)
	{
		ConnectionText->SetText(FText::FromString(
			FString::Printf(TEXT("연결된 패널: %d"), Count)));
	}
}

void UExhibitionHudWidget::SetQrTexture(UTexture2D* Texture)
{
	if (QrImage && Texture)
	{
		QrImage->SetBrushFromTexture(Texture, /*bMatchSize=*/true);
	}
}

void UExhibitionHudWidget::SetQrVisible(bool bVisible)
{
	if (QrImage)
	{
		QrImage->SetVisibility(bVisible ? ESlateVisibility::Visible : ESlateVisibility::Collapsed);
	}
}

void UExhibitionHudWidget::SetTextBlocksVisible(bool bVisible)
{
	const ESlateVisibility Vis = bVisible ? ESlateVisibility::Visible : ESlateVisibility::Collapsed;
	if (UrlText)       { UrlText->SetVisibility(Vis); }
	if (ConnectionText){ ConnectionText->SetVisibility(Vis); }
}

void UExhibitionHudWidget::ShowChatReply(const FString& Reply)
{
	if (!ChatReplyText)
	{
		return;
	}

	ChatReplyText->SetAutoWrapText(true);
	ChatReplyText->SetText(FText::FromString(Reply));
	ChatReplyText->SetVisibility(ESlateVisibility::Visible);
}

void UExhibitionHudWidget::HideChatReply()
{
	if (ChatReplyText)
	{
		ChatReplyText->SetVisibility(ESlateVisibility::Collapsed);
	}
}
