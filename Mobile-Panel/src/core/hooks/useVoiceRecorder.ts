import { useState, useRef, useCallback } from 'react';

// 브라우저별 지원 포맷 우선순위
const MIME_CANDIDATES = [
  'audio/webm;codecs=opus',
  'audio/webm',
  'audio/mp4',
  'audio/wav',
];

function detectSupportedMime(): string {
  for (const mime of MIME_CANDIDATES) {
    if (MediaRecorder.isTypeSupported(mime)) {
      return mime;
    }
  }
  return '';
}

function mimeToExtension(mime: string): string {
  if (mime.startsWith('audio/webm')) return 'audio.webm';
  if (mime.startsWith('audio/mp4'))  return 'audio.mp4';
  if (mime.startsWith('audio/wav'))  return 'audio.wav';
  return 'audio.webm';
}

export interface UseVoiceRecorderReturn {
  isRecording: boolean;
  start: () => Promise<void>;
  stop: () => Promise<Blob | null>;
  /** 마이크 권한 없을 때 true */
  permissionDenied: boolean;
}

/**
 * MediaRecorder 기반 음성 녹음 훅.
 * start() 호출 후 stop() 호출로 Blob 반환.
 * 모바일 브라우저 지원 포맷을 런타임에 자동 감지.
 */
export function useVoiceRecorder(): UseVoiceRecorderReturn {
  const [isRecording, setIsRecording]         = useState(false);
  const [permissionDenied, setPermissionDenied] = useState(false);

  const recorderRef = useRef<MediaRecorder | null>(null);
  const chunksRef   = useRef<Blob[]>([]);
  const streamRef   = useRef<MediaStream | null>(null);

  const start = useCallback(async () => {
    if (isRecording) return;

    try {
      const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
      streamRef.current = stream;

      const mime    = detectSupportedMime();
      const options = mime ? { mimeType: mime } : undefined;
      const recorder = new MediaRecorder(stream, options);

      chunksRef.current = [];

      recorder.ondataavailable = (e) => {
        if (e.data.size > 0) {
          chunksRef.current.push(e.data);
        }
      };

      recorderRef.current = recorder;
      recorder.start();
      setIsRecording(true);
      setPermissionDenied(false);
    } catch (err) {
      if (err instanceof DOMException && err.name === 'NotAllowedError') {
        setPermissionDenied(true);
      }
      console.warn('[useVoiceRecorder] start failed:', err);
    }
  }, [isRecording]);

  const stop = useCallback((): Promise<Blob | null> => {
    return new Promise((resolve) => {
      const recorder = recorderRef.current;
      if (!recorder || recorder.state === 'inactive') {
        resolve(null);
        return;
      }

      recorder.onstop = () => {
        const mime     = recorder.mimeType || 'audio/webm';
        const fileName = mimeToExtension(mime);
        const blob     = new Blob(chunksRef.current, { type: mime });

        // 스트림 트랙 종료
        streamRef.current?.getTracks().forEach(t => t.stop());
        streamRef.current  = null;
        recorderRef.current = null;
        chunksRef.current  = [];

        setIsRecording(false);
        resolve(Object.assign(blob, { name: fileName }));
      };

      recorder.stop();
    });
  }, []);

  return { isRecording, start, stop, permissionDenied };
}
