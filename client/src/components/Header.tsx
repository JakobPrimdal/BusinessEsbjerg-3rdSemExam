import { NavLink } from "react-router";
import { UserRole } from "@/api/Api";
import { Button } from "@/components/ui/button";
import { useAuth } from "@/auth/AuthContext";
import { ThemeToggle } from "./ThemeToggle";

const navClass = ({ isActive }: { isActive: boolean }) =>
    isActive ? "font-medium text-foreground" : "text-muted-foreground hover:text-foreground";

export function Header() {
    const { user, logout } = useAuth();
    const isAdmin = user?.role?.id === UserRole.Admin;
    const isAccountant = user?.role?.id === UserRole.Accountant;
    
    return (
        <header className="flex items-center justify-between gap-2 border-b px-4 py-3">
            <div className="flex items-center gap-6">
                <span className="font-semibold">Business Esbjerg - Ferie</span>
                
                {user && (
                    <nav className="flex items-center gap-4 text-sm">
                        <NavLink to="/hjem" className={navClass}>
                            Min ferie
                        </NavLink>
                        <NavLink to="/ferie/anmod" className={navClass}>
                            Anmod om ferie
                        </NavLink>
                        {isAccountant && (
                            <NavLink to="/ferie/anmodninger" className={navClass}>
                                Anmodninger
                            </NavLink>
                        )}
                    </nav>
                )}
                {isAdmin && (
                    <nav className="flex items-center gap-4 text-sm">
                        <NavLink to="/admin/brugere" className={navClass}>
                            Brugere
                        </NavLink>
                        <NavLink to="/admin/regler" className={navClass}>
                            Regler
                        </NavLink>
                    </nav>
                )}
            </div>
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