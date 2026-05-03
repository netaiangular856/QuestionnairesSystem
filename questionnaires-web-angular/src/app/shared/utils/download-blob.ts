import { HttpResponse } from '@angular/common/http';

/** Saves a blob response as a file using Content-Disposition filename when present. */
export function triggerBlobDownload(response: HttpResponse<Blob>, fallbackFileName: string): void {
  const body = response.body;
  if (!body || body.size === 0) {
    return;
  }

  let name = fallbackFileName;
  const cd = response.headers.get('Content-Disposition');
  if (cd) {
    const utfMatch = /filename\*=(?:UTF-8'')?([^;\n]+)/i.exec(cd);
    const asciiMatch = /filename="([^"]+)"/i.exec(cd);
    const loose = /filename=([^;\n]+)/i.exec(cd);
    const raw = utfMatch?.[1]?.trim() ?? asciiMatch?.[1]?.trim() ?? loose?.[1]?.trim();
    if (raw) {
      try {
        name = decodeURIComponent(raw.replace(/^["']|["']$/g, ''));
      } catch {
        name = raw.replace(/^["']|["']$/g, '');
      }
    }
  }

  const url = URL.createObjectURL(body);
  const anchor = document.createElement('a');
  anchor.href = url;
  anchor.download = name;
  anchor.click();
  URL.revokeObjectURL(url);
}
