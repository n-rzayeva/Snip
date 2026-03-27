import { useState, useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import { Copy, Check, BarChart2, Pencil, Trash2, X, Save } from 'lucide-react'
import { api } from '../services/api'
import { auth } from '../services/auth'
import Navbar from '../components/Navbar'
import styles from '../styles/dashboard.module.css'

export default function Dashboard() {
  const [links, setLinks] = useState([])
  const [destinationUrl, setDestinationUrl] = useState('')
  const [loading, setLoading] = useState(false)
  const [creating, setCreating] = useState(false)
  const [error, setError] = useState('')
  const [copiedSlug, setCopiedSlug] = useState(null)
  const [editingId, setEditingId] = useState(null)
  const [editUrl, setEditUrl] = useState('')
  const navigate = useNavigate()
  const user = auth.getUser()

  useEffect(() => {
    fetchLinks()
  }, [])

  async function fetchLinks() {
    setLoading(true)
    try {
      const res = await api.get('/api/links')
      const data = await res.json()
      setLinks(data)
    } catch {
      setError('Failed to load links')
    } finally {
      setLoading(false)
    }
  }

  async function createLink(e) {
    e.preventDefault()
    if (!destinationUrl) return

    // Sanitize — only allow http and https URLs
    try {
      const parsed = new URL(destinationUrl)
      if (!['http:', 'https:'].includes(parsed.protocol)) {
        setError('Only http and https URLs are allowed')
        return
      }
    } catch {
      setError('Please enter a valid URL')
      return
    }

    setCreating(true)
    setError('')

    try {
      const res = await api.post('/api/links', { destinationUrl })
      const data = await res.json()

      if (!res.ok) {
        setError('Failed to create link')
        return
      }

      setLinks(prev => [data, ...prev])
      setDestinationUrl('')
    } catch {
      setError('Failed to create link')
    } finally {
      setCreating(false)
    }
  }

  async function updateLink(id) {
    if (!editUrl) return

    try {
      const parsed = new URL(editUrl)
      if (!['http:', 'https:'].includes(parsed.protocol)) {
        setError('Only http and https URLs are allowed')
        return
      }
    } catch {
      setError('Please enter a valid URL')
      return
    }

    try {
      const res = await api.put(`/api/links/${id}`, { destinationUrl: editUrl })
      if (!res.ok) {
        setError('Failed to update link')
        return
      }
      setLinks(prev => prev.map(l =>
        l.id === id ? { ...l, destinationUrl: editUrl } : l
      ))
      setEditingId(null)
      setEditUrl('')
    } catch {
      setError('Failed to update link')
    }
  }

  async function deleteLink(id) {
    try {
      await api.delete(`/api/links/${id}`)
      setLinks(prev => prev.filter(l => l.id !== id))
    } catch {
      setError('Failed to delete link')
    }
  }

  function copyLink(slug) {
    navigator.clipboard.writeText(`http://localhost:5000/r/${slug}`)
    setCopiedSlug(slug)
    setTimeout(() => setCopiedSlug(null), 2000)
  }

  function logout() {
    auth.clearSession()
    navigate('/login')
  }

  return (
    <div className={styles.container}>
      <Navbar user={user} onLogout={logout} />

      <div className={styles.main}>
        <div className={styles.header}>
          <h1 className={styles.title}>Your Links</h1>
        </div>

        {error && <div className={styles.error}>{error}</div>}

        <form className={styles.createForm} onSubmit={createLink}>
          <input
            type="url"
            value={destinationUrl}
            onChange={e => setDestinationUrl(e.target.value)}
            placeholder="https://example.com/your-long-url"
            required
          />
          <button
            className={styles.createButton}
            type="submit"
            disabled={creating}
          >
            {creating ? 'Creating...' : 'Shorten'}
          </button>
        </form>

        {loading ? (
          <div className={styles.empty}>Loading...</div>
        ) : links.length === 0 ? (
          <div className={styles.empty}>
            No links yet. Create your first one above.
          </div>
        ) : (
          <div className={styles.linkList}>
            {links.map(link => (
              <div key={link.id} className={styles.linkCard}>
                <div className={styles.linkInfo}>
                  <div className={styles.slug}>snip/{link.slug}</div>
                  {editingId === link.id ? (
                    <input
                      className={styles.editInput}
                      value={editUrl}
                      onChange={e => setEditUrl(e.target.value)}
                      placeholder="https://new-destination.com"
                      autoFocus
                    />
                  ) : (
                    <div className={styles.destination}>{link.destinationUrl}</div>
                  )}
                </div>
                <div className={styles.linkActions}>
                  {editingId === link.id ? (
                    <>
                      <button
                        className={styles.saveButton}
                        onClick={() => updateLink(link.id)}
                        title="Save"
                      >
                        <Save size={16} />
                      </button>
                      <button
                        className={styles.cancelButton}
                        onClick={() => { setEditingId(null); setEditUrl('') }}
                        title="Cancel"
                      >
                        <X size={16} />
                      </button>
                    </>
                  ) : (
                    <>
                      <button
                        className={styles.copyButton}
                        onClick={() => copyLink(link.slug)}
                        title="Copy link"
                      >
                        {copiedSlug === link.slug ? <Check size={16} /> : <Copy size={16} />}
                      </button>
                      <button
                        className={styles.editButton}
                        onClick={() => { setEditingId(link.id); setEditUrl(link.destinationUrl) }}
                        title="Edit"
                      >
                        <Pencil size={16} />
                      </button>
                      <button
                        className={styles.analyticsButton}
                        onClick={() => navigate(`/links/${link.slug}`)}
                        title="Analytics"
                      >
                        <BarChart2 size={16} />
                      </button>
                      <button
                        className={styles.deleteButton}
                        onClick={() => deleteLink(link.id)}
                        title="Delete"
                      >
                        <Trash2 size={16} />
                      </button>
                    </>
                  )}
                </div>
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  )
}