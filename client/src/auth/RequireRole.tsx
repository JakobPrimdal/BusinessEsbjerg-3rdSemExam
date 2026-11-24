import type { ReactNode } from "react";
import { Navigate } from "react-router";
import { UserRole, type UserDTO } from "@/api/Api";
import { useAuth } from "./AuthContext";

// UX only: the real protection is [Authorize] on the server.
export function RequireRole({ role, children }: { role?: UserRole; children: ReactNode }) {
  const { user, loading } = useAuth();

  if (loading) return null;
  if (!user) return <Navigate to="/" replace />;
  if (role !== undefined && user.role?.id !== role) return <Navigate to={homeFor(user)} replace />;

  return children;
}

// Where a user lands after login
export function homeFor(user: UserDTO): string {
    if (user.role?.id === UserRole.Admin) return "/admin/brugere";
    if (user.role?.id === UserRole.Accountant) return "/ferie/anmodninger";
    return "/hjem";
}