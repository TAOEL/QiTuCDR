import { OctagonX, Ruler } from 'lucide-react';
import { useState } from 'react';
import { nativeBridge } from '../../bridge/nativeBridge';
import type { ResponseDto } from '../../bridge/types';
import { Field } from '../components/Field';
import { ToolPage } from '../components/ToolPage';

export function NormalizePage({ onResponse }: { onResponse: (response: ResponseDto) => void }) {
  const [width, setWidth] = useState('100');
  const [height, setHeight] = useState('100');
  const [outlineWidth, setOutlineWidth] = useState('0.2');
  const [lockRatio, setLockRatio] = useState(true);
  const [isRunning, setIsRunning] = useState(false);

  const execute = async () => {
    setIsRunning(true);
    try {
      onResponse(
        await nativeBridge.send('normalizeSize', {
          width: readOptionalNumber(width),
          height: lockRatio ? undefined : readOptionalNumber(height),
          outlineWidth: readOptionalNumber(outlineWidth),
          lockRatio
        })
      );
    } finally {
      setIsRunning(false);
    }
  };

  const cancel = async () => {
    onResponse(await nativeBridge.send('cancelCurrentTask'));
  };

  return (
    <ToolPage title="尺寸规整" meta="统一宽高与描边，批量应用至选中对象">
      <div className="grid-two">
        <Field label="宽度">
          <input value={width} disabled={isRunning} onChange={(event) => setWidth(event.target.value)} inputMode="decimal" />
        </Field>
        <Field label="高度">
          <input value={height} disabled={lockRatio || isRunning} onChange={(event) => setHeight(event.target.value)} inputMode="decimal" />
        </Field>
      </div>
      <Field label="描边宽度">
        <input value={outlineWidth} disabled={isRunning} onChange={(event) => setOutlineWidth(event.target.value)} inputMode="decimal" />
      </Field>
      <label className="check-row">
        <input type="checkbox" checked={lockRatio} disabled={isRunning} onChange={(event) => setLockRatio(event.target.checked)} />
        等比例锁定
      </label>
      <div className="action-row">
        <button className="primary" disabled={isRunning} onClick={execute}>
          <Ruler size={16} />
          {isRunning ? '正在规整' : '应用规整'}
        </button>
        <button className="secondary" disabled={!isRunning} onClick={cancel}>
          <OctagonX size={16} />
          取消
        </button>
      </div>
    </ToolPage>
  );
}

function readOptionalNumber(value: string): number | undefined {
  if (!value.trim()) {
    return undefined;
  }

  const parsed = Number(value);
  return Number.isFinite(parsed) ? parsed : undefined;
}
