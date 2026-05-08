/**
 * TTS 오디오 URL을 순서대로 재생하는 큐 플레이어.
 * 서버에서 audioUrl을 받으면 enqueue() 호출.
 * 모든 오디오 재생이 끝나면 onComplete 콜백 실행.
 *
 * AudioContext 기반: 모바일 브라우저에서 안정적인 오디오 재생.
 *
 * 프리패치 전략:
 *   enqueue() 시점에 즉시 fetch + decodeAudioData 시작.
 *   재생 차례가 됐을 때 AudioBuffer가 이미 준비되어 있으므로
 *   청크 간 무음 갭이 제거된다.
 *
 * onComplete 발동 조건:
 *   finalize()가 호출된 이후(SSE 스트림 종료) + 큐가 비어 있을 때만 발동.
 *   Ack 오디오가 짧게 끝나도 TTS 청크가 오는 동안은 onComplete가 일찍 발동하지 않는다.
 */
export class TtsQueuePlayer {
  private readonly queue: string[] = [];
  private readonly prefetchMap = new Map<string, Promise<AudioBuffer | null>>();
  private playing = false;
  private streamEnded = false;
  private context: AudioContext | null = null;
  private readonly serverBaseUrl: string;

  onComplete: (() => void) | null = null;

  constructor(serverBaseUrl: string) {
    this.serverBaseUrl = serverBaseUrl;
  }

  /** 오디오 URL을 큐에 추가하고 즉시 프리패치를 시작한다. */
  enqueue(audioUrl: string): void {
    this.queue.push(audioUrl);
    this.startPrefetch(audioUrl);
    if (!this.playing) {
      void this.playNext();
    }
  }

  /**
   * SSE 스트림 종료를 알린다.
   * 이 시점 이후로 큐가 비면 onComplete를 호출한다.
   * 큐가 이미 비어 있고 재생 중도 아니면 즉시 호출한다.
   */
  finalize(): void {
    this.streamEnded = true;
    if (!this.playing && this.queue.length === 0) {
      this.onComplete?.();
    }
  }

  /** 큐와 재생을 즉시 중단한다. */
  stop(): void {
    this.queue.length = 0;
    this.prefetchMap.clear();
    this.playing = false;
    this.streamEnded = false;
    this.context?.close();
    this.context = null;
  }

  // ── 내부 ────────────────────────────────────────────────────────────────

  private startPrefetch(relativeUrl: string): void {
    if (this.prefetchMap.has(relativeUrl)) return;
    this.prefetchMap.set(relativeUrl, this.fetchAndDecode(relativeUrl));
  }

  private async fetchAndDecode(relativeUrl: string): Promise<AudioBuffer | null> {
    try {
      const fullUrl = `${this.serverBaseUrl}${relativeUrl}`;
      const response = await fetch(fullUrl);
      if (!response.ok) throw new Error(`fetch failed: ${response.status}`);

      const arrayBuffer = await response.arrayBuffer();
      const ctx = this.ensureContext();
      return await ctx.decodeAudioData(arrayBuffer);
    } catch (err) {
      console.warn('[TtsQueuePlayer] prefetch failed:', err);
      return null;
    }
  }

  private async playNext(): Promise<void> {
    if (this.queue.length === 0) {
      this.playing = false;
      // streamEnded 이후에만 onComplete 발동
      if (this.streamEnded) {
        this.onComplete?.();
      }
      return;
    }

    this.playing = true;
    const url = this.queue.shift()!;

    try {
      const buffer = await (this.prefetchMap.get(url) ?? this.fetchAndDecode(url));
      this.prefetchMap.delete(url);

      if (buffer) {
        await this.playBuffer(buffer);
      }
    } catch (err) {
      console.warn('[TtsQueuePlayer] playback failed, skipping:', err);
    }

    void this.playNext();
  }

  private playBuffer(buffer: AudioBuffer): Promise<void> {
    const ctx = this.ensureContext();
    return new Promise((resolve) => {
      const source = ctx.createBufferSource();
      source.buffer = buffer;
      source.connect(ctx.destination);
      source.onended = () => resolve();
      source.start(0);
    });
  }

  private ensureContext(): AudioContext {
    if (!this.context || this.context.state === 'closed') {
      this.context = new AudioContext();
    }
    return this.context;
  }
}
