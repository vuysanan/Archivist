import { useState } from 'react';

const API_URL = import.meta.env.VITE_API_URL ?? 'https://localhost:7266';

// Review queue, opened by visiting the site with #admin on the end. Protected by the admin key, not by the page address.
export default function AdminPanel() {
  const [key, setKey] = useState('');
  const [items, setItems] = useState(null);
  const [error, setError] = useState('');

  const call = (path, method = 'GET') =>
    fetch(`${API_URL}/api/admin/suggestions${path}`, { method, headers: { 'X-Admin-Key': key } });

  const load = async (e) => {
    e.preventDefault();
    setError('');
    try {
      const res = await call('?status=Pending');
      if (res.status === 401 || res.status === 404) throw new Error('Wrong key, or admin is switched off.');
      if (!res.ok) throw new Error('Something went wrong.');
      setItems(await res.json());
    } catch (err) {
      setItems(null);
      setError(err.message);
    }
  };

  const decide = async (id, action) => {
    const res = await call(`/${id}/${action}`, 'POST');
    if (res.ok) setItems(items.filter((i) => i.id !== id));
    else setError('Could not update that suggestion.');
  };

  return (
    <div className="min-h-screen p-8 md:p-16 max-w-3xl mx-auto font-sans">
      <h1 className="text-3xl font-extrabold tracking-tighter mb-1">
        ARCHIVIST<span className="text-neutral-500">.</span> <span className="text-neutral-500 font-light">admin</span>
      </h1>
      <a href="/" className="text-sm text-neutral-500 hover:text-neutral-200 underline underline-offset-4">Back to the archive</a>

      <form onSubmit={load} className="mt-10 flex gap-3">
        <input
          type="password"
          value={key}
          onChange={(e) => setKey(e.target.value)}
          placeholder="Admin key"
          aria-label="Admin key"
          autoComplete="off"
          className="flex-1 bg-surface border border-neutral-800 rounded-full px-5 py-3 text-sm focus:outline-none focus:border-neutral-500"
        />
        <button type="submit" className="text-sm bg-neutral-50 text-neutral-950 rounded-full px-5 py-2 font-semibold hover:bg-neutral-300 transition-colors cursor-pointer">
          Load pending
        </button>
      </form>

      {error && <p role="alert" className="mt-4 text-sm text-neutral-300">{error}</p>}

      {items && (
        <div className="mt-10 space-y-4">
          {items.length === 0 && <p className="text-neutral-500">Nothing waiting for review.</p>}
          {items.map((i) => (
            <div key={i.id} className="bg-surface border border-neutral-800 rounded-xl p-4">
              <p className="text-xs font-mono text-neutral-500 mb-2">
                ON {i.trackTitle} · {i.trackArtist}
              </p>
              <p className="font-bold">
                {i.sampleTitle} <span className="font-normal text-neutral-400">· {i.sampleArtist}{i.sampleYear ? ` (${i.sampleYear})` : ''}</span>
              </p>
              <p className="text-xs font-mono text-neutral-500 mt-1">{i.type}</p>
              <div className="flex gap-3 mt-4">
                <button type="button" onClick={() => decide(i.id, 'approve')} className="text-sm bg-neutral-50 text-neutral-950 rounded-full px-4 py-1.5 font-semibold hover:bg-neutral-300 transition-colors cursor-pointer">Approve</button>
                <button type="button" onClick={() => decide(i.id, 'reject')} className="text-sm text-neutral-400 hover:text-neutral-100 border border-neutral-800 rounded-full px-4 py-1.5 transition-colors cursor-pointer">Reject</button>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}