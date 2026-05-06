import type { WorkItemPriority } from '@/api/generated'
import { priorityChip } from '@/lib/priority'
import { cn } from '@/lib/utils'

type Props = {
  priority: WorkItemPriority | undefined
  className?: string
}

export function PriorityChip({ priority, className }: Props) {
  const chip = priorityChip(priority)
  return (
    <span
      title={chip.label}
      aria-label={`Priority: ${chip.label}`}
      className={cn(
        'inline-flex items-center justify-center size-4 rounded-[4px] font-medium leading-none text-[11px] tabular-nums shrink-0',
        className,
      )}
      style={{ backgroundColor: chip.background, color: chip.foreground }}
    >
      {chip.letter}
    </span>
  )
}
