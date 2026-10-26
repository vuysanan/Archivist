import { useState, useRef } from 'react';
import { motion, AnimatePresence, MotionConfig } from 'framer-motion';
import { Search, Disc3, Mic2, Activity } from 'lucide-react';
import ErrorState from './ErrorState';
import AlbumView from './AlbumView';

const API_URL = import.meta.env.VITE_API_URL ?? 'https://localhost:7266';
const EXAMPLES = ['DNA.', 'Juicy', 'Nice For What'];

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

  const runSearch = async (term, searchMode = mode) => {
    const q = term.trim();
    if (!q) return;

    requestRef.current?.abort();
    const controller = new AbortController();
    requestRef.current = controller;
    setLoading(true);
    setError(null);

    const path = searchMode === 'album' ? 'album' : 'search';
    try {
      const res = await fetch(`${API_URL}/api/archive/${path}?q=${encodeURIComponent(q)}`, {
        signal: controller.signal,
      });
      if (res.status === 404 || res.status === 400) throw { kind: 'notfound' };
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

  const openTrack = (term) => {
    setMode('track');
    setQuery(term);
    runSearch(term, 'track');
  };

  const changeMode = (next) => {
    setMode(next);
    if (query.trim()) runSearch(query, next);
  };

  const credits = data?.credits;
  const coProducers = credits?.coProducers ?? [];
  const writers = credits?.writers ?? [];
  const samples = data?.samples ?? [];

  return (
    <MotionConfig reducedMotion="user">
      <div className="min-h-screen p-8 md:p-16 max-w-5xl mx-auto font-sans">
        <header className="mb-12">
          <h1 className="text-4xl font-extrabold tracking-tighter mb-4">
            ARCHIVIST<span className="text-neutral-500">.</span>
          </h1>

          <form onSubmit={(e) => { e.preventDefault(); runSearch(query); }} className="relative max-w-xl" role="search">
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

        <div aria-live="polite">
          <AnimatePresence mode="wait">
            {loading ? (
              <motion.div key="loader" initial={{ opacity: 0 }} animate={{ opacity: 1 }} exit={{ opacity: 0 }}
                className="flex gap-2 items-center text-neutral-500 font-mono text-sm">
                <Disc3 className="animate-spin w-4 h-4" /> Aggregating metadata...
              </motion.div>
            ) : error ? (
              <ErrorState key="error" kind={error.kind} term={error.term}
                onRetry={() => runSearch(error.term, error.mode)} />
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
                        {data.bpm > 0 && (
                          <div className="flex items-center gap-2 mt-2 text-neutral-500 font-mono text-xs">
                            <Activity className="w-4 h-4" /> {data.bpm} BPM
                          </div>
                        )}
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
                        <Heading>Sample Tree</Heading>
                        {samples.length === 0 ? (
                          <p className="text-sm text-neutral-500">No samples found for this track.</p>
                        ) : (
                          <ul className="space-y-4">
                            {samples.map((s) => (
                              <li key={s.id}>
                                <motion.button type="button" onClick={() => openTrack(`${s.artist} ${s.title}`)}
                                  whileHover={{ scale: 1.02, x: 5 }} whileTap={{ scale: 0.98 }}
                                  className="w-full text-left bg-surface p-4 rounded-xl border border-neutral-800 flex items-center justify-between gap-4 cursor-pointer focus-visible:outline-2 focus-visible:outline-neutral-500">
                                  <div className="flex items-center gap-4">
                                    <div className="bg-neutral-800 p-3 rounded-lg"><Mic2 className="w-5 h-5 text-neutral-400" /></div>
                                    <div>
                                      <p className="font-bold">{s.title}</p>
                                      <p className="text-sm text-neutral-400">{s.artist}{s.year ? ` (${s.year})` : ''}</p>
                                    </div>
                                  </div>
                                  <span className="text-xs font-mono text-neutral-500 bg-neutral-900 px-3 py-1 rounded-full shrink-0">{s.type}</span>
                                </motion.button>
                              </li>
                            ))}
                          </ul>
                        )}
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
                    <button key={ex} type="button" onClick={() => openTrack(ex)}
                      className="text-sm bg-surface border border-neutral-800 hover:border-neutral-500 rounded-full px-4 py-2 transition-colors focus-visible:outline-2 focus-visible:outline-neutral-500">
                      {ex}
                    </button>
                  ))}
                </div>
              </motion.div>
            )}
          </AnimatePresence>
        </div>
      </div>
    </MotionConfig>
  );
}