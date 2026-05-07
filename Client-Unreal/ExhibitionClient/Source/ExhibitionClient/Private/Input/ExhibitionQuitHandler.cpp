#include "Input/ExhibitionQuitHandler.h"

#include "GameFramework/Pawn.h"
#include "GameFramework/PlayerController.h"
#include "Kismet/KismetSystemLibrary.h"

DEFINE_LOG_CATEGORY_STATIC(LogExhibitionQuit, Log, All);

UExhibitionQuitHandler::UExhibitionQuitHandler()
{
    PrimaryComponentTick.bCanEverTick = false;
}

void UExhibitionQuitHandler::BeginPlay()
{
    Super::BeginPlay();

    APawn* Pawn = Cast<APawn>(GetOwner());
    if (!Pawn)
    {
        UE_LOG(LogExhibitionQuit, Warning, TEXT("[QuitHandler] Owner is not a Pawn."));
        return;
    }

    if (!Pawn->InputComponent)
    {
        UE_LOG(LogExhibitionQuit, Warning,
            TEXT("[QuitHandler] Pawn has no InputComponent. "
                 "Ensure the Pawn has input enabled (Auto Receive Input or EnableInput called)."));
        return;
    }

    Pawn->InputComponent->BindKey(QuitKey, IE_Pressed, this, &UExhibitionQuitHandler::HandleQuit);

    UE_LOG(LogExhibitionQuit, Log,
        TEXT("[QuitHandler] Quit key bound: %s"), *QuitKey.GetDisplayName().ToString());
}

void UExhibitionQuitHandler::HandleQuit()
{
    UE_LOG(LogExhibitionQuit, Log, TEXT("[QuitHandler] Quit key pressed — exiting."));
    UKismetSystemLibrary::QuitGame(this, nullptr, EQuitPreference::Quit, /*bIgnorePlatformRestrictions=*/false);
}
