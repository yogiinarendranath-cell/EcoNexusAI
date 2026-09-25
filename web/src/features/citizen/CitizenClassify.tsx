import { useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { classifyWaste, fetchClassifications } from './citizenClassifyApi';
import type {
  ClassificationListItem,
  ClassifyWasteResponse,
} from '../../types/citizen';

const PRESET_IMAGES = [
  { label: 'Plastic bottle', url: 'https://example.com/plastic-bottle.jpg' },
  { label: 'Banana peel', url: 'https://example.com/banana-peel.jpg' },
  { label: 'Glass jar', url: 'https://example.com/glass-jar.jpg' },
  { label: 'Old phone', url: 'https://example.com/old-phone.jpg' },
];

export default function CitizenClassify() {
  const qc = useQueryClient();
  const [imageUrl, setImageUrl] = useState('');

  const historyQuery = useQuery({
    queryKey: ['citizen', 'classifications'],
    queryFn: fetchClassifications,
  });

  const classify = useMutation({
    mutationFn: (url: string) => classifyWaste(url),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['citizen', 'classifications'] });
    },
  });

  const submit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!imageUrl.trim()) return;
    classify.mutate(imageUrl.trim());
  };

  const usePreset = (url: string) => {
    setImageUrl(url);
    classify.mutate(url);
  };

  return (
    <div className="max-w-md mx-auto px-5 pt-8 pb-4">
      <h1 className="text-2xl font-bold tracking-tight mb-1">Classify an item</h1>
      <p className="text-sm text-slate-400 mb-6">
        Paste an image URL or pick a sample. Our AI will identify the material.
      </p>

      {/* Input form */}
      <form onSubmit={submit} className="space-y-3 mb-4">
        <div>
          <label htmlFor="imageUrl" className="block text-sm text-slate-300 mb-2">
            Image URL
          </label>
          <input
            id="imageUrl"
            type="url"
            value={imageUrl}
            onChange={(e) => setImageUrl(e.target.value)}
            placeholder="https://…"
            maxLength={2048}
            className="w-full rounded-lg border border-slate-700 bg-slate-950 px-4 py-2.5 text-slate-100 placeholder-slate-600 focus:outline-none focus:border-emerald-500"
          />
        </div>

        <button
          type="submit"
          disabled={classify.isPending || !imageUrl.trim()}
          className="w-full rounded-lg bg-emerald-500 px-4 py-3 font-medium text-slate-950 hover:bg-emerald-400 transition disabled:opacity-50 disabled:cursor-not-allowed"
        >
          {classify.isPending ? 'Analyzing…' : 'Classify'}
        </button>
      </form>

      {/* Preset samples */}
      <div className="mb-6">
        <div className="text-xs uppercase tracking-wide text-slate-500 mb-2">
          Sample images
        </div>
        <div className="flex flex-wrap gap-2">
          {PRESET_IMAGES.map((p) => (
            <button
              key={p.url}
              type="button"
              onClick={() => usePreset(p.url)}
              disabled={classify.isPending}
              className="px-3 py-1.5 rounded-full border border-slate-700 bg-slate-900/50 text-xs text-slate-300 hover:border-emerald-500/60 hover:text-emerald-300 transition disabled:opacity-50"
            >
              {p.label}
            </button>
          ))}
        </div>
      </div>

      {/* Error */}
      {classify.isError && (
        <div className="rounded-lg border border-red-500/30 bg-red-500/5 px-4 py-3 text-sm text-red-400 mb-4">
          {classify.error instanceof Error
            ? classify.error.message
            : 'Failed to classify image.'}
        </div>
      )}

      {/* Result card */}
      {classify.data && <ResultCard result={classify.data} />}

      {/* History */}
      <div className="mt-8">
        <div className="text-xs uppercase tracking-wide text-slate-500 mb-3">
          Your classification history
        </div>

        {historyQuery.isLoading && (
          <div className="rounded-2xl border border-slate-800 bg-slate-900/50 p-8 text-center">
            <div className="text-sm text-slate-400">Loading history…</div>
          </div>
        )}

        {historyQuery.data && historyQuery.data.length === 0 && (
          <div className="rounded-2xl border border-slate-800 bg-slate-900/50 p-8 text-center">
            <div className="text-sm text-slate-500">No classifications yet.</div>
          </div>
        )}

        {historyQuery.data && historyQuery.data.length > 0 && (
          <div className="space-y-2">
            {historyQuery.data.map((item) => (
              <HistoryRow key={item.id} item={item} />
            ))}
          </div>
        )}
      </div>
    </div>
  );
}

function ResultCard({ result }: { result: ClassifyWasteResponse }) {
  const confident = result.isConfident;
  const borderClass = confident
    ? 'border-emerald-500/40 bg-emerald-500/5'
    : 'border-amber-500/40 bg-amber-500/5';

  return (
    <div className={'rounded-2xl border p-6 ' + borderClass}>
      <div className="flex items-start justify-between gap-4">
        <div>
          <div className="text-xs uppercase tracking-wide text-slate-400">
            Category
          </div>
          <div className="text-3xl font-bold mt-1 text-slate-100">
            {result.category}
          </div>
        </div>
        <div className="text-right">
          <div className="text-xs uppercase tracking-wide text-slate-400">
            Confidence
          </div>
          <div
            className={
              'text-2xl font-bold mt-1 ' +
              (confident ? 'text-emerald-400' : 'text-amber-400')
            }
          >
            {Math.round(result.confidence * 100)}%
          </div>
        </div>
      </div>

      {!confident && (
        <div className="mt-3 text-xs text-amber-300">
          The model was not confident. Consider uploading a clearer photo.
        </div>
      )}

      <div className="mt-5 flex flex-wrap gap-2">
        <Badge label={result.isRecyclable ? 'Recyclable' : 'Not recyclable'} tone={result.isRecyclable ? 'green' : 'slate'} />
        <Badge label={result.isCompostable ? 'Compostable' : 'Not compostable'} tone={result.isCompostable ? 'green' : 'slate'} />
        <Badge label={'via ' + result.providerName} tone="slate" />
      </div>

      <div className="mt-5 pt-5 border-t border-slate-800/50">
        <div className="text-xs uppercase tracking-wide text-slate-400 mb-1">
          What to do
        </div>
        <div className="text-sm text-slate-200">{result.disposalInstruction}</div>
      </div>
    </div>
  );
}

function Badge({ label, tone }: { label: string; tone: 'green' | 'slate' }) {
  const cls =
    tone === 'green'
      ? 'border-emerald-500/40 bg-emerald-500/10 text-emerald-300'
      : 'border-slate-700 bg-slate-900/50 text-slate-400';
  return (
    <span className={'px-2.5 py-1 rounded-full border text-xs font-medium ' + cls}>
      {label}
    </span>
  );
}

function HistoryRow({ item }: { item: ClassificationListItem }) {
  return (
    <div className="rounded-xl border border-slate-800 bg-slate-900/50 px-4 py-3 flex items-center gap-3">
      <div className="flex-1 min-w-0">
        <div className="text-sm font-medium text-slate-100">
          {item.category}
        </div>
        <div className="text-xs text-slate-500 truncate">
          {item.disposalInstruction}
        </div>
        <div className="text-[10px] text-slate-500 mt-0.5">
          {new Date(item.classifiedAt).toLocaleString()}
        </div>
      </div>
      <div className="text-right">
        <div className="text-sm font-mono text-emerald-400">
          {Math.round(item.confidence * 100)}%
        </div>
      </div>
    </div>
  );
}
