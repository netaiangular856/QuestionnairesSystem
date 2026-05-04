import { Component, Input, ViewChild, forwardRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ControlValueAccessor, FormsModule, NG_VALUE_ACCESSOR } from '@angular/forms';
import { Editor, EditorInitEvent } from 'primeng/editor';
import { TranslatePipe } from '../../pipes/translate.pipe';

export type PublicArticleEditorLang = 'ar' | 'en';

@Component({
  selector: 'app-public-article-editor',
  standalone: true,
  imports: [CommonModule, FormsModule, Editor, TranslatePipe],
  templateUrl: './public-article-editor.component.html',
  styleUrl: './public-article-editor.component.scss',
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => PublicArticleEditorComponent),
      multi: true,
    },
  ],
})
export class PublicArticleEditorComponent implements ControlValueAccessor {
  @ViewChild(Editor) editorCmp?: Editor;

  /** Language for inserted snippet placeholders (section titles and hint text). */
  @Input() lang: PublicArticleEditorLang = 'ar';

  @Input() placeholderKey = 'q.surveys.form.publicArticleEditorPlaceholder';

  value = '';
  disabled = false;
  private quill: { clipboard: { dangerouslyPasteHTML: (i: number, h: string) => void }; getSelection: (f?: boolean) => { index: number } | null; getLength: () => number; setSelection: (i: number, len?: number) => void } | null =
    null;

  private onChange: (v: string) => void = () => {};
  private onTouched: () => void = () => {};

  writeValue(v: string | null | undefined): void {
    this.value = v ?? '';
  }

  registerOnChange(fn: (v: string) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    this.disabled = isDisabled;
  }

  onEditorChange(html: string | null): void {
    const v = html ?? '';
    this.value = v;
    this.onChange(v);
    this.onTouched();
  }

  onEditorInit(e: EditorInitEvent): void {
    this.quill = e.editor;
  }

  private pasteHtml(fragment: string): void {
    const q = this.quill ?? this.editorCmp?.getQuill?.() ?? null;
    if (!q) return;
    const range = q.getSelection(true);
    const index = range ? range.index : Math.max(0, q.getLength() - 1);
    q.clipboard.dangerouslyPasteHTML(index, fragment);
    this.onTouched();
  }

  insertSection(): void {
    const html =
      this.lang === 'ar'
        ? '<h2>عنوان القسم</h2><p>اكتب المحتوى هنا. يمكنك استخدام العناوين الفرعية والقوائم من شريط الأدوات.</p>'
        : '<h2>Section title</h2><p>Write your content here. Use subheadings and lists from the toolbar.</p>';
    this.pasteHtml(html);
  }

  insertCallout(): void {
    const html =
      this.lang === 'ar'
        ? '<blockquote>نص مميز للتنبيه أو الاقتباس المهم.</blockquote><p></p>'
        : '<blockquote>Highlighted note or important quote.</blockquote><p></p>';
    this.pasteHtml(html);
  }

  /** Multi-section layout similar to the public portal structure. */
  insertFullTemplate(): void {
    const html =
      this.lang === 'ar'
        ? `<h2>لماذا هذا الاستبيان؟</h2><p>صف الهدف بجملة أو اثنتين.</p><h2>كيف تُستخدم إجاباتك</h2><p>اشرح الخصوصية والاستخدام باختصار.</p><h2>قبل أن تبدأ</h2><ul><li>خذ الوقت الكافي</li><li>يمكنك العودة قبل الإرسال</li></ul><p></p>`
        : `<h2>Why this survey?</h2><p>State the purpose in a sentence or two.</p><h2>How your answers are used</h2><p>Briefly explain privacy and usage.</p><h2>Before you start</h2><ul><li>Take your time</li><li>You can review before submit</li></ul><p></p>`;
    this.pasteHtml(html);
  }
}
