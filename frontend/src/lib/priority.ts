import type { WorkItemPriority } from '@/api/generated'

type Chip = {
  letter: string
  label: string
  background: string
  foreground: string
}

// Palette captured from YouTrack 2026.x dark theme (color-fields__background-N).
// Letters mirror YT's per-row chip glyph ("Show stopper" = S, "Minor" = m).
const CHIPS: Record<WorkItemPriority, Chip> = {
  ShowStopper: {
    letter: 'S',
    label: 'Show-stopper',
    background: 'rgb(219, 92, 92)',
    foreground: 'rgb(255, 243, 244)',
  },
  Critical: {
    letter: 'C',
    label: 'Critical',
    background: 'rgb(238, 75, 167)',
    foreground: 'rgb(51, 9, 32)',
  },
  Major: {
    letter: 'M',
    label: 'Major',
    background: 'rgb(245, 210, 115)',
    foreground: 'rgb(71, 58, 39)',
  },
  Normal: {
    letter: 'N',
    label: 'Normal',
    background: 'rgb(184, 231, 188)',
    foreground: 'rgb(55, 82, 57)',
  },
  Minor: {
    letter: 'm',
    label: 'Minor',
    background: 'rgb(168, 226, 220)',
    foreground: 'rgb(9, 106, 110)',
  },
}

export function priorityChip(priority: WorkItemPriority | undefined): Chip {
  return CHIPS[priority ?? 'Normal']
}

export const PRIORITY_ORDER: WorkItemPriority[] = [
  'ShowStopper',
  'Critical',
  'Major',
  'Normal',
  'Minor',
]
