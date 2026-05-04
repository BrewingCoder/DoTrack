import { useQuery } from '@tanstack/react-query'
import { Link, useParams } from '@tanstack/react-router'
import { projectsClient } from '@/lib/api'
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
  const { wsSlug } = useParams({ from: '/workspaces/$wsSlug/' })

  const projectsQuery = useQuery({
    queryKey: ['projects', wsSlug],
    queryFn: () => projectsClient.projectsAll(wsSlug),
  })

  return (
    <section className="p-8 max-w-4xl mx-auto space-y-6">
      <header>
        <h1 className="text-2xl font-semibold tracking-tight">Projects</h1>
        <p className="text-muted-foreground text-sm">
          Workspace: <code className="font-mono">{wsSlug}</code>
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
              <TableCell className="font-mono">
                <Link
                  to="/workspaces/$wsSlug/projects/$projKey/items"
                  params={{ wsSlug, projKey: p.key }}
                  className="hover:underline"
                >
                  {p.key}
                </Link>
              </TableCell>
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
