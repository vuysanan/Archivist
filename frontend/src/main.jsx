import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import './index.css'
import App from './App.jsx'
import AdminPanel from './AdminPanel.jsx'

// Visiting the site with #admin on the end opens the review queue instead of the archive.
const isAdmin = window.location.hash === '#admin'

createRoot(document.getElementById('root')).render(
  <StrictMode>{isAdmin ? <AdminPanel /> : <App />}</StrictMode>,
)