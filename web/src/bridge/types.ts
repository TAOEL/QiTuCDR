export type RequestDto = {
  version: '1.0';
  requestId: string;
  action: string;
  payload: Record<string, unknown>;
};

export type ResponseDto = {
  version: '1.0';
  requestId: string;
  success: boolean;
  errorCode: string | null;
  message: string;
  payload: Record<string, unknown>;
};

export type EventDto = {
  event: string;
  timestamp: number;
  payload: Record<string, unknown>;
};

export type BridgeMessage = ResponseDto | EventDto;
