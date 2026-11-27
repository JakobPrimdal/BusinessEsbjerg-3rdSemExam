import { useEffect, useState } from "react";
import type { CancellationRequestDTO } from "@/api/Api";
import { api } from "@/api/client";
import { holidayErrorMessage } from "@/api/errors";
import { formatDate, formatHours, typeLabel } from "@/holiday/balance";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";

export function CancellationRequestsPage() {
    const [requests, setRequests] = useState<CancellationRequestDTO[] | null>(null);
    const [error, setError] = useState<string | null>(null);
    const [message, setMessage] = useState<string | null>(null);
    const [busyId, setBusyId] = useState<string | null>(null);
    const [denyId, setDenyId] = useState<string | null>(null);
    const [reason, setReason] = useState("");

    async function load() {
        try {
            const { data } = await api.holiday.holidayGetCancellationRequests();
            setRequests(data);
        } catch (err) {
            setError(holidayErrorMessage(err));
        }
    }

    useEffect(() => {
        load();
    }, []);

    async function approve(r: CancellationRequestDTO) {
        setError(null);
        setMessage(null);
        setBusyId(r.id!);
        try {
            const { data } = await api.holiday.holidayApproveCancellation(r.id!);
            setMessage(`Annulleringen for ${r.username} er godkendt. ${formatHours(data.hoursReturned ?? 0)} er givet tilbage.`);
            await load();
        } catch (err) {
            setError(holidayErrorMessage(err));
        } finally {
            setBusyId(null);
        }
    }

    async function deny(r: CancellationRequestDTO) {
        setError(null);
        setMessage(null);
        setBusyId(r.id!);
        try {
            await api.holiday.holidayDenyCancellation(r.id!, { reason });
            setMessage(`Annulleringen for ${r.username} er afvist. Ferien er stadig godkendt.`);
            setDenyId(null);
            setReason("");
            await load();
        } catch (err) {
            setError(holidayErrorMessage(err));
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
                <CardTitle>Annulleringer</CardTitle>
                <CardDescription>
                    Medarbejdere, der vil annullere godkendt ferie. Ældste først. Ferien gælder, indtil du godkender annulleringen.
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

                {requests === null && !error && <p className="text-sm text-muted-foreground">Henter annulleringer...</p>}
                {requests?.length === 0 && <p className="text-sm text-muted-foreground">Der er ingen annulleringer, der venter.</p>}

                {requests && requests.length > 0 && (
                    <Table>
                        <TableHeader>
                            <TableRow>
                                <TableHead>Medarbejder</TableHead>
                                <TableHead>Periode</TableHead>
                                <TableHead>Type</TableHead>
                                <TableHead className="text-right">Timer tilbage</TableHead>
                                <TableHead>Anmodet</TableHead>
                                <TableHead />
                            </TableRow>
                        </TableHeader>
                        <TableBody>
                            {requests.map(r => {
                                const busy = busyId === r.id;
                                return (
                                    <TableRow key={r.id}>
                                        <TableCell className="font-medium">{r.username}</TableCell>
                                        <TableCell>
                                            {formatDate(r.startDate)} - {formatDate(r.endDate)}
                                        </TableCell>
                                        <TableCell>{typeLabel(r.holidayType ?? "")}</TableCell>
                                        <TableCell className="text-right">{formatHours(r.hours ?? 0)}</TableCell>
                                        <TableCell>{formatDate(r.cancellationRequestedAt?.slice(0, 10))}</TableCell>
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
                                                    <Button size="sm" disabled={busy} onClick={() => approve(r)}>
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