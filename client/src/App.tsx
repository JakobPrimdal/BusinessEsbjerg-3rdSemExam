import "./styles/globals.css";
import { BrowserRouter, Navigate, Route, Routes } from "react-router";
import { UserRole } from "@/api/Api";
import { AuthProvider } from "@/auth/AuthContext";
import { RequireRole } from "@/auth/RequireRole";
import { Header } from "@/components/Header";
import { CreateUserPage } from "@/pages/CreateUserPage";
import { LoginPage } from "@/pages/LoginPage";
import { MyBalancePage } from "@/pages/MyBalancePage";
import { RuleSetsPage } from "@/pages/RuleSetsPage";
import { CreateRuleSetPage} from "@/pages/CreateRuleSetPage";

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
                    <MyBalancePage />
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
              <Route
                path="/admin/regler"
                element={
                    <RequireRole role={UserRole.Admin}>
                      <RuleSetsPage />
                    </RequireRole>
                }
              />
              <Route
                path="/admin/regler/ny"
                element={
                    <RequireRole role={UserRole.Admin}>
                      <CreateRuleSetPage />
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
