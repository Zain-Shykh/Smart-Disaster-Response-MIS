import { AuthProvider } from './context/AuthContext'
import { NotificationProvider } from './context/NotificationContext'
import { AppRouter } from './app/AppRouter'
import './App.css'

function App() {
  return (
    <NotificationProvider>
      <AuthProvider>
        <AppRouter />
      </AuthProvider>
    </NotificationProvider>
  )
}

export default App
