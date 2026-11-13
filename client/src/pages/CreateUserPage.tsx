import { useState, type FormEvent } from "react";
import { api } from "@/api/client";
import { errorMessage } from "@/api/errors";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";

// /Register takes the role as its enum NAME (string), not the number
const roles = [
  { value: "User", label: "Medarbejder" },
  { value: "Accountant", label: "Ferieansvarlig" },
  { value: "Admin", label: "Administrator" },
];

// Same rules as AuthService.Register.
function validate(username: string, password: string): string | null {
  if (username.length < 3) return "Brugernavnet skal være mindst 3 tegn";
  if (username.length > 30) return "Brugernavnet må højst være 30 tegn";
  if (password.length < 7) return "Adgangskoden skal være mindst 7 tegn";
  return null;
}

export function CreateUserPage() {
  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");
  const [role, setRole] = useState("User");
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  async function onSubmit(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    setError(null);
    setSuccess(null);

    const name = username.trim();
    const invalid = validate(name, password);
    if (invalid) {
      setError(invalid);
      return;
    }

    setBusy(true);
    try {
      const { data } = await api.auth.authRegister({ username: name, password, role });
      setSuccess(`Brugeren "${data.username}" er oprettet`);
      setUsername("");
      setPassword("");
      setRole("User");
    } catch (err) {
      setError(errorMessage(err));
    } finally {
      setBusy(false);
    }
  }

  return (
    <Card className="w-full max-w-md">
      <CardHeader>
        <CardTitle>Opret bruger</CardTitle>
        <CardDescription>Opret en ny medarbejder, bogholder eller administrator.</CardDescription>
      </CardHeader>
      <CardContent>
        <form onSubmit={onSubmit} className="grid gap-4">
          <div className="grid gap-2">
            <Label htmlFor="new-username">Brugernavn</Label>
            <Input
              id="new-username"
              autoComplete="off"
              value={username}
              onChange={e => setUsername(e.target.value)}
              required
            />
          </div>
          <div className="grid gap-2">
            <Label htmlFor="new-password">Adgangskode</Label>
            <Input
              id="new-password"
              type="password"
              autoComplete="new-password"
              value={password}
              onChange={e => setPassword(e.target.value)}
              required
            />
          </div>
          <div className="grid gap-2">
            <Label htmlFor="role">Rolle</Label>
            <Select value={role} onValueChange={setRole}>
              <SelectTrigger id="role" className="w-full">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                {roles.map(r => (
                  <SelectItem key={r.value} value={r.value}>
                    {r.label}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>
          {error && (
            <Alert variant="destructive">
              <AlertDescription>{error}</AlertDescription>
            </Alert>
          )}
          {success && (
            <Alert>
              <AlertDescription>{success}</AlertDescription>
            </Alert>
          )}
          <Button type="submit" disabled={busy}>
            {busy ? "Opretter…" : "Opret bruger"}
          </Button>
        </form>
      </CardContent>
    </Card>
  );
}
