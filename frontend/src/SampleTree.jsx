import { motion } from 'framer-motion';
import { Play, Pause, Plus, BadgeCheck, Disc3, VolumeX } from 'lucide-react';

// The round button on each sample: play or pause a 30-second preview.
function PlayButton({ sample, preview, onToggle }) {
  const status = preview.id === sample.id ? preview.status : 'idle';
  const noPreview = status === 'none';
  const Icon = status === 'playing' ? Pause : noPreview ? VolumeX : Play;

  return (
    <button
      type="button"
      onClick={() => onToggle(sample)}
      disabled={noPreview}
      aria-label={status === 'playing' ? `Pause preview of ${sample.title}` : `Play preview of ${sample.title}`}
      title={noPreview ? 'No preview available' : status === 'playing' ? 'Pause' : 'Play 30-second preview'}
      className="shrink-0 bg-neutral-800 hover:bg-neutral-700 disabled:opacity-40 disabled:hover:bg-neutral-800 p-3 rounded-lg transition-colors cursor-pointer disabled:cursor-not-allowed focus-visible:outline-2 focus-visible:outline-neutral-500"
    >
      {status === 'loading' ? (
        <Disc3 className="w-5 h-5 text-neutral-400 animate-spin" />
      ) : (
        <Icon className="w-5 h-5 text-neutral-400" />
      )}
    </button>
  );
}

export default function SampleTree({ samples, preview, onTogglePreview, onOpen, onSuggest }) {
  return (
    <>
      <h3 className="text-sm font-mono text-neutral-500 uppercase tracking-widest mb-6 border-b border-neutral-800 pb-2">
        Sample Tree
      </h3>

      {samples.length === 0 ? (
        <p className="text-sm text-neutral-500">No samples found for this track.</p>
      ) : (
        <ul className="space-y-4">
          {samples.map((s) => (
            <li key={s.id}>
              <motion.div
                whileHover={{ scale: 1.02, x: 5 }}
                className="bg-surface p-4 rounded-xl border border-neutral-800 flex items-center gap-4"
              >
                <PlayButton sample={s} preview={preview} onToggle={onTogglePreview} />

                {/* Clicking the rest of the card searches for that song, so you can follow the tree back */}
                <button
                  type="button"
                  onClick={() => onOpen(s)}
                  className="flex-1 min-w-0 flex items-center justify-between gap-4 text-left cursor-pointer focus-visible:outline-2 focus-visible:outline-neutral-500"
                >
                  <div className="min-w-0">
                    <p className="font-bold">{s.title}</p>
                    <p className="text-sm text-neutral-400">
                      {s.artist}
                      {s.year ? ` (${s.year})` : ''}
                    </p>
                    {s.communityVerified && (
                      <p className="mt-1 inline-flex items-center gap-1 text-xs text-neutral-400">
                        <BadgeCheck className="w-3.5 h-3.5" /> Community verified
                      </p>
                    )}
                  </div>
                  <span className="text-xs font-mono text-neutral-500 bg-neutral-900 px-3 py-1 rounded-full shrink-0">
                    {s.type}
                  </span>
                </button>
              </motion.div>
            </li>
          ))}
        </ul>
      )}

      <button
        type="button"
        onClick={onSuggest}
        className="mt-6 inline-flex items-center gap-2 text-sm text-neutral-500 hover:text-neutral-200 transition-colors cursor-pointer focus-visible:outline-2 focus-visible:outline-neutral-500"
      >
        <Plus className="w-4 h-4" /> Suggest a sample
      </button>
    </>
  );
}