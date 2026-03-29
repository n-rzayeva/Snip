import { useState, useEffect, useRef } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { HubConnectionBuilder } from '@microsoft/signalr'
import {
  LineChart, Line, XAxis, YAxis, CartesianGrid,
  Tooltip, ResponsiveContainer, BarChart, Bar
} from 'recharts'
import { api } from '../services/api'
import { auth } from '../services/auth'
import Navbar from '../components/Navbar'
import styles from '../styles/analytics.module.css'

export default function LinkAnalytics() {
  const { slug } = useParams()
  const navigate = useNavigate()
  const [analytics, setAnalytics] = useState(null)
  const [totalClicks, setTotalClicks] = useState(0)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [toasts, setToasts] = useState([])
  const toastIdRef = useRef(0)
  const connectionRef = useRef(null)
  const user = auth.getUser()

  useEffect(() => {
    fetchAnalytics()
    setupSignalR()

    return () => {
      if (connectionRef.current) {
        connectionRef.current.stop()
        connectionRef.current = null
      }
    }
  }, [slug])

  async function fetchAnalytics() {
    setLoading(true)
    try {
      const res = await api.get(`/api/links/${slug}/analytics`)
      const data = await res.json()
      setAnalytics(data)
      setTotalClicks(data.totalClicks)
    } catch {
      setError('Failed to load analytics')
    } finally {
      setLoading(false)
    }
  }

  async function setupSignalR() {
  if (connectionRef.current?.state === 'Connected' || 
      connectionRef.current?.state === 'Connecting') return

    const connection = new HubConnectionBuilder()
      .withUrl('http://localhost:5000/hubs/clicks')
      .withAutomaticReconnect()
      .build()

    connectionRef.current = connection

    connection.on('ReceiveClickUpdate', async (updatedSlug) => {
      if (updatedSlug === slug) {
        const res = await api.get(`/api/links/${slug}/analytics`)
        const data = await res.json()
        setTotalClicks(data.totalClicks)
        setAnalytics(data)
      }
    })

    connection.on('ReceiveAlert', (updatedSlug, milestone, realTotal) => {
      if (updatedSlug === slug) {
        addToast(`🎉 ${milestone.toLocaleString()} clicks milestone reached!`)
      }
    })

    try {
      await connection.start()
      await connection.invoke('JoinLinkGroup', slug)
    } catch (err) {
      console.error('SignalR connection failed:', err)
    }
  }

  function addToast(message) {
    const id = ++toastIdRef.current
    setToasts(prev => [...prev, { id, message }])
    setTimeout(() => setToasts(prev => prev.filter(t => t.id !== id)), 5000)
  }

  function formatHour(dateStr) {
    return new Date(dateStr).toLocaleTimeString([], {
      hour: '2-digit',
      minute: '2-digit'
    })
  }

  function logout() {
    auth.clearSession()
    navigate('/login')
  }

  if (loading) {
    return (
      <div className={styles.container}>
        <Navbar user={user} onLogout={logout} />
        <div className={styles.loading}>Loading analytics...</div>
      </div>
    )
  }

  return (
    <div className={styles.container}>
      <Navbar user={user} onLogout={logout} />

      <div className={styles.main}>
        <button className={styles.back} onClick={() => navigate('/dashboard')}>
          ← Back to dashboard
        </button>

        <div className={styles.header}>
          <div className={styles.slug}>snip/{slug}</div>
        </div>

        {error && <div className={styles.error}>{error}</div>}

        <div className={styles.statsRow}>
          <div className={styles.statCard}>
            <div className={styles.statValue}>{totalClicks.toLocaleString()}</div>
            <div className={styles.statLabel}>
              <span className={styles.liveDot} />
              Total clicks
            </div>
          </div>
          <div className={styles.statCard}>
            <div className={styles.statValue}>
              {analytics?.clicksByDevice?.[0]?.device ?? '—'}
            </div>
            <div className={styles.statLabel}>Top device</div>
          </div>
          <div className={styles.statCard}>
            <div className={styles.statValue}>
              {analytics?.clicksByCountry?.[0]?.country ?? '—'}
            </div>
            <div className={styles.statLabel}>Top country</div>
          </div>
        </div>

        {analytics?.clicksByHour?.length > 0 && (
          <div className={styles.card}>
            <div className={styles.cardTitle}>Clicks over time</div>
            <ResponsiveContainer width="100%" height={200}>
              <LineChart data={analytics.clicksByHour}>
                <CartesianGrid strokeDasharray="3 3" stroke="#f3f4f6" />
                <XAxis
                  dataKey="hour"
                  tickFormatter={formatHour}
                  tick={{ fontSize: 11 }}
                />
                <YAxis tick={{ fontSize: 11 }} />
                <Tooltip
                  labelFormatter={v => new Date(v).toLocaleString()}
                  formatter={v => [v, 'Clicks']}
                />
                <Line
                  type="monotone"
                  dataKey="clicks"
                  stroke="#4f46e5"
                  strokeWidth={2}
                  dot={false}
                />
              </LineChart>
            </ResponsiveContainer>
          </div>
        )}

        <div className={styles.grid}>
          {analytics?.clicksByDevice?.length > 0 && (
            <div className={styles.card}>
              <div className={styles.cardTitle}>By device</div>
              <ResponsiveContainer width="100%" height={160}>
                <BarChart data={analytics.clicksByDevice}>
                  <CartesianGrid strokeDasharray="3 3" stroke="#f3f4f6" />
                  <XAxis dataKey="device" tick={{ fontSize: 11 }} />
                  <YAxis tick={{ fontSize: 11 }} />
                  <Tooltip formatter={v => [v, 'Clicks']} />
                  <Bar dataKey="clicks" fill="#4f46e5" radius={[4, 4, 0, 0]} />
                </BarChart>
              </ResponsiveContainer>
            </div>
          )}

          {analytics?.clicksByCountry?.length > 0 && (
            <div className={styles.card}>
              <div className={styles.cardTitle}>By country</div>
              {analytics.clicksByCountry.map(c => (
                <div key={c.country} className={styles.countryRow}>
                  <span className={styles.countryName}>{c.country}</span>
                  <span className={styles.countryClicks}>{c.clicks}</span>
                </div>
              ))}
            </div>
          )}
        </div>
      </div>
      {toasts.length > 0 && (
        <div style={{
          position: 'fixed',
          bottom: '1.5rem',
          right: '1.5rem',
          display: 'flex',
          flexDirection: 'column',
          gap: '0.5rem',
          zIndex: 1000
        }}>
          {toasts.map(toast => (
            <div key={toast.id} style={{
              background: '#4f46e5',
              color: 'white',
              padding: '0.875rem 1.25rem',
              borderRadius: '10px',
              fontSize: '0.9rem',
              boxShadow: '0 4px 12px rgba(0,0,0,0.15)',
              animation: 'slideIn 0.3s ease'
            }}>
              {toast.message}
            </div>
          ))}
        </div>
      )}
    </div>
  )
}