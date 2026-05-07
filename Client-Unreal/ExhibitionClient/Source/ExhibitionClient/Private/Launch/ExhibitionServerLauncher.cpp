#include "Launch/ExhibitionServerLauncher.h"

#include "HAL/PlatformProcess.h"
#include "Misc/Paths.h"

DEFINE_LOG_CATEGORY_STATIC(LogExhibitionLauncher, Log, All);

void UExhibitionServerLauncher::Initialize(FSubsystemCollectionBase& Collection)
{
    Super::Initialize(Collection);

    if (bAutoLaunch)
    {
        LaunchServer();
    }
}

void UExhibitionServerLauncher::Deinitialize()
{
    StopServer();
    Super::Deinitialize();
}

void UExhibitionServerLauncher::LaunchServer()
{
    if (IsServerRunning())
    {
        UE_LOG(LogExhibitionLauncher, Warning, TEXT("Server is already running."));
        return;
    }

    if (ServerExePath.IsEmpty())
    {
        UE_LOG(LogExhibitionLauncher, Error, TEXT("ServerExePath is empty. Set it in DefaultGame.ini."));
        return;
    }

    TArray<FString> CandidatePaths;

    if (FPaths::IsRelative(ServerExePath))
    {
        CandidatePaths.Add(FPaths::Combine(FPaths::LaunchDir(), ServerExePath));
        CandidatePaths.Add(FPaths::Combine(FPlatformProcess::BaseDir(), ServerExePath));
        CandidatePaths.Add(FPaths::Combine(FPaths::ProjectDir(), ServerExePath));
    }
    else
    {
        CandidatePaths.Add(ServerExePath);
    }

    FString AbsPath;

    for (const FString& CandidatePath : CandidatePaths)
    {
        FString NormalizedPath = FPaths::ConvertRelativePathToFull(CandidatePath);
        FPaths::CollapseRelativeDirectories(NormalizedPath);
        FPaths::NormalizeFilename(NormalizedPath);

        if (FPaths::FileExists(NormalizedPath))
        {
            AbsPath = NormalizedPath;
            break;
        }
    }

    if (AbsPath.IsEmpty())
    {
        UE_LOG(LogExhibitionLauncher, Error,
            TEXT("Server executable not found: %s\n"
                 "Run 'dotnet publish' in Server-AspNet/ExhibitionServer to generate it."),
            *ServerExePath);
        return;
    }

    // Use the server folder as the working directory so appsettings.json and wwwroot are resolved correctly.
    FString WorkingDir = FPaths::GetPath(AbsPath);

    UE_LOG(LogExhibitionLauncher, Log, TEXT("Launching server: %s"), *AbsPath);

    ServerProcess = FPlatformProcess::CreateProc(
        *AbsPath,
        TEXT(""),
        true,
        false,
        false,
        nullptr,
        0,
        *WorkingDir,
        nullptr
    );

    if (!ServerProcess.IsValid())
    {
        UE_LOG(LogExhibitionLauncher, Error, TEXT("Failed to launch server process."));
        return;
    }

    UE_LOG(LogExhibitionLauncher, Log, TEXT("Server process launched successfully."));
}

void UExhibitionServerLauncher::StopServer()
{
    if (!ServerProcess.IsValid())
    {
        return;
    }

    if (FPlatformProcess::IsProcRunning(ServerProcess))
    {
        UE_LOG(LogExhibitionLauncher, Log, TEXT("Terminating server process."));
        FPlatformProcess::TerminateProc(ServerProcess, true);
    }

    FPlatformProcess::CloseProc(ServerProcess);
    ServerProcess = FProcHandle();
}

bool UExhibitionServerLauncher::IsServerRunning()
{
    return ServerProcess.IsValid() && FPlatformProcess::IsProcRunning(ServerProcess);
}
