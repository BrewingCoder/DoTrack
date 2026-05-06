import { useEffect, useMemo, useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { Link, useParams } from '@tanstack/react-router'
import {
  type ColumnDef,
  type ColumnOrderState,
  type ExpandedState,
  type RowSelectionState,
  type SortingState,
  type VisibilityState,
  flexRender,
  getCoreRowModel,
  getExpandedRowModel,
  getSortedRowModel,
  useReactTable,
} from '@tanstack/react-table'
import {
  ArrowDown,
  ArrowUp,
  ChevronDown,
  ChevronRight,
  ChevronsUpDown,
  MoveDown,
  MoveUp,
  Settings2,
} from 'lucide-react'
import { workItemsClient } from '@/lib/api'
import type { WorkItemResponse, WorkItemState, WorkItemTier } from '@/api/generated'
import { PriorityChip } from '@/components/PriorityChip'
import { SelectionToolbar } from '@/components/SelectionToolbar'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Checkbox } from '@/components/ui/checkbox'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'

const STORAGE_KEY = 'dotrack:work-items-table'

const COLUMN_LABELS: Record<string, string> = {
  key: 'Key',
  title: 'Title',
  tier: 'Tier',
  type: 'Type',
  state: 'State',
  reporter: 'Reporter',
  assignee: 'Assignee',
  estimate: 'Estimate',
  createdAt: 'Created',
  updatedAt: 'Updated',
}

const DEFAULT_VISIBILITY: VisibilityState = {
  key: true,
  title: true,
  tier: true,
  type: true,
  state: true,
  reporter: false,
  assignee: false,
  estimate: false,
  createdAt: false,
  updatedAt: false,
}

const DEFAULT_ORDER: ColumnOrderState = [
  'key',
  'title',
  'tier',
  'type',
  'state',
  'reporter',
  'assignee',
  'estimate',
  'createdAt',
  'updatedAt',
]

type Prefs = {
  sort: SortingState
  visibility: VisibilityState
  order: ColumnOrderState
}

function loadPrefs(): Prefs | null {
  try {
    const raw = localStorage.getItem(STORAGE_KEY)
    if (!raw) return null
    return JSON.parse(raw) as Prefs
  } catch {
    return null
  }
}

function savePrefs(prefs: Prefs) {
  try {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(prefs))
  } catch {
    // localStorage unavailable (private mode, quota) — silently skip.
  }
}

export function WorkItemsPage() {
  const { wsSlug, projKey } = useParams({
    from: '/workspaces/$wsSlug/projects/$projKey/items',
  })

  const itemsQuery = useQuery({
    queryKey: ['work-items', wsSlug, projKey],
    queryFn: () => workItemsClient.workItemsAll(wsSlug, projKey),
  })

  const initial = useMemo<Prefs>(
    () =>
      loadPrefs() ?? {
        sort: [],
        visibility: DEFAULT_VISIBILITY,
        order: DEFAULT_ORDER,
      },
    [],
  )

  const [sorting, setSorting] = useState<SortingState>(initial.sort)
  const [columnVisibility, setColumnVisibility] = useState<VisibilityState>(initial.visibility)
  const [columnOrder, setColumnOrder] = useState<ColumnOrderState>(initial.order)
  const [rowSelection, setRowSelection] = useState<RowSelectionState>({})
  const [expanded, setExpanded] = useState<ExpandedState>(true)

  // Build a parent → children map and the top-level subset.
  // Items whose parent isn't in this project's visible set (e.g. cross-project
  // Epic→Feature link) render as roots so the tree stays well-formed.
  const { topLevelRows, childrenByParent } = useMemo(() => {
    const all = itemsQuery.data ?? []
    const visible = new Set(all.map((r) => r.key))
    const map = new Map<string, WorkItemResponse[]>()
    const tops: WorkItemResponse[] = []
    for (const row of all) {
      const parent = row.parentKey
      if (parent && visible.has(parent)) {
        const list = map.get(parent) ?? []
        list.push(row)
        map.set(parent, list)
      } else {
        tops.push(row)
      }
    }
    return { topLevelRows: tops, childrenByParent: map }
  }, [itemsQuery.data])

  useEffect(() => {
    savePrefs({ sort: sorting, visibility: columnVisibility, order: columnOrder })
  }, [sorting, columnVisibility, columnOrder])

  const columns = useMemo<ColumnDef<WorkItemResponse>[]>(
    () => [
      {
        id: 'select',
        header: ({ table }) => (
          <Checkbox
            checked={
              table.getIsAllRowsSelected()
                ? true
                : table.getIsSomeRowsSelected()
                  ? 'indeterminate'
                  : false
            }
            onCheckedChange={(checked) => table.toggleAllRowsSelected(!!checked)}
            aria-label="Select all rows"
          />
        ),
        cell: ({ row }) => (
          <Checkbox
            checked={row.getIsSelected()}
            onCheckedChange={(checked) => row.toggleSelected(!!checked)}
            aria-label={`Select ${row.original.key}`}
          />
        ),
        enableSorting: false,
        enableHiding: false,
        size: 32,
      },
      {
        id: 'key',
        accessorFn: (row) => row.number,
        header: 'Key',
        cell: ({ row }) => {
          const canExpand = row.getCanExpand()
          const isExpanded = row.getIsExpanded()
          return (
            <div
              className="inline-flex items-center gap-1"
              style={{ paddingLeft: `${row.depth * 18}px` }}
            >
              {canExpand ? (
                <button
                  type="button"
                  onClick={(e) => {
                    e.stopPropagation()
                    row.toggleExpanded()
                  }}
                  className="size-4 inline-flex items-center justify-center text-muted-foreground hover:text-foreground"
                  aria-label={isExpanded ? 'Collapse' : 'Expand'}
                  aria-expanded={isExpanded}
                >
                  {isExpanded ? (
                    <ChevronDown className="size-3.5" />
                  ) : (
                    <ChevronRight className="size-3.5" />
                  )}
                </button>
              ) : (
                <span className="size-4" aria-hidden />
              )}
              <Link
                to="/workspaces/$wsSlug/projects/$projKey/items/$number"
                params={{ wsSlug, projKey, number: String(row.original.number) }}
                className="inline-flex items-center gap-2 font-mono hover:underline"
              >
                <PriorityChip priority={row.original.priority} />
                {row.original.key}
              </Link>
            </div>
          )
        },
        enableSorting: true,
      },
      {
        id: 'title',
        accessorKey: 'title',
        header: 'Title',
        cell: ({ row }) => (
          <Link
            to="/workspaces/$wsSlug/projects/$projKey/items/$number"
            params={{ wsSlug, projKey, number: String(row.original.number) }}
            className="font-medium hover:underline"
          >
            {row.original.title}
          </Link>
        ),
        enableSorting: true,
      },
      {
        id: 'tier',
        accessorKey: 'tier',
        header: 'Tier',
        cell: ({ getValue }) => {
          const v = getValue<WorkItemTier>()
          return <Badge variant={tierVariant(v)}>{v}</Badge>
        },
      },
      {
        id: 'type',
        accessorFn: (row) => (row.type ? String(row.type) : ''),
        header: 'Type',
        cell: ({ getValue }) => {
          const v = getValue<string>()
          return v ? (
            <span className="text-muted-foreground">{v}</span>
          ) : (
            <span className="text-muted-foreground">—</span>
          )
        },
      },
      {
        id: 'state',
        accessorKey: 'state',
        header: 'State',
        cell: ({ getValue }) => {
          const v = getValue<WorkItemState>()
          return <Badge variant={stateVariant(v)}>{stateLabel(v)}</Badge>
        },
      },
      {
        id: 'reporter',
        accessorKey: 'reporterId',
        header: 'Reporter',
        cell: ({ getValue }) => (
          <code className="font-mono text-xs">{shortId(getValue<string>())}</code>
        ),
      },
      {
        id: 'assignee',
        accessorFn: (row) => row.assigneeId ?? '',
        header: 'Assignee',
        cell: ({ getValue }) => {
          const v = getValue<string>()
          return v ? (
            <code className="font-mono text-xs">{shortId(v)}</code>
          ) : (
            <span className="text-muted-foreground italic">Unassigned</span>
          )
        },
      },
      {
        id: 'estimate',
        accessorFn: (row) => row.estimatePoints ?? null,
        header: 'Estimate',
        cell: ({ getValue }) => {
          const v = getValue<number | null>()
          return v != null ? (
            <span className="tabular-nums">{v} pts</span>
          ) : (
            <span className="text-muted-foreground">—</span>
          )
        },
        enableSorting: true,
      },
      {
        id: 'createdAt',
        accessorKey: 'createdAt',
        header: 'Created',
        cell: ({ getValue }) => (
          <span className="text-muted-foreground text-xs whitespace-nowrap">
            {fmtDate(getValue<string>())}
          </span>
        ),
        enableSorting: true,
      },
      {
        id: 'updatedAt',
        accessorKey: 'updatedAt',
        header: 'Updated',
        cell: ({ getValue }) => (
          <span className="text-muted-foreground text-xs whitespace-nowrap">
            {fmtDate(getValue<string>())}
          </span>
        ),
        enableSorting: true,
      },
    ],
    [wsSlug, projKey],
  )

  // The select column is always pinned leftmost — it doesn't appear in the user's
  // saved column order, but the table needs to know to render it first.
  const effectiveColumnOrder = useMemo(
    () => ['select', ...columnOrder.filter((id) => id !== 'select')],
    [columnOrder],
  )

  const table = useReactTable({
    data: topLevelRows,
    columns,
    state: {
      sorting,
      columnVisibility,
      columnOrder: effectiveColumnOrder,
      rowSelection,
      expanded,
    },
    onSortingChange: setSorting,
    onColumnVisibilityChange: setColumnVisibility,
    onColumnOrderChange: setColumnOrder,
    onRowSelectionChange: setRowSelection,
    onExpandedChange: setExpanded,
    enableRowSelection: true,
    getRowId: (row) => row.id,
    getSubRows: (row) => childrenByParent.get(row.key),
    getCoreRowModel: getCoreRowModel(),
    getSortedRowModel: getSortedRowModel(),
    getExpandedRowModel: getExpandedRowModel(),
  })

  const selectedItems = useMemo(
    () => table.getSelectedRowModel().rows.map((r) => r.original),
    // eslint-disable-next-line react-hooks/exhaustive-deps
    [rowSelection, itemsQuery.data],
  )

  const visibleColumnCount = table.getVisibleLeafColumns().length

  return (
    <section className="p-8 max-w-7xl mx-auto space-y-6">
      <header className="flex items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight">
            {projKey} — Work items
          </h1>
          <p className="text-muted-foreground text-sm">
            Workspace: <code className="font-mono">{wsSlug}</code>
          </p>
        </div>
        <ColumnsMenu
          columnVisibility={columnVisibility}
          setColumnVisibility={setColumnVisibility}
          columnOrder={columnOrder}
          setColumnOrder={setColumnOrder}
        />
      </header>

      <Table>
        <TableHeader>
          {table.getHeaderGroups().map((headerGroup) => (
            <TableRow key={headerGroup.id}>
              {headerGroup.headers.map((header) => {
                const canSort = header.column.getCanSort()
                const sorted = header.column.getIsSorted()
                return (
                  <TableHead key={header.id}>
                    {canSort ? (
                      <button
                        type="button"
                        className="flex items-center gap-1 hover:text-foreground"
                        onClick={header.column.getToggleSortingHandler()}
                      >
                        {flexRender(header.column.columnDef.header, header.getContext())}
                        {sorted === 'asc' ? (
                          <ArrowUp className="size-3" />
                        ) : sorted === 'desc' ? (
                          <ArrowDown className="size-3" />
                        ) : (
                          <ChevronsUpDown className="size-3 opacity-40" />
                        )}
                      </button>
                    ) : (
                      flexRender(header.column.columnDef.header, header.getContext())
                    )}
                  </TableHead>
                )
              })}
            </TableRow>
          ))}
        </TableHeader>
        <TableBody>
          {itemsQuery.isLoading && (
            <TableRow>
              <TableCell colSpan={visibleColumnCount} className="text-muted-foreground">
                Loading items…
              </TableCell>
            </TableRow>
          )}
          {itemsQuery.isError && (
            <TableRow>
              <TableCell colSpan={visibleColumnCount} className="text-destructive">
                Failed to load work items.
              </TableCell>
            </TableRow>
          )}
          {!itemsQuery.isLoading &&
            table.getRowModel().rows.map((row) => (
              <TableRow key={row.id}>
                {row.getVisibleCells().map((cell) => (
                  <TableCell key={cell.id}>
                    {flexRender(cell.column.columnDef.cell, cell.getContext())}
                  </TableCell>
                ))}
              </TableRow>
            ))}
          {!itemsQuery.isLoading && table.getRowModel().rows.length === 0 && (
            <TableRow>
              <TableCell colSpan={visibleColumnCount} className="text-muted-foreground">
                No work items in this project yet.
              </TableCell>
            </TableRow>
          )}
        </TableBody>
      </Table>

      <p className="text-xs text-muted-foreground">
        {itemsQuery.data?.length ?? 0} item
        {itemsQuery.data?.length === 1 ? '' : 's'} in this project.
      </p>

      <SelectionToolbar
        wsSlug={wsSlug}
        projKey={projKey}
        selected={selectedItems}
        onClear={() => setRowSelection({})}
      />
    </section>
  )
}

function ColumnsMenu(props: {
  columnVisibility: VisibilityState
  setColumnVisibility: React.Dispatch<React.SetStateAction<VisibilityState>>
  columnOrder: ColumnOrderState
  setColumnOrder: React.Dispatch<React.SetStateAction<ColumnOrderState>>
}) {
  const { columnVisibility, setColumnVisibility, columnOrder, setColumnOrder } = props

  function move(id: string, direction: -1 | 1) {
    setColumnOrder((current) => {
      const idx = current.indexOf(id)
      if (idx < 0) return current
      const swapWith = idx + direction
      if (swapWith < 0 || swapWith >= current.length) return current
      const next = [...current]
      const tmp = next[idx]
      next[idx] = next[swapWith]
      next[swapWith] = tmp
      return next
    })
  }

  return (
    <DropdownMenu>
      <DropdownMenuTrigger
        render={
          <Button variant="outline" size="sm" className="gap-2">
            <Settings2 className="size-4" />
            Columns
          </Button>
        }
      />
      <DropdownMenuContent align="end" className="w-72 p-2">
        <div className="space-y-0.5">
          {columnOrder.map((id, idx) => {
            const visible = columnVisibility[id] ?? true
            return (
              <div
                key={id}
                className="flex items-center gap-2 px-2 py-1.5 rounded-md hover:bg-accent"
              >
                <Checkbox
                  checked={visible}
                  onCheckedChange={(checked) =>
                    setColumnVisibility((v) => ({ ...v, [id]: !!checked }))
                  }
                  aria-label={`Toggle ${COLUMN_LABELS[id] ?? id}`}
                />
                <span className="flex-1 text-sm">{COLUMN_LABELS[id] ?? id}</span>
                <Button
                  variant="ghost"
                  size="icon"
                  className="size-6"
                  disabled={idx === 0}
                  onClick={() => move(id, -1)}
                  aria-label={`Move ${COLUMN_LABELS[id] ?? id} up`}
                >
                  <MoveUp className="size-3" />
                </Button>
                <Button
                  variant="ghost"
                  size="icon"
                  className="size-6"
                  disabled={idx === columnOrder.length - 1}
                  onClick={() => move(id, 1)}
                  aria-label={`Move ${COLUMN_LABELS[id] ?? id} down`}
                >
                  <MoveDown className="size-3" />
                </Button>
              </div>
            )
          })}
        </div>
      </DropdownMenuContent>
    </DropdownMenu>
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

function shortId(uuid: string): string {
  return uuid.length > 13 ? `${uuid.slice(0, 8)}…${uuid.slice(-4)}` : uuid
}

function fmtDate(iso: string | null | undefined): string {
  if (!iso) return '—'
  return new Date(iso).toLocaleString(undefined, {
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  })
}
