using System.Diagnostics;
using System.Text.Json;
using Exhibition.Shared.Ai;
using Exhibition.Shared.Commands;
using ExhibitionServer.Application.Abstractions;
using ExhibitionServer.Application.Knowledge;
using ExhibitionServer.Realtime.Abstractions;

namespace ExhibitionServer.Application.Chat;

public sealed class ChatGuideService : IChatGuideService
{
    private const int MaxMessageLength = 300;
    private const int MaxCommands = 3;

    private static readonly HashSet<string> AllowedEmotions = new(StringComparer.OrdinalIgnoreCase)
    {
        "happy", "sad", "angry", "surprise", "neutral",
        "curious", "thinking", "explaining", "greeting"
    };

    private static readonly HashSet<string> AllowedAnimations = new(StringComparer.OrdinalIgnoreCase)
    {
        "wave", "bow", "clap", "explain", "idle"
    };

    private static readonly HashSet<string> AllowedStageEvents = new(StringComparer.OrdinalIgnoreCase)
    {
        "light.spotOn", "light.spotOff", "scene.reset"
    };

    private static readonly Dictionary<string, int> StyleIndexMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "style_classic", 0 },
        { "style_aged", 1 },
        { "style_modern", 2 }
    };

    private readonly IExhibitionKnowledgeStore _knowledgeStore;
    private readonly IAiGatewayClient _aiGatewayClient;
    private readonly IConversationMemoryStore _conversationMemoryStore;
    private readonly ICommandDispatcher _dispatcher;
    private readonly IRawUnrealBroadcaster _unrealBroadcaster;
    private readonly IConversationLogger _conversationLogger;
    private readonly ILogger<ChatGuideService> _logger;

    public ChatGuideService(
        IExhibitionKnowledgeStore knowledgeStore,
        IAiGatewayClient aiGatewayClient,
        IConversationMemoryStore conversationMemoryStore,
        ICommandDispatcher dispatcher,
        IRawUnrealBroadcaster unrealBroadcaster,
        IConversationLogger conversationLogger,
        ILogger<ChatGuideService> logger)
    {
        _knowledgeStore = knowledgeStore;
        _aiGatewayClient = aiGatewayClient;
        _conversationMemoryStore = conversationMemoryStore;
        _dispatcher = dispatcher;
        _unrealBroadcaster = unrealBroadcaster;
        _conversationLogger = conversationLogger;
        _logger = logger;
    }

    public async Task<ChatResponse> ProcessAsync(
        ChatRequest request,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var message = request.Message.Trim();

        if (string.IsNullOrWhiteSpace(message))
        {
            return Failure("메시지를 입력해 주세요.", stopwatch.ElapsedMilliseconds);
        }

        if (message.Length > MaxMessageLength)
        {
            return Failure($"메시지는 {MaxMessageLength}자 이하로 입력해 주세요.", stopwatch.ElapsedMilliseconds);
        }

        var sources = SelectSources(message, request.SelectedArtifactId);
        var conversationHistory = _conversationMemoryStore.GetRecentTurns(request.ConversationId);
        var aiResponse = await _aiGatewayClient.CreateReplyAsync(
            request with { Message = message },
            sources,
            conversationHistory,
            cancellationToken);

        var commands = aiResponse is { Success: true }
            ? CompleteCommands(
                BuildCommandsFromSuggestions(aiResponse.SuggestedCommands, request.CharacterId),
                message,
                request.CharacterId,
                sources)
            : CompleteCommands(
                BuildRuleCommands(message, request.CharacterId, sources),
                message,
                request.CharacterId,
                sources);

        var reply = aiResponse is { Success: true }
            ? aiResponse.Reply
            : BuildRuleReply(message, sources, commands);

        var executedIds = await DispatchCommandsAsync(commands, cancellationToken);
        await BroadcastChatReplyAsync(reply, cancellationToken);
        stopwatch.Stop();

        var response = new ChatResponse
        {
            Reply = reply,
            Commands = commands,
            RetrievedSources = sources.Select(source => source.Id).ToArray(),
            Success = true,
            LatencyMs = stopwatch.ElapsedMilliseconds
        };

        _conversationMemoryStore.AppendExchange(request.ConversationId, message, response.Reply);

        await _conversationLogger.LogAsync(new ChatLogEntry
        {
            ConversationId = request.ConversationId,
            UserText = message,
            SelectedArtifactId = request.SelectedArtifactId,
            RetrievedSources = response.RetrievedSources,
            Reply = response.Reply,
            GeneratedCommands = response.Commands,
            ExecutedCommandIds = executedIds,
            Success = response.Success,
            LatencyMs = response.LatencyMs,
            PromptVersion = aiResponse is { Success: true } ? "ai-gateway-v1" : "guide-rule-v1"
        }, cancellationToken);

        return response;
    }

    public async IAsyncEnumerable<AiChatStreamEvent> StreamAsync(
        ChatRequest request,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var message = request.Message.Trim();

        if (string.IsNullOrWhiteSpace(message))
        {
            yield return AiChatStreamEvent.Error("EMPTY_MESSAGE", "Please enter a message.");
            yield break;
        }

        if (message.Length > MaxMessageLength)
        {
            yield return AiChatStreamEvent.Error(
                "MESSAGE_TOO_LONG",
                $"Message must be {MaxMessageLength} characters or fewer.");
            yield break;
        }

        var sources = SelectSources(message, request.SelectedArtifactId);
        var conversationHistory = _conversationMemoryStore.GetRecentTurns(request.ConversationId);
        var completed = false;

        await foreach (var streamEvent in _aiGatewayClient.StreamReplyAsync(
            request with { Message = message },
            sources,
            conversationHistory,
            cancellationToken))
        {
            if (streamEvent.EventType == AiChatStreamEventTypes.Delta)
            {
                yield return streamEvent;
                continue;
            }

            if (streamEvent.EventType == AiChatStreamEventTypes.Complete &&
                streamEvent.CompleteResponse is not null)
            {
                var commands = CompleteCommands(
                    BuildCommandsFromSuggestions(
                        streamEvent.CompleteResponse.SuggestedCommands,
                        request.CharacterId),
                    message,
                    request.CharacterId,
                    sources);

                var executedIds = await DispatchCommandsAsync(commands, cancellationToken);
                await BroadcastChatReplyAsync(streamEvent.CompleteResponse.Reply, cancellationToken);
                stopwatch.Stop();

                _conversationMemoryStore.AppendExchange(
                    request.ConversationId,
                    message,
                    streamEvent.CompleteResponse.Reply);

                await _conversationLogger.LogAsync(new ChatLogEntry
                {
                    ConversationId = request.ConversationId,
                    UserText = message,
                    SelectedArtifactId = request.SelectedArtifactId,
                    RetrievedSources = sources.Select(source => source.Id).ToArray(),
                    Reply = streamEvent.CompleteResponse.Reply,
                    GeneratedCommands = commands,
                    ExecutedCommandIds = executedIds,
                    Success = true,
                    LatencyMs = stopwatch.ElapsedMilliseconds,
                    PromptVersion = "ai-gateway-stream-v1"
                }, cancellationToken);

                yield return AiChatStreamEvent.Complete(streamEvent.CompleteResponse with
                {
                    Success = true,
                    LatencyMs = stopwatch.ElapsedMilliseconds
                });
                completed = true;
                break;
            }

            if (streamEvent.EventType == AiChatStreamEventTypes.Error)
            {
                break;
            }
        }

        if (!completed)
        {
            var fallbackCommands = CompleteCommands(
                BuildRuleCommands(message, request.CharacterId, sources),
                message,
                request.CharacterId,
                sources);
            var fallbackReply = BuildRuleReply(message, sources, fallbackCommands);

            var executedIds = await DispatchCommandsAsync(fallbackCommands, cancellationToken);
            await BroadcastChatReplyAsync(fallbackReply, cancellationToken);
            stopwatch.Stop();

            _conversationMemoryStore.AppendExchange(request.ConversationId, message, fallbackReply);

            await _conversationLogger.LogAsync(new ChatLogEntry
            {
                ConversationId = request.ConversationId,
                UserText = message,
                SelectedArtifactId = request.SelectedArtifactId,
                RetrievedSources = sources.Select(source => source.Id).ToArray(),
                Reply = fallbackReply,
                GeneratedCommands = fallbackCommands,
                ExecutedCommandIds = executedIds,
                Success = true,
                LatencyMs = stopwatch.ElapsedMilliseconds,
                PromptVersion = "guide-rule-stream-v1"
            }, cancellationToken);

            yield return AiChatStreamEvent.Delta(fallbackReply);
            yield return AiChatStreamEvent.Complete(new AiChatResponse
            {
                Reply = fallbackReply,
                SuggestedCommands = ToSuggestions(fallbackCommands),
                Success = true,
                LatencyMs = stopwatch.ElapsedMilliseconds,
                Provider = "local-fallback",
                Model = "rule-rag"
            });
        }
    }

    private async Task BroadcastChatReplyAsync(string reply, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(reply))
        {
            return;
        }

        try
        {
            var json = JsonSerializer.Serialize(new { type = "chatReply", text = reply });
            await _unrealBroadcaster.BroadcastRawAsync(json, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "chatReply broadcast to Unreal failed.");
        }
    }

    private async Task<IReadOnlyList<Guid>> DispatchCommandsAsync(
        IReadOnlyList<ExhibitionCommand> commands,
        CancellationToken cancellationToken)
    {
        var executedIds = new List<Guid>();

        foreach (var command in commands)
        {
            try
            {
                await _dispatcher.DispatchAsync(command, cancellationToken);
                executedIds.Add(command.CommandId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Chat command dispatch failed. CommandId={CommandId}", command.CommandId);
            }
        }

        return executedIds;
    }

    private IReadOnlyList<ExhibitionKnowledgeDocument> SelectSources(string message, string? selectedArtifactId)
    {
        var lower = message.ToLowerInvariant();
        if (IsSceneControlQuestion(message) &&
            (IsExplicitSceneChangeRequest(lower) || IsCapabilityQuestion(lower)) &&
            !IsExplainRequest(message))
        {
            return Array.Empty<ExhibitionKnowledgeDocument>();
        }

        if (IsCatalogQuestion(message))
        {
            return _knowledgeStore.GetCatalogDocuments();
        }

        return _knowledgeStore.Search(message, selectedArtifactId, maxResults: 8);
    }

    private static ChatResponse Failure(string reply, long latencyMs) => new()
    {
        Reply = reply,
        Success = false,
        Error = reply,
        LatencyMs = latencyMs
    };

    private static IEnumerable<ExhibitionCommand> BuildCommandsFromSuggestions(
        IReadOnlyList<AiSuggestedCommandDto> suggestions,
        string fallbackCharacterId)
    {
        foreach (var suggestion in suggestions)
        {
            var characterId = string.IsNullOrWhiteSpace(suggestion.CharacterId)
                ? fallbackCharacterId
                : suggestion.CharacterId;

            switch (suggestion.Type)
            {
                case "setEmotion" when !string.IsNullOrWhiteSpace(suggestion.Emotion) &&
                                       AllowedEmotions.Contains(suggestion.Emotion):
                    yield return new SetEmotionCommand
                    {
                        CharacterId = characterId,
                        EmotionKey = suggestion.Emotion
                    };
                    break;

                case "playAnimation" when !string.IsNullOrWhiteSpace(suggestion.Animation) &&
                                          AllowedAnimations.Contains(suggestion.Animation):
                    yield return new PlayAnimationCommand
                    {
                        CharacterId = characterId,
                        AnimationKey = suggestion.Animation
                    };
                    break;

                case "triggerStageEvent" when !string.IsNullOrWhiteSpace(suggestion.StageEvent):
                    var command = BuildStageEventCommand(characterId, suggestion.StageEvent);
                    if (command is not null)
                    {
                        yield return command;
                    }
                    break;
            }
        }
    }

    private static IEnumerable<ExhibitionCommand> BuildRuleCommands(
        string message,
        string characterId,
        IReadOnlyList<ExhibitionKnowledgeDocument> sources)
    {
        var lower = message.ToLowerInvariant();
        var primary = sources.FirstOrDefault();

        if (IsSceneControlQuestion(message) && (!IsCapabilityQuestion(lower) || IsExplicitSceneChangeRequest(lower)))
        {
            var requestedStageEvent = DetectStageEventV2(lower) ?? DetectStageEvent(lower);
            if (!string.IsNullOrWhiteSpace(requestedStageEvent))
            {
                var command = BuildStageEventCommand(characterId, requestedStageEvent);
                if (command is not null)
                {
                    yield return command;
                }
            }

            yield break;
        }

        if (IsCatalogQuestion(message))
        {
            yield return new PlayAnimationCommand
            {
                CharacterId = characterId,
                AnimationKey = "explain"
            };

            yield break;
        }

        var emotion = DetectEmotion(lower) ?? primary?.RecommendedEmotion;
        if (!string.IsNullOrWhiteSpace(emotion) && AllowedEmotions.Contains(emotion))
        {
            yield return new SetEmotionCommand
            {
                CharacterId = characterId,
                EmotionKey = emotion
            };
        }

        var animation = DetectAnimation(lower) ?? primary?.RecommendedAnimation;
        if (!string.IsNullOrWhiteSpace(animation) && AllowedAnimations.Contains(animation))
        {
            yield return new PlayAnimationCommand
            {
                CharacterId = characterId,
                AnimationKey = animation
            };
        }

        var stageEvent = DetectStageEventV2(lower) ?? DetectStageEvent(lower) ?? primary?.RecommendedStageEvent;
        if (!string.IsNullOrWhiteSpace(stageEvent))
        {
            var command = BuildStageEventCommand(characterId, stageEvent);
            if (command is not null)
            {
                yield return command;
            }
        }
    }

    private static IReadOnlyList<ExhibitionCommand> CompleteCommands(
        IEnumerable<ExhibitionCommand> baseCommands,
        string message,
        string characterId,
        IReadOnlyList<ExhibitionKnowledgeDocument> sources)
    {
        var commands = baseCommands.ToList();

        foreach (var command in BuildDeterministicCommands(message, characterId, sources))
        {
            if (!commands.Any(existing => IsSameCommand(existing, command)))
            {
                commands.Add(command);
            }
        }

        // 항상 표정 하나는 포함 — setEmotion이 없으면 default 추가
        if (!commands.OfType<SetEmotionCommand>().Any())
        {
            commands.Insert(0, new SetEmotionCommand
            {
                CharacterId = characterId,
                EmotionKey = PickDefaultEmotion(message)
            });
        }

        return commands.Take(MaxCommands).ToArray();
    }

    private static string PickDefaultEmotion(string message)
    {
        var lower = message.ToLowerInvariant();
        if (ContainsAny(lower, "안녕", "반가", "처음", "hello", "hi")) return "greeting";
        if (IsSceneControlQuestion(message))                              return "neutral";
        return "explaining";
    }

    private static IEnumerable<ExhibitionCommand> BuildDeterministicCommands(
        string message,
        string characterId,
        IReadOnlyList<ExhibitionKnowledgeDocument> sources)
    {
        var lower = message.ToLowerInvariant();

        if (!IsCapabilityQuestion(lower) || IsExplicitSceneChangeRequest(lower))
        {
            var stageEvent = DetectStageEventV2(lower) ?? DetectStageEvent(lower);
            if (!string.IsNullOrWhiteSpace(stageEvent))
            {
                var stageCommand = BuildStageEventCommand(characterId, stageEvent);
                if (stageCommand is not null)
                {
                    yield return stageCommand;
                }
            }
        }

        if ((IsExplainRequest(message) || IsCatalogQuestion(message)) && sources.Count > 0)
        {
            yield return new PlayAnimationCommand
            {
                CharacterId = characterId,
                AnimationKey = "explain"
            };
        }
    }

    private static bool IsSameCommand(ExhibitionCommand left, ExhibitionCommand right)
    {
        if (left.GetType() != right.GetType())
        {
            return false;
        }

        return (left, right) switch
        {
            (SetEmotionCommand l, SetEmotionCommand r) =>
                string.Equals(l.CharacterId, r.CharacterId, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(l.EmotionKey, r.EmotionKey, StringComparison.OrdinalIgnoreCase),
            (PlayAnimationCommand l, PlayAnimationCommand r) =>
                string.Equals(l.CharacterId, r.CharacterId, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(l.AnimationKey, r.AnimationKey, StringComparison.OrdinalIgnoreCase),
            (TriggerStageEventCommand l, TriggerStageEventCommand r) =>
                string.Equals(l.CharacterId, r.CharacterId, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(l.StageEventKey, r.StageEventKey, StringComparison.OrdinalIgnoreCase),
            (SwitchStyleCommand l, SwitchStyleCommand r) =>
                string.Equals(l.CharacterId, r.CharacterId, StringComparison.OrdinalIgnoreCase) &&
                l.StyleIndex == r.StyleIndex,
            _ => false
        };
    }

    private static IReadOnlyList<AiSuggestedCommandDto> ToSuggestions(
        IReadOnlyList<ExhibitionCommand> commands)
    {
        return commands.Select(command => command switch
        {
            SetEmotionCommand c => new AiSuggestedCommandDto
            {
                Type = "setEmotion",
                CharacterId = c.CharacterId,
                Emotion = c.EmotionKey
            },
            PlayAnimationCommand c => new AiSuggestedCommandDto
            {
                Type = "playAnimation",
                CharacterId = c.CharacterId,
                Animation = c.AnimationKey
            },
            TriggerStageEventCommand c => new AiSuggestedCommandDto
            {
                Type = "triggerStageEvent",
                CharacterId = c.CharacterId,
                StageEvent = c.StageEventKey
            },
            SwitchStyleCommand c => new AiSuggestedCommandDto
            {
                Type = "triggerStageEvent",
                CharacterId = c.CharacterId,
                StageEvent = StyleIndexMap.FirstOrDefault(x => x.Value == c.StyleIndex).Key
            },
            _ => new AiSuggestedCommandDto
            {
                Type = command.GetType().Name,
                CharacterId = command.CharacterId
            }
        }).ToArray();
    }

    private static ExhibitionCommand? BuildStageEventCommand(string characterId, string stageEvent)
    {
        if (StyleIndexMap.TryGetValue(stageEvent, out var styleIndex))
        {
            return new SwitchStyleCommand
            {
                CharacterId = characterId,
                StyleIndex = styleIndex
            };
        }

        if (AllowedStageEvents.Contains(stageEvent))
        {
            return new TriggerStageEventCommand
            {
                CharacterId = characterId,
                StageEventKey = stageEvent
            };
        }

        return null;
    }

    private static string BuildRuleReply(
        string message,
        IReadOnlyList<ExhibitionKnowledgeDocument> sources,
        IReadOnlyList<ExhibitionCommand> commands)
    {
        if (IsCatalogQuestion(message) && sources.Count > 0)
        {
            var titles = string.Join(", ", sources.Select(source => source.Title));
            return $"현재 전시장에는 {titles} 전시물이 있습니다. 궁금한 작품 이름을 말하면 더 자세히 안내할게요.";
        }

        if (IsSceneControlQuestion(message))
        {
            return commands.Count > 0
                ? "요청한 전시장 분위기 변경을 적용했습니다. Classic, Aged, Modern 분위기와 조명 연출을 채팅으로 바꿀 수 있습니다."
                : "네, 채팅으로 Classic, Aged, Modern 분위기와 조명 연출을 바꿀 수 있습니다. 예를 들어 '클래식 분위기로 바꿔줘'처럼 요청하면 됩니다.";
        }

        if (sources.Count == 0)
        {
            return commands.Count > 0
                ? "요청한 전시 연출 명령을 적용했습니다."
                : "아직 연결된 전시 정보가 부족합니다. 작품 설명이나 분위기 변경을 다시 요청해 주세요.";
        }

        var primary = sources[0];
        var commandNote = commands.Count > 0
            ? " 요청한 분위기와 캐릭터 연출도 함께 적용했습니다."
            : string.Empty;

        if (IsExplainRequest(message))
        {
            return $"{primary.Title}은 {primary.Summary} {primary.Description}{commandNote}";
        }

        return $"{primary.Title} 전시를 기준으로 안내할게요. {primary.Description}{commandNote}";
    }

    private static string? DetectEmotion(string lower)
    {
        // 인사/환영 → greeting
        if (ContainsAny(lower, "안녕", "반가", "처음", "환영", "hello", "hi", "greet", "welcome")) return "greeting";
        // 설명/안내 → explaining
        if (ContainsAny(lower, "설명", "소개", "알려", "말해", "뭐야", "뭐지", "describe", "explain", "tell", "what is")) return "explaining";
        // 궁금/탐색 → curious
        if (ContainsAny(lower, "궁금", "신기", "흥미", "어떻게", "왜", "정말", "진짜", "대박", "신비", "wow", "amazing", "curious", "wonder")) return "curious";
        // 생각/분석 → thinking
        if (ContainsAny(lower, "생각", "분석", "비교", "차이", "의미", "역사", "배경", "hmm", "think", "analyze", "compare")) return "thinking";
        // 즐거움/흥분 → happy
        if (ContainsAny(lower, "밝", "신나", "즐거", "기뻐", "행복", "활기", "설레", "흥겨", "유쾌", "재밌", "재미있", "happy", "joy", "excited", "fun")) return "happy";
        // 놀람 → surprise
        if (ContainsAny(lower, "놀", "깜짝", "충격", "뜻밖", "surprise", "shocked", "incredible")) return "surprise";
        // 슬픔/차분 → sad
        if (ContainsAny(lower, "슬", "우울", "쓸쓸", "외로", "차분", "잔잔", "고요", "sad", "calm", "quiet")) return "sad";
        // 강렬/긴장 → angry
        if (ContainsAny(lower, "화", "강렬", "분노", "긴장", "엄숙", "격렬", "강하", "angry", "intense", "dramatic")) return "angry";
        return null;
    }

    private static string? DetectAnimation(string lower)
    {
        if (ContainsAny(lower, "인사", "반가", "안녕", "하이", "손흔", "wave", "hello", "hi", "greet")) return "wave";
        if (ContainsAny(lower, "고개", "숙", "절", "bow")) return "bow";
        if (ContainsAny(lower, "박수", "clap")) return "clap";
        if (ContainsAny(lower, "설명", "소개", "알려", "말해", "describe", "explain", "tell", "guide")) return "explain";
        return null;
    }

    private static string? DetectStageEvent(string lower)
    {
        if (ContainsAny(lower, "클래식", "고전", "전통", "classic")) return "style_classic";
        if (ContainsAny(lower, "오래", "낡", "빈티지", "앤틱", "aged")) return "style_aged";
        if (ContainsAny(lower, "현대", "모던", "미래", "깔끔", "modern")) return "style_modern";
        if (ContainsAny(lower, "조명 켜", "불 켜", "밝게", "spot on")) return "light.spotOn";
        if (ContainsAny(lower, "조명 꺼", "불 꺼", "어둡게", "spot off")) return "light.spotOff";
        return null;
    }

    private static bool IsExplainRequest(string message) =>
        IsExplainRequestV2(message) ||
        message.Contains("설명", StringComparison.OrdinalIgnoreCase) ||
        message.Contains("소개", StringComparison.OrdinalIgnoreCase) ||
        message.Contains("guide", StringComparison.OrdinalIgnoreCase);

    private static string? DetectStageEventV2(string lower)
    {
        if (ContainsAny(lower, "클래식", "고전", "classic")) return "style_classic";
        if (ContainsAny(lower, "오래", "낡", "빈티지", "앤틱", "aged")) return "style_aged";
        if (ContainsAny(lower, "현대", "모던", "미래", "modern")) return "style_modern";
        if (ContainsAny(lower, "조명 켜", "불 켜", "밝게", "spot on")) return "light.spotOn";
        if (ContainsAny(lower, "조명 꺼", "불 꺼", "어둡게", "spot off")) return "light.spotOff";
        return null;
    }

    private static bool IsExplainRequestV2(string message) =>
        ContainsAny(
            message,
            "설명",
            "소개",
            "알려",
            "뭐야",
            "what",
            "tell",
            "explain");

    private static bool IsCatalogQuestion(string message) =>
        ContainsAny(
            message.ToLowerInvariant(),
            "전시물",
            "전시품",
            "작품 목록",
            "작품 종류",
            "종류 뭐",
            "뭐 있어",
            "뭐있어",
            "목록",
            "리스트",
            "what exhibits",
            "what artifacts",
            "list exhibits");

    private static bool IsSceneControlQuestion(string message) =>
        ContainsAny(
            message.ToLowerInvariant(),
            "분위기",
            "무드",
            "조명",
            "연출",
            "classic",
            "aged",
            "modern",
            "mood",
            "lighting");

    private static bool IsCapabilityQuestion(string lower) =>
        ContainsAny(lower, "할 수 있어", "가능", "되냐", "돼", "can you", "possible");

    private static bool IsExplicitSceneChangeRequest(string lower) =>
        ContainsAny(lower, "바꿔", "변경", "해줘", "켜", "꺼", "switch", "change", "set");

    private static bool ContainsAny(string text, params string[] values) =>
        values.Any(value => text.Contains(value, StringComparison.OrdinalIgnoreCase));
}
