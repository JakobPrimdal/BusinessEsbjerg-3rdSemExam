import type { HolidayRuleWarningDTO } from "@/api/Api";

// The API leaves the list out when it is empty in some cases, so always work on an array
export function warningsOf(warnings?: HolidayRuleWarningDTO[] | null): HolidayRuleWarningDTO[] {
    return warnings ?? [];
}

export function warningTitle(count: number): string {
    return count === 1 ? "1 advarsel" : `${count} advarsler`;
}

// Warnings do not block, but the button should make it clear that rules are broken
export function submitLabel(busy: boolean, warnings: HolidayRuleWarningDTO[]): string {
    if (busy) return "Sender...";
    return warnings.length > 0 ? "Send alligevel" : "Send anmodning";
}