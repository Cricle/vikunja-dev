import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import { AllEventTypes, EventMetadataMap, type EventType, type EventMetadata } from '@/types/events'

export const useEventStore = defineStore('event', () => {
  const selectedEventType = ref<EventType | null>(null)

  const allEvents = computed(() => AllEventTypes)
  
  const eventsByCategory = computed(() => {
    const categories: Record<string, EventMetadata[]> = {
      task: [],
      project: [],
      team: [],
      label: [],
      user: []
    }

    AllEventTypes.forEach(eventType => {
      const metadata = EventMetadataMap[eventType]
      if (!categories[metadata.category]) {
        categories[metadata.category] = []
      }
      categories[metadata.category].push(metadata)
    })

    return categories
  })

  function getEventMetadata(eventType: EventType): EventMetadata {
    return EventMetadataMap[eventType]
  }

  function selectEvent(eventType: EventType) {
    selectedEventType.value = eventType
  }

  function clearSelection() {
    selectedEventType.value = null
  }

  return {
    selectedEventType,
    allEvents,
    eventsByCategory,
    getEventMetadata,
    selectEvent,
    clearSelection
  }
})
