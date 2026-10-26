import { motion } from 'framer-motion';

// Rendered inside App's staggered <motion.main>, so it inherits the hidden/show variants.
export default function AlbumView({ album, fadeUp, onPickTrack }) {
  const tracks = album.tracks ?? [];

  return (
    <>
      <motion.div variants={fadeUp} className="md:col-span-1 space-y-6">
        <img
          src={album.albumArt}
          alt={`Cover of ${album.title}`}
          className="w-full rounded-2xl shadow-2xl shadow-neutral-900/50 object-cover aspect-square bg-surface"
        />
        <div>
          <h2 className="text-3xl font-bold tracking-tight">{album.title}</h2>
          <p className="text-xl text-neutral-400">
            {album.artist}
            {album.year ? ` • ${album.year}` : ''}
          </p>
        </div>
      </motion.div>

      <motion.section variants={fadeUp} className="md:col-span-2">
        <h3 className="text-sm font-mono text-neutral-500 uppercase tracking-widest mb-6 border-b border-neutral-800 pb-2">
          Tracklist
        </h3>
        <ol>
          {tracks.map((t) => (
            <li key={t.number}>
              <button
                type="button"
                onClick={() => onPickTrack(`${album.artist} ${t.title}`)}
                className="group w-full flex items-baseline gap-4 py-3 border-b border-neutral-900 text-left focus-visible:outline-2 focus-visible:outline-neutral-500"
              >
                <span className="w-6 text-right font-mono text-xs text-neutral-600">{t.number}</span>
                <span className="font-medium group-hover:translate-x-1 transition-transform">{t.title}</span>
              </button>
            </li>
          ))}
        </ol>
      </motion.section>
    </>
  );
}