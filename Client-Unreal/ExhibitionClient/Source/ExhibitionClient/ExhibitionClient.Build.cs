// Copyright Epic Games, Inc. All Rights Reserved.

using UnrealBuildTool;

public class ExhibitionClient : ModuleRules
{
	public ExhibitionClient(ReadOnlyTargetRules Target) : base(Target)
	{
		PCHUsage = PCHUsageMode.UseExplicitOrSharedPCHs;
	
		PublicDependencyModuleNames.AddRange(new string[] { "Core", "CoreUObject", "Engine", "InputCore", "EnhancedInput" });

		PrivateDependencyModuleNames.AddRange(new string[]
		{
			"WebSockets", "Json", "JsonUtilities",
			"UMG",          // HUD 위젯
			"Sockets",      // 로컬 IP 조회
			"HTTP",         // QR PNG 다운로드
			"ImageWrapper", // PNG → UTexture2D 변환
		});

		// To include OnlineSubsystemSteam, add it to the plugins section in your uproject file with the Enabled attribute set to true
	}
}
