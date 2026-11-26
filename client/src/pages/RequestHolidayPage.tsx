import { useEffect, useState, type FormEvent } from "react";
import type { HolidayRequestPreviewDTO } from "@/api/Api";
import { api } from "@/api/client";
import { errorMessage } from "@/api/errors";
import { WEEK_6, WEEKS_1_5, formatDate, formatHours, todayIso, typeLabel } from "@/holiday/balance";
import { submitLabel, warningsOf } from "@/holiday/warnings";
import { RuleWarnings } from "@/components/RuleWarnings";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";

export function RequestHolidayPage() {
    const today = todayIso();
    const [startDate, setStartDate] = useState("");
    const [endDate, setEndDate] = useState("");
    const [holidayType, setHolidayType] = useState(WEEKS_1_5);
    const [preview, setPreview] = useState<HolidayRequestPreviewDTO | null>(null);
    const [error, setError] = useState<string | null>(null);
    const [sent, setSent] = useState<string | null>(null);
    const [busy, setBusy] = useState(false);

    // Ask the server what the request costs, and which rules it breaks, every time the dates or the type change
    useEffect(() => {
        setPreview(null);
        setError(null);
        if (!startDate || !endDate || endDate < startDate) return;

        setSent(null);
        let cancelled = false;
        api.holiday
            .holidayPreviewRequest({ startDate, endDate, holidayType })
            .then(res => {
                if (!cancelled) setPreview(res.data);
            })
            .catch(err => {
                if (!cancelled) setError(errorMessage(err));
            });
        return () => {
            cancelled = true;
        };
    }, [startDate, endDate, holidayType]);

    async function onSubmit(e: FormEvent<HTMLFormElement>) {
        e.preventDefault();
        setError(null);
        setBusy(true);
        try {
            const { data } = await api.holiday.holidayRequestHoliday({ startDate, endDate, holidayType });
            setSent(`Din ferie ${formatDate(data.startDate)} - ${formatDate(data.endDate)} er sendt og afventer godkendelse.`);
            setStartDate("");
            setEndDate("");
        } catch (err) {
            setError(errorMessage(err));
        } finally {
            setBusy(false);
        }
    }

    const hours = preview?.hours ?? 0;
    const after = preview?.balanceAfter ?? 0;
    const maxDebt = preview?.maxDebt ?? 0;
    const warnings = warningsOf(preview?.warnings);

    return (
        <Card className="w-full max-w-md">
            <CardHeader>
                <CardTitle>Anmod om ferie</CardTitle>
                <CardDescription>Weekender og fridage koster ikke ferie.</CardDescription>
            </CardHeader>
            <CardContent>
                <form onSubmit={onSubmit} className="grid gap-4">
                    <div className="grid grid-cols-2 gap-4">
                        <div className="grid gap-2">
                            <Label htmlFor="start-date">Fra</Label>
                            <Input
                                id="start-date"
                                type="date"
                                min={today}
                                value={startDate}
                                onChange={e => setStartDate(e.target.value)}
                                required
                            />
                        </div>
                        <div className="grid gap-2">
                            <Label htmlFor="end-date">Til og med</Label>
                            <Input
                                id="end-date"
                                type="date"
                                min={startDate || today}
                                value={endDate}
                                onChange={e => setEndDate(e.target.value)}
                                required
                            />
                        </div>
                    </div>

                    <div className="grid gap-2">
                        <Label htmlFor="holiday-type">Type</Label>
                        <Select value={holidayType} onValueChange={setHolidayType}>
                            <SelectTrigger id="holiday-type" className="w-full">
                                <SelectValue />
                            </SelectTrigger>
                            <SelectContent>
                                <SelectItem value={WEEKS_1_5}>{typeLabel(WEEKS_1_5)}</SelectItem>
                                <SelectItem value={WEEK_6}>{typeLabel(WEEK_6)}</SelectItem>
                            </SelectContent>
                        </Select>
                    </div>

                    {preview && hours > 0 && (
                        <div className="grid gap-1 rounded-md border p-3 text-sm">
                            <p>
                                {preview.workingDays} arbejdsdage, {formatHours(hours)}
                            </p>
                            <p className="text-muted-foreground">
                                Saldo ved start: {formatHours(preview.balanceBefore ?? 0)}. Bagefter: {formatHours(after)}
                            </p>
                            {after < 0 && maxDebt > 0 && (
                                <p className="text-muted-foreground">
                                    Du må højst have {formatHours(maxDebt)} ferie på forskud.
                                </p>
                            )}
                        </div>
                    )}

                    {/* Broken rule that blocks the request */}
                    {preview?.error && (
                        <Alert variant="destructive">
                            <AlertDescription>{preview.error}</AlertDescription>
                        </Alert>
                    )}

                    {/* Broken rules that only warn: the request can still be sent */}
                    {preview?.canRequest && (
                        <RuleWarnings
                            warnings={warnings}
                            footer="Du kan godt sende anmodningen, men den ferieansvarlige kan afvise den. Ret datoerne, hvis du vil undgå det."
                        />
                    )}

                    {error && (
                        <Alert variant="destructive">
                            <AlertDescription>{error}</AlertDescription>
                        </Alert>
                    )}

                    {sent && (
                        <Alert>
                            <AlertDescription>{sent}</AlertDescription>
                        </Alert>
                    )}

                    <Button type="submit" disabled={busy || !preview?.canRequest}>
                        {submitLabel(busy, warnings)}
                    </Button>
                </form>
            </CardContent>
        </Card>
    );
}