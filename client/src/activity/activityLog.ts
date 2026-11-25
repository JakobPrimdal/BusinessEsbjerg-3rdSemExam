import type { ActivityLogDTO } from "@/api/Api";

// Select value for "no user filter" (Radix Select does not allow an empty string)
export const ALL_USERS = "all";

// Danish labels for the action codes in ActivityLogActions (backend).
// Add a line here when the backend starts logging a new kind of action.
const ACTION_LABELS: Record<string, string> = {
    "user.created": "Bruger oprettet",
    "workhours.added": "Arbejdstid registreret",
    "holiday.monthly-added": "Månedlig ferie optjent",
    "holiday.requested": "Ferie anmodet",
    "holiday.approved": "Ferie godkendt",
    "holiday.denied": "Ferie afvist",
    "ruleset.created": "Regelsæt oprettet",
    "ruleset.toggled": "Regelsæt aktiveret/deaktiveret",
};

// Unknown codes are shown as they are, so a new action is visible before it gets a label
export function actionLabel(action?: string): string {
    if (!action) return "–";
    return ACTION_LABELS[action] ?? action;
}

export function isSystem(entry: ActivityLogDTO): boolean {
    return !entry.userId;
}

// "2026-11-25" (from <input type="date">) -> that day's local midnight as an ISO time.
// The browser knows Danish time (and summer time), so the server only compares points in time.
export function startOfDay(date: string): string {
    const [y, m, d] = date.split("-").map(Number);
    return new Date(y!, m! - 1, d!).toISOString();
}

export function startOfNextDay(date: string): string {
    const [y, m, d] = date.split("-").map(Number);
    return new Date(y!, m! - 1, d! + 1).toISOString();
}

// Same day is fine: both dates are included
export function isValidRange(from: string, to: string): boolean {
    return !from || !to || from <= to;
}

export type LogQuery = { userId?: string; from?: string; to?: string; page: number };

// Turns the filter fields into query parameters. Empty filters are left out.
export function buildLogQuery(userId: string, from: string, to: string, page: number): LogQuery {
    return {
        userId: userId === ALL_USERS ? undefined : userId,
        from: from ? startOfDay(from) : undefined,
        // the "to" date is included, so ask for everything before the next day starts
        to: to ? startOfNextDay(to) : undefined,
        page,
    };
}

// "1–50" of a total, for "Viser 1–50 af 123"
export function pageRange(page: number, pageSize: number, total: number): { first: number; last: number } {
    if (total === 0) return { first: 0, last: 0 };
    return { first: (page - 1) * pageSize + 1, last: Math.min(page * pageSize, total) };
}

// Date and time are formatted separately: browsers disagree on the separator between them
const dateFormat = new Intl.DateTimeFormat("da-DK", { dateStyle: "short" });
const timeFormat = new Intl.DateTimeFormat("da-DK", { timeStyle: "short" });

// UTC time from the API -> Danish date and time, e.g. "25.11.2026 14.05"
export function formatDateTime(iso?: string): string {
    if (!iso) return "–";
    const date = new Date(iso);
    return `${dateFormat.format(date)} ${timeFormat.format(date)}`;
}