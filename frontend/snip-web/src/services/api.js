import { auth } from './auth'

const BASE_URL = 'http://localhost:5000'

const getHeaders = () => {
  const token = auth.getToken()
  return {
    'Content-Type': 'application/json',
    ...(token && { Authorization: `Bearer ${token}` })
  }
}

const handleUnauthorized = (res) => {
  if (res.status === 401 && auth.getToken()) {
    auth.clearSession()
    window.location.href = '/login'
  }
  return res
}

export const api = {
  async post(path, body) {
    const res = await fetch(`${BASE_URL}${path}`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(body)
    })
    return handleUnauthorized(res)
  },

  async get(path) {
    const res = await fetch(`${BASE_URL}${path}`, {
      headers: getHeaders()
    })
    return handleUnauthorized(res)
  },

  async put(path, body) {
    const res = await fetch(`${BASE_URL}${path}`, {
      method: 'PUT',
      headers: getHeaders(),
      body: JSON.stringify(body)
    })
    return handleUnauthorized(res)
  },

  async delete(path) {
    const res = await fetch(`${BASE_URL}${path}`, {
      method: 'DELETE',
      headers: getHeaders()
    })
    return handleUnauthorized(res)
  }
}