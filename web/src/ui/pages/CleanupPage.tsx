import { OctagonX, Trash2 } from 'lucide-react';
import { useState } from 'react';
import { nativeBridge } from '../../bridge/nativeBridge';
import type { ResponseDto } from '../../bridge/types';
import { ToolPage } from '../components/ToolPage';

export function CleanupPage({ onResponse }: { onResponse: (response: ResponseDto) => void }) {
  const [confirmed, setConfirmed] = useState(false);
  const [isRunning, setIsRunning] = useState(false);

  const execute = async () => {
    setIsRunning(true);
    try {
      onResponse(await nativeBridge.send('cleanupRedundant', { confirmed }));
    } finally {
      setIsRunning(false);
    }
  };

  const cancel = async () => {
    onResponse(await nativeBridge.send('cancelCurrentTask'));
  };

  return (
    <ToolPage title="冗余清理" meta="执行前必须确认，避免误删生产文件内容">
      <label className="check-row warning">
        <input type="checkbox" checked={confirmed} disabled={isRunning} onChange={(event) => setConfirmed(event.target.checked)} />
        我确认清理页面辅助线、隐藏空图层和空文本对象
      </label>
      <div className="action-row">
        <button className="danger" disabled={!confirmed || isRunning} onClick={execute}>
          <Trash2 size={16} />
          {isRunning ? '正在清理' : '开始清理'}
        </button>
        <button className="secondary" disabled={!isRunning} onClick={cancel}>
          <OctagonX size={16} />
          取消
        </button>
      </div>
    </ToolPage>
  );
}
