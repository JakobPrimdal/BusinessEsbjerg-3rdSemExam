import type { HttpResponse } from "./Api";

type Problem = { status?: number; detail?: string };

const danish: Record<string, string> = {
  "Invalid username or password": "Forkert brugernavn eller adgangskode",
  "Username is already taken": "Brugernavnet er allerede taget",
  "Username must be at least 3 characters": "Brugernavnet skal være mindst 3 tegn",
  "Username can be at most 30 characters": "Brugernavnet må højst være 30 tegn",
  "Password must be at least 7 characters": "Adgangskoden skal være mindst 7 tegn",
  "User not found": "Brugeren blev ikke fundet",
};

export function errorMessage(e: unknown): string {
  // fetch itself failed (server down, CORS, no network)
  if (e instanceof TypeError) return "Kunne ikke kontakte serveren";

  const res = e as HttpResponse<unknown, Problem>;
  const detail = res?.error?.detail;
  if (detail && danish[detail]) return danish[detail];

  // 401/403 from [Authorize] come without a body
  if (res?.status === 401) return "Din session er udløbet. Log ind igen.";
  if (res?.status === 403) return "Du har ikke adgang til dette";

  return "Noget gik galt. Prøv igen.";
}
