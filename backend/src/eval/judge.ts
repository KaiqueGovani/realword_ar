import axios from 'axios';
import { SentencesDto } from '../sentences/dto/sentences.dto';

export type JudgeScores = {
  quality: number;
  correctness: number;
  usefulness: number;
  notes: string[];
};

export async function judgeWithGemini(candidate: SentencesDto, options?: {
  apiKey?: string;
  model?: string;
  apiVersion?: 'v1' | 'v1beta';
  timeoutMs?: number;
}): Promise<JudgeScores> {
  const apiKey = options?.apiKey || process.env.GOOGLE_API_KEY || process.env.GEMINI_API_KEY || '';
  const model = options?.model || process.env.JUDGE_MODEL || process.env.GOOGLE_MODEL || 'gemini-2.5-flash';
  const apiVersion: 'v1' | 'v1beta' = options?.apiVersion || (process.env.GOOGLE_API_VERSION === 'v1beta' ? 'v1beta' : 'v1');
  const timeout = options?.timeoutMs ?? 30000;

  if (!apiKey) {
    return { quality: 0, correctness: 0, usefulness: 0, notes: ['Missing GOOGLE_API_KEY'] };
  }

  const rubric = `You are an evaluator for beginner English learning content.
Assess the candidate output with three metrics from 1-5 (integers only):
- quality: grammar/clarity and natural English
- correctness: translation matches each English sentence in meaning
- usefulness: simple and practical for absolute beginners

Rules:
- Only return strict JSON matching this schema:
  { "quality": n, "correctness": n, "usefulness": n, "notes": ["...", "..."] }
- Do not include markdown or extra text.
- Be concise in notes (bulleted strings).`;

  const packed = JSON.stringify(candidate);
  const user = `Candidate output to evaluate:\n${packed}`;

  const url = `https://generativelanguage.googleapis.com/${apiVersion}/models/${encodeURIComponent(model)}:generateContent?key=${encodeURIComponent(apiKey)}`;

  const body = {
    contents: [
      { role: 'user', parts: [{ text: `${rubric}\n\n${user}` }] },
    ],
    generationConfig: {
      temperature: 0.1,
    },
  } as const;

  try {
    const resp = await axios.post(url, body, {
      headers: { 'Content-Type': 'application/json', 'x-goog-api-key': apiKey },
      timeout,
    });

    const text = cleanForJson(extractText(resp.data));
    try {
      const parsed = JSON.parse(text);
      return normalizeScores(parsed);
    } catch {
      return { quality: 0, correctness: 0, usefulness: 0, notes: ['Judge returned non-JSON', text.slice(0, 200)] };
    }
  } catch (err: any) {
    const status = err?.response?.status;
    const detail = safeStringify(err?.response?.data) || String(err?.message || err);
    return {
      quality: 0,
      correctness: 0,
      usefulness: 0,
      notes: [`Judge error status=${status ?? 'n/a'}`, detail.slice(0, 300)],
    };
  }
}

function extractText(data: any): string {
  const c = data?.candidates;
  if (Array.isArray(c) && c.length > 0) {
    const parts = c[0]?.content?.parts;
    if (Array.isArray(parts) && parts.length > 0) {
      const t = parts[0]?.text ?? '';
      if (typeof t === 'string') return t.trim();
    }
  }
  return '';
}

function normalizeScores(obj: any): JudgeScores {
  const toInt = (v: any) => {
    const n = Number.parseInt(String(v), 10);
    if (Number.isFinite(n)) return Math.max(1, Math.min(5, n));
    return 0;
  };
  const notes = Array.isArray(obj?.notes) ? obj.notes.map((s: any) => String(s)).slice(0, 5) : [];
  return {
    quality: toInt(obj?.quality),
    correctness: toInt(obj?.correctness),
    usefulness: toInt(obj?.usefulness),
    notes,
  };
}

function safeStringify(v: any): string {
  try { return JSON.stringify(v); } catch { return ''; }
}

function cleanForJson(text: string): string {
  const t = (text || '').trim();
  if (t.startsWith('```')) {
    return t.replace(/```json|```/gi, '').trim();
  }
  return t;
}
