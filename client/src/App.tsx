import "./styles/globals.css";
import { BrowserRouter, Navigate, Route, Routes } from "react-router";
import { UserRole } from "@/api/Api";
import { AuthProvider } from "@/auth/AuthContext";
import { RequireRole } from "@/auth/RequireRole";
import { Header } from "@/components/Header";
import { CreateUserPage } from "@/pages/CreateUserPage";
import { HomePage } from "@/pages/HomePage";
import { LoginPage } from "@/pages/LoginPage";

export function App() {
  return (
    <BrowserRouter>
      <AuthProvider>
        <div className="min-h-screen">
          <Header />
          <main className="flex justify-center px-4 py-12">
            <Routes>
              <Route path="/" element={<LoginPage />} />
              <Route
                path="/hjem"
                element={
                  <RequireRole>
                    <HomePage />
                  </RequireRole>
                }
              />
              <Route
                path="/admin/brugere"
                element={
                  <RequireRole role={UserRole.Admin}>
                    <CreateUserPage />
                  </RequireRole>
                }
              />
              <Route path="*" element={<Navigate to="/" replace />} />
            </Routes>
          </main>
        </div>
      </AuthProvider>
    </BrowserRouter>
  );
}

export default App;
