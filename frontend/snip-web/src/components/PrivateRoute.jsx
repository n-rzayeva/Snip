import { Navigate } from 'react-router-dom'
import { auth } from '../services/auth'

export default function PrivateRoute({ children }) {
  if (!auth.isAuthenticated()) {
    return <Navigate to="/login" replace />
  }
  return children
}