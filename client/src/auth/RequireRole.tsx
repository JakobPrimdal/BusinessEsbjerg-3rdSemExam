import type { ReactNode } from "react";
import { Navigate } from "react-router";
import { UserRole, type UserDTO } from "@/api/Api";
import { useAuth } from "./AuthContext";

export function RequireRole({ role, children }: { role?: UserRole; children: ReactNode }) {
  const { user, loading } = useAuth();

  if (loading) return null;
  if (!user) return <Navigate to="/" replace />;
  if (role !== undefined && user.role !== role) return <Navigate to={homeFor(user)} replace />;

  return children;
}

export function homeFor(user: UserDTO): string {
  return user.role === UserRole.Admin ? "/admin/brugere" : "/hjem";
}
