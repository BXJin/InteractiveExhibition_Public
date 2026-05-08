export interface VoiceChatHandlers {
  onAck: (audioUrl: string | null, text: string) => void;
  onTranscript: (text: string) => void;
  onTextChunk: (text: string) => void;
  onTtsChunk: (audioUrl: string, text: string) => void;
  onDone: () => void;
  onError: (errorCode: string, message: string) => void;
}

interface VoiceStreamEvent {
  type: string;
  text?: string;
  audioUrl?: string;
  errorCode?: string;
}

/**
 * POST /api/voice/chat — 음성 채팅 SSE 스트리밍.
 *
 * EventSource 대신 fetch + ReadableStream 사용:
 * POST body에 audio blob을 실어야 하므로 EventSource로는 불가.
 */
export async function streamVoiceChat(
  serverUrl: string,
  audioBlob: Blob,
  conversationId: string,
  characterId: string,
  handlers: VoiceChatHandlers,
  signal?: AbortSignal,
): Promise<void> {
  const form = new FormData();

  // blob.name이 없는 경우 MIME type으로 파일명 추론
  const fileName = (audioBlob as File).name || mimeToFileName(audioBlob.type);
  form.append('file', audioBlob, fileName);
  form.append('conversationId', conversationId);
  form.append('characterId', characterId);

  const response = await fetch(`${serverUrl}/api/voice/chat`, {
    method: 'POST',
    body: form,
    signal,
  });

  if (!response.ok || !response.body) {
    handlers.onError('VOICE_CHAT_REQUEST_FAILED', `서버 응답 오류: ${response.status}`);
    return;
  }

  const reader = response.body.getReader();
  const decoder = new TextDecoder();
  let buffer = '';

  // eslint-disable-next-line no-constant-condition
  while (true) {
    const { done, value } = await reader.read();
    if (done) break;

    buffer += decoder.decode(value, { stream: true });

    // SSE 이벤트는 \n\n 으로 구분
    const parts = buffer.split('\n\n');
    buffer = parts.pop() ?? '';

    for (const part of parts) {
      const event = parseSseBlock(part);
      if (!event) continue;
      dispatchEvent(event, handlers);
    }
  }

  // 마지막 버퍼 처리
  if (buffer.trim()) {
    const event = parseSseBlock(buffer);
    if (event) dispatchEvent(event, handlers);
  }
}

/**
 * 패널 TTS 큐 재생 완료 시 서버에 알린다.
 * 서버 → UE: playAnimation(explain, loop:false)
 */
export async function notifySpeakingComplete(
  serverUrl: string,
  characterId: string,
): Promise<void> {
  try {
    await fetch(`${serverUrl}/api/voice/speaking-complete`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ characterId }),
    });
  } catch {
    // 실패해도 UX에 영향 없음
  }
}

// ── 내부 헬퍼 ────────────────────────────────────────────────────────────────

function parseSseBlock(block: string): VoiceStreamEvent | null {
  let data = '';

  for (const line of block.split('\n')) {
    if (line.startsWith('data:')) {
      data = line.slice('data:'.length).trim();
    }
  }

  if (!data) return null;

  try {
    return JSON.parse(data) as VoiceStreamEvent;
  } catch {
    return null;
  }
}

function dispatchEvent(event: VoiceStreamEvent, handlers: VoiceChatHandlers): void {
  switch (event.type) {
    case 'ack':
      handlers.onAck(event.audioUrl ?? null, event.text ?? '');
      break;
    case 'transcript':
      handlers.onTranscript(event.text ?? '');
      break;
    case 'text_chunk':
      handlers.onTextChunk(event.text ?? '');
      break;
    case 'tts_chunk':
      if (event.audioUrl) {
        handlers.onTtsChunk(event.audioUrl, event.text ?? '');
      }
      break;
    case 'done':
      handlers.onDone();
      break;
    case 'error':
      handlers.onError(event.errorCode ?? 'UNKNOWN', event.text ?? '오류가 발생했습니다.');
      break;
  }
}

function mimeToFileName(mime: string): string {
  if (mime.startsWith('audio/webm')) return 'audio.webm';
  if (mime.startsWith('audio/mp4'))  return 'audio.mp4';
  if (mime.startsWith('audio/wav'))  return 'audio.wav';
  return 'audio.webm';
}
