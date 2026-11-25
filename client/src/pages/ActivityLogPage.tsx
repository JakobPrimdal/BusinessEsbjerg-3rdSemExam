import { useEffect, useState } from "react";
import type { ActivityLogPageDTO, ActivityLogUserDTO } from "@/api/Api";
import { api } from "@/api/client";
import { errorMessage } from "@/api/errors";
import {
    ALL_USERS,
    actionLabel,
    buildLogQuery,
    formatDateTime,
    isSystem,
    isValidRange,
    pageRange,
} from "@/activity/activityLog";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";

export function ActivityLogPage() {
    const [users, setUsers] = useState<ActivityLogUserDTO[]>([]);
    const [userId, setUserId] = useState(ALL_USERS);
    const [fromDate, setFromDate] = useState("");
    const [toDate, setToDate] = useState("");
    const [page, setPage] = useState(1);

    const [result, setResult] = useState<ActivityLogPageDTO | null>(null);
    const [error, setError] = useState<string | null>(null);
    const [loading, setLoading] = useState(false);

    const rangeValid = isValidRange(fromDate, toDate);

    // Users for the filter dropdown, once
    useEffect(() => {
        api.activityLog
            .activityLogGetUsers()
            .then(res => setUsers(res.data))
            .catch(err => setError(errorMessage(err)));
    }, []);

    // Fetch the log whenever a filter or the page changes
    useEffect(() => {
        if (!rangeValid) return;

        let cancelled = false; // ignore old answers if the filters changed meanwhile
        setLoading(true);
        api.activityLog
            .activityLogGetActivityLog(buildLogQuery(userId, fromDate, toDate, page))
            .then(res => {
                if (cancelled) return;
                setResult(res.data);
                setError(null);
            })
            .catch(err => {
                if (!cancelled) setError(errorMessage(err));
            })
            .finally(() => {
                if (!cancelled) setLoading(false);
            });

        return () => {
            cancelled = true;
        };
    }, [userId, fromDate, toDate, page, rangeValid]);

    // A changed filter starts from page 1 again
    function changeUser(value: string) {
        setUserId(value);
        setPage(1);
    }
    function changeFrom(value: string) {
        setFromDate(value);
        setPage(1);
    }
    function changeTo(value: string) {
        setToDate(value);
        setPage(1);
    }
    function resetFilters() {
        setUserId(ALL_USERS);
        setFromDate("");
        setToDate("");
        setPage(1);
    }

    const items = result?.items ?? [];
    const total = result?.totalCount ?? 0;
    const pageSize = result?.pageSize ?? 50;
    const { first, last } = pageRange(page, pageSize, total);
    const hasFilters = userId !== ALL_USERS || fromDate !== "" || toDate !== "";

    return (
        <div className="w-full max-w-5xl space-y-6">
            <div>
                <h1 className="text-2xl font-semibold">Aktivitetslog</h1>
                <p className="text-muted-foreground">Hvem har ændret hvad og hvornår.</p>
            </div>

            <Card>
                <CardHeader>
                    <CardTitle>Filtre</CardTitle>
                    <CardDescription>Begge datoer er medregnet.</CardDescription>
                </CardHeader>
                <CardContent className="grid gap-4 sm:grid-cols-[1fr_auto_auto_auto] sm:items-end">
                    <div className="grid gap-2">
                        <Label htmlFor="log-user">Bruger</Label>
                        <Select value={userId} onValueChange={changeUser}>
                            <SelectTrigger id="log-user" className="w-full">
                                <SelectValue />
                            </SelectTrigger>
                            <SelectContent>
                                <SelectItem value={ALL_USERS}>Alle brugere</SelectItem>
                                {users.map(u => (
                                    <SelectItem key={u.userId} value={u.userId!}>
                                        {u.username}
                                    </SelectItem>
                                ))}
                            </SelectContent>
                        </Select>
                    </div>
                    <div className="grid gap-2">
                        <Label htmlFor="log-from">Fra</Label>
                        <Input id="log-from" type="date" value={fromDate} onChange={e => changeFrom(e.target.value)} />
                    </div>
                    <div className="grid gap-2">
                        <Label htmlFor="log-to">Til</Label>
                        <Input id="log-to" type="date" value={toDate} onChange={e => changeTo(e.target.value)} />
                    </div>
                    <Button variant="outline" onClick={resetFilters} disabled={!hasFilters}>
                        Nulstil
                    </Button>
                </CardContent>
            </Card>

            {!rangeValid && (
                <Alert variant="destructive">
                    <AlertDescription>Fra-datoen skal ligge før eller på til-datoen.</AlertDescription>
                </Alert>
            )}

            {error && (
                <Alert variant="destructive">
                    <AlertDescription>{error}</AlertDescription>
                </Alert>
            )}

            {!result && !error && rangeValid && <p className="text-muted-foreground">Henter log…</p>}

            {result && rangeValid && (
                <Card>
                    <CardContent className="space-y-4">
                        {items.length === 0 ? (
                            <p className="text-muted-foreground">Ingen handlinger matcher filtrene.</p>
                        ) : (
                            <Table className={loading ? "opacity-60" : undefined}>
                                <TableHeader>
                                    <TableRow>
                                        <TableHead>Tidspunkt</TableHead>
                                        <TableHead>Bruger</TableHead>
                                        <TableHead>Handling</TableHead>
                                        <TableHead>Detaljer</TableHead>
                                    </TableRow>
                                </TableHeader>
                                <TableBody>
                                    {items.map(entry => (
                                        <TableRow key={entry.id}>
                                            <TableCell className="whitespace-nowrap">{formatDateTime(entry.createdAt)}</TableCell>
                                            <TableCell className={isSystem(entry) ? "italic text-muted-foreground" : undefined}>
                                                {entry.username}
                                            </TableCell>
                                            <TableCell>
                                                <Badge variant="secondary">{actionLabel(entry.action)}</Badge>
                                            </TableCell>
                                            <TableCell className="whitespace-normal">{entry.details}</TableCell>
                                        </TableRow>
                                    ))}
                                </TableBody>
                            </Table>
                        )}

                        <div className="flex items-center justify-between gap-2 text-sm text-muted-foreground">
                            <span>{total === 0 ? "0 handlinger" : `Viser ${first}–${last} af ${total}`}</span>
                            <div className="flex gap-2">
                                <Button variant="outline" size="sm" onClick={() => setPage(p => p - 1)} disabled={page <= 1 || loading}>
                                    Forrige
                                </Button>
                                <Button variant="outline" size="sm" onClick={() => setPage(p => p + 1)} disabled={last >= total || loading}>
                                    Næste
                                </Button>
                            </div>
                        </div>
                    </CardContent>
                </Card>
            )}
        </div>
    );
}