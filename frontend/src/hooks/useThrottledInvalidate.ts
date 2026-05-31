import { useCallback, useRef } from 'react'

/**
 * Returns a function that runs `fn` at most once per `intervalMs`.
 * Used to coalesce bursts of SignalR events into occasional query invalidations.
 */
export function useThrottledInvalidate(intervalMs = 1500) {
  const last = useRef(0)
  return useCallback(
    (fn: () => void) => {
      const now = Date.now()
      if (now - last.current >= intervalMs) {
        last.current = now
        fn()
      }
    },
    [intervalMs],
  )
}
