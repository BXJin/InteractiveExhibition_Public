import React from 'react';
import { Mic, MicOff } from 'lucide-react';

export interface MicButtonProps {
  isRecording: boolean;
  disabled?: boolean;
  permissionDenied?: boolean;
  onPressStart: () => void;
  onPressEnd: () => void;
  /** 'sm' = 채팅 폼 인라인용 (36px), 'lg' = 독립 플로팅 버튼용 (64px) */
  size?: 'sm' | 'lg';
}

/**
 * 누르는 동안 녹음하는 마이크 버튼.
 * pointerdown/pointerup으로 터치/마우스 모두 대응.
 */
export const MicButton: React.FC<MicButtonProps> = ({
  isRecording,
  disabled = false,
  permissionDenied = false,
  onPressStart,
  onPressEnd,
  size = 'sm',
}) => {
  const handlePointerDown = (e: React.PointerEvent) => {
    e.preventDefault();
    if (disabled) return;
    onPressStart();
  };

  const handlePointerUp = (e: React.PointerEvent) => {
    e.preventDefault();
    if (disabled) return;
    onPressEnd();
  };

  const isLg = size === 'lg';

  return (
    <button
      type="button"
      disabled={disabled}
      onPointerDown={handlePointerDown}
      onPointerUp={handlePointerUp}
      onPointerLeave={handlePointerUp}
      onPointerCancel={handlePointerUp}
      className={`
        ${isLg ? 'h-16 w-16 rounded-2xl' : 'h-9 w-9 shrink-0 rounded-lg'}
        flex flex-col items-center justify-center gap-1
        select-none touch-none transition-colors
        ${permissionDenied
          ? 'bg-red-500/20 text-red-400 border border-red-500/30'
          : isRecording
            ? 'bg-red-500/30 text-red-300 border border-red-400/40 animate-pulse'
            : 'bg-white/8 text-white/50 border border-white/10 active:bg-white/15 active:text-white/80'}
        disabled:opacity-30 disabled:pointer-events-none
      `}
      aria-label={isRecording ? '녹음 중 — 손 떼면 전송' : '음성 입력'}
      title={permissionDenied ? '마이크 권한이 필요합니다' : '누르는 동안 녹음'}
    >
      {permissionDenied
        ? <MicOff size={isLg ? 22 : 14} />
        : <Mic size={isLg ? 22 : 14} />}
      {isLg && (
        <span className="text-[8px] font-mono uppercase tracking-widest opacity-60">
          {isRecording ? 'Release' : 'Hold'}
        </span>
      )}
    </button>
  );
};
