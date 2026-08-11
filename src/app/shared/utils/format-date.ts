export function formatDate(dateStr: string | null | undefined): string {
  if (!dateStr) return '';

  const parsedDate = new Date(dateStr);

  if (isNaN(parsedDate.getTime())) return '';

  return parsedDate.toLocaleDateString('en-US', {
    day: 'numeric',
    month: 'short',
    year: 'numeric',
  });
}
