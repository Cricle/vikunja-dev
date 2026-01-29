export const EventTypes = {
  // Task events
  TaskCreated: 'task.created',
  TaskUpdated: 'task.updated',
  TaskDeleted: 'task.deleted',
  
  // Project events
  ProjectCreated: 'project.created',
  ProjectUpdated: 'project.updated',
  ProjectDeleted: 'project.deleted',
  
  // Task assignee events
  TaskAssigneeCreated: 'task.assignee.created',
  TaskAssigneeDeleted: 'task.assignee.deleted',
  
  // Task comment events
  TaskCommentCreated: 'task.comment.created',
  TaskCommentUpdated: 'task.comment.updated',
  TaskCommentDeleted: 'task.comment.deleted',
  
  // Task attachment events
  TaskAttachmentCreated: 'task.attachment.created',
  TaskAttachmentDeleted: 'task.attachment.deleted',
  
  // Task relation events
  TaskRelationCreated: 'task.relation.created',
  TaskRelationDeleted: 'task.relation.deleted',
  
  // Label events
  LabelCreated: 'label.created',
  LabelUpdated: 'label.updated',
  LabelDeleted: 'label.deleted',
  
  // Task label events
  TaskLabelCreated: 'task.label.created',
  TaskLabelDeleted: 'task.label.deleted',
  
  // User events
  UserCreated: 'user.created',
  
  // Team events
  TeamCreated: 'team.created',
  TeamUpdated: 'team.updated',
  TeamDeleted: 'team.deleted',
  
  // Team member events
  TeamMemberAdded: 'team.member.added',
  TeamMemberRemoved: 'team.member.removed'
} as const

export type EventType = typeof EventTypes[keyof typeof EventTypes]

export const AllEventTypes: EventType[] = Object.values(EventTypes)

export interface EventMetadata {
  type: EventType
  label: string
  icon: string
  category: 'task' | 'project' | 'team' | 'label' | 'user'
}

export const EventMetadataMap: Record<EventType, EventMetadata> = {
  // Task events
  [EventTypes.TaskCreated]: {
    type: EventTypes.TaskCreated,
    label: 'Task Created',
    icon: '📝',
    category: 'task'
  },
  [EventTypes.TaskUpdated]: {
    type: EventTypes.TaskUpdated,
    label: 'Task Updated',
    icon: '✏️',
    category: 'task'
  },
  [EventTypes.TaskDeleted]: {
    type: EventTypes.TaskDeleted,
    label: 'Task Deleted',
    icon: '🗑️',
    category: 'task'
  },
  
  // Project events
  [EventTypes.ProjectCreated]: {
    type: EventTypes.ProjectCreated,
    label: 'Project Created',
    icon: '📁',
    category: 'project'
  },
  [EventTypes.ProjectUpdated]: {
    type: EventTypes.ProjectUpdated,
    label: 'Project Updated',
    icon: '📝',
    category: 'project'
  },
  [EventTypes.ProjectDeleted]: {
    type: EventTypes.ProjectDeleted,
    label: 'Project Deleted',
    icon: '🗑️',
    category: 'project'
  },
  
  // Task assignee events
  [EventTypes.TaskAssigneeCreated]: {
    type: EventTypes.TaskAssigneeCreated,
    label: 'Task Assignee Added',
    icon: '👤',
    category: 'task'
  },
  [EventTypes.TaskAssigneeDeleted]: {
    type: EventTypes.TaskAssigneeDeleted,
    label: 'Task Assignee Removed',
    icon: '👤',
    category: 'task'
  },
  
  // Task comment events
  [EventTypes.TaskCommentCreated]: {
    type: EventTypes.TaskCommentCreated,
    label: 'Comment Created',
    icon: '💬',
    category: 'task'
  },
  [EventTypes.TaskCommentUpdated]: {
    type: EventTypes.TaskCommentUpdated,
    label: 'Comment Updated',
    icon: '💬',
    category: 'task'
  },
  [EventTypes.TaskCommentDeleted]: {
    type: EventTypes.TaskCommentDeleted,
    label: 'Comment Deleted',
    icon: '💬',
    category: 'task'
  },
  
  // Task attachment events
  [EventTypes.TaskAttachmentCreated]: {
    type: EventTypes.TaskAttachmentCreated,
    label: 'Attachment Added',
    icon: '📎',
    category: 'task'
  },
  [EventTypes.TaskAttachmentDeleted]: {
    type: EventTypes.TaskAttachmentDeleted,
    label: 'Attachment Removed',
    icon: '📎',
    category: 'task'
  },
  
  // Task relation events
  [EventTypes.TaskRelationCreated]: {
    type: EventTypes.TaskRelationCreated,
    label: 'Relation Created',
    icon: '🔗',
    category: 'task'
  },
  [EventTypes.TaskRelationDeleted]: {
    type: EventTypes.TaskRelationDeleted,
    label: 'Relation Removed',
    icon: '🔗',
    category: 'task'
  },
  
  // Label events
  [EventTypes.LabelCreated]: {
    type: EventTypes.LabelCreated,
    label: 'Label Created',
    icon: '🏷️',
    category: 'label'
  },
  [EventTypes.LabelUpdated]: {
    type: EventTypes.LabelUpdated,
    label: 'Label Updated',
    icon: '🏷️',
    category: 'label'
  },
  [EventTypes.LabelDeleted]: {
    type: EventTypes.LabelDeleted,
    label: 'Label Deleted',
    icon: '🏷️',
    category: 'label'
  },
  
  // Task label events
  [EventTypes.TaskLabelCreated]: {
    type: EventTypes.TaskLabelCreated,
    label: 'Task Label Added',
    icon: '🏷️',
    category: 'task'
  },
  [EventTypes.TaskLabelDeleted]: {
    type: EventTypes.TaskLabelDeleted,
    label: 'Task Label Removed',
    icon: '🏷️',
    category: 'task'
  },
  
  // User events
  [EventTypes.UserCreated]: {
    type: EventTypes.UserCreated,
    label: 'User Created',
    icon: '👤',
    category: 'user'
  },
  
  // Team events
  [EventTypes.TeamCreated]: {
    type: EventTypes.TeamCreated,
    label: 'Team Created',
    icon: '👥',
    category: 'team'
  },
  [EventTypes.TeamUpdated]: {
    type: EventTypes.TeamUpdated,
    label: 'Team Updated',
    icon: '👥',
    category: 'team'
  },
  [EventTypes.TeamDeleted]: {
    type: EventTypes.TeamDeleted,
    label: 'Team Deleted',
    icon: '👥',
    category: 'team'
  },
  
  // Team member events
  [EventTypes.TeamMemberAdded]: {
    type: EventTypes.TeamMemberAdded,
    label: 'Member Added',
    icon: '👥',
    category: 'team'
  },
  [EventTypes.TeamMemberRemoved]: {
    type: EventTypes.TeamMemberRemoved,
    label: 'Member Removed',
    icon: '👥',
    category: 'team'
  }
}

export const PlaceholdersByEventType: Record<string, string[]> = {
  task: [
    'event.type',
    'event.timestamp',
    'event.url',
    'task.title',
    'task.description',
    'task.id',
    'task.done',
    'task.dueDate',
    'task.priority',
    'task.url',
    'project.title',
    'project.id',
    'project.description',
    'project.url',
    'assignees',
    'assignee.count',
    'labels',
    'label.count'
  ],
  project: [
    'event.type',
    'event.timestamp',
    'event.url',
    'project.title',
    'project.id',
    'project.description',
    'project.url'
  ],
  team: [
    'event.type',
    'event.timestamp',
    'event.url',
    'team.name',
    'team.id',
    'team.description',
    'user.name',
    'user.username',
    'user.email',
    'user.id'
  ],
  label: [
    'event.type',
    'event.timestamp',
    'event.url',
    'label.title',
    'label.id',
    'label.description'
  ],
  user: [
    'event.type',
    'event.timestamp',
    'event.url',
    'user.name',
    'user.username',
    'user.email',
    'user.id'
  ]
}

// Get placeholders for specific event type (more precise than category)
export function getPlaceholdersForEvent(eventType: EventType): string[] {
  const metadata = EventMetadataMap[eventType]
  const basePlaceholders = PlaceholdersByEventType[metadata.category] || []
  
  // Add event-specific placeholders
  const specificPlaceholders: string[] = []
  
  if (eventType.includes('comment')) {
    specificPlaceholders.push('comment.id', 'comment.text', 'comment.author')
  }
  
  if (eventType.includes('attachment')) {
    specificPlaceholders.push('attachment.id', 'attachment.fileName')
  }
  
  if (eventType.includes('relation')) {
    specificPlaceholders.push('relation.taskId', 'relation.relatedTaskId', 'relation.relationType')
  }
  
  return [...basePlaceholders, ...specificPlaceholders]
}
