import { useQuery } from '@tanstack/react-query'
import { Link, useParams } from '@tanstack/react-router'
import { auditClient, commentsClient, workItemsClient } from '@/lib/api'
import { Badge } from '@/components/ui/badge'
import { Separator } from '@/components/ui/separator'
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs'
import type { AuditLogResponse, CommentResponse, WorkItemState, WorkItemTier } from '@/api/generated'

export function WorkItemDetailPage() {
  const { wsSlug, projKey, number: numberParam } = useParams({
    from: '/workspaces/$wsSlug/projects/$projKey/items/$number',
  })
  const number = Number(numberParam)

  const itemQuery = useQuery({
    queryKey: ['work-item', wsSlug, projKey, number],
    queryFn: () => workItemsClient.workItemsGET(wsSlug, projKey, number),
  })
  const commentsQuery = useQuery({
    queryKey: ['comments', wsSlug, projKey, number],
    queryFn: () => commentsClient.commentsAll(wsSlug, projKey, number, undefined),
    enabled: !!itemQuery.data,
  })
  const historyQuery = useQuery({
    queryKey: ['history', wsSlug, projKey, number],
    queryFn: () => auditClient.history(wsSlug, projKey, number, undefined),
    enabled: !!itemQuery.data,
  })

  if (itemQuery.isLoading) {
    return <Status>Loading work item…</Status>
  }
  if (itemQuery.isError || !itemQuery.data) {
    return <Status tone="error">Work item not found.</Status>
  }

  const item = itemQuery.data

  return (
    <section className="p-8 max-w-5xl mx-auto space-y-8">
      <header className="space-y-2">
        <Link
          to="/workspaces/$wsSlug/projects/$projKey/items"
          params={{ wsSlug, projKey }}
          className="text-sm text-muted-foreground hover:underline"
        >
          ← {projKey} items
        </Link>
        <div className="flex items-baseline gap-3">
          <code className="font-mono text-muted-foreground text-lg">{item.key}</code>
          <h1 className="text-2xl font-semibold tracking-tight">{item.title}</h1>
        </div>
        <div className="flex gap-2">
          <Badge variant={tierVariant(item.tier)}>{item.tier}</Badge>
          {item.type && <Badge variant="outline">{String(item.type)}</Badge>}
          <Badge variant={stateVariant(item.state)}>{stateLabel(item.state)}</Badge>
        </div>
      </header>

      <div className="grid grid-cols-1 md:grid-cols-[1fr_220px] gap-8">
        <div className="space-y-6">
          <Section title="Description">
            {item.description ? (
              <p className="whitespace-pre-wrap leading-relaxed text-sm">{item.description}</p>
            ) : (
              <p className="text-muted-foreground text-sm italic">No description.</p>
            )}
          </Section>

          <Tabs defaultValue="comments" className="w-full">
            <TabsList>
              <TabsTrigger value="comments">
                Comments {commentsQuery.data ? `(${commentsQuery.data.length})` : ''}
              </TabsTrigger>
              <TabsTrigger value="history">
                History {historyQuery.data ? `(${historyQuery.data.length})` : ''}
              </TabsTrigger>
            </TabsList>
            <TabsContent value="comments" className="pt-4">
              <CommentsList query={commentsQuery} />
            </TabsContent>
            <TabsContent value="history" className="pt-4">
              <HistoryList query={historyQuery} />
            </TabsContent>
          </Tabs>
        </div>

        <aside className="space-y-4 text-sm">
          <Meta label="Reporter" value={short(item.reporterId)} mono />
          <Meta label="Assignee" value={item.assigneeId ? short(item.assigneeId) : 'Unassigned'} mono={!!item.assigneeId} />
          <Meta label="Estimate" value={item.estimatePoints != null ? `${item.estimatePoints} pts` : '—'} />
          <Separator />
          <Meta label="Created" value={fmtDate(item.createdAt)} />
          <Meta label="Updated" value={fmtDate(item.updatedAt)} />
        </aside>
      </div>
    </section>
  )
}

function CommentsList({
  query,
}: {
  query: { isLoading: boolean; isError: boolean; data?: CommentResponse[] }
}) {
  if (query.isLoading) return <Note>Loading comments…</Note>
  if (query.isError) return <Note tone="error">Failed to load comments.</Note>
  if (!query.data || query.data.length === 0) return <Note>No comments yet.</Note>
  return (
    <ul className="space-y-4">
      {query.data.map((c) => (
        <li key={c.id} className="rounded-lg border p-4 space-y-2">
          <div className="flex items-center justify-between text-xs text-muted-foreground">
            <code className="font-mono">{short(c.authorId)}</code>
            <span>{fmtDate(c.createdAt)}</span>
          </div>
          <p className="whitespace-pre-wrap text-sm leading-relaxed">{c.body}</p>
          {c.isInternal && <Badge variant="secondary">Internal</Badge>}
        </li>
      ))}
    </ul>
  )
}

function HistoryList({
  query,
}: {
  query: { isLoading: boolean; isError: boolean; data?: AuditLogResponse[] }
}) {
  if (query.isLoading) return <Note>Loading history…</Note>
  if (query.isError) return <Note tone="error">Failed to load history.</Note>
  if (!query.data || query.data.length === 0) return <Note>No history yet.</Note>
  return (
    <ul className="space-y-2">
      {query.data.map((h) => (
        <li key={h.id} className="border-l-2 pl-4 py-1 text-sm">
          <div className="flex items-center justify-between text-xs text-muted-foreground">
            <span>{String(h.changeType)} · {h.source}</span>
            <span>{fmtDate(h.occurredAt)}</span>
          </div>
          {h.fieldChanges && h.fieldChanges.length > 0 && (
            <ul className="mt-1 space-y-1 text-muted-foreground">
              {h.fieldChanges.map((fc, i) => (
                <li key={i}>
                  <code className="font-mono text-xs">{fc.fieldName}</code>:{' '}
                  <span className="line-through opacity-60">{fc.oldValue ?? '∅'}</span>{' → '}
                  <span>{fc.newValue ?? '∅'}</span>
                </li>
              ))}
            </ul>
          )}
        </li>
      ))}
    </ul>
  )
}

function Section({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <div className="space-y-2">
      <h2 className="text-sm font-medium uppercase tracking-wide text-muted-foreground">{title}</h2>
      {children}
    </div>
  )
}

function Meta({ label, value, mono }: { label: string; value: string; mono?: boolean }) {
  return (
    <div>
      <div className="text-xs uppercase tracking-wide text-muted-foreground">{label}</div>
      <div className={mono ? 'font-mono text-xs break-all' : ''}>{value}</div>
    </div>
  )
}

function Note({ children, tone }: { children: React.ReactNode; tone?: 'error' }) {
  return (
    <p className={tone === 'error' ? 'text-destructive text-sm' : 'text-muted-foreground text-sm italic'}>
      {children}
    </p>
  )
}

function Status({ children, tone }: { children: React.ReactNode; tone?: 'error' }) {
  return (
    <main className="min-h-[40svh] flex items-center justify-center p-8">
      <p className={tone === 'error' ? 'text-destructive' : 'text-muted-foreground'}>{children}</p>
    </main>
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

function short(uuid: string): string {
  return uuid.length > 13 ? `${uuid.slice(0, 8)}…${uuid.slice(-4)}` : uuid
}

function fmtDate(iso: string): string {
  return new Date(iso).toLocaleString()
}
