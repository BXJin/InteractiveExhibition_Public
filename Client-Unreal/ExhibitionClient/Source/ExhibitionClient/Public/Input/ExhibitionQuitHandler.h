#pragma once

#include "CoreMinimal.h"
#include "Components/ActorComponent.h"
#include "InputCoreTypes.h"
#include "ExhibitionQuitHandler.generated.h"

/**
 * 관리자 키보드 종료 핸들러.
 *
 * 패널로 조작하는 관람객과 달리 관리자는 키보드로 프로그램을 종료할 수 있어야 합니다.
 * 이 컴포넌트를 BP_ThirdPersonCharacter에 추가하면 지정된 키(기본: ESC) 입력 시
 * 게임이 즉시 종료됩니다.
 *
 * 사용법:
 *   1. BP_ThirdPersonCharacter에 이 컴포넌트 추가
 *   2. QuitKey를 원하는 키로 변경 (기본: Escape)
 *   3. 끝.
 */
UCLASS(ClassGroup="Exhibition", meta=(BlueprintSpawnableComponent),
       DisplayName="Exhibition Quit Handler")
class EXHIBITIONCLIENT_API UExhibitionQuitHandler : public UActorComponent
{
    GENERATED_BODY()

public:
    UExhibitionQuitHandler();

    virtual void BeginPlay() override;

    /** 게임 종료 트리거 키. 기본값: Escape */
    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category="Exhibition|Input",
              meta=(DisplayName="Quit Key"))
    FKey QuitKey = EKeys::Escape;

private:
    void HandleQuit();
};
