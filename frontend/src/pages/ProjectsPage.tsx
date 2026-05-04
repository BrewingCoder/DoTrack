import { useQuery } from '@tanstack/react-query'
import { projectsClient, workspacesClient } from '@/lib/api'
import {
  Table,
  TableBody,
  TableCaption,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'

export function ProjectsPage() {
  const workspacesQuery = useQuery({
    queryKey: ['workspaces'],
    queryFn: () => workspacesClient.workspacesAll(),
  })

  const firstWsSlug = workspacesQuery.data?.[0]?.slug

  const projectsQuery = useQuery({
    queryKey: ['projects', firstWsSlug],
    queryFn: () => projectsClient.projectsAll(firstWsSlug!),
    enabled: Boolean(firstWsSlug),
  })

  if (workspacesQuery.isLoading) {
    return <Status>Loading workspaces…</Status>
  }
  if (workspacesQuery.isError) {
    return <Status tone="error">Couldn't reach the API. Is it running on http://localhost:5259?</Status>
  }
  if (!firstWsSlug) {
    return <Status>No workspaces yet. Create one via POST /api/v1/workspaces.</Status>
  }

  return (
    <section className="p-8 max-w-4xl mx-auto space-y-6">
      <header>
        <h1 className="text-2xl font-semibold tracking-tight">Projects</h1>
        <p className="text-muted-foreground text-sm">
          Workspace: <code className="font-mono">{firstWsSlug}</code>
        </p>
      </header>

      <Table>
        <TableCaption>
          {projectsQuery.data?.length ?? 0} project
          {projectsQuery.data?.length === 1 ? '' : 's'} in this workspace.
        </TableCaption>
        <TableHeader>
          <TableRow>
            <TableHead className="w-[120px]">Key</TableHead>
            <TableHead>Name</TableHead>
            <TableHead>Description</TableHead>
            <TableHead className="text-right">Next #</TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          {projectsQuery.isLoading && (
            <TableRow>
              <TableCell colSpan={4} className="text-muted-foreground">
                Loading projects…
              </TableCell>
            </TableRow>
          )}
          {projectsQuery.isError && (
            <TableRow>
              <TableCell colSpan={4} className="text-destructive">
                Failed to load projects.
              </TableCell>
            </TableRow>
          )}
          {projectsQuery.data?.map((p) => (
            <TableRow key={p.id}>
              <TableCell className="font-mono">{p.key}</TableCell>
              <TableCell className="font-medium">{p.name}</TableCell>
              <TableCell className="text-muted-foreground">{p.description ?? '—'}</TableCell>
              <TableCell className="text-right tabular-nums">{p.nextWorkItemNumber}</TableCell>
            </TableRow>
          ))}
          {projectsQuery.data?.length === 0 && (
            <TableRow>
              <TableCell colSpan={4} className="text-muted-foreground">
                No projects in this workspace yet.
              </TableCell>
            </TableRow>
          )}
        </TableBody>
      </Table>
    </section>
  )
}

function Status({ children, tone }: { children: React.ReactNode; tone?: 'error' }) {
  return (
    <main className="min-h-svh flex items-center justify-center p-8">
      <p className={tone === 'error' ? 'text-destructive' : 'text-muted-foreground'}>{children}</p>
    </main>
  )
}
