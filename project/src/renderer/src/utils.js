export function extractName(input) {
  if (!input) return ''
  const m = input.match(/botrix\.live\/k\/([^/]+)/)
  return m ? m[1] : input.trim()
}
