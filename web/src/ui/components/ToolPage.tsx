import type { ReactNode } from 'react';

export function ToolPage({ title, meta, children }: { title: string; meta: string; children: ReactNode }) {
  return (
    <section className="tool-page">
      <div className="tool-head">
        <h2>{title}</h2>
        <p>{meta}</p>
      </div>
      <div className="tool-body">{children}</div>
    </section>
  );
}
