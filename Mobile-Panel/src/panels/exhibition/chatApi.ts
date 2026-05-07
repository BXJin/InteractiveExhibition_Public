export interface ChatCommand {
  type: string;
  characterId?: string;
  emotionKey?: string;
  animationKey?: string;
  stageEventKey?: string;
  styleIndex?: number;
}

export interface ChatResponse {
  reply: string;
  commands: ChatCommand[];
  retrievedSources: string[];
  success: boolean;
  error?: string;
  latencyMs: number;
}

export type ChatTransportMode = 'stream' | 'json' | 'auto';

export interface AiSuggestedCommand {
  type: string;
  characterId?: string;
  emotion?: string;
  animation?: string;
  stageEvent?: string;
}

export interface AiChatCompleteResponse {
  reply: string;
  suggestedCommands: AiSuggestedCommand[];
  success: boolean;
  latencyMs: number;
  provider?: string;
  model?: string;
  errorCode?: string;
}

export interface AiChatStreamEvent {
  eventType: 'delta' | 'complete' | 'error' | string;
  text?: string;
  completeResponse?: AiChatCompleteResponse;
  errorCode?: string;
  message?: string;
}

export interface ChatStreamHandlers {
  onDelta?: (text: string) => void;
  onComplete?: (response: AiChatCompleteResponse) => void;
  onError?: (errorCode: string, message: string) => void;
}

export async function sendGuideChat(
  baseUrl: string,
  message: string,
  conversationId?: string,
): Promise<ChatResponse> {
  const response = await fetch(`${baseUrl.replace(/\/$/, '')}/api/chat`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
      message,
      conversationId,
      characterId: 'Character_01',
      mode: 'guide',
    }),
  });

  const payload = (await response.json()) as ChatResponse;
  if (!response.ok) {
    throw new Error(payload.error || payload.reply || `Chat request failed: ${response.status}`);
  }

  return payload;
}

export function resolveChatTransportMode(
  message: string,
  mode: ChatTransportMode,
): Exclude<ChatTransportMode, 'auto'> {
  if (mode !== 'auto') {
    return mode;
  }

  const normalized = message.trim().toLowerCase();
  if (normalized.length <= 20) {
    return 'json';
  }

  if (/(안녕|하이|hello|hi|웃|인사|wave|happy|bow|clap|고개|박수)/i.test(normalized)) {
    return 'json';
  }

  return 'stream';
}

export async function sendGuideChatStream(
  baseUrl: string,
  message: string,
  conversationId: string | undefined,
  handlers: ChatStreamHandlers,
): Promise<void> {
  const response = await fetch(`${baseUrl.replace(/\/$/, '')}/api/chat/stream`, {
    method: 'POST',
    headers: {
      Accept: 'text/event-stream',
      'Content-Type': 'application/json',
    },
    body: JSON.stringify({
      message,
      conversationId,
      characterId: 'Character_01',
      mode: 'guide',
    }),
  });

  if (!response.ok || !response.body) {
    throw new Error(`Chat stream request failed: ${response.status}`);
  }

  const reader = response.body.getReader();
  const decoder = new TextDecoder();
  let buffer = '';

  while (true) {
    const { done, value } = await reader.read();
    if (done) {
      break;
    }

    buffer += decoder.decode(value, { stream: true });
    const frames = buffer.split(/\r?\n\r?\n/);
    buffer = frames.pop() ?? '';

    for (const frame of frames) {
      handleSseFrame(frame, handlers);
    }
  }

  if (buffer.trim().length > 0) {
    handleSseFrame(buffer, handlers);
  }
}

function handleSseFrame(frame: string, handlers: ChatStreamHandlers): void {
  const data = frame
    .split(/\r?\n/)
    .filter(line => line.startsWith('data:'))
    .map(line => line.slice('data:'.length).trim())
    .join('');

  if (!data) {
    return;
  }

  let streamEvent: AiChatStreamEvent;
  try {
    streamEvent = JSON.parse(data) as AiChatStreamEvent;
  } catch {
    handlers.onError?.('STREAM_PARSE_FAILED', 'Failed to parse chat stream event.');
    return;
  }

  if (streamEvent.eventType === 'delta' && streamEvent.text) {
    handlers.onDelta?.(streamEvent.text);
    return;
  }

  if (streamEvent.eventType === 'complete' && streamEvent.completeResponse) {
    handlers.onComplete?.(streamEvent.completeResponse);
    return;
  }

  if (streamEvent.eventType === 'error') {
    handlers.onError?.(
      streamEvent.errorCode ?? 'STREAM_ERROR',
      streamEvent.message ?? 'Chat stream failed.',
    );
  }
}
