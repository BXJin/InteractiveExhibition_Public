#pragma once

#include "CoreMinimal.h"
#include "Subsystems/GameInstanceSubsystem.h"
#include "ExhibitionServerLauncher.generated.h"

/**
 * GameInstance 시작 시 ASP.NET 서버 exe를 자동으로 실행하고,
 * GameInstance 종료 시 프로세스를 정리합니다.
 *
 * Config(DefaultGame.ini):
 *   [/Script/ExhibitionClient.ExhibitionServerLauncher]
 *   ServerExePath=ExhibitionServer/ExhibitionServer.exe
 *   bAutoLaunch=true
 */
UCLASS(Config=Game, DefaultConfig)
class EXHIBITIONCLIENT_API UExhibitionServerLauncher : public UGameInstanceSubsystem
{
	GENERATED_BODY()

public:
	virtual void Initialize(FSubsystemCollectionBase& Collection) override;
	virtual void Deinitialize() override;

	/** 수동으로 서버를 시작합니다. bAutoLaunch=false일 때 호출하세요. */
	UFUNCTION(BlueprintCallable, Category="Exhibition|Launch")
	void LaunchServer();

	/** 서버 프로세스를 종료합니다. */
	UFUNCTION(BlueprintCallable, Category="Exhibition|Launch")
	void StopServer();

	/** 서버가 현재 실행 중인지 확인합니다. */
	UFUNCTION(BlueprintPure, Category="Exhibition|Launch")
	bool IsServerRunning();

private:
	/** 서버 exe 절대/상대 경로. 상대 경로는 프로젝트 루트 기준입니다. */
	UPROPERTY(Config)
	FString ServerExePath = TEXT("ExhibitionServer/ExhibitionServer.exe");

	/** GameInstance 시작 시 자동으로 서버를 실행할지 여부. */
	UPROPERTY(Config)
	bool bAutoLaunch = true;

	FProcHandle ServerProcess;
};
