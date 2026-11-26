import { TriangleAlert } from "lucide-react";
import type { HolidayRuleWarningDTO } from "@/api/Api";
import { warningTitle } from "@/holiday/warnings";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";

// Broken holiday rules that do not block the request (the backend writes the Danish text)
export function RuleWarnings({ warnings, footer }: { warnings: HolidayRuleWarningDTO[]; footer?: string }) {
    if (warnings.length === 0) return null;

    return (
        <Alert>
            <TriangleAlert />
            <AlertTitle>{warningTitle(warnings.length)}</AlertTitle>
            <AlertDescription>
                <ul className="list-disc space-y-1 pl-4">
                    {warnings.map(w => (
                        <li key={w.code}>{w.message}</li>
                    ))}
                </ul>
                {footer && <p className="mt-2">{footer}</p>}
            </AlertDescription>
        </Alert>
    );
}

// Compact version for a table cell
export function InlineRuleWarnings({ warnings }: { warnings: HolidayRuleWarningDTO[] }) {
    return (
        <>
            {warnings.map(w => (
                <p key={w.code} className="mt-1 flex gap-1 text-sm text-muted-foreground">
                    <TriangleAlert className="mt-0.5 size-4 shrink-0 text-amber-600 dark:text-amber-500" />
                    <span>{w.message}</span>
                </p>
            ))}
        </>
    );
}