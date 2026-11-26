import { useEffect, useState } from "react";
import type { MyHolidayRequestDTO } from "@/api/Api";
import { api } from "@/api/client";
import { holidayErrorMessage } from "@/api/errors";
import { formatDate, formatHours, typeLabel } from "@/holiday/balance";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";


const statusLabels: Record<string, string> = {
    pending: "Afventer",
    approved: "Godkendt",
    denied: "Afvist",
    cancelled: "Annulleret",
    cancellation_requested: "Annullering afventer",
};

function statusVariant(status?: string): "default" | "secondary" | "destructive" | "outline" {
    if (status === "approved") return "default";
    if (status === "denied") return "destructive";
    if (status === "cancelled") return "outline";
    return "secondary";
}

export function MyRequestsPage() {
    const [requests, setRequests] = useState<MyHolidayRequestDTO[] | null>(null);
    const [error, setError] = useState<string | null>(null);
    const [message, setMessage] = useState<string | null>(null);
    const [busyId, setBusyId] = useState<string | null>(null);
    const [confirmId, setConfirmId] = useState<string | null>(null);

    async function load() {
        try {
            const { data } = await api.holiday.holidayGetMyRequests();
            setRequests(data);
        } catch (err) {
            setError(holidayErrorMessage(err));
        }
    }

    useEffect(() => {
        load();
    }, []);

    async function cancel(r: MyHolidayRequestDTO) {
        setError(null);
        setMessage(null);
        setBusyId(r.id!);
        try {
            const { data } = await api.holiday.holidayCancelRequest(r.id!);
            setMessage(
                data.hoursReturned
                    ? `Anmodningen er trukket tilbage. ${formatHours(data.hours ?? 0)} er tilbage i din saldo.`
                    : "Din annullering er sendt til den ferieansvarlige. Ferien gælder, indtil den er godkendt."
            );
            setConfirmId(null);
            await load();
        } catch (err) {
            setError(holidayErrorMessage(err));
        } finally {
            setBusyId(null);
        }
    }

    function startCancel(id: string) {
        setConfirmId(id);
        setError(null);
        setMessage(null);
    }

    return (
        <Card className="w-full max-w-5xl">
            <CardHeader>
                <CardTitle>Mine anmodninger</CardTitle>
                <CardDescription>
                    En ventende anmodning trækkes tilbage med det samme. Godkendt ferie, der ikke er startet, kræver at den
                    ferieansvarlige godkender annulleringen.
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
                {requests?.length === 0 && <p className="text-sm text-muted-foreground">Du har ingen ferieanmodninger.</p>}

                {requests && requests.length > 0 && (
                    <Table>
                        <TableHeader>
                            <TableRow>
                                <TableHead>Periode</TableHead>
                                <TableHead>Type</TableHead>
                                <TableHead className="text-right">Timer</TableHead>
                                <TableHead>Status</TableHead>
                                <TableHead>Sendt</TableHead>
                                <TableHead />
                            </TableRow>
                        </TableHeader>
                        <TableBody>
                            {requests.map(r => {
                                const busy = busyId === r.id;
                                // why it was denied, or why a cancellation was denied
                                const note = r.status === "denied" ? r.decisionReason : r.cancellationDecisionReason;
                                return (
                                    <TableRow key={r.id}>
                                        <TableCell className="font-medium">
                                            {formatDate(r.startDate)} - {formatDate(r.endDate)}
                                        </TableCell>
                                        <TableCell>{typeLabel(r.holidayType ?? "")}</TableCell>
                                        <TableCell className="text-right">{formatHours(r.hours ?? 0)}</TableCell>
                                        <TableCell>
                                            <Badge variant={statusVariant(r.status)}>
                                                {statusLabels[r.status ?? ""] ?? r.status}
                                            </Badge>
                                            {note && (
                                                <p className="mt-1 text-sm text-muted-foreground">
                                                    {r.status === "denied" ? "Begrundelse" : "Annullering afvist"}: {note}
                                                </p>
                                            )}
                                        </TableCell>
                                        <TableCell>{formatDate(r.createdAt?.slice(0, 10))}</TableCell>
                                        <TableCell className="text-right">
                                            {r.canCancel &&
                                                (confirmId === r.id ? (
                                                    <div className="flex items-center justify-end gap-2">
                                                        <Button size="sm" variant="destructive" disabled={busy} onClick={() => cancel(r)}>
                                                            {r.cancelNeedsApproval ? "Send annullering" : "Træk tilbage"}
                                                        </Button>
                                                        <Button size="sm" variant="outline" disabled={busy} onClick={() => setConfirmId(null)}>
                                                            Fortryd
                                                        </Button>
                                                    </div>
                                                ) : (
                                                    <Button size="sm" variant="outline" disabled={busy} onClick={() => startCancel(r.id!)}>
                                                        Annuller
                                                    </Button>
                                                ))}
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