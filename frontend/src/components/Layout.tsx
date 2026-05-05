import { Link, useNavigate, useParams } from '@tanstack/react-router'
import { useQuery } from '@tanstack/react-query'
import {
  Bell,
  Boxes,
  CalendarRange,
  ChevronsLeft,
  CircleHelp,
  ClipboardList,
  Clock,
  FolderKanban,
  Gauge,
  KanbanSquare,
  Plus,
  Settings,
  User2,
  type LucideIcon,
} from 'lucide-react'
import { workspacesClient } from '@/lib/api'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { cn } from '@/lib/utils'

export function Layout({ children }: { children: React.ReactNode }) {
  return (
    <div className="h-svh flex bg-background text-foreground">
      <Sidebar />
      <main className="flex-1 min-w-0 overflow-auto">{children}</main>
    </div>
  )
}

function Sidebar() {
  const params = useParams({ strict: false }) as { wsSlug?: string }
  const wsSlug = params.wsSlug

  return (
    <aside className="w-[200px] shrink-0 bg-sidebar text-sidebar-foreground border-r border-sidebar-border flex flex-col">
      <div className="px-3 pt-4 pb-3 flex flex-col gap-3">
        <Link to="/" className="px-2 font-semibold tracking-tight text-[15px]">
          DoTrack
        </Link>
        <WorkspaceSelector />
      </div>

      <nav className="flex-1 overflow-y-auto px-2 pb-3 flex flex-col gap-4">
        <NavGroup>
          <NavLink icon={Gauge} label="Dashboard" disabled />
          <NavLink
            icon={ClipboardList}
            label="Issues"
            to={wsSlug ? '/workspaces/$wsSlug' : undefined}
            params={wsSlug ? { wsSlug } : undefined}
            disabled={!wsSlug}
          />
          <NavLink icon={KanbanSquare} label="Boards" disabled />
          <NavLink icon={Boxes} label="Reports" disabled />
        </NavGroup>

        <NavGroup>
          <NavLink
            icon={FolderKanban}
            label="Projects"
            to={wsSlug ? '/workspaces/$wsSlug' : undefined}
            params={wsSlug ? { wsSlug } : undefined}
            disabled={!wsSlug}
          />
          <NavLink icon={CalendarRange} label="Sprints" disabled />
          <NavLink icon={Clock} label="Time" disabled />
        </NavGroup>
      </nav>

      <div className="px-2 pb-3 pt-2 border-t border-sidebar-border flex flex-col gap-1">
        <NavLink icon={Plus} label="Create" disabled />
        <NavLink icon={Settings} label="Settings" disabled />
        <NavLink icon={CircleHelp} label="Help" disabled />
        <NavLink icon={Bell} label="Notifications" disabled />
        <NavLink icon={User2} label="Account" disabled />
        <NavLink icon={ChevronsLeft} label="Collapse" disabled />
      </div>
    </aside>
  )
}

function NavGroup({ children }: { children: React.ReactNode }) {
  return <div className="flex flex-col gap-0.5">{children}</div>
}

type NavLinkProps = {
  icon: LucideIcon
  label: string
  to?: string
  params?: Record<string, string>
  disabled?: boolean
}

function NavLink({ icon: Icon, label, to, params, disabled }: NavLinkProps) {
  const baseClass =
    'group flex items-center gap-2.5 h-10 px-2.5 rounded-md text-sm transition-colors'
  const idleClass = 'text-sidebar-foreground hover:bg-[var(--sidebar-hover)]'
  const disabledClass = 'text-sidebar-muted/70 cursor-default'

  if (disabled || !to) {
    return (
      <span
        className={cn(baseClass, disabled ? disabledClass : idleClass)}
        title={disabled ? 'Coming soon' : undefined}
      >
        <Icon className="size-4 text-[var(--sidebar-icon)]" />
        <span className="truncate">{label}</span>
      </span>
    )
  }

  return (
    <Link
      to={to}
      params={params as never}
      className={cn(baseClass, idleClass)}
      activeProps={{
        className: cn(
          baseClass,
          'bg-sidebar-accent text-sidebar-accent-foreground',
        ),
      }}
      activeOptions={{ exact: true }}
    >
      <Icon className="size-4 text-[var(--sidebar-icon)]" />
      <span className="truncate">{label}</span>
    </Link>
  )
}

function WorkspaceSelector() {
  const navigate = useNavigate()
  const params = useParams({ strict: false }) as { wsSlug?: string }
  const currentSlug = params.wsSlug

  const workspacesQuery = useQuery({
    queryKey: ['workspaces'],
    queryFn: () => workspacesClient.workspacesAll(),
  })

  if (workspacesQuery.isLoading || !workspacesQuery.data) {
    return (
      <div className="px-2 text-xs text-sidebar-muted">Loading workspaces…</div>
    )
  }

  const workspaces = workspacesQuery.data
  if (workspaces.length === 0) {
    return <div className="px-2 text-xs text-sidebar-muted">No workspaces</div>
  }

  return (
    <Select
      value={currentSlug ?? ''}
      onValueChange={(slug) => {
        if (slug) navigate({ to: '/workspaces/$wsSlug', params: { wsSlug: slug } })
      }}
    >
      <SelectTrigger className="h-9 w-full bg-transparent border-sidebar-border">
        <SelectValue placeholder="Select workspace" />
      </SelectTrigger>
      <SelectContent>
        {workspaces.map((ws) => (
          <SelectItem key={ws.slug} value={ws.slug}>
            {ws.name}
          </SelectItem>
        ))}
      </SelectContent>
    </Select>
  )
}
