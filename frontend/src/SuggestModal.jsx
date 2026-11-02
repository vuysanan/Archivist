import { useEffect, useRef, useState } from 'react';
import { motion } from 'framer-motion';
import { X, Disc3 } from 'lucide-react';

const inputClass =
  'w-full bg-neutral-900 border border-neutral-800 rounded-xl px-4 py-3 text-sm focus:outline-none focus:border-neutral-500 transition-colors';

export default function SuggestModal({ track, apiUrl, onClose }) {
  const dialogRef = useRef(null);
  const [form, setForm] = useState({ sampleTitle: '', sampleArtist: '', sampleYear: '', type: 'Sample', website: '' });
  const [status, setStatus] = useState('idle'); // idle | sending | done | error
  const [message, setMessage] = useState('');

  // The built-in <dialog> gives us the dimmed backdrop, focus trapping and the Escape key for free.
  useEffect(() => {
    dialogRef.current?.showModal();
  }, []);

  const set = (field) => (e) => setForm({ ...form, [field]: e.target.value });
  const close = () => dialogRef.current?.close();

  const submit = async (e) => {
    e.preventDefault();
    setStatus('sending');
    setMessage('');
    try {
      const res = await fetch(`${apiUrl}/api/suggestions`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ trackTitle: track.title, trackArtist: track.artist, ...form }),
      });
      if (res.status === 429) throw new Error('You’ve sent a few suggestions already. Please try again later.');
      if (!res.ok) throw new Error('That didn’t go through. Check the details and try again.');
      setStatus('done');
    } catch (err) {
      setStatus('error');
      setMessage(err instanceof TypeError ? 'Can’t reach the archive. Check your connection, then try again.' : err.message);
    }
  };

  return (
    <dialog
      ref={dialogRef}
      onClose={onClose}
      onClick={(e) => e.target === dialogRef.current && close()}
      aria-labelledby="suggest-title"
      className="m-auto w-[min(32rem,calc(100%-2rem))] rounded-3xl border border-neutral-800 bg-surface p-0 text-neutral-50 backdrop:bg-black/70 backdrop:backdrop-blur-sm"
    >
      <motion.div initial={{ opacity: 0, y: 16 }} animate={{ opacity: 1, y: 0 }} className="p-8">
        <div className="flex items-start justify-between gap-4 mb-6">
          <div>
            <p className="text-xs uppercase tracking-widest text-neutral-500 mb-2">Suggest a sample</p>
            <h2 id="suggest-title" className="text-2xl font-light tracking-tight">
              {track.title}
              <span className="text-neutral-500"> · {track.artist}</span>
            </h2>
          </div>
          <button type="button" onClick={close} aria-label="Close" className="text-neutral-500 hover:text-neutral-100 transition-colors cursor-pointer">
            <X className="w-5 h-5" />
          </button>
        </div>

        {status === 'done' ? (
          <div>
            <p className="text-neutral-300 mb-6">
              Thanks! Your suggestion is in the queue and will appear on this track once it’s been reviewed.
            </p>
            <button type="button" onClick={close} className="text-sm bg-neutral-50 text-neutral-950 rounded-full px-5 py-2 font-semibold hover:bg-neutral-300 transition-colors cursor-pointer">
              Done
            </button>
          </div>
        ) : (
          <form onSubmit={submit} className="space-y-4">
            <p className="text-sm text-neutral-400">Which song does this track sample or reuse?</p>

            <input className={inputClass} placeholder="Song title" value={form.sampleTitle} onChange={set('sampleTitle')} maxLength={150} required autoFocus aria-label="Sample song title" />
            <input className={inputClass} placeholder="Artist" value={form.sampleArtist} onChange={set('sampleArtist')} maxLength={150} required aria-label="Sample artist" />

            <div className="flex items-center gap-3">
              <input className={`${inputClass} w-28`} placeholder="Year" value={form.sampleYear} onChange={set('sampleYear')} inputMode="numeric" pattern="\d{4}" maxLength={4} aria-label="Year (optional)" />
              <div className="flex gap-1 text-sm" role="group" aria-label="Type">
                {['Sample', 'Interpolation'].map((t) => (
                  <button
                    key={t}
                    type="button"
                    aria-pressed={form.type === t}
                    onClick={() => setForm({ ...form, type: t })}
                    className={`rounded-full px-4 py-1.5 transition-colors cursor-pointer ${
                      form.type === t ? 'bg-neutral-50 text-neutral-950 font-semibold' : 'text-neutral-500 hover:text-neutral-300'
                    }`}
                  >
                    {t}
                  </button>
                ))}
              </div>
            </div>

            {/* Hidden trap for bots. People never see or fill this in. */}
            <div className="absolute -left-[9999px]" aria-hidden="true">
              <input tabIndex={-1} autoComplete="off" name="company_site" value={form.website} onChange={set('website')} />
            </div>

            {status === 'error' && (
              <p role="alert" className="text-sm text-neutral-300">
                {message}
              </p>
            )}

            <div className="flex items-center gap-4 pt-2">
              <button
                type="submit"
                disabled={status === 'sending'}
                className="text-sm bg-neutral-50 text-neutral-950 rounded-full px-5 py-2 font-semibold hover:bg-neutral-300 disabled:opacity-60 transition-colors cursor-pointer inline-flex items-center gap-2"
              >
                {status === 'sending' && <Disc3 className="w-4 h-4 animate-spin" />}
                Send suggestion
              </button>
              <p className="text-xs text-neutral-500">Suggestions are reviewed before they appear.</p>
            </div>
          </form>
        )}
      </motion.div>
    </dialog>
  );
}