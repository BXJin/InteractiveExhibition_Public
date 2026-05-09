#include "UI/ExhibitionHudManager.h"

#include "Blueprint/UserWidget.h"
#include "Engine/GameInstance.h"
#include "Engine/Texture2D.h"
#include "HAL/PlatformProcess.h"
#include "HttpModule.h"
#include "IImageWrapper.h"
#include "IImageWrapperModule.h"
#include "Realtime/ExhibitionRealtimeSubsystem.h"
#include "RenderUtils.h"
#include "SocketSubsystem.h"
#include "IPAddress.h"

DEFINE_LOG_CATEGORY_STATIC(LogExhibitionHud, Log, All);

void UExhibitionHudManager::Initialize(FSubsystemCollectionBase& Collection)
{
    Super::Initialize(Collection);

    Collection.InitializeDependency<UExhibitionRealtimeSubsystem>();

    if (UExhibitionRealtimeSubsystem* Realtime = GetGameInstance()->GetSubsystem<UExhibitionRealtimeSubsystem>())
    {
        Realtime->OnPanelConnected.AddDynamic(this, &UExhibitionHudManager::OnPanelConnected);
        Realtime->OnPanelDisconnected.AddDynamic(this, &UExhibitionHudManager::OnPanelDisconnected);
        Realtime->OnChatReply.AddDynamic(this, &UExhibitionHudManager::OnChatReply);
    }
}

void UExhibitionHudManager::Deinitialize()
{
    if (UExhibitionRealtimeSubsystem* Realtime = GetGameInstance()->GetSubsystem<UExhibitionRealtimeSubsystem>())
    {
        Realtime->OnPanelConnected.RemoveDynamic(this, &UExhibitionHudManager::OnPanelConnected);
        Realtime->OnPanelDisconnected.RemoveDynamic(this, &UExhibitionHudManager::OnPanelDisconnected);
        Realtime->OnChatReply.RemoveDynamic(this, &UExhibitionHudManager::OnChatReply);
    }

    HideHud();
    Super::Deinitialize();
}

void UExhibitionHudManager::ShowHud()
{
    UWorld* World = GetWorld();
    if (!World)
    {
        UE_LOG(LogExhibitionHud, Error, TEXT("ShowHud called but World is null."));
        return;
    }

    if (HudWidget)
    {
        UE_LOG(LogExhibitionHud, Warning, TEXT("HUD is already visible."));
        return;
    }

    UClass* WidgetClass = HudWidgetClass.LoadSynchronous();
    if (!WidgetClass)
    {
        UE_LOG(LogExhibitionHud, Error,
            TEXT("HudWidgetClass is not set. Configure it in DefaultGame.ini:\n"
                 "[/Script/ExhibitionClient.ExhibitionHudManager]\n"
                 "HudWidgetClass=/Game/UI/BP_ExhibitionHud.BP_ExhibitionHud_C"));
        return;
    }

    APlayerController* PC = World->GetFirstPlayerController();
    if (!PC)
    {
        UE_LOG(LogExhibitionHud, Error, TEXT("No PlayerController found."));
        return;
    }

    HudWidget = CreateWidget<UExhibitionHudWidget>(PC, WidgetClass);
    if (!HudWidget)
    {
        UE_LOG(LogExhibitionHud, Error, TEXT("Failed to create HUD widget."));
        return;
    }

    // 접속 URL 설정 후 뷰포트에 추가
    const FString LocalIp  = GetLocalIpAddress();
    const FString PanelUrl = GetPanelUrl(LocalIp);

    HudWidget->SetPanelUrl(PanelUrl);
    HudWidget->SetConnectionCount(ConnectedPanelCount);
    HudWidget->SetTextBlocksVisible(false);
    HudWidget->HideChatReply();
    HudWidget->AddToViewport();

    UE_LOG(LogExhibitionHud, Log, TEXT("HUD visible. Panel URL: %s"), *PanelUrl);

    // 서버가 기동된 직후라 약간 지연 후 QR 요청 (서버 준비 대기)
    FTimerHandle TimerHandle;
    GetWorld()->GetTimerManager().SetTimer(TimerHandle, [this]()
    {
        FetchQrCodeAsync();
    }, 1.5f, false);
}

void UExhibitionHudManager::HideHud()
{
    if (HudWidget)
    {
        HudWidget->RemoveFromParent();
        HudWidget = nullptr;
    }
}

void UExhibitionHudManager::OnPanelConnected(const FString& ConnectionId)
{
    ConnectedPanelCount++;
    if (HudWidget)
    {
        HudWidget->SetConnectionCount(ConnectedPanelCount);
        // 첫 번째 패널이 연결되면 QR 숨김
        if (ConnectedPanelCount == 1)
        {
            HudWidget->SetQrVisible(false);
        }
    }
}

void UExhibitionHudManager::OnPanelDisconnected(const FString& ConnectionId)
{
    ConnectedPanelCount = FMath::Max(0, ConnectedPanelCount - 1);
    if (HudWidget)
    {
        HudWidget->SetConnectionCount(ConnectedPanelCount);
        // 모든 패널이 해제되면 QR 다시 표시
        if (ConnectedPanelCount == 0)
        {
            HudWidget->SetQrVisible(true);
        }
    }
}

void UExhibitionHudManager::OnChatReply(const FString& ReplyText)
{
    if (!HudWidget)
    {
        return;
    }

    UWorld* World = GetWorld();
    if (!World)
    {
        return;
    }

    // 이전 타이머가 있으면 취소
    World->GetTimerManager().ClearTimer(ChatReplyTimerHandle);

    HudWidget->ShowChatReply(ReplyText);

    // 일정 시간 후 자동 숨김
    World->GetTimerManager().SetTimer(
        ChatReplyTimerHandle,
        [this]()
        {
            if (HudWidget)
            {
                HudWidget->HideChatReply();
            }
        },
        ChatReplyDisplaySeconds,
        false);
}

void UExhibitionHudManager::FetchQrCodeAsync()
{
    const FString QrUrl = GetQrEndpointUrl(GetLocalIpAddress());

    TSharedRef<IHttpRequest, ESPMode::ThreadSafe> HttpRequest = FHttpModule::Get().CreateRequest();
    HttpRequest->SetURL(QrUrl);
    HttpRequest->SetVerb(TEXT("GET"));
    HttpRequest->OnProcessRequestComplete().BindUObject(this, &UExhibitionHudManager::OnQrPngReceived);
    HttpRequest->ProcessRequest();

    UE_LOG(LogExhibitionHud, Log, TEXT("QR PNG 요청: %s"), *QrUrl);
}

void UExhibitionHudManager::OnQrPngReceived(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bSuccess)
{
    if (!bSuccess || !Response || Response->GetResponseCode() != 200)
    {
        UE_LOG(LogExhibitionHud, Warning, TEXT("QR PNG 수신 실패. Code=%d"),
            Response ? Response->GetResponseCode() : -1);
        return;
    }

    // PNG → 원시 픽셀 디코딩
    IImageWrapperModule& ImageWrapperModule =
        FModuleManager::LoadModuleChecked<IImageWrapperModule>(FName("ImageWrapper"));

    TSharedPtr<IImageWrapper> ImageWrapper =
        ImageWrapperModule.CreateImageWrapper(EImageFormat::PNG);

    const TArray<uint8>& PngData = Response->GetContent();
    if (!ImageWrapper || !ImageWrapper->SetCompressed(PngData.GetData(), PngData.Num()))
    {
        UE_LOG(LogExhibitionHud, Warning, TEXT("QR PNG 디코딩 실패."));
        return;
    }

    TArray<uint8> RawPixels;
    if (!ImageWrapper->GetRaw(ERGBFormat::BGRA, 8, RawPixels))
    {
        UE_LOG(LogExhibitionHud, Warning, TEXT("QR 픽셀 추출 실패."));
        return;
    }

    const int32 Width  = ImageWrapper->GetWidth();
    const int32 Height = ImageWrapper->GetHeight();

    // UTexture2D 생성 (Transient — 런타임 전용, 저장 안 됨)
    UTexture2D* QrTexture = UTexture2D::CreateTransient(Width, Height, PF_B8G8R8A8, TEXT("QrCodeTexture"));
    if (!QrTexture)
    {
        UE_LOG(LogExhibitionHud, Warning, TEXT("QR UTexture2D 생성 실패."));
        return;
    }

    // QR은 선명한 픽셀이 필요하므로 필터링 없음
    QrTexture->Filter = TF_Nearest;
    QrTexture->SRGB   = false;

    FTexture2DMipMap& Mip = QrTexture->GetPlatformData()->Mips[0];
    void* MipData = Mip.BulkData.Lock(LOCK_READ_WRITE);
    FMemory::Memcpy(MipData, RawPixels.GetData(), RawPixels.Num());
    Mip.BulkData.Unlock();
    QrTexture->UpdateResource();

    if (HudWidget)
    {
        HudWidget->SetQrTexture(QrTexture);
        UE_LOG(LogExhibitionHud, Log, TEXT("QR 텍스처 설정 완료. %dx%d"), Width, Height);
    }
}

FString UExhibitionHudManager::GetLocalIpAddress()
{
    ISocketSubsystem* SocketSubsystem = ISocketSubsystem::Get(PLATFORM_SOCKETSUBSYSTEM);
    if (!SocketSubsystem)
    {
        return TEXT("127.0.0.1");
    }

    bool bCanBind = false;
    TSharedRef<FInternetAddr> Addr = SocketSubsystem->GetLocalHostAddr(*GLog, bCanBind);

    // 루프백이면 실제 IP를 못 얻은 것이므로 fallback
    uint32 RawIp = 0;
    Addr->GetIp(RawIp);
    if (RawIp == 0x7F000001 /* 127.0.0.1 */ || RawIp == 0)
    {
        return TEXT("127.0.0.1");
    }

    return Addr->ToString(false);
}

bool UExhibitionHudManager::IsLocalHttpsAvailable() const
{
    TArray<FString> CandidatePaths;

    if (FPaths::IsRelative(LocalHttpsCertificatePath))
    {
        CandidatePaths.Add(FPaths::Combine(FPaths::LaunchDir(), LocalHttpsCertificatePath));
        CandidatePaths.Add(FPaths::Combine(FPlatformProcess::BaseDir(), LocalHttpsCertificatePath));
        CandidatePaths.Add(FPaths::Combine(FPaths::ProjectDir(), LocalHttpsCertificatePath));
    }
    else
    {
        CandidatePaths.Add(LocalHttpsCertificatePath);
    }

    for (const FString& CandidatePath : CandidatePaths)
    {
        FString NormalizedPath = FPaths::ConvertRelativePathToFull(CandidatePath);
        FPaths::CollapseRelativeDirectories(NormalizedPath);
        FPaths::NormalizeFilename(NormalizedPath);

        if (FPaths::FileExists(NormalizedPath))
        {
            return true;
        }
    }

    return false;
}

FString UExhibitionHudManager::GetPanelScheme() const
{
    return IsLocalHttpsAvailable() ? TEXT("https") : TEXT("http");
}

int32 UExhibitionHudManager::GetPanelPort() const
{
    return IsLocalHttpsAvailable() ? SecureMobilePanelPort : MobilePanelPort;
}

FString UExhibitionHudManager::GetPanelUrl(const FString& Host) const
{
    return FString::Printf(TEXT("%s://%s:%d"), *GetPanelScheme(), *Host, GetPanelPort());
}

FString UExhibitionHudManager::GetQrEndpointUrl(const FString& Host) const
{
    return FString::Printf(TEXT("%s/api/panel/qr.png"), *GetPanelUrl(Host));
}
