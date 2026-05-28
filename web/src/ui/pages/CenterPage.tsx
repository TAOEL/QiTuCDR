import { Crosshair, OctagonX } from 'lucide-react';
import { useState } from 'react';
import { nativeBridge } from '../../bridge/nativeBridge';
import type { ResponseDto } from '../../bridge/types';
import { Field } from '../components/Field';
import { ToolPage } from '../components/ToolPage';

export function CenterPage({ onResponse }: { onResponse: (response: ResponseDto) => void }) {
  const [mode, setMode] = useState('group');
  const [isRunning, setIsRunning] = useState(false);

  const execute = async () => {
    setIsRunning(true);
    try {
      onResponse(await nativeBridge.send('centerObjects', { mode }));
    } finally {
      setIsRunning(false);
    }
  };

  const cancel = async () => {
    onResponse(await nativeBridge.send('cancelCurrentTask'));
  };

  return (
    <ToolPage title="一键居中" meta="支持整体居中与独立居中">
      <Field label="居中模式">
        <select value={mode} disabled={isRunning} onChange={(event) => setMode(event.target.value)}>
          <option value="group">多对象整体居中</option>
          <option value="individual">多对象独立居中</option>
        </select>
      </Field>
      <div className="action-row">
        <button className="primary" disabled={isRunning} onClick={execute}>
          <Crosshair size={16} />
          {isRunning ? '正在居中' : '居中到页面'}
        </button>
        <button className="secondary" disabled={!isRunning} onClick={cancel}>
          <OctagonX size={16} />
          取消
        </button>
      </div>
    </ToolPage>
  );
}
