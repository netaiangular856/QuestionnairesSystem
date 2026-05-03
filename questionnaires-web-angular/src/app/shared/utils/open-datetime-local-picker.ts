/** Opens the native picker for `<input type="datetime-local">` when supported. */
export function openDatetimeLocalPicker(input: HTMLInputElement): void {
  try {
    const el = input as HTMLInputElement & { showPicker?: () => void };
    if (typeof el.showPicker === 'function') {
      el.showPicker();
      return;
    }
  } catch {
    /* InvalidStateError or unsupported */
  }
  input.focus();
  input.click();
}

/** Opens the native picker for `<input type="date">` when supported. */
export function openDatePicker(input: HTMLInputElement): void {
  openDatetimeLocalPicker(input);
}
