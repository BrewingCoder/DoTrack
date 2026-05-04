import { useQuery } from '@tanstack/react-query'
import { Link, useParams } from '@tanstack/react-router'
import { workItemsClient } from '@/lib/api'
import { Badge } from '@/components/ui/badge'
import {
  Table,
  TableBody,
  TableCaption,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'
import type { WorkItemState, WorkItemTier } from '@/api/generated'

export function WorkItemsPage() {
  const { wsSlug, projKey } = useParams({
    from: '/workspaces/$wsSlug/projects/$projKey/items',
  })

  const itemsQuery = useQuery({
    queryKey: ['work-items', wsSlug, projKey],
    queryFn: () => workItemsClient.workItemsAll(wsSlug, projKey),
  })

  return (
    <section className="p-8 max-w-5xl mx-auto space-y-6">
      <header>
        <Link
          to="/workspaces/$wsSlug"
          params={{ wsSlug }}
          className="text-sm text-muted-foreground hover:underline"
        >
          ← Projects
        </Link>
        <h1 className="text-2xl font-semibold tracking-tight mt-2">
          {projKey} — Work items
        </h1>
        <p className="text-muted-foreground text-sm">
          Workspace: <code className="font-mono">{wsSlug}</code>
        </p>
      </header>

      <Table>
        <TableCaption>
          {itemsQuery.data?.length ?? 0} item
          {itemsQuery.data?.length === 1 ? '' : 's'} in this project.
        </TableCaption>
        <TableHeader>
          <TableRow>
            <TableHead className="w-[110px]">Key</TableHead>
            <TableHead>Title</TableHead>
            <TableHead className="w-[100px]">Tier</TableHead>
            <TableHead className="w-[100px]">Type</TableHead>
            <TableHead className="w-[160px]">State</TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          {itemsQuery.isLoading && (
            <TableRow>
              <TableCell colSpan={5} className="text-muted-foreground">
                Loading items…
              </TableCell>
            </TableRow>
          )}
          {itemsQuery.isError && (
            <TableRow>
              <TableCell colSpan={5} className="text-destructive">
                {itemsQuery.error instanceof Error && itemsQuery.error.message.includes('404')
                  ? 'Project or workspace not found.'
                  : 'Failed to load work items.'}
              </TableCell>
            </TableRow>
          )}
          {itemsQuery.data?.map((w) => (
            <TableRow key={w.id}>
              <TableCell className="font-mono">{w.key}</TableCell>
              <TableCell className="font-medium">{w.title}</TableCell>
              <TableCell>
                <Badge variant={tierVariant(w.tier)}>{w.tier}</Badge>
              </TableCell>
              <TableCell className="text-muted-foreground">{w.type ? String(w.type) : '—'}</TableCell>
              <TableCell>
                <Badge variant={stateVariant(w.state)}>{stateLabel(w.state)}</Badge>
              </TableCell>
            </TableRow>
          ))}
          {itemsQuery.data?.length === 0 && (
            <TableRow>
              <TableCell colSpan={5} className="text-muted-foreground">
                No work items in this project yet.
              </TableCell>
            </TableRow>
          )}
        </TableBody>
      </Table>
    </section>
  )
}

function tierVariant(tier: WorkItemTier): 'default' | 'secondary' | 'outline' {
  switch (tier) {
    case 'Epic':
      return 'default'
    case 'Feature':
      return 'secondary'
    case 'Item':
      return 'outline'
  }
}

function stateVariant(state: WorkItemState): 'default' | 'secondary' | 'outline' | 'destructive' {
  switch (state) {
    case 'Open':
      return 'outline'
    case 'InProgress':
      return 'default'
    case 'AwaitingClientReview':
      return 'secondary'
    case 'Accepted':
      return 'secondary'
  }
}

function stateLabel(state: WorkItemState): string {
  switch (state) {
    case 'InProgress':
      return 'In progress'
    case 'AwaitingClientReview':
      return 'Awaiting review'
    default:
      return state
  }
}
