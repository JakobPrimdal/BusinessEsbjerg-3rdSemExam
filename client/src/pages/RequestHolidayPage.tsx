import { useEffect, useState, type FormEvent } from "react";
import type { HolidayRequestPreviewDTO } from "@/api/Api";
import { api } from "@/api/client";
import { errorMessage } from "@/api/errors";
import { WEEK_6, WEEKS_1_5, formatDate, formatHours, todayIso, typeLabel } from "@/holiday/balance";
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

    // ask the server what the request costs every time the dates or the type change
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
    const months = preview?.noticeMonths ?? 0;

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
        {after < 0 && (
            <p className="text-muted-foreground">
                Du må højst have {formatHours(preview.maxDebt ?? 0)} ferie på forskud.
        </p>
        )}
        </div>
    )}

    {preview?.error && (
        <Alert variant="destructive">
            <AlertDescription>{preview.error}</AlertDescription>
            </Alert>
    )}

    {preview?.shortNotice && (
        <Alert>
            <AlertDescription>
                Ferien starter om mindre end {months} {months === 1 ? "måned" : "måneder"}. Du kan godt sende den, men
        bogholderiet kan afvise den.
    </AlertDescription>
    </Alert>
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
    {busy ? "Sender..." : "Send anmodning"}
    </Button>
    </form>
    </CardContent>
    </Card>
);
}