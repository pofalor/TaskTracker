/**
 * Приводит время к формату ввода 2h 3m 11s (в т.ч. из ответа API вида 02:03:11).
 */
export function formatTimeSpentForInput(value: string | undefined): string | undefined {
  if (!value?.trim()) {
    return undefined;
  }

  const trimmed = value.trim();
  if (/[hms](\s|$)/i.test(trimmed)) {
    return trimmed;
  }

  let hours = 0;
  let minutes = 0;
  let seconds = 0;
  let timeSegment = trimmed;

  const dotIndex = trimmed.indexOf('.');
  if (dotIndex >= 0) {
    const days = parseInt(trimmed.substring(0, dotIndex), 10) || 0;
    hours += days * 24;
    timeSegment = trimmed.substring(dotIndex + 1);
  }

  const parts = timeSegment.split(':').map((part) => parseInt(part, 10) || 0);
  if (parts.length >= 3) {
    hours += parts[0];
    minutes = parts[1];
    seconds = parts[2];
  } else if (parts.length === 2) {
    minutes = parts[0];
    seconds = parts[1];
  } else if (parts.length === 1) {
    seconds = parts[0];
  } else {
    return trimmed;
  }

  return `${hours}h ${minutes}m ${seconds}s`;
}
