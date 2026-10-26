import { motion } from 'framer-motion';

const VARIANTS = {
  notfound: {
    code: '404',
    label: 'Off the record',
    title: (term) => `“${term}” isn’t in the archive.`,
    hint: 'Check the spelling, or try the artist and title together.',
  },
  server: {
    code: '500',
    label: 'Missed beat',
    title: () => 'The archive hit a problem.',
    hint: 'That one’s on us. Try again in a moment.',
  },
  network: {
    code: 'OFF',
    label: 'No signal',
    title: () => 'Can’t reach the archive.',
    hint: 'Make sure the API is running, then try again.',
  },
};

export default function ErrorState({ kind = 'server', term = '', onRetry }) {
  const v = VARIANTS[kind] ?? VARIANTS.server;

  return (
    <motion.div
      role="alert"
      initial={{ opacity: 0 }}
      animate={{ opacity: 1 }}
      exit={{ opacity: 0 }}
      className="relative overflow-hidden rounded-3xl border border-neutral-800 bg-surface min-h-[24rem] md:min-h-[28rem]"
    >
      {/* Giant ghosted code, the way the reference sits behind its text */}
      <motion.span
        aria-hidden="true"
        initial={{ y: 60, opacity: 0 }}
        animate={{ y: 0, opacity: 1 }}
        transition={{ type: 'spring', stiffness: 120, damping: 20 }}
        className="pointer-events-none select-none absolute inset-x-0 bottom-[-0.14em] text-center font-extrabold leading-none tracking-tighter text-neutral-800 text-[clamp(9rem,34vw,24rem)]"
      >
        {v.code}
      </motion.span>

      <div className="relative p-8 md:p-12 max-w-xl">
        <p className="text-xs uppercase tracking-widest text-neutral-500 mb-4">{v.label}</p>
        <h2 className="text-3xl md:text-4xl font-light tracking-tight text-neutral-50 mb-3">
          {v.title(term)}
        </h2>
        <p className="text-neutral-400 mb-6">{v.hint}</p>
        {onRetry && (
          <button
            type="button"
            onClick={onRetry}
            className="text-sm bg-neutral-50 text-neutral-950 rounded-full px-5 py-2 font-semibold hover:bg-neutral-300 transition-colors focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-neutral-50"
          >
            Try again
          </button>
        )}
      </div>
    </motion.div>
  );
}