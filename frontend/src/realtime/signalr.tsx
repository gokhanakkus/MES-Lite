import * as signalR from '@microsoft/signalr'
import {
  createContext,
  useContext,
  useEffect,
  useRef,
  useState,
  type ReactNode,
} from 'react'

export type HubEvent =
  | 'MachineStatusChanged'
  | 'ProductionUpdated'
  | 'DowntimeCreated'
  | 'OeeUpdated'
  | 'MachineTelemetry'
  | 'AlarmRaised'
  | 'AlarmResolved'

const HUB_URL = (import.meta.env.VITE_API_BASE ?? '') + '/hubs/production'

interface SignalRContextValue {
  connection: signalR.HubConnection | null
  connected: boolean
}

const SignalRContext = createContext<SignalRContextValue>({ connection: null, connected: false })

export function SignalRProvider({ children }: { children: ReactNode }) {
  const [connected, setConnected] = useState(false)
  const [connection, setConnection] = useState<signalR.HubConnection | null>(null)

  useEffect(() => {
    const conn = new signalR.HubConnectionBuilder()
      .withUrl(HUB_URL)
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Warning)
      .build()

    conn.onreconnected(() => setConnected(true))
    conn.onreconnecting(() => setConnected(false))
    conn.onclose(() => setConnected(false))

    setConnection(conn)
    conn
      .start()
      .then(() => setConnected(true))
      .catch(() => setConnected(false))

    return () => {
      void conn.stop()
      setConnection(null)
    }
  }, [])

  return (
    <SignalRContext.Provider value={{ connection, connected }}>{children}</SignalRContext.Provider>
  )
}

export function useConnectionState() {
  return useContext(SignalRContext).connected
}

/** Subscribe to a hub event for the lifetime of the calling component. */
export function useHubEvent<T = unknown>(event: HubEvent, handler: (payload: T) => void) {
  const { connection } = useContext(SignalRContext)
  const handlerRef = useRef(handler)
  handlerRef.current = handler

  useEffect(() => {
    if (!connection) return
    const cb = (payload: T) => handlerRef.current(payload)
    connection.on(event, cb)
    return () => connection.off(event, cb)
  }, [connection, event])
}
