using System.Text.Json.Serialization;

namespace Exhibition.Shared.Commands
{
    [JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
    [JsonDerivedType(typeof(SetEmotionCommand), "setEmotion")]
    [JsonDerivedType(typeof(PlayAnimationCommand), "playAnimation")]
    [JsonDerivedType(typeof(TriggerStageEventCommand), "triggerStageEvent")]
    [JsonDerivedType(typeof(MoveToPointCommand), "moveToPoint")]
    [JsonDerivedType(typeof(MoveDirectionCommand), "moveDirection")]
    [JsonDerivedType(typeof(RotateCommand), "rotate")]
    [JsonDerivedType(typeof(SwitchStyleCommand), "switchStyle")]
    public abstract record ExhibitionCommand
    {
        /// <summary>
        /// 1ĳ���� MVP��, 2ĳ���� Ȯ���� ���� ��� ���ɿ� �����մϴ�.
        /// </summary>
        public required string CharacterId { get; init; }

        /// <summary>
        /// ĳ���� �� Ÿ��(������Ʈ/ī�޶�/���� ��)�� ���� ���� ����� Ȯ�� ����Ʈ�Դϴ�.
        /// </summary>
        public string? TargetId { get; init; }

        /// <summary>
        /// ��û ������� ����(�α�/����� HUD)�� ���� ID �Դϴ�.
        /// </summary>
        public Guid CommandId { get; init; } = Guid.NewGuid();

        public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    }

    public sealed record SetEmotionCommand : ExhibitionCommand
    {
        /// <summary>
        /// ��: "happy", "sad", "angry", "surprise"
        /// </summary>
        public required string EmotionKey { get; init; }
    }

    public sealed record PlayAnimationCommand : ExhibitionCommand
    {
        /// <summary>
        /// ��: "wave", "bow", "clap"
        /// </summary>
        public required string AnimationKey { get; init; }

        public bool Loop { get; init; }
    }

    public sealed record TriggerStageEventCommand : ExhibitionCommand
    {
        /// <summary>
        /// ��: "stage.day", "stage.night", "light.spotOn"
        /// </summary>
        public required string StageEventKey { get; init; }
    }

    public sealed record MoveToPointCommand : ExhibitionCommand
    {
        public required Vector3Dto Position { get; init; }

        public float? Speed { get; init; }
    }

    public sealed record MoveDirectionCommand : ExhibitionCommand
    {
        /// <summary>
        /// ����ȭ�� ���� ���͸� �����մϴ�.
        /// </summary>
        public required Vector3Dto Direction { get; init; }

        public float? Speed { get; init; }

        public float? DurationSeconds { get; init; }
    }

    public sealed record RotateCommand : ExhibitionCommand
    {
        public required RotatorDto Rotation { get; init; }
    }

    /// <summary>
    /// 전시장 머티리얼 스타일 전환. CharacterId는 무시됨 (씬 전체 적용).
    /// 0 = Classic, 1 = Aged, 2 = Modern
    /// </summary>
    public sealed record SwitchStyleCommand : ExhibitionCommand
    {
        public required int StyleIndex { get; init; }
    }

    public sealed record Vector3Dto
    {
        public required float X { get; init; }
        public required float Y { get; init; }
        public required float Z { get; init; }
    }

    public sealed record RotatorDto
    {
        public required float Pitch { get; init; }
        public required float Yaw { get; init; }
        public required float Roll { get; init; }
    }
}