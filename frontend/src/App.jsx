import { useState, useRef, useEffect } from 'react';
import { motion, AnimatePresence, MotionConfig } from 'framer-motion';
import { Search, Disc3, ArrowLeft } from 'lucide-react';
import ErrorState from './ErrorState';
import AlbumView from './AlbumView';
import SampleTree from './SampleTree';
import SuggestModal from './SuggestModal';

const API_URL = import.meta.env.VITE_API_URL ?? 'https://localhost:7266';
const EXAMPLES = [
  { label: 'N.Y State of Mind', query: 'N.Y State of Mind Nas' },
  { label: 'SOS', query: 'SOS SZA' },
  { label: 'Shela', query: 'Shela Sam Deep' },
  { label: 'Nice For What', query: 'Nice For What Drake' },
  { label: 'No Role Modelz', query: 'No Role Modelz J. Cole' },
];

const staggerContainer = { hidden: { opacity: 0 }, show: { opacity: 1, transition: { staggerChildren: 0.1 } } };
const fadeUp = {
  hidden: { opacity: 0, y: 20 },
  show: { opacity: 1, y: 0, transition: { type: 'spring', stiffness: 300, damping: 24 } },
};

const Heading = ({ children }) => (
  <h3 className="text-sm font-mono text-neutral-500 uppercase tracking-widest mb-6 border-b border-neutral-800 pb-2">
    {children}
  </h3>
);

const Field = ({ label, children, className = '' }) => (
  <div className={className}>
    <p className="text-xs font-mono text-neutral-500 mb-1">{label}</p>
    <p className="font-medium">{children}</p>
  </div>
);

export default function App() {
  const [query, setQuery] = useState('');
  const [mode, setMode] = useState('track'); // 'track' | 'album'
  const [data, setData] = useState(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null); // { kind, term, mode }
  const requestRef = useRef(null);
  const [navIndex, setNavIndex] = useState(0); // how many pages deep we are in this visit

  // ---- 30-second previews on the Sample Tree ----
  const [preview, setPreview] = useState({ id: null, status: 'idle' }); // idle | loading | playing | none
  const [suggestOpen, setSuggestOpen] = useState(false);
  const audioRef = useRef(null);
  const previewToken = useRef(0); // lets us ignore a preview that finished loading after you moved on

  const stopPreview = () => {
    previewToken.current++;
    audioRef.current?.pause();
    audioRef.current = null;
    setPreview({ id: null, status: 'idle' });
  };

  const togglePreview = async (sample) => {
    if (preview.id === sample.id && preview.status === 'playing') {
      stopPreview();
      return;
    }
    stopPreview();
    const token = previewToken.current;
    setPreview({ id: sample.id, status: 'loading' });
    try {
      const res = await fetch(
        `${API_URL}/api/archive/preview?title=${encodeURIComponent(sample.title)}&artist=${encodeURIComponent(sample.artist)}`
      );
      if (!res.ok) throw new Error('no preview');
      const { url } = await res.json();
      if (token !== previewToken.current) return;

      const audio = new Audio(url);
      audio.onended = () => setPreview({ id: null, status: 'idle' });
      audioRef.current = audio;
      await audio.play();
      if (token === previewToken.current) setPreview({ id: sample.id, status: 'playing' });
    } catch {
      if (token === previewToken.current) setPreview({ id: sample.id, status: 'none' });
    }
  };

  const load = async (term, searchMode = mode) => {
    const q = term.trim();
    if (!q) return;

    requestRef.current?.abort();
    const controller = new AbortController();
    requestRef.current = controller;
    stopPreview();
    setSuggestOpen(false);
    setLoading(true);
    setError(null);

    const path = searchMode === 'album' ? 'album' : 'search';
    try {
      const res = await fetch(`${API_URL}/api/archive/${path}?q=${encodeURIComponent(q)}`, {
        signal: controller.signal,
      });
      if (res.status === 404 || res.status === 400) throw { kind: 'notfound' };
      if (res.status === 429) throw { kind: 'busy' };
      if (!res.ok) throw { kind: 'server' };
      setData(await res.json());
    } catch (err) {
      if (err?.name === 'AbortError') return;
      setData(null);
      setError({ kind: err?.kind ?? 'network', term: q, mode: searchMode });
    } finally {
      if (requestRef.current === controller) setLoading(false);
    }
  };

  const resetToHome = () => {
    requestRef.current?.abort();
    stopPreview();
    setLoading(false);
    setData(null);
    setError(null);
    setQuery('');
  };

  // Opens a new "page" in the browser history, then loads it.
  const go = (term, searchMode = mode) => {
    if (!term.trim()) return;
    const idx = navIndex + 1;
    window.history.pushState({ term, mode: searchMode, idx }, '');
    setNavIndex(idx);
    load(term, searchMode);
  };

  const openTrack = (term) => {
    setMode('track');
    setQuery(term);
    go(term, 'track');
  };

  const changeMode = (next) => {
    setMode(next);
    if (query.trim()) go(query, next);
  };

  const goHome = () => {
    if (!data && !error && !loading) {
      setQuery('');
      return;
    }
    const idx = navIndex + 1;
    window.history.pushState({ home: true, idx }, '');
    setNavIndex(idx);
    resetToHome();
  };

  // The in-app Back button and the browser's Back/Forward buttons share one history.
  useEffect(() => {
    window.history.replaceState({ home: true, idx: 0 }, '');
    const onPop = (e) => {
      const s = e.state;
      setNavIndex(s?.idx ?? 0);
      if (!s || s.home) {
        resetToHome();
        return;
      }
      setMode(s.mode);
      setQuery(s.term);
      load(s.term, s.mode);
    };
    window.addEventListener('popstate', onPop);
    return () => window.removeEventListener('popstate', onPop);
  }, []);

  const credits = data?.credits;
  const coProducers = credits?.coProducers ?? [];
  const writers = credits?.writers ?? [];
  const samples = data?.samples ?? [];
  const hasView = loading || error || data;

  return (
    <MotionConfig reducedMotion="user">
      <div className="min-h-screen p-8 md:p-16 max-w-5xl mx-auto font-sans flex flex-col">
        <header className="mb-12">
          <h1 className="text-4xl font-extrabold tracking-tighter mb-4">
            <button
              type="button"
              onClick={goHome}
              aria-label="Archivist home"
              className="cursor-pointer focus-visible:outline-2 focus-visible:outline-neutral-500"
            >
              ARCHIVIST<span className="text-neutral-500">.</span>
            </button>
          </h1>

          <form onSubmit={(e) => { e.preventDefault(); go(query); }} className="relative max-w-xl" role="search">
            <Search className="absolute left-4 top-1/2 -translate-y-1/2 text-neutral-500 w-5 h-5" aria-hidden="true" />
            <input
              type="text"
              value={query}
              onChange={(e) => setQuery(e.target.value)}
              placeholder={`Search ${mode}...`}
              aria-label={`Search a ${mode}`}
              className="w-full bg-surface border border-neutral-800 rounded-full py-4 pl-12 pr-6 text-sm focus:outline-none focus:border-neutral-500 transition-colors"
            />
          </form>

          <div className="mt-4 flex gap-1 text-sm" role="group" aria-label="Search type">
            {['track', 'album'].map((m) => (
              <button
                key={m}
                type="button"
                aria-pressed={mode === m}
                onClick={() => changeMode(m)}
                className={`rounded-full px-4 py-1.5 capitalize transition-colors focus-visible:outline-2 focus-visible:outline-neutral-500 ${
                  mode === m ? 'bg-neutral-50 text-neutral-950 font-semibold' : 'text-neutral-500 hover:text-neutral-300'
                }`}
              >
                {m}
              </button>
            ))}
          </div>
        </header>

        {navIndex > 0 && hasView && (
          <button
            type="button"
            onClick={() => window.history.back()}
            className="self-start mb-8 flex items-center gap-2 text-sm text-neutral-500 hover:text-neutral-200 transition-colors cursor-pointer focus-visible:outline-2 focus-visible:outline-neutral-500"
          >
            <ArrowLeft className="w-4 h-4" /> Back
          </button>
        )}

        <div aria-live="polite">
          <AnimatePresence mode="wait">
            {loading ? (
              <motion.div key="loader" initial={{ opacity: 0 }} animate={{ opacity: 1 }} exit={{ opacity: 0 }}
                className="flex gap-2 items-center text-neutral-500 font-mono text-sm">
                <Disc3 className="animate-spin w-4 h-4" /> Digging the crates...
              </motion.div>
            ) : error ? (
              <ErrorState key="error" kind={error.kind} term={error.term}
                onRetry={() => load(error.term, error.mode)} />
            ) : data ? (
              <motion.main key={data.id ?? data.title} variants={staggerContainer} initial="hidden" animate="show"
                className="grid grid-cols-1 md:grid-cols-3 gap-12">
                {data.tracks ? (
                  <AlbumView album={data} fadeUp={fadeUp} onPickTrack={openTrack} />
                ) : (
                  <>
                    <motion.div variants={fadeUp} className="md:col-span-1 space-y-6">
                      <img src={data.albumArt} alt={`Album art for ${data.title}`}
                        className="w-full rounded-2xl shadow-2xl shadow-neutral-900/50 object-cover aspect-square bg-surface" />
                      <div>
                        <h2 className="text-3xl font-bold tracking-tight">{data.title}</h2>
                        <p className="text-xl text-neutral-400">{data.artist}{data.year ? ` • ${data.year}` : ''}</p>
                      </div>
                    </motion.div>

                    <div className="md:col-span-2 space-y-12">
                      <motion.section variants={fadeUp}>
                        <Heading>Production Credits</Heading>
                        <div className="grid grid-cols-2 gap-6">
                          <Field label="Lead Producer">{credits?.leadProducer || 'Not listed'}</Field>
                          <Field label="Mixing Engineer">{credits?.mixingEngineer || 'Not listed'}</Field>
                          {coProducers.length > 0 && (
                            <Field label="Co-Producers" className="col-span-2">{coProducers.join(', ')}</Field>
                          )}
                          <Field label="Writers" className="col-span-2">
                            {writers.length > 0 ? writers.join(', ') : 'Not listed'}
                          </Field>
                        </div>
                      </motion.section>

                      <motion.section variants={fadeUp}>
                        <SampleTree
                          samples={samples}
                          preview={preview}
                          onTogglePreview={togglePreview}
                          onOpen={(s) => openTrack(`${s.artist} ${s.title}`)}
                          onSuggest={() => setSuggestOpen(true)}
                        />
                      </motion.section>
                    </div>
                  </>
                )}
              </motion.main>
            ) : (
              <motion.div key="empty" initial={{ opacity: 0 }} animate={{ opacity: 1 }} exit={{ opacity: 0 }} className="max-w-xl">
                <p className="text-neutral-400 mb-4">Search a track to see who produced it, who wrote it, and what it samples.</p>
                <div className="flex flex-wrap gap-2">
                  {EXAMPLES.map((ex) => (
                    <button key={ex.label} type="button" onClick={() => openTrack(ex.query)}
                      className="text-sm bg-surface border border-neutral-800 hover:border-neutral-500 rounded-full px-4 py-2 transition-colors focus-visible:outline-2 focus-visible:outline-neutral-500">
                      {ex.label}
                    </button>
                  ))}
                </div>
              </motion.div>
            )}
          </AnimatePresence>
        </div>

        {suggestOpen && data && !data.tracks && (
          <SuggestModal track={data} apiUrl={API_URL} onClose={() => setSuggestOpen(false)} />
        )}

        <footer className="mt-auto pt-24 text-sm text-neutral-600">
          Site by{' '}
          <a
            href="https://vuyisanani.vercel.app"
            target="_blank"
            rel="noopener noreferrer"
            className="text-neutral-400 underline underline-offset-4 decoration-neutral-700 hover:text-neutral-50 hover:decoration-neutral-400 transition-colors focus-visible:outline-2 focus-visible:outline-neutral-500"
          >
            Vuyisanani
          </a>
        </footer>
      </div>
    </MotionConfig>
  );
}