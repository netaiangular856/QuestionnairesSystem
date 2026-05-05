import type { AppLang } from '../i18n/translations';

/**
 * Decide which side to translate from for AR/EN pairs.
 * - Only one side filled → translate from that side into the empty field.
 * - Both filled → prefer updating the “other” language from the UI language column (ar UI → AR→EN).
 */
export function resolveBilingualTranslateSource(
  ar: string,
  en: string,
  uiLang: AppLang
): { from: 'ar' | 'en'; source: string } | null {
  const a = ar?.trim() ?? '';
  const e = en?.trim() ?? '';
  if (!a && !e) return null;
  if (a && !e) return { from: 'ar', source: ar };
  if (!a && e) return { from: 'en', source: en };
  return uiLang === 'ar' ? { from: 'ar', source: ar } : { from: 'en', source: en };
}

/**
 * Models often wrap HTML in markdown fences despite instructions: ```html ... ```
 * Strip opening fence (+ optional lang line) and closing ```.
 */
export function stripMarkdownCodeFences(text: string): string {
  const t = text.trim();
  if (!t.startsWith('```')) return text.trim();

  let i = 3;
  while (i < t.length && t[i] !== '\n' && t[i] !== '\r') {
    i++;
  }
  while (i < t.length && (t[i] === '\n' || t[i] === '\r')) {
    i++;
  }
  let body = t.slice(i);
  const close = body.lastIndexOf('```');
  if (close >= 0) {
    body = body.slice(0, close);
  }
  return body.trim();
}

/** Visible text from HTML returned by translate-rich-text (for plain inputs/textareas). */
export function htmlFragmentToPlainText(fragment: string): string {
  const cleaned = stripMarkdownCodeFences(fragment);
  if (!cleaned.trim()) return '';
  if (typeof document === 'undefined') {
    return cleaned.replace(/<[^>]+>/g, ' ').replace(/\s+/g, ' ').trim();
  }
  const el = document.createElement('div');
  el.innerHTML = cleaned;
  return (el.innerText ?? el.textContent ?? '').replace(/\u00a0/g, ' ').trim();
}
