import { useEffect, useState } from "react";
import type { PendingHolidayRequestDTO } from "@/api/Api";
import { api } from "@/api/client";
import { errorMessage } from "@/api/errors";
import { formatDate, formatHours, typeLabel } from "@/holiday/balance";
import { warningsOf } from "@/holiday/warnings";
import { InlineRuleWarnings } from "@/components/RuleWarnings";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";

export function PendingRequestsPage() {
    const [requests, setRequests] = useState<PendingHolidayRequestDTO[] | null>(null);
    const [error, setError] = useState<string | null>(null);
    const [message, setMessage] = useState<string | null>(null);
    const [busyId, setBusyId] = useState<string | null>(null);
    const [denyId, setDenyId] = useState<string | null>(null);
    const [reason, setReason] = useState("");

    async function load() {
        try {
            const { data } = await api.holiday.holidayGetPendingRequests();
            setRequests(data);
        } catch (err) {
            setError(errorMessage(err));
        }
    }

    useEffect(() => {
        load();
    }, []);

    async function approve(r: PendingHolidayRequestDTO) {
        setError(null);
        setMessage(null);
        setBusyId(r.id!);
        try {
            await api.holiday.holidayApproveRequest(r.id!);
            setMessage(`Ferien for ${r.username} er godkendt.`);
            await load();
        } catch (err) {
            setError(errorMessage(err));
        } finally {
            setBusyId(null);
        }
    }

    async function deny(r: PendingHolidayRequestDTO) {
        setError(null);
        setMessage(null);
        setBusyId(r.id!);
        try {
            await api.holiday.holidayDenyRequest(r.id!, { reason });
            setMessage(`Ferien for ${r.username} er afvist.`);
            setDenyId(null);
            setReason("");
            await load();
        } catch (err) {
            setError(errorMessage(err));
        } finally {
            setBusyId(null);
        }
    }

    function startDeny(id: string) {
        setDenyId(id);
        setReason("");
        setError(null);
        setMessage(null);
    }

    return (
        <Card className="w-full max-w-5xl">
            <CardHeader>
                <CardTitle>Ferieanmodninger</CardTitle>
                <CardDescription>
                    Ældste først. Saldoen er medarbejderens saldo ved feriens start, hvis du godkender. Røde fejl
                    forhindrer godkendelse, advarsler er op til dig.
                </CardDescription>
            </CardHeader>
            <CardContent className="grid gap-4">
                {error && (
                    <Alert variant="destructive">
                        <AlertDescription>{error}</AlertDescription>
                    </Alert>
                )}
                {message && (
                    <Alert>
                        <AlertDescription>{message}</AlertDescription>
                    </Alert>
                )}

                {requests === null && !error && <p className="text-sm text-muted-foreground">Henter anmodninger...</p>}
                {requests?.length === 0 && <p className="text-sm text-muted-foreground">Der er ingen anmodninger, der venter.</p>}

                {requests && requests.length > 0 && (
                    <Table>
                        <TableHeader>
                            <TableRow>
                                <TableHead>Medarbejder</TableHead>
                                <TableHead>Periode</TableHead>
                                <TableHead>Type</TableHead>
                                <TableHead className="text-right">Timer</TableHead>
                                <TableHead className="text-right">Saldo bagefter</TableHead>
                                <TableHead>Sendt</TableHead>
                                <TableHead />
                            </TableRow>
                        </TableHeader>
                        <TableBody>
                            {requests.map(r => {
                                const after = r.balanceAfter ?? 0;
                                const busy = busyId === r.id;
                                return (
                                    <TableRow key={r.id}>
                                        <TableCell className="font-medium">{r.username}</TableCell>
                                        <TableCell>
                                            {formatDate(r.startDate)} - {formatDate(r.endDate)}
                                            {r.problem && <p className="text-sm text-destructive">{r.problem}</p>}
                                            <InlineRuleWarnings warnings={warningsOf(r.warnings)} />
                                        </TableCell>
                                        <TableCell>{typeLabel(r.holidayType ?? "")}</TableCell>
                                        <TableCell className="text-right">{formatHours(r.hours ?? 0)}</TableCell>
                                        <TableCell className={after < 0 ? "text-right text-destructive" : "text-right"}>
                                            {formatHours(after)}
                                        </TableCell>
                                        <TableCell>{formatDate(r.createdAt?.slice(0, 10))}</TableCell>
                                        <TableCell className="text-right">
                                            {denyId === r.id ? (
                                                <div className="flex items-center justify-end gap-2">
                                                    <Input
                                                        className="w-56"
                                                        placeholder="Begrundelse"
                                                        value={reason}
                                                        maxLength={500}
                                                        onChange={e => setReason(e.target.value)}
                                                        autoFocus
                                                    />
                                                    <Button size="sm" variant="destructive" disabled={busy || !reason.trim()} onClick={() => deny(r)}>
                                                        Afvis
                                                    </Button>
                                                    <Button size="sm" variant="outline" disabled={busy} onClick={() => setDenyId(null)}>
                                                        Fortryd
                                                    </Button>
                                                </div>
                                            ) : (
                                                <div className="flex justify-end gap-2">
                                                    <Button size="sm" disabled={busy || !!r.problem} onClick={() => approve(r)}>
                                                        Godkend
                                                    </Button>
                                                    <Button size="sm" variant="outline" disabled={busy} onClick={() => startDeny(r.id!)}>
                                                        Afvis
                                                    </Button>
                                                </div>
                                            )}
                                        </TableCell>
                                    </TableRow>
                                );
                            })}
                        </TableBody>
                    </Table>
                )}
            </CardContent>
        </Card>
    );
}