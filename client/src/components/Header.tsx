import { Button } from "@/components/ui/button";
import { useAuth } from "@/auth/AuthContext";
import { ThemeToggle } from "./ThemeToggle";

export function Header() {
  const { user, logout } = useAuth();

  return (
    <header className="flex items-center justify-between gap-2 border-b px-4 py-3">
      <span className="font-semibold">Business Esbjerg - Ferie</span>
      <div className="flex items-center gap-2">
        {user && (
          <>
            <span className="hidden text-sm text-muted-foreground sm:inline">{user.username}</span>
            <Button variant="outline" onClick={logout}>
              Log ud
            </Button>
          </>
        )}
        <ThemeToggle />
      </div>
    </header>
  );
}
