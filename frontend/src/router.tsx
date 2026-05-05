import {
  createRootRoute,
  createRoute,
  createRouter,
  Outlet,
  redirect,
} from '@tanstack/react-router'
import { Layout } from '@/components/Layout'
import { workspacesClient } from '@/lib/api'
import { ProjectsPage } from '@/pages/ProjectsPage'
import { WorkItemDetailPage } from '@/pages/WorkItemDetailPage'
import { WorkItemsPage } from '@/pages/WorkItemsPage'

const rootRoute = createRootRoute({
  component: () => (
    <Layout>
      <Outlet />
    </Layout>
  ),
})

const indexRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/',
  beforeLoad: async () => {
    const workspaces = await workspacesClient.workspacesAll()
    if (workspaces.length > 0) {
      throw redirect({
        to: '/workspaces/$wsSlug',
        params: { wsSlug: workspaces[0].slug },
      })
    }
  },
  component: () => (
    <section className="p-8 max-w-md mx-auto text-center text-muted-foreground">
      No workspaces yet. Create one with{' '}
      <code className="font-mono">POST /api/v1/workspaces</code>.
    </section>
  ),
})

export const workspaceRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: 'workspaces/$wsSlug',
})

const workspaceIndexRoute = createRoute({
  getParentRoute: () => workspaceRoute,
  path: '/',
  component: ProjectsPage,
})

const projectItemsRoute = createRoute({
  getParentRoute: () => workspaceRoute,
  path: 'projects/$projKey/items',
  component: WorkItemsPage,
})

const projectItemDetailRoute = createRoute({
  getParentRoute: () => workspaceRoute,
  path: 'projects/$projKey/items/$number',
  component: WorkItemDetailPage,
})

const routeTree = rootRoute.addChildren([
  indexRoute,
  workspaceRoute.addChildren([
    workspaceIndexRoute,
    projectItemsRoute,
    projectItemDetailRoute,
  ]),
])

export const router = createRouter({ routeTree })

declare module '@tanstack/react-router' {
  interface Register {
    router: typeof router
  }
}
