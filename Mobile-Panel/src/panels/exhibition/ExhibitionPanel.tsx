import React, { useState, useCallback, useRef, useEffect } from 'react';
import { motion, AnimatePresence } from 'motion/react';
import {
  Smile, Frown, Zap, Ghost,
  Clock, Crown, Layers, Moon, Sparkles, Wrench,
  Settings, X, Smartphone,
  RotateCcw, Play, MessageCircle, Send,
} from 'lucide-react';

import {
  Joystick,
  TouchPad,
  ActionButton,
  MicButton,
  ControllerShell,
  StatusBar,
  SideMenu,
  useTransport,
  useConnectionState,
  useThrottle,
  useVoiceRecorder,
} from '../../core';
import type { ConnectionStatus } from '../../core';
import type { SendResult } from '../../core/transport/types';
import { TtsQueuePlayer } from '../../core/audio/TtsQueuePlayer';
import { ExhibitionCommands } from './commands';
import {
  resolveChatTransportMode,
  sendGuideChat,
  sendGuideChatStream,
} from './chatApi';
import type { ChatResponse, ChatTransportMode } from './chatApi';
import { streamVoiceChat, notifySpeakingComplete } from './voiceApi';

// ─────────────────────────────────────
// Constants
// ─────────────────────────────────────

const STORAGE_KEY    = 'exhibition_server_url';
const CHAT_CONVERSATION_KEY = 'exhibition_chat_conversation_id';
const CHARACTER_ID   = 'Character_01';
const DEFAULT_URL    = `${window.location.protocol}//${window.location.hostname}:${window.location.port || (window.location.protocol === 'https:' ? 7225 : 5225)}`;
const THROTTLE_MS    = 80;  // 조이스틱/터치패드 전송 간격
const FEEDBACK_MS    = 1500; // 버튼 피드백 표시 시간

function getStoredUrl(): string {
  try {
    const stored = localStorage.getItem(STORAGE_KEY);
    if (!stored) {
      return DEFAULT_URL;
    }

    const url = new URL(stored);
    const isRemotePanel = window.location.hostname !== 'localhost' && window.location.hostname !== '127.0.0.1';
    const isStoredLocalhost = url.hostname === 'localhost' || url.hostname === '127.0.0.1';
    if (isRemotePanel && isStoredLocalhost) {
      localStorage.setItem(STORAGE_KEY, DEFAULT_URL);
      return DEFAULT_URL;
    }

    return stored;
  }
  catch { return DEFAULT_URL; }
}

function getConversationId(): string {
  try {
    const existing = localStorage.getItem(CHAT_CONVERSATION_KEY);
    if (existing) {
      return existing;
    }

    const next = `panel-${Date.now()}-${Math.random().toString(36).slice(2)}`;
    localStorage.setItem(CHAT_CONVERSATION_KEY, next);
    return next;
  } catch {
    return `panel-${Date.now()}-${Math.random().toString(36).slice(2)}`;
  }
}

// ─────────────────────────────────────
// Section helper
// ─────────────────────────────────────

function Section({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <div className="space-y-2">
      <div className="text-[9px] font-mono uppercase tracking-[0.2em] text-white/25 px-0.5">{title}</div>
      {children}
    </div>
  );
}

// ─────────────────────────────────────
// Last result banner
// ─────────────────────────────────────

interface FeedbackBanner {
  label: string;
  ok: boolean;
  unrealConnections?: number;
  errorMsg?: string;
}

interface ChatMessage {
  id?: string;
  role: 'user' | 'assistant';
  text: string;
  sources?: string[];
  commandCount?: number;
  provider?: string;
  model?: string;
  mode?: 'stream' | 'json';
  streaming?: boolean;
}

function ResultBanner({ banner }: { banner: FeedbackBanner | null }) {
  return (
    <AnimatePresence>
      {banner && (
        <motion.div
          key={banner.label + banner.ok}
          initial={{ opacity: 0, y: 8 }}
          animate={{ opacity: 1, y: 0 }}
          exit={{ opacity: 0 }}
          className={`absolute bottom-4 left-4 right-10 px-4 py-2 rounded-xl text-[10px] font-mono flex items-center justify-between gap-2 pointer-events-none
            ${banner.ok
              ? 'bg-emerald-500/10 border border-emerald-500/15 text-emerald-400'
              : 'bg-red-500/10 border border-red-500/15 text-red-400'}`}
        >
          <span className="truncate">
            {banner.ok ? `✓ ${banner.label}` : `✗ ${banner.label} — ${banner.errorMsg}`}
          </span>
          {banner.ok && banner.unrealConnections !== undefined && (
            <span className="shrink-0 text-white/20">
              UE {banner.unrealConnections > 0 ? banner.unrealConnections : '—'}
            </span>
          )}
        </motion.div>
      )}
    </AnimatePresence>
  );
}

// ─────────────────────────────────────
// Exhibition Panel
// ─────────────────────────────────────

export const ExhibitionPanel: React.FC = () => {
  const transport = useTransport();
  const connState = useConnectionState();

  // UI state — 연결 상태를 ConnectionStatus로 매핑
  const [status, setStatus]     = useState<ConnectionStatus>('ready');
  const [banner, setBanner]     = useState<FeedbackBanner | null>(null);
  const [activeBtn, setActiveBtn] = useState<string | null>(null);
  const [sideOpen, setSideOpen] = useState(false);
  const [chatOpen, setChatOpen] = useState(false);
  const [chatInput, setChatInput] = useState('');
  const [chatBusy, setChatBusy] = useState(false);
  const [chatTransportMode, setChatTransportMode] = useState<ChatTransportMode>('stream');
  const [chatMessages, setChatMessages] = useState<ChatMessage[]>([
    {
      role: 'assistant',
      text: '안녕하세요! 저는 이 전시장의 AI 가이드예요 👋\n\n현재 전시 중인 작품:\n• 트리케라톱스 호리두스 (자연사)\n• 벨 X-1 (항공 역사)\n• 청동 의례용 술그릇 방이 (고대 중국)\n\n전시물 설명, 분위기 연출(클래식·빈티지·모던), 조명 제어, 캐릭터 감정 표현 등을 할 수 있어요. 궁금한 게 있으면 무엇이든 물어봐요!',
    },
  ]);
  const [showSettings, setShowSettings] = useState(false);
  const [serverUrl, setServerUrl] = useState(getStoredUrl);
  const [urlDraft, setUrlDraft] = useState(serverUrl);

  const [voiceBusy, setVoiceBusy] = useState(false);

  const timerRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const conversationIdRef = useRef(getConversationId());
  const ttsPlayerRef = useRef<TtsQueuePlayer | null>(null);
  const voiceAbortRef = useRef<AbortController | null>(null);
  useEffect(() => () => { if (timerRef.current) clearTimeout(timerRef.current); }, []);

  const { isRecording, start: startRecording, stop: stopRecording, permissionDenied } = useVoiceRecorder();

  // ── Settings ──
  const saveUrl = () => {
    const trimmed = urlDraft.trim().replace(/\/$/, '');
    setServerUrl(trimmed);
    try { localStorage.setItem(STORAGE_KEY, trimmed); } catch { /* no-op */ }
    setShowSettings(false);
    // URL이 바뀌면 App에서 TransportProvider를 리빌드해야 함
    // → window.location.reload()가 가장 단순. 더 우아한 방법은 App 레벨 state 사용.
    window.location.reload();
  };

  // ── Button command with feedback ──
  const sendButton = useCallback(async (
    payload: Record<string, unknown>,
    label: string,
    key: string,
  ) => {
    if (status === 'sending') return;
    setActiveBtn(key);
    setStatus('sending');
    if (timerRef.current) clearTimeout(timerRef.current);

    const result: SendResult = await transport.send(payload);

    if (result.ok) {
      setStatus('ok');
      setBanner({
        label,
        ok: true,
        unrealConnections: (result.data as Record<string, number> | undefined)?.unrealConnections,
      });
    } else {
      setStatus('error');
      setBanner({ label, ok: false, errorMsg: result.error });
    }

    timerRef.current = setTimeout(() => {
      setStatus('ready');
      setActiveBtn(null);
    }, FEEDBACK_MS);
  }, [transport, status]);

  // ── Throttled continuous inputs ──

  // [이동] onChange 기반 throttle 대신 setInterval 사용.
  // onChange는 값이 바뀔 때만 호출되므로 조이스틱을 고정하면 전송이 멈춤.
  // → 서버의 dead-man's-switch 타임아웃에 걸려 캐릭터가 멈추는 버그 발생.
  // setInterval은 누르고 있는 동안 THROTTLE_MS마다 항상 전송을 보장.
  const joystickRef = useRef({ x: 0, y: 0, active: false });

  useEffect(() => {
    const id = setInterval(() => {
      const { x, y, active } = joystickRef.current;
      if (active) {
        transport.sendRaw(ExhibitionCommands.moveDirection(x, -y)); // y축 반전
      }
    }, THROTTLE_MS);
    return () => clearInterval(id);
  }, [transport]);

  const [throttledRotate] = useThrottle((dx: number, dy: number) => {
    transport.sendRaw(ExhibitionCommands.rotate(-dy * 0.08, dx * 0.18));
  }, THROTTLE_MS);

  // ── Shorthand senders ──
  const emotion    = (key: string, label: string) => {
    transport.sendRaw(ExhibitionCommands.setEmotion(key));       // face only
    sendButton(ExhibitionCommands.playAnimation(key), label, `emotion_${key}`); // body + UI feedback
  };
  const animate    = (key: string, label: string) => sendButton(ExhibitionCommands.playAnimation(key),         label, `anim_${key}`);
  const stageEvt   = (key: string, label: string) => sendButton(ExhibitionCommands.triggerStageEvent(key),     label, `stage_${key}`);
  const style      = (idx: 0 | 1 | 2 | 3, label: string) => sendButton(ExhibitionCommands.switchStyle(idx),       label, `style_${idx}`);

  const submitChat = useCallback(async (messageOverride?: string) => {
    const message = (messageOverride ?? chatInput).trim();
    if (!message || chatBusy) return;

    setChatInput('');
    setChatBusy(true);
    setStatus('sending');

    try {
      const effectiveMode = resolveChatTransportMode(message, chatTransportMode);
      let commandCount = 0;
      if (effectiveMode === 'json') {
        setChatMessages(prev => [...prev, { role: 'user', text: message }]);

        const response: ChatResponse = await sendGuideChat(
          serverUrl,
          message,
          conversationIdRef.current,
        );
        setChatMessages(prev => [
          ...prev,
          {
            role: 'assistant',
            text: response.reply,
            sources: response.retrievedSources,
            commandCount: response.commands?.length ?? 0,
            mode: 'json',
          },
        ]);
        setBanner({
          label: `Guide JSON ${response.commands?.length ?? 0} cmd`,
          ok: true,
        });
        commandCount = response.commands?.length ?? 0;
      } else {
        const assistantId = `assistant-${Date.now()}-${Math.random().toString(36).slice(2)}`;

        setChatMessages(prev => [
          ...prev,
          { role: 'user', text: message },
          { id: assistantId, role: 'assistant', text: '', mode: 'stream', streaming: true },
        ]);

        let streamError: string | null = null;

        await sendGuideChatStream(serverUrl, message, conversationIdRef.current, {
          onDelta: text => {
            setChatMessages(prev => prev.map(item => (
              item.id === assistantId
                ? { ...item, text: `${item.text}${text}` }
                : item
            )));
          },
          onComplete: response => {
            setChatMessages(prev => prev.map(item => (
              item.id === assistantId
                ? {
                    ...item,
                    // 스트리밍으로 이미 구성된 텍스트를 우선 사용.
                    // response.reply는 스트리밍이 비었을 때만 fallback.
                    text: item.text || response.reply,
                    commandCount: response.suggestedCommands?.length ?? 0,
                    provider: response.provider,
                    model: response.model,
                    streaming: false,
                  }
                : item
            )));
            commandCount = response.suggestedCommands?.length ?? 0;
          },
          onError: (_errorCode, errorMessage) => {
            streamError = errorMessage;
            setChatMessages(prev => prev.map(item => (
              item.id === assistantId
                ? { ...item, text: errorMessage, streaming: false }
                : item
            )));
          },
        });

        if (streamError) {
          throw new Error(streamError);
        }
      }
      setStatus('ok');
      setBanner({
        label: `Guide · ${commandCount} cmd`,
        ok: true,
      });
    } catch (error) {
      const errorMsg = error instanceof Error ? error.message : String(error);
      setStatus('error');
      setBanner({ label: 'Guide', ok: false, errorMsg });
      setChatMessages(prev => [...prev, { role: 'assistant', text: errorMsg }]);
    } finally {
      setChatBusy(false);
      timerRef.current = setTimeout(() => {
        setStatus('ready');
        setActiveBtn(null);
      }, FEEDBACK_MS);
    }
  }, [chatBusy, chatInput, chatTransportMode, serverUrl]);

  const submitVoiceChat = useCallback(async (blob: Blob) => {
    if (voiceBusy || chatBusy) return;

    // 진행 중인 TTS·스트림 중단
    ttsPlayerRef.current?.stop();
    voiceAbortRef.current?.abort();

    const abortController = new AbortController();
    voiceAbortRef.current = abortController;
    setVoiceBusy(true);

    const assistantId = `voice-${Date.now()}-${Math.random().toString(36).slice(2)}`;
    let userMessageAdded = false;

    const player = new TtsQueuePlayer(serverUrl);
    ttsPlayerRef.current = player;

    player.onComplete = () => {
      notifySpeakingComplete(serverUrl, CHARACTER_ID).catch(() => {});
      setVoiceBusy(false);
    };

    await streamVoiceChat(
      serverUrl,
      blob,
      conversationIdRef.current,
      CHARACTER_ID,
      {
        onAck: (audioUrl, _text) => {
          if (audioUrl) player.enqueue(audioUrl);
        },
        onTranscript: (text) => {
          userMessageAdded = true;
          setChatMessages(prev => [
            ...prev,
            { role: 'user', text },
            { id: assistantId, role: 'assistant', text: '', mode: 'stream', streaming: true },
          ]);
        },
        onTextChunk: (text) => {
          setChatMessages(prev => prev.map(item =>
            item.id === assistantId ? { ...item, text: item.text + text } : item,
          ));
        },
        onTtsChunk: (audioUrl, _text) => {
          player.enqueue(audioUrl);
        },
        onDone: () => {
          setChatMessages(prev => prev.map(item =>
            item.id === assistantId ? { ...item, streaming: false } : item,
          ));
          // TTS 청크가 없었으면 즉시 완료 처리
          player.finalize();
        },
        onError: (_errorCode, message) => {
          if (userMessageAdded) {
            setChatMessages(prev => prev.map(item =>
              item.id === assistantId ? { ...item, text: message, streaming: false } : item,
            ));
          } else {
            setChatMessages(prev => [...prev, { role: 'assistant', text: message }]);
          }
          setVoiceBusy(false);
        },
      },
      abortController.signal,
    );
  }, [voiceBusy, chatBusy, serverUrl]);

  // ─────────────────────────────────────
  // Render
  // ─────────────────────────────────────

  return (
    <ControllerShell
      header={
        <>
          <StatusBar
            title="Exhibition"
            subtitle={`${serverUrl} · ${connState}`}
            status={connState === 'disconnected' ? 'error' : connState === 'connecting' ? 'sending' : status}
            icon={<Smartphone size={14} className="text-emerald-400" />}
            actions={
              <button
                onClick={() => { setShowSettings(v => !v); setUrlDraft(serverUrl); }}
                className={`p-2 rounded-lg transition-colors ${showSettings ? 'bg-white/10 text-white' : 'text-white/25 active:text-white/60'}`}
              >
                {showSettings ? <X size={15} /> : <Settings size={15} />}
              </button>
            }
          />

          {/* Settings dropdown */}
          <AnimatePresence>
            {showSettings && (
              <motion.div
                initial={{ height: 0, opacity: 0 }}
                animate={{ height: 'auto', opacity: 1 }}
                exit={{ height: 0, opacity: 0 }}
                className="shrink-0 overflow-hidden border-b border-white/5 bg-[#0d0d0d] z-40"
              >
                <div className="px-4 py-4 space-y-3">
                  <div className="text-[9px] text-white/25 uppercase tracking-widest font-mono">Server URL</div>
                  <div className="flex gap-2">
                    <input
                      type="url"
                      value={urlDraft}
                      onChange={e => setUrlDraft(e.target.value)}
                      onKeyDown={e => e.key === 'Enter' && saveUrl()}
                      placeholder="https://192.168.x.x:7225"
                      className="flex-1 bg-white/5 border border-white/8 rounded-xl px-3 py-2.5 text-[12px] font-mono text-white/80 outline-none focus:border-emerald-500/40 transition-colors"
                    />
                    <button onClick={saveUrl} className="px-4 py-2.5 bg-emerald-600 active:bg-emerald-700 rounded-xl text-[11px] font-bold">
                      Save
                    </button>
                  </div>
                  <p className="text-[9px] text-white/20 font-mono leading-relaxed">
                    모바일 테스트 시 PC의 로컬 IP로 변경하세요
                  </p>
                </div>
              </motion.div>
            )}
          </AnimatePresence>
        </>
      }
      footer={<>Character_01 · Exhibition Controller</>}
    >
      {/* TouchPad: 메인 영역 전체가 드래그 회전 표면 */}
      {/* 마이크: 하단 중앙 — 항상 표시, 채팅 패널과 겹치지 않는 위치 */}
      <div className="absolute bottom-10 left-1/2 -translate-x-1/2 z-30">
        <MicButton
          size="lg"
          isRecording={isRecording}
          disabled={chatBusy || voiceBusy}
          permissionDenied={permissionDenied}
          onPressStart={startRecording}
          onPressEnd={async () => {
            const blob = await stopRecording();
            if (blob && blob.size > 0) void submitVoiceChat(blob);
          }}
        />
      </div>

      {/* 채팅: 오른쪽 하단 */}
      <button
        onClick={() => { setChatOpen(v => !v); setSideOpen(false); }}
        className={`absolute bottom-10 right-8 z-30 h-11 w-11 rounded-xl border flex items-center justify-center transition-colors
          ${chatOpen ? 'bg-emerald-500/20 border-emerald-400/30 text-emerald-300' : 'bg-white/5 border-white/10 text-white/40 active:text-white'}`}
        aria-label="Open guide chat"
      >
        <MessageCircle size={18} />
      </button>

      <AnimatePresence>
        {chatOpen && (
          <motion.div
            initial={{ x: '105%', opacity: 0 }}
            animate={{ x: 0, opacity: 1 }}
            exit={{ x: '105%', opacity: 0 }}
            transition={{ type: 'spring', stiffness: 360, damping: 34 }}
            className="absolute top-0 bottom-0 right-0 z-20 w-[min(340px,88vw)] bg-[#0d0d0d]/98 backdrop-blur-md border-l border-white/8 flex flex-col pt-16 touch-auto"
            onTouchStart={e => e.stopPropagation()}
            onTouchMove={e => e.stopPropagation()}
            onTouchEnd={e => e.stopPropagation()}
            onPointerDown={e => e.stopPropagation()}
          >
            <div className="px-4 pb-3 border-b border-white/5">
              <div className="text-[10px] font-bold uppercase tracking-[0.22em] text-white/70">AI Guide</div>
              <div className="mt-3 grid grid-cols-3 rounded-lg bg-white/5 p-1">
                {(['stream', 'json', 'auto'] as ChatTransportMode[]).map(mode => (
                  <button
                    key={mode}
                    type="button"
                    onClick={() => setChatTransportMode(mode)}
                    className={`h-7 rounded-md text-[9px] font-mono uppercase transition-colors ${
                      chatTransportMode === mode
                        ? 'bg-emerald-500/20 text-emerald-200'
                        : 'text-white/35 active:text-white/70'
                    }`}
                  >
                    {mode}
                  </button>
                ))}
              </div>
              <div className="mt-1 text-[9px] font-mono text-white/25">RAG fallback · command safe mode</div>
            </div>

            <div className="flex-1 overflow-y-auto px-4 py-3 space-y-3">
              {chatMessages.map((message, index) => (
                <div
                  key={`${message.role}-${index}`}
                  className={`rounded-lg px-3 py-2 border text-[12px] leading-relaxed select-text
                    ${message.role === 'user'
                      ? 'ml-8 bg-emerald-500/10 border-emerald-500/15 text-emerald-50'
                      : 'mr-6 bg-white/5 border-white/8 text-white/75'}`}
                >
                  <div className="whitespace-pre-wrap">{message.text || (message.streaming ? '...' : '')}</div>
                  {message.role === 'assistant' && (
                    <div className="mt-2 flex flex-wrap gap-1.5 text-[8px] font-mono text-white/25">
                      {message.sources?.map(source => (
                        <span key={source} className="px-1.5 py-0.5 rounded bg-white/5">{source}</span>
                      ))}
                      {message.commandCount !== undefined && (
                        <span className="px-1.5 py-0.5 rounded bg-white/5">cmd {message.commandCount}</span>
                      )}
                      {message.mode && (
                        <span className="px-1.5 py-0.5 rounded bg-white/5">{message.mode}</span>
                      )}
                      {message.provider && (
                        <span className="px-1.5 py-0.5 rounded bg-white/5">{message.provider}</span>
                      )}
                      {message.model && (
                        <span className="px-1.5 py-0.5 rounded bg-white/5">{message.model}</span>
                      )}
                      {message.streaming && (
                        <span className="px-1.5 py-0.5 rounded bg-emerald-500/10 text-emerald-200">typing</span>
                      )}
                    </div>
                  )}
                </div>
              ))}
            </div>

            <div className="shrink-0 border-t border-white/5 p-3 space-y-2">
              <div className="grid grid-cols-1 gap-1.5">
                {[
                  '여기 전시물이 뭐야?',
                  '트리케라톱스 밝고 신나게 소개해줘',
                  '뭘 할 수 있어?',
                  '클래식 분위기로 바꿔줘',
                ].map(prompt => (
                  <button
                    key={prompt}
                    onClick={() => submitChat(prompt)}
                    disabled={chatBusy}
                    className="text-left px-2.5 py-1.5 rounded-lg bg-white/5 active:bg-white/10 disabled:opacity-40 text-[10px] text-white/45"
                  >
                    {prompt}
                  </button>
                ))}
              </div>

              <form
                onSubmit={e => {
                  e.preventDefault();
                  submitChat();
                }}
                className="flex gap-2"
              >
                <input
                  value={chatInput}
                  onChange={e => setChatInput(e.target.value)}
                  disabled={chatBusy || voiceBusy}
                  maxLength={300}
                  placeholder="전시물이나 분위기를 물어봐"
                  className="min-w-0 flex-1 bg-white/5 border border-white/10 rounded-lg px-3 py-2 text-[12px] text-white/80 outline-none focus:border-emerald-500/40 disabled:opacity-40"
                />
                <button
                  type="submit"
                  disabled={chatBusy || voiceBusy || !chatInput.trim()}
                  className="h-9 w-9 shrink-0 rounded-lg bg-emerald-600 disabled:bg-white/10 disabled:text-white/20 flex items-center justify-center"
                >
                  <Send size={14} />
                </button>
              </form>
            </div>
          </motion.div>
        )}
      </AnimatePresence>

      <TouchPad
        onDelta={d => throttledRotate(d.dx, d.dy)}
        sensitivity={1.0}
        className="absolute inset-0"
      >
        {/* Rotation hint */}
        <div className="absolute inset-0 flex items-center justify-center pointer-events-none">
          <p className="text-[8px] text-white/8 uppercase tracking-[0.5em]">Drag to Rotate</p>
        </div>
      </TouchPad>

      {/* Joystick: 왼쪽 하단 */}
      <div className="absolute bottom-10 left-8 z-20">
        <Joystick
          radius={62}
          deadzone={0.1}
          hapticMs={10}
          hapticZones={[{ threshold: 0.6, ms: 8 }, { threshold: 0.92, ms: 18 }]}
          size={160}
          knobSize={56}
          label="Move"
          onChange={o => { joystickRef.current = { x: o.x, y: o.y, active: true }; }}
          onRelease={() => {
            joystickRef.current = { x: 0, y: 0, active: false };
            transport.sendRaw(ExhibitionCommands.moveDirection(0, 0));
          }}
        />
      </div>

      {/* Side Menu */}
      <SideMenu open={sideOpen} onToggle={() => { setSideOpen(v => !v); setChatOpen(false); }}>
        <div className="p-4 space-y-5">

          <Section title="Emotion">
            <div className="grid grid-cols-2 gap-2">
              <ActionButton icon={<Smile size={20} />} label="Happy"    activeColor="bg-amber-500/25"  iconColor="text-amber-400"  size="lg" active={activeBtn === 'emotion_happy'}    onClick={() => emotion('happy',    'Happy')} />
              <ActionButton icon={<Frown size={20} />} label="Sad"      activeColor="bg-blue-500/25"   iconColor="text-blue-400"   size="lg" active={activeBtn === 'emotion_sad'}      onClick={() => emotion('sad',      'Sad')} />
              <ActionButton icon={<Zap   size={20} />} label="Angry"    activeColor="bg-red-500/25"    iconColor="text-red-400"    size="lg" active={activeBtn === 'emotion_angry'}    onClick={() => emotion('angry',    'Angry')} />
              <ActionButton icon={<Ghost size={20} />} label="Surprise" activeColor="bg-purple-500/25" iconColor="text-purple-400" size="lg" active={activeBtn === 'emotion_surprise'} onClick={() => emotion('surprise', 'Surprise')} />
            </div>
          </Section>

          <Section title="Atmosphere">
            <div className="grid grid-cols-2 gap-1.5">
              <ActionButton icon={<Crown  size={15} />} label="Classic"    activeColor="bg-yellow-500/25" iconColor="text-yellow-400" active={activeBtn === 'style_0'} onClick={() => style(0, 'Classic')} />
              <ActionButton icon={<Clock  size={15} />} label="Aged"       activeColor="bg-amber-500/25"  iconColor="text-amber-400"  active={activeBtn === 'style_1'} onClick={() => style(1, 'Aged')} />
              <ActionButton icon={<Layers size={15} />} label="Modern"     activeColor="bg-sky-500/25"    iconColor="text-sky-400"    active={activeBtn === 'style_2'} onClick={() => style(2, 'Modern')} />
              <ActionButton icon={<Wrench size={15} />} label="Industrial" activeColor="bg-zinc-500/25"   iconColor="text-zinc-400"   active={activeBtn === 'style_3'} onClick={() => style(3, 'Industrial')} />
            </div>
            <div className="grid grid-cols-2 gap-1.5">
              <ActionButton icon={<Sparkles size={15} />} label="Spot On"  active={activeBtn === 'stage_light.spotOn'}  onClick={() => stageEvt('light.spotOn',  'Spot On')} />
              <ActionButton icon={<Moon     size={15} />} label="Spot Off" active={activeBtn === 'stage_light.spotOff'} onClick={() => stageEvt('light.spotOff', 'Spot Off')} />
            </div>
          </Section>

          <Section title="Animation">
            <div className="grid grid-cols-3 gap-1.5">
              <ActionButton icon={<Play size={13} />} label="Wave" active={activeBtn === 'anim_wave'} onClick={() => animate('wave', 'Wave')} />
              <ActionButton icon={<Play size={13} />} label="Bow"  active={activeBtn === 'anim_bow'}  onClick={() => animate('bow',  'Bow')} />
              <ActionButton icon={<Play size={13} />} label="Clap" active={activeBtn === 'anim_clap'} onClick={() => animate('clap', 'Clap')} />
            </div>
          </Section>

          <ActionButton
            icon={<RotateCcw size={12} />}
            label="Reset Scene"
            size="sm"
            active={activeBtn === 'reset'}
            onClick={() => sendButton(ExhibitionCommands.reset(), 'Reset', 'reset')}
            className="w-full"
          />

        </div>
      </SideMenu>

      {/* Feedback banner */}
      <ResultBanner banner={banner} />
    </ControllerShell>
  );
};
