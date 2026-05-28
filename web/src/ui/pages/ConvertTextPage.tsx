import { OctagonX, Play } from 'lucide-react';
import { useState } from 'react';
import { nativeBridge } from '../../bridge/nativeBridge';
import type { ResponseDto } from '../../bridge/types';
import { Field } from '../components/Field';
import { ToolPage } from '../components/ToolPage';
import type { TaskProgress } from '../taskProgress';

export function ConvertTextPage({
  progress,
  onResponse
}: {
  progress: TaskProgress | null;
  onResponse: (response: ResponseDto) => void;
}) {
  const [range, setRange] = useState('Selection');
  const [includeHidden, setIncludeHidden] = useState(false);
  const [isRunning, setIsRunning] = useState(false);

  const execute = async () => {
    setIsRunning(true);
    try {
      onResponse(await nativeBridge.send('convertText', { range, includeHidden }));
    } finally {
      setIsRunning(false);
    }
  };

  const cancel = async () => {
    onResponse(await nativeBridge.send('cancelCurrentTask'));
  };

  return (
    <ToolPage title="批量转曲" meta="按范围分批执行，锁定对象自动跳过">
      <Field label="处理范围">
        <select value={range} disabled={isRunning} onChange={(event) => setRange(event.target.value)}>
          <option value="Selection">选中对象</option>
          <option value="CurrentPage">当前页</option>
          <option value="Document">全文档</option>
        </select>
      </Field>
      <label className="check-row">
        <input
          type="checkbox"
          checked={includeHidden}
          disabled={isRunning}
          onChange={(event) => setIncludeHidden(event.target.checked)}
        />
        包含隐藏对象
      </label>
      {progress ? (
        <div className="task-progress" aria-label="批量转曲进度">
          <div className="progress-meta">
            <span>{progress.percent}%</span>
            <span>
              已转曲 {progress.converted} / 跳过 {progress.skipped} / 总计 {progress.total}
            </span>
          </div>
          <div className="progress-track">
            <span style={{ width: `${progress.percent}%` }} />
          </div>
        </div>
      ) : null}
      <div className="action-row">
        <button className="primary" disabled={isRunning} onClick={execute}>
          <Play size={16} />
          {isRunning ? '正在转曲' : '执行转曲'}
        </button>
        <button className="secondary" disabled={!isRunning} onClick={cancel}>
          <OctagonX size={16} />
          取消
        </button>
      </div>
    </ToolPage>
  );
}
