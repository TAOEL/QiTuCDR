import type { BridgeMessage, EventDto, RequestDto, ResponseDto } from './types';

declare global {
  interface Window {
    chrome?: {
      webview?: {
        postMessage: (message: RequestDto) => void;
        addEventListener: (event: 'message', callback: (event: MessageEvent) => void) => void;
        removeEventListener: (event: 'message', callback: (event: MessageEvent) => void) => void;
      };
    };
  }
}

type Pending = {
  resolve: (response: ResponseDto) => void;
  reject: (error: Error) => void;
};

class NativeBridge {
  private readonly pending = new Map<string, Pending>();
  private readonly listeners = new Set<(event: EventDto) => void>();

  constructor() {
    window.chrome?.webview?.addEventListener('message', this.handleMessage);
  }

  send(action: string, payload: Record<string, unknown> = {}): Promise<ResponseDto> {
    const request: RequestDto = {
      version: '1.0',
      requestId: crypto.randomUUID(),
      action,
      payload
    };

    if (!window.chrome?.webview) {
      return Promise.resolve({
        version: '1.0',
        requestId: request.requestId,
        success: false,
        errorCode: 'WEBVIEW_NOT_READY',
        message: 'Native bridge is not available in browser preview.',
        payload: {}
      });
    }

    return new Promise((resolve, reject) => {
      this.pending.set(request.requestId, { resolve, reject });
      window.chrome?.webview?.postMessage(request);
    });
  }

  onEvent(callback: (event: EventDto) => void): () => void {
    this.listeners.add(callback);
    return () => this.listeners.delete(callback);
  }

  private readonly handleMessage = (event: MessageEvent) => {
    const message = event.data as BridgeMessage;
    if ('requestId' in message) {
      const pending = this.pending.get(message.requestId);
      if (pending) {
        this.pending.delete(message.requestId);
        pending.resolve(message);
      }
      return;
    }

    if ('event' in message) {
      this.listeners.forEach((listener) => listener(message));
    }
  };
}

export const nativeBridge = new NativeBridge();
