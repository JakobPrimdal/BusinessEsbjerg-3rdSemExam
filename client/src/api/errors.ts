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

export function httpStatus(e: unknown): number | undefined {
  return (e as { status?: number })?.status;
}

// RuleSets

const ruleSetDanish: Record<string, string> = {
  "No HolidayRuleSets exists.": "Der findes ingen regelsæt endnu",
  "No date was specified.": "Vælg en dato",
  "RuleSet for the specified date doesn't exist.": "Der findes intet aktivt regelsæt for den dato",
  "Weeks 1-5 earning rate cannot be negative.": "Optjeningssatsen for uge 1–5 må ikke være negativ",
  "Week 6 earning rate cannot be negative.": "Optjeningssatsen for uge 6 må ikke være negativ",
  "Maximum transfer days must be between 0 and 5.": "Maks. overførselsdage skal være mellem 0 og 5",
  "Sick waiting days cannot be negative.": "Sygedage uden erstatningsferie må ikke være negative",
  "Employee notice period cannot be negative.": "Varsel for hovedferie må ikke være negativt",
  "Employer notice period cannot be negative.": "Varsel for anden ferie må ikke være negativt",
};

const ruleSetNames: Record<string, string> = {
  "Weeks 1-5 earning period": "Uge 1–5 optjeningsperiode",
  "Week 6 earning period": "Uge 6 optjeningsperiode",
  "Weeks 1-5 usage period": "Uge 1–5 brugsperiode",
  "Week 6 usage period": "Uge 6 brugsperiode",
  "Main holiday start": "Hovedferie start",
  "Main holiday end": "Hovedferie slut",
  "Weeks 1-5 transfer deadline": "Uge 1–5 overførselsfrist",
  "Week 6 transfer deadline": "Uge 6 overførselsfrist",
};

const ruleSetReasons: Record<string, string> = {
  "year offset must be between 0 and 100": "år-offset skal være mellem 0 og 100",
  "month must be between 1 and 12": "måneden skal være mellem 1 og 12",
  "invalid day for the selected month": "ugyldig dag for den valgte måned",
  "start date must be before or equal to end date": "startdatoen skal ligge før eller på slutdatoen",
};

function translateRuleSet(detail: string): string | null {
  if (ruleSetDanish[detail]) return ruleSetDanish[detail];

    const [, name, reason] = detail.match(/^(.+): (.+)\.$/) ?? [];
    if (name && reason && ruleSetReasons[reason]) return `${ruleSetNames[name] ?? name}: ${ruleSetReasons[reason]}`;
  
    if (detail.startsWith("ValidFrom must be later")) {
    return "Gyldig fra ligger for tidligt – der findes allerede et aktivt regelsæt med en senere startdato";
  }

  if (detail.startsWith("Cannot activate rule set")) {
    return "Kan ikke aktiveres: der findes allerede et aktivt regelsæt med samme startdato. Deaktivér det først.";
  }

  if (/^Ruleset with id: .* (was )?not found\.$/.test(detail)) return "Regelsættet blev ikke fundet";

  return null;
}

export function ruleSetErrorMessage(e: unknown): string {
  const detail = (e as HttpResponse<unknown, Problem>)?.error?.detail;
  if (detail) {
    const translated = translateRuleSet(detail);
    if (translated) return translated;
    // Unknown backend validation error -> show error
    if (httpStatus(e) === 400) return detail;
  }
  return errorMessage(e);
}