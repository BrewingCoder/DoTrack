import {
  AuditClient,
  CommentsClient,
  ProjectsClient,
  WorkItemsClient,
  WorkspacesClient,
} from '@/api/generated'

// Empty baseUrl → relative requests against the page origin. In dev, Vite's
// server.proxy forwards /api, /healthz, /openapi to the .NET API. In prod,
// the same paths are served by the same reverse proxy that serves the SPA.
const baseUrl = ''

export const workspacesClient = new WorkspacesClient(baseUrl)
export const projectsClient = new ProjectsClient(baseUrl)
export const workItemsClient = new WorkItemsClient(baseUrl)
export const commentsClient = new CommentsClient(baseUrl)
export const auditClient = new AuditClient(baseUrl)
