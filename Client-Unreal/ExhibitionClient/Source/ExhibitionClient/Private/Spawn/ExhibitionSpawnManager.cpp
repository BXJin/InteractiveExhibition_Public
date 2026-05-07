#include "Spawn/ExhibitionSpawnManager.h"

#include "Engine/World.h"
#include "GameFramework/PlayerController.h"
#include "GameFramework/PlayerStart.h"
#include "Kismet/GameplayStatics.h"
#include "Realtime/ExhibitionRealtimeSubsystem.h"
#include "UObject/SoftObjectPtr.h"

DEFINE_LOG_CATEGORY_STATIC(LogExhibitionSpawn, Log, All);

void UExhibitionSpawnManager::Initialize(FSubsystemCollectionBase& Collection)
{
    Super::Initialize(Collection);

    // RealtimeSubsystem이 먼저 초기화되도록 의존성 선언
    Collection.InitializeDependency<UExhibitionRealtimeSubsystem>();

    if (UExhibitionRealtimeSubsystem* Realtime = GetGameInstance()->GetSubsystem<UExhibitionRealtimeSubsystem>())
    {
        Realtime->OnPanelConnected.AddDynamic(this, &UExhibitionSpawnManager::OnPanelConnected);
        Realtime->OnPanelDisconnected.AddDynamic(this, &UExhibitionSpawnManager::OnPanelDisconnected);
    }
}

void UExhibitionSpawnManager::Deinitialize()
{
    if (UExhibitionRealtimeSubsystem* Realtime = GetGameInstance()->GetSubsystem<UExhibitionRealtimeSubsystem>())
    {
        Realtime->OnPanelConnected.RemoveDynamic(this, &UExhibitionSpawnManager::OnPanelConnected);
        Realtime->OnPanelDisconnected.RemoveDynamic(this, &UExhibitionSpawnManager::OnPanelDisconnected);
    }

    // 남아있는 캐릭터 모두 제거
    ConnectedPanelIds.Empty();
    if (bOwnsActiveCharacter && ActiveCharacter.IsValid())
    {
        ActiveCharacter->Destroy();
    }
    ActiveCharacter.Reset();
    bOwnsActiveCharacter = false;

    Super::Deinitialize();
}

int32 UExhibitionSpawnManager::GetSpawnedCharacterCount() const
{
    return ActiveCharacter.IsValid() ? 1 : 0;
}

void UExhibitionSpawnManager::OnPanelConnected(const FString& ConnectionId)
{
    UE_LOG(LogExhibitionSpawn, Log, TEXT("Panel connected: %s — spawning character."), *ConnectionId);

    ConnectedPanelIds.Add(ConnectionId);

    if (ActiveCharacter.IsValid())
    {
        UE_LOG(LogExhibitionSpawn, Log, TEXT("Panel connected: %s - using active character."), *ConnectionId);
        return;
    }

    if (ACharacter* ExistingCharacter = FindExistingPlayerCharacter())
    {
        ActiveCharacter = ExistingCharacter;
        bOwnsActiveCharacter = false;
        UE_LOG(LogExhibitionSpawn, Log, TEXT("Panel connected: %s - reusing existing player character."), *ConnectionId);
        return;
    }

    if (ACharacter* Spawned = SpawnCharacterAtPlayerStart())
    {
        ActiveCharacter = Spawned;
        bOwnsActiveCharacter = true;
        UE_LOG(LogExhibitionSpawn, Log, TEXT("Character spawned for first panel connection: %s"), *ConnectionId);
    }
    else
    {
        UE_LOG(LogExhibitionSpawn, Error, TEXT("Failed to spawn character for connection: %s"), *ConnectionId);
    }
}

void UExhibitionSpawnManager::OnPanelDisconnected(const FString& ConnectionId)
{
    UE_LOG(LogExhibitionSpawn, Log, TEXT("Panel disconnected: %s — despawning character."), *ConnectionId);

    ConnectedPanelIds.Remove(ConnectionId);
    if (ConnectedPanelIds.Num() > 0)
    {
        return;
    }

    if (bOwnsActiveCharacter && ActiveCharacter.IsValid())
    {
        ActiveCharacter->Destroy();
    }

    ActiveCharacter.Reset();
    bOwnsActiveCharacter = false;
}

ACharacter* UExhibitionSpawnManager::FindExistingPlayerCharacter() const
{
    UWorld* World = GetWorld();
    if (!World)
    {
        return nullptr;
    }

    APlayerController* PlayerController = World->GetFirstPlayerController();
    if (!PlayerController)
    {
        return nullptr;
    }

    return Cast<ACharacter>(PlayerController->GetPawn());
}

ACharacter* UExhibitionSpawnManager::SpawnCharacterAtPlayerStart()
{
    UWorld* World = GetWorld();
    if (!World)
    {
        UE_LOG(LogExhibitionSpawn, Error, TEXT("World is null — cannot spawn character."));
        return nullptr;
    }

    // CharacterClass 동기 로드 (패키지된 빌드에서는 이미 메모리에 있음)
    UClass* ClassToSpawn = CharacterClass.LoadSynchronous();
    if (!ClassToSpawn)
    {
        UE_LOG(LogExhibitionSpawn, Error, TEXT("CharacterClass could not be loaded: %s"),
            *CharacterClass.ToString());
        return nullptr;
    }

    // PlayerStart 위치 탐색 (없으면 원점에 스폰)
    TArray<AActor*> PlayerStarts;
    UGameplayStatics::GetAllActorsOfClass(World, APlayerStart::StaticClass(), PlayerStarts);

    FVector  SpawnLocation = FVector::ZeroVector;
    FRotator SpawnRotation = FRotator::ZeroRotator;

    if (PlayerStarts.Num() > 0)
    {
        AActor* Start   = PlayerStarts[0];
        SpawnLocation   = Start->GetActorLocation();
        SpawnRotation   = Start->GetActorRotation();
    }
    else
    {
        UE_LOG(LogExhibitionSpawn, Warning, TEXT("No PlayerStart found — spawning at origin."));
    }

    FActorSpawnParameters Params;
    Params.SpawnCollisionHandlingOverride = ESpawnActorCollisionHandlingMethod::AdjustIfPossibleButAlwaysSpawn;

    ACharacter* Spawned = World->SpawnActor<ACharacter>(ClassToSpawn, SpawnLocation, SpawnRotation, Params);

    if (Spawned)
    {
        if (APlayerController* PC = World->GetFirstPlayerController())
        {
            PC->Possess(Spawned);
        }
    }

    return Spawned;
}
