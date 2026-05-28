import {
  Box,
  Clock3,
  Info,
  Layers,
  MessageCircle,
  Minus,
  Pause,
  Search,
  Settings,
  UserRound,
  X
} from 'lucide-react';
import type { ReactNode } from 'react';
import { useEffect, useMemo, useState } from 'react';
import { nativeBridge } from '../bridge/nativeBridge';

type PageKey = 'market' | 'ai' | 'user' | 'setting';
type ThemeMode = 'light' | 'dark';

type Category = {
  key: string;
  label: string;
  cards: string[];
};

type Page = {
  key: PageKey;
  title: string;
  icon: ReactNode;
  searchable?: boolean;
  categories: Category[];
};

const pages: Page[] = [
  {
    key: 'market',
    title: '功能市场',
    icon: <Box size={20} />,
    searchable: true,
    categories: [
      { key: 'text', label: '文本工具', cards: ['CDR文字一键转曲', '批量字体替换', '文字转路径'] },
      { key: 'batch', label: '批量工具', cards: ['批量导出图片', '批量出血处理', '批量颜色替换'] },
      { key: 'io', label: '导入导出', cards: ['PDF批量导入CDR', 'CDR批量导出PDF', '文件格式互转'] },
      { key: 'image', label: '图像工具', cards: ['图片批量压缩', '智能抠图', '尺寸统一裁剪'] }
    ]
  },
  {
    key: 'ai',
    title: 'AI工具',
    icon: <Clock3 size={20} />,
    categories: [
      { key: 'ai-img', label: '图片生成', cards: ['AI一键生成设计图', '矢量图智能生成'] },
      { key: 'ai-layout', label: '智能排版', cards: ['自动图文排版', '版式智能优化'] }
    ]
  },
  {
    key: 'user',
    title: '用户中心',
    icon: <UserRound size={20} />,
    categories: [
      { key: 'user-dash', label: '仪表盘', cards: ['使用统计仪表盘', '功能使用概况'] },
      { key: 'user-order', label: '订单信息', cards: ['历史订单记录', '会员购买记录'] }
    ]
  },
  {
    key: 'setting',
    title: '设置',
    icon: <Settings size={20} />,
    categories: [
      { key: 'set-normal', label: '常规设置', cards: ['主题颜色设置', '默认导出格式'] },
      { key: 'set-adv', label: '高级设置', cards: ['快捷键自定义', '高级缓存清理'] }
    ]
  }
];

const hotTags = ['文字工具', '批量导出', '智能抠图', '出血处理'];
const initialHistory = ['批量转曲', 'AI抠图', '导出PDF'];

export function App() {
  const [activePageKey, setActivePageKey] = useState<PageKey>('market');
  const [activeCategoryKey, setActiveCategoryKey] = useState('text');
  const [isSearchOpen, setIsSearchOpen] = useState(false);
  const [searchValue, setSearchValue] = useState('');
  const [history, setHistory] = useState(initialHistory);
  const [isAboutOpen, setIsAboutOpen] = useState(false);
  const [isCollapsed, setIsCollapsed] = useState(false);
  const [isPageMenuOpen, setIsPageMenuOpen] = useState(false);
  const [status, setStatus] = useState('Preheating');
  const [theme, setTheme] = useState<ThemeMode>(() => {
    const saved = localStorage.getItem('qitucdr-theme');
    return saved === 'dark' ? 'dark' : 'light';
  });

  useEffect(() => {
    nativeBridge.send('getState').then((response) => {
      if (response.success && typeof response.payload.state === 'string') {
        setStatus(response.payload.state);
      }
    });

    return nativeBridge.onEvent((event) => {
      if (event.event === 'plugin.stateChanged' && typeof event.payload.state === 'string') {
        setStatus(event.payload.state);
      }
    });
  }, []);

  useEffect(() => {
    document.documentElement.dataset.theme = theme;
    localStorage.setItem('qitucdr-theme', theme);
  }, [theme]);

  const activePage = useMemo(() => pages.find((page) => page.key === activePageKey) ?? pages[0], [activePageKey]);
  const activeCategory = useMemo(
    () => activePage.categories.find((category) => category.key === activeCategoryKey) ?? activePage.categories[0],
    [activeCategoryKey, activePage]
  );

  const switchPage = (page: Page) => {
    setActivePageKey(page.key);
    setActiveCategoryKey(page.categories[0].key);
    setIsSearchOpen(false);
    setIsPageMenuOpen(false);
  };

  const chooseSearchTerm = (term: string) => {
    setSearchValue(term);
    setIsSearchOpen(false);
    setHistory((current) => [term, ...current.filter((item) => item !== term)].slice(0, 5));
  };

  const handleCardClick = (card: string) => {
    if (card === '主题颜色设置') {
      setTheme((current) => (current === 'dark' ? 'light' : 'dark'));
    }
  };

  return (
    <div className="portal-stage" onClick={() => setIsPageMenuOpen(false)}>
      <section className={`portal-window${isCollapsed ? ' collapsed' : ''}`}>
        <aside className="left-nav">
          <div className="nav-top">
            {pages.slice(0, 3).map((page) => (
              <NavIcon
                key={page.key}
                active={activePageKey === page.key}
                title={page.title}
                onClick={() => switchPage(page)}
              >
                {page.icon}
              </NavIcon>
            ))}
          </div>
          <div className="nav-bottom">
            <NavIcon active={activePageKey === 'setting'} title="设置" onClick={() => switchPage(pages[3])}>
              <Settings size={20} />
            </NavIcon>
            <NavIcon active={false} title="关于" onClick={() => setIsAboutOpen(true)}>
              <Info size={20} />
            </NavIcon>
          </div>
        </aside>

        <aside className="middle-list">
          {activePage.searchable ? (
            <div className="search-box" onClick={(event) => event.stopPropagation()}>
              <input
                className="search-input"
                placeholder="搜索插件..."
                value={searchValue}
                onChange={(event) => setSearchValue(event.target.value)}
                onFocus={() => setIsSearchOpen(true)}
              />
              <Search className="search-icon" size={16} />
              {isSearchOpen ? (
                <div className="search-dropdown">
                  <div className="search-title">
                    <span>搜索历史</span>
                    <button type="button" onClick={() => setHistory([])}>
                      清空
                    </button>
                  </div>
                  <div className="search-history-list">
                    {history.map((item) => (
                      <button className="search-history-item" type="button" key={item} onClick={() => chooseSearchTerm(item)}>
                        <span>{item}</span>
                        <span
                          className="del-history"
                          onClick={(event) => {
                            event.stopPropagation();
                            setHistory((current) => current.filter((historyItem) => historyItem !== item));
                          }}
                        >
                          x
                        </span>
                      </button>
                    ))}
                  </div>
                  <div className="search-title single">热门推荐</div>
                  <div className="search-hot-tags">
                    {hotTags.map((tag) => (
                      <button className="hot-tag" type="button" key={tag} onClick={() => chooseSearchTerm(tag)}>
                        {tag}
                      </button>
                    ))}
                  </div>
                </div>
              ) : null}
            </div>
          ) : null}

          <div className="category-wrap">
            {activePage.categories.map((category) => (
              <button
                className={category.key === activeCategory.key ? 'category-item active' : 'category-item'}
                type="button"
                key={category.key}
                onClick={() => setActiveCategoryKey(category.key)}
              >
                {category.label}
              </button>
            ))}
          </div>
        </aside>

        <main className="right-content-wrap">
          <header className="inner-title-bar">
            {isCollapsed ? (
              <button
                className="dropdown-btn"
                type="button"
                onClick={(event) => {
                  event.stopPropagation();
                  setIsPageMenuOpen((value) => !value);
                }}
              >
                <Layers size={16} />
              </button>
            ) : null}
            <div className="bar-title">{activeCategory.label}</div>
            <div className="win-buttons">
              <button className="win-btn" type="button" title={`状态：${status}`}>
                ○
              </button>
              <button className="win-btn" type="button" title="收起" onClick={() => setIsCollapsed((value) => !value)}>
                {isCollapsed ? '□' : <Minus size={13} />}
              </button>
              <button className="win-btn close" type="button" title="关闭面板">
                <X size={13} />
              </button>
            </div>
            {isPageMenuOpen ? (
              <div className="dropdown-menu" onClick={(event) => event.stopPropagation()}>
                {pages.map((page) => (
                  <button
                    className={page.key === activePageKey ? 'dropdown-item active' : 'dropdown-item'}
                    type="button"
                    key={page.key}
                    onClick={() => switchPage(page)}
                  >
                    {page.title}
                  </button>
                ))}
              </div>
            ) : null}
          </header>

          <section className="content-panel">
            {activeCategory.cards.map((card) => (
              <button className="plugin-card" type="button" key={card} onClick={() => handleCardClick(card)}>
                <span>{card}</span>
                {card === '主题颜色设置' ? (
                  <span className="card-meta">{theme === 'dark' ? '当前：暗黑' : '当前：浅色'}</span>
                ) : null}
              </button>
            ))}
          </section>
        </main>
      </section>

      {isAboutOpen ? <AboutModal onClose={() => setIsAboutOpen(false)} /> : null}
    </div>
  );
}

function NavIcon({ active, title, children, onClick }: { active: boolean; title: string; children: ReactNode; onClick: () => void }) {
  return (
    <button className={active ? 'nav-icon active' : 'nav-icon'} type="button" onClick={onClick}>
      {children}
      <span className="nav-tip">{title}</span>
    </button>
  );
}

function AboutModal({ onClose }: { onClose: () => void }) {
  return (
    <div className="modal" role="dialog" aria-modal="true">
      <div className="modal-window">
        <button className="modal-close" type="button" onClick={onClose}>
          x
        </button>
        <Layers className="about-logo" size={48} />
        <div className="about-version">企图插件 · V1.0.0.0</div>
        <div className="about-copyright">
          © 2025 企图软件 版权所有
          <br />
          用心打造高效CDR插件工具
        </div>
        <div className="social-wrap">
          <Info size={22} />
          <Box size={22} />
          <MessageCircle size={22} />
          <Pause size={22} />
        </div>
      </div>
    </div>
  );
}
