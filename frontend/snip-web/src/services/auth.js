export const auth = {
  getToken: () => localStorage.getItem('snip_token'),
  getUser: () => JSON.parse(localStorage.getItem('snip_user') || 'null'),
  
  setSession(token, user) {
    localStorage.setItem('snip_token', token)
    localStorage.setItem('snip_user', JSON.stringify(user))
  },

  clearSession() {
    localStorage.removeItem('snip_token')
    localStorage.removeItem('snip_user')
  },

  isAuthenticated() {
    return !!localStorage.getItem('snip_token')
  }
}