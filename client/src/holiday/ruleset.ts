import type {HolidayRuleSetRequestDTO, HolidayRuleSetResponseDTO} from "@/api/Api";

export type RuleSet = HolidayRuleSetResponseDTO;

// All number-fields in a RuleSet (everything except id, dato, metadata and the two boolean values)
export type NumKey = Exclude<
    keyof RuleSet,
    "id" | "validFrom" | "createdBy" | "createdAt" | "isActive" | "holidayTransferEnabled"
>;

export type DateKeys = { day: NumKey; month: NumKey; offset?: NumKey };
export type PeriodDef = {
    name: string;
    title: string;
    start: DateKeys;
    end: DateKeys;
    compare: boolean;
};
export type DeadlineDef = { name: string; title: string; date: DateKeys };

// Field-definitions

export const EARNING_PERIODS: PeriodDef[] = [
    {
        name: "Uge 1–5",
        title: "Uge 1–5 optjeningsperiode",
        compare: true,
        start: { day: "weeks1To5EarningStartDay", month: "weeks1To5EarningStartMonth" },
        end: {
            day: "weeks1To5EarningEndDay",
            month: "weeks1To5EarningEndMonth",
            offset: "weeks1To5EarningEndYearOffset",
        },
    },
    {
        name: "Uge 6",
        title: "Uge 6 optjeningsperiode",
        compare: true,
        start: { day: "week6EarningStartDay", month: "week6EarningStartMonth" },
        end: {
            day: "week6EarningEndDay",
            month: "week6EarningEndMonth",
            offset: "week6EarningEndYearOffset",
        },
    },
];

export const USAGE_PERIODS: PeriodDef[] = [
    {
        name: "Uge 1–5",
        title: "Uge 1–5 brugsperiode",
        compare: true,
        start: {
            day: "weeks1To5UsageStartDay",
            month: "weeks1To5UsageStartMonth",
            offset: "weeks1To5UsageStartYearOffset",
        },
        end: {
            day: "weeks1To5UsageEndDay",
            month: "weeks1To5UsageEndMonth",
            offset: "weeks1To5UsageEndYearOffset",
        },
    },
    {
        name: "Uge 6",
        title: "Uge 6 brugsperiode",
        compare: true,
        start: {
            day: "week6UsageStartDay",
            month: "week6UsageStartMonth",
            offset: "week6UsageStartYearOffset",
        },
        end: {
            day: "week6UsageEndDay",
            month: "week6UsageEndMonth",
            offset: "week6UsageEndYearOffset",
        },
    },
];

export const MAIN_HOLIDAY: PeriodDef = {
    name: "Hovedferieperiode",
    title: "Hovedferieperiode",
    compare: false,
    start: { day: "mainHolidayStartDay", month: "mainHolidayStartMonth" },
    end: { day: "mainHolidayEndDay", month: "mainHolidayEndMonth" },
};

export const DEADLINES: DeadlineDef[] = [
    {
        name: "Uge 1–5",
        title: "Uge 1–5 overførselsfrist",
        date: {
            day: "weeks1To5TransferDeadlineDay",
            month: "weeks1To5TransferDeadlineMonth",
            offset: "weeks1To5TransferDeadlineYearOffset",
        },
    },
    {
        name: "Uge 6",
        title: "Uge 6 overførselsfrist",
        date: {
            day: "week6TransferDeadlineDay",
            month: "week6TransferDeadlineMonth",
            offset: "week6TransferDeadlineYearOffset",
        },
    },
];

const ALL_PERIODS: PeriodDef[] = [...EARNING_PERIODS, ...USAGE_PERIODS, MAIN_HOLIDAY];

function keysOf(d: DateKeys): NumKey[] {
    return d.offset ? [d.day, d.month, d.offset] : [d.day, d.month];
}

export const NUM_KEYS: NumKey[] = [
    "weeks1To5EarningRate",
    "week6EarningRate",
    ...ALL_PERIODS.flatMap(p => [...keysOf(p.start), ...keysOf(p.end)]),
    "maximumTransferDays",
    ...DEADLINES.flatMap(d => keysOf(d.date)),
    "sickWaitingDays",
    "mainHolidayNoticePeriodMonths",
    "otherHolidayNoticePeriodMonths",
];

const DECIMAL_KEYS = new Set<NumKey>(["weeks1To5EarningRate", "week6EarningRate"]);

const LABELS = new Map<NumKey, string>([
    ["weeks1To5EarningRate", "Optjeningssats uge 1–5"],
    ["week6EarningRate", "Optjeningssats uge 6"],
    ["maximumTransferDays", "Maks. overførselsdage"],
    ["sickWaitingDays", "Sygedage uden erstatningsferie"],
    ["mainHolidayNoticePeriodMonths", "Varsel for hovedferie"],
    ["otherHolidayNoticePeriodMonths", "Varsel for anden ferie"],
]);

function registerDate(d: DateKeys, prefix: string) {
    LABELS.set(d.day, `${prefix} – dag`);
    LABELS.set(d.month, `${prefix} – måned`);
    if (d.offset) LABELS.set(d.offset, `${prefix} – år-offset`);
}
for (const p of ALL_PERIODS) {
    registerDate(p.start, `${p.title}, start`);
    registerDate(p.end, `${p.title}, slut`);
}
for (const d of DEADLINES) registerDate(d.date, d.title);

// Status
export type RuleSetStatus = "current" | "future" | "historical" | "inactive";

export function ruleSetStatus(rs: RuleSet, currentId: string | null, today: string): RuleSetStatus {
    if (!rs.isActive) return "inactive";
    if (rs.id === currentId) return "current";
    if ((rs.validFrom ?? "") > today) return "future";
    return "historical";
}

const MONTHS = ["jan", "feb", "mar", "apr", "maj", "jun", "jul", "aug", "sep", "okt", "nov", "dec"];

function dateText(rs: RuleSet, d: DateKeys): string {
    const day = rs[d.day] ?? 0;
    const month = rs[d.month] ?? 0;
    const offset = d.offset ? (rs[d.offset] ?? 0) : 0;
    const base = `${day}. ${MONTHS[month - 1] ?? "?"}`;
    return offset ? `${base} (år +${offset})` : base;
}

export function periodText(rs: RuleSet, p: PeriodDef): string {
    return `${dateText(rs, p.start)} – ${dateText(rs, p.end)}`;
}

export function deadlineText(rs: RuleSet, d: DeadlineDef): string {
    return dateText(rs, d.date);
}

export function formatNumber(n?: number): string {
    return new Intl.NumberFormat("da-DK", { maximumFractionDigits: 3 }).format(n ?? 0);
}

// Form

export type RuleSetForm = {
    validFrom: string; // "YYYY-MM-DD"
    isActive: boolean;
    holidayTransferEnabled: boolean;
    values: Record<NumKey, string>;
};

export function emptyForm(): RuleSetForm {
    return {
        validFrom: "",
        isActive: true,
        holidayTransferEnabled: false,
        values: Object.fromEntries(NUM_KEYS.map(k => [k, ""])) as Record<NumKey, string>,
    };
}

export function valuesFromRuleSet(rs: RuleSet): Record<NumKey, string> {
    return Object.fromEntries(NUM_KEYS.map(k => [k, String(rs[k] ?? 0)])) as Record<NumKey, string>;
}

// Builds the JSON-body CreateHolidayRuleSet
export function toRequestBody(form: RuleSetForm): HolidayRuleSetRequestDTO {
    const body: HolidayRuleSetRequestDTO = {
        validFrom: form.validFrom,
        isActive: form.isActive,
        holidayTransferEnabled: form.holidayTransferEnabled,
    };
    for (const k of NUM_KEYS) {
        body[k] = Number(form.values[k]);
    }
    return body;
}

// Only basic validation as the rest is validated by the backend
export function validateForm(form: RuleSetForm): string | null {
    if (!form.validFrom) return "Vælg en gyldig fra-dato";

    for (const k of NUM_KEYS) {
        const raw = form.values[k].trim();
        const label = LABELS.get(k) ?? k;
        if (raw === "" || Number.isNaN(Number(raw))) return `Udfyld ${label} med et tal`;
        if (!DECIMAL_KEYS.has(k) && !Number.isInteger(Number(raw))) return `${label} skal være et helt tal`;
    }

    return null;
}