#pragma once

#include "CoreMinimal.h"
#include "Subsystems/GameInstanceSubsystem.h"
#include "GameFramework/Character.h"
#include "ExhibitionSpawnManager.generated.h"

/**
 * 패널 연결/해제 이벤트에 따라 캐릭터를 스폰/디스폰합니다.
 *
 * - panelConnected  → ThirdPersonMap의 PlayerStart에 캐릭터 스폰
 * - panelDisconnected → 해당 connectionId에 연결된 캐릭터 제거
 *
 * Config(DefaultGame.ini):
 *   [/Script/ExhibitionClient.ExhibitionSpawnManager]
 *   CharacterClass=/Game/ThirdPerson/Blueprints/BP_ThirdPersonCharacter.BP_ThirdPersonCharacter_C
 */
UCLASS(Config=Game, DefaultConfig)
class EXHIBITIONCLIENT_API UExhibitionSpawnManager : public UGameInstanceSubsystem
{
	GENERATED_BODY()

public:
	virtual void Initialize(FSubsystemCollectionBase& Collection) override;
	virtual void Deinitialize() override;

	/** 현재 스폰된 캐릭터 수를 반환합니다. */
	UFUNCTION(BlueprintPure, Category="Exhibition|Spawn")
	int32 GetSpawnedCharacterCount() const;

private:
	UFUNCTION()
	void OnPanelConnected(const FString& ConnectionId);

	UFUNCTION()
	void OnPanelDisconnected(const FString& ConnectionId);

	ACharacter* FindExistingPlayerCharacter() const;

	ACharacter* SpawnCharacterAtPlayerStart();

private:
	/**
	 * 스폰할 캐릭터 Blueprint 클래스.
	 * DefaultGame.ini에서 설정하거나, 에디터에서 CDO를 통해 설정합니다.
	 */
	UPROPERTY(Config)
	TSoftClassPtr<ACharacter> CharacterClass =
		TSoftClassPtr<ACharacter>(FSoftObjectPath(
			TEXT("/Game/ThirdPerson/Blueprints/BP_ThirdPersonCharacter.BP_ThirdPersonCharacter_C")));

	/** connectionId → 스폰된 캐릭터 */
	TSet<FString> ConnectedPanelIds;
	TWeakObjectPtr<ACharacter> ActiveCharacter;
	bool bOwnsActiveCharacter = false;
};
