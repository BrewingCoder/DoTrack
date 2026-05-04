import { Link, useNavigate, useParams } from '@tanstack/react-router'
import { useQuery } from '@tanstack/react-query'
import { workspacesClient } from '@/lib/api'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'

export function Layout({ children }: { children: React.ReactNode }) {
  return (
    <div className="min-h-svh flex flex-col">
      <header className="border-b">
        <div className="max-w-6xl mx-auto px-6 h-14 flex items-center justify-between gap-6">
          <Link to="/" className="font-semibold tracking-tight">
            DoTrack
          </Link>
          <WorkspaceSelector />
        </div>
      </header>
      <main className="flex-1">{children}</main>
    </div>
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
    return <div className="text-sm text-muted-foreground">Loading workspaces…</div>
  }

  const workspaces = workspacesQuery.data
  if (workspaces.length === 0) {
    return <div className="text-sm text-muted-foreground">No workspaces</div>
  }

  return (
    <Select
      value={currentSlug ?? ''}
      onValueChange={(slug) => {
        if (slug) navigate({ to: '/workspaces/$wsSlug', params: { wsSlug: slug } })
      }}
    >
      <SelectTrigger className="w-[220px]">
        <SelectValue placeholder="Select a workspace" />
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
