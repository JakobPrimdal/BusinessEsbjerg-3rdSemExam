import type { HolidayAccountDTO } from "@/api/Api";

// Values of HolidayAccountDTO.holidayType (HolidayService.Weeks1To5 / Week6)
export const WEEKS_1_5 = "weeks_1_5";
export const WEEK_6 = "week_6";

// Used to convert hours to days when no weekly hours are known (37 h / 5 days = 7.4 h)
const DEFAULT_WEEKLY_HOURS = 37;

// Today as "YYYY-MM-DD" in local time, same format as the API's DateOnly.
// ISO dates can then be compared as plain strings.
export function todayIso(now: Date = new Date()): string {
  const y = now.getFullYear();
  const m = String(now.getMonth() + 1).padStart(2, "0");
  const d = String(now.getDate()).padStart(2, "0");
  return `${y}-${m}-${d}`;
}

export type UsageState = "now" | "later" | "expired" | "closed";

// Can the hours on this account be used today?
export function usageState(account: HolidayAccountDTO, today: string): UsageState {
  if (account.status !== "open") return "closed";
  if ((account.usageStart ?? "") > today) return "later";
  if ((account.usageEnd ?? "") < today) return "expired";
  return "now";
}

export type TypeSummary = {
  availableHours: number; // can be used today
  laterHours: number; // earned, but the usage period has not started yet
  laterFrom: string | null; // earliest date the "later" hours can be used
};

export function summarize(accounts: HolidayAccountDTO[], holidayType: string, today: string): TypeSummary {
  const summary: TypeSummary = { availableHours: 0, laterHours: 0, laterFrom: null };

  for (const account of accounts) {
    if (account.holidayType !== holidayType) continue;
    const hours = account.balanceHours ?? 0;
    const state = usageState(account, today);

    if (state === "now") {
      summary.availableHours += hours;
    } else if (state === "later") {
      summary.laterHours += hours;
      const start = account.usageStart ?? null;
      if (start && (!summary.laterFrom || start < summary.laterFrom)) summary.laterFrom = start;
    }
  }
  return summary;
}

// Weeks 1-5 before week 6 (same order as the summary cards), newest first within each type
export function sortAccounts(accounts: HolidayAccountDTO[]): HolidayAccountDTO[] {
  const typeOrder = (a: HolidayAccountDTO) => (a.holidayType === WEEKS_1_5 ? 0 : 1);
  return [...accounts].sort(
    (a, b) => typeOrder(a) - typeOrder(b) || (b.earningStart ?? "").localeCompare(a.earningStart ?? ""),
  );
}

// Average hours per holiday day, from the newest month's weekly hours.
// Approximate: a real day is 7.5 h (Mon-Thu) or 7 h (Fri) for full time.
export function hoursPerDay(accounts: HolidayAccountDTO[]): number {
  const months = accounts
    .flatMap(a => a.months ?? [])
    .filter(m => m.month && m.weeklyHours)
    .sort((a, b) => (a.month! < b.month! ? -1 : 1));

  const weekly = months.at(-1)?.weeklyHours ?? DEFAULT_WEEKLY_HOURS;
  return weekly / 5;
}

const decimal = new Intl.NumberFormat("da-DK", { maximumFractionDigits: 2 });

export const formatHours = (hours: number) => `${decimal.format(hours)} timer`;
export const formatDays = (days: number) => `${decimal.format(days)} dage`;

function parseIso(iso: string): Date {
  const [y, m, d] = iso.split("-").map(Number);
  return new Date(y!, m! - 1, d!);
}

// "2027-05-01" -> "1. maj 2027"
export function formatDate(iso?: string | null): string {
  if (!iso) return "–";
  return parseIso(iso).toLocaleDateString("da-DK", { day: "numeric", month: "long", year: "numeric" });
}

// "2026-01-01" -> "januar 2026"
export function formatMonth(iso?: string | null): string {
  if (!iso) return "–";
  return parseIso(iso).toLocaleDateString("da-DK", { month: "long", year: "numeric" });
}

export function typeLabel(holidayType?: string): string {
  return holidayType === WEEK_6 ? "6. ferieuge" : "Ferie (uge 1–5)";
}

// weeks_1_5 earned Sep 2025 - Aug 2026 -> "Ferieår 2025/26"; week_6 earned 2026 -> "Optjent i 2026"
export function periodLabel(account: HolidayAccountDTO): string {
  const year = Number((account.earningStart ?? "").slice(0, 4));
  if (account.holidayType === WEEK_6) return `Optjent i ${year}`;
  return `Ferieår ${year}/${String(year + 1).slice(2)}`;
}
