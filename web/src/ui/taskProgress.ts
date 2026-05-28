import type { EventDto } from '../bridge/types';

export type TaskProgress = {
  action: string;
  converted: number;
  skipped: number;
  total: number;
  percent: number;
};

export type TaskNotice = {
  event: 'task.completed' | 'task.failed';
  action: string;
  message: string;
};

export function readTaskProgress(event: EventDto): TaskProgress | null {
  if (event.event !== 'task.progress') {
    return null;
  }

  const action = readString(event.payload.action);
  const converted = readNumber(event.payload.converted);
  const skipped = readNumber(event.payload.skipped);
  const total = readNumber(event.payload.total);

  if (!action || total <= 0) {
    return null;
  }

  const processed = Math.min(converted + skipped, total);
  return {
    action,
    converted,
    skipped,
    total,
    percent: Math.round((processed / total) * 100)
  };
}

export function readTaskNotice(event: EventDto): TaskNotice | null {
  if (event.event !== 'task.completed' && event.event !== 'task.failed') {
    return null;
  }

  const action = readString(event.payload.action);
  if (!action) {
    return null;
  }

  if (event.event === 'task.completed') {
    if (action === 'centerObjects') {
      const centered = readNumber(event.payload.centered);
      return {
        event: event.event,
        action,
        message: `完成：居中 ${centered} 个对象`
      };
    }

    if (action === 'normalizeSize') {
      const normalized = readNumber(event.payload.normalized);
      return {
        event: event.event,
        action,
        message: `完成：规整 ${normalized} 个对象`
      };
    }

    if (action === 'cleanupRedundant') {
      const removed = readNumber(event.payload.removed);
      return {
        event: event.event,
        action,
        message: `完成：清理 ${removed} 项`
      };
    }

    const converted = readNumber(event.payload.converted);
    const skipped = readNumber(event.payload.skipped);
    const total = readNumber(event.payload.total);
    return {
      event: event.event,
      action,
      message: `完成：转曲 ${converted}，跳过 ${skipped}，总计 ${total}`
    };
  }

  const errorCode = readString(event.payload.errorCode);
  const message = readString(event.payload.message);
  return {
    event: event.event,
    action,
    message: errorCode ? `${errorCode}: ${message}` : message
  };
}

function readString(value: unknown): string {
  return typeof value === 'string' ? value : '';
}

function readNumber(value: unknown): number {
  return typeof value === 'number' && Number.isFinite(value) ? value : 0;
}
