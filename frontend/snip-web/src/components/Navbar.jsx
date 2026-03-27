import styles from '../styles/navbar.module.css'

export default function Navbar({ user, onLogout }) {
  return (
    <nav className={styles.navbar}>
      <div className={styles.logo}>Snip</div>
      <div className={styles.right}>
        {user && <span className={styles.username}>{user.username}</span>}
        <button className={styles.logoutButton} onClick={onLogout}>
          Logout
        </button>
      </div>
    </nav>
  )
}