import { useMutation, useQueryClient } from '@tanstack/react-query'
import {
  Activity,
  Flag,
  Link2,
  MoreHorizontal,
  Tag,
  Trash2,
  UserPlus,
  X,
} from 'lucide-react'
import type {
  WorkItemPriority,
  WorkItemResponse,
  WorkItemState,
} from '@/api/generated'
import { workItemsClient } from '@/lib/api'
import { priorityChip, PRIORITY_ORDER } from '@/lib/priority'
import { Button } from '@/components/ui/button'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'

type Props = {
  wsSlug: string
  projKey: string
  selected: WorkItemResponse[]
  onClear: () => void
}

const STATE_OPTIONS: { value: WorkItemState; label: string }[] = [
  { value: 'Open', label: 'Open' },
  { value: 'InProgress', label: 'In progress' },
  { value: 'AwaitingClientReview', label: 'Awaiting review' },
  { value: 'Accepted', label: 'Accepted' },
]

export function SelectionToolbar({ wsSlug, projKey, selected, onClear }: Props) {
  const queryClient = useQueryClient()
  const count = selected.length

  const patch = useMutation({
    mutationFn: async (body: { state?: WorkItemState; priority?: WorkItemPriority }) => {
      await Promise.all(
        selected.map((item) =>
          workItemsClient.workItemsPATCH(wsSlug, projKey, item.number, {
            title: undefined,
            description: undefined,
            assigneeId: undefined,
            estimatePoints: undefined,
            state: body.state,
            priority: body.priority,
          }),
        ),
      )
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['work-items', wsSlug, projKey] })
    },
  })

  if (count === 0) return null

  return (
    <div
      className="fixed bottom-4 left-[200px] right-0 flex justify-center pointer-events-none z-10"
      data-test="selection-controls-panel"
    >
      <div className="pointer-events-auto flex items-center gap-1 h-10 pl-4 pr-2 rounded-md bg-popover text-popover-foreground border border-border shadow-md">
        <span className="text-xs text-muted-foreground tabular-nums pr-2 border-r border-border">
          {count} selected
        </span>

        <DropdownMenu>
          <DropdownMenuTrigger
            render={
              <Button
                variant="ghost"
                size="icon"
                className="size-8"
                title="Update state"
                aria-label="Update state"
                disabled={patch.isPending}
              >
                <Activity className="size-4" />
              </Button>
            }
          />
          <DropdownMenuContent align="center">
            {STATE_OPTIONS.map((opt) => (
              <DropdownMenuItem
                key={opt.value}
                onClick={() => patch.mutate({ state: opt.value })}
              >
                {opt.label}
              </DropdownMenuItem>
            ))}
          </DropdownMenuContent>
        </DropdownMenu>

        <DropdownMenu>
          <DropdownMenuTrigger
            render={
              <Button
                variant="ghost"
                size="icon"
                className="size-8"
                title="Update priority"
                aria-label="Update priority"
                disabled={patch.isPending}
              >
                <Flag className="size-4" />
              </Button>
            }
          />
          <DropdownMenuContent align="center">
            {PRIORITY_ORDER.map((p) => {
              const chip = priorityChip(p)
              return (
                <DropdownMenuItem
                  key={p}
                  onClick={() => patch.mutate({ priority: p })}
                  className="gap-2"
                >
                  <span
                    className="inline-flex items-center justify-center size-4 rounded-[4px] text-[11px] font-medium leading-none shrink-0"
                    style={{ backgroundColor: chip.background, color: chip.foreground }}
                  >
                    {chip.letter}
                  </span>
                  <span>{chip.label}</span>
                </DropdownMenuItem>
              )
            })}
          </DropdownMenuContent>
        </DropdownMenu>

        <PlaceholderButton icon={UserPlus} label="Update assignee" />
        <PlaceholderButton icon={Tag} label="Add tag" />
        <PlaceholderButton icon={Link2} label="Add link" />
        <PlaceholderButton icon={MoreHorizontal} label="More actions" />

        <span className="mx-1 h-6 w-px bg-border" aria-hidden />

        <Button
          variant="ghost"
          size="icon"
          className="size-8 text-destructive hover:text-destructive"
          title={`Delete ${count} ${count === 1 ? 'item' : 'items'} (coming soon)`}
          aria-label={`Delete ${count} ${count === 1 ? 'item' : 'items'} (coming soon)`}
          disabled
        >
          <Trash2 className="size-4" />
        </Button>

        <Button
          variant="ghost"
          size="icon"
          className="size-8"
          onClick={onClear}
          title="Clear selection"
          aria-label="Clear selection"
        >
          <X className="size-4" />
        </Button>
      </div>
    </div>
  )
}

function PlaceholderButton({
  icon: Icon,
  label,
}: {
  icon: typeof Tag
  label: string
}) {
  return (
    <Button
      variant="ghost"
      size="icon"
      className="size-8"
      title={`${label} (coming soon)`}
      aria-label={`${label} (coming soon)`}
      disabled
    >
      <Icon className="size-4" />
    </Button>
  )
}
