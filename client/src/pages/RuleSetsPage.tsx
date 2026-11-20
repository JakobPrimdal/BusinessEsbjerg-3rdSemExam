import { Fragment, useCallback, useEffect, useState, type FormEvent } from "react";
import { useLocation, useNavigate } from "react-router";
import { ChevronDown, ChevronRight, Plus } from "lucide-react";
import { api } from "@/api/client";
import { errorMessage, httpStatus, ruleSetErrorMessage } from "@/api/errors";
import { formatDate, todayIso } from "@/holiday/balance";
import {
    DEADLINES,
    EARNING_PERIODS,
    MAIN_HOLIDAY,
    USAGE_PERIODS,
    deadlineText,
    formatNumber,
    periodText,
    ruleSetStatus,
    type RuleSet,
    type RuleSetStatus,
} from "@/holiday/ruleset";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";

export function RuleSetsPage() {
    const navigate = useNavigate();
    const location = useLocation();
    const today = todayIso();

    // Is sent on successful create
    const created = (location.state as { created?: string } | null)?.created;

    const [ruleSets, setRuleSets] = useState<RuleSet[] | null>(null);
    const [currentId, setCurrentId] = useState<string | null>(null);
    const [error, setError] = useState<string | null>(null);

    const [expandedId, setExpandedId] = useState<string | null>(null);
    const [details, setDetails] = useState<Record<string, RuleSet>>({});
    const [detailError, setDetailError] = useState<string | null>(null);

    const [confirmId, setConfirmId] = useState<string | null>(null);
    const [togglingId, setTogglingId] = useState<string | null>(null);

    const [lookupDate, setLookupDate] = useState(today);
    const [lookupResult, setLookupResult] = useState<{ date: string; ruleSet: RuleSet | null } | null>(null);
    const [lookupError, setLookupError] = useState<string | null>(null);

    // Fetches HolidayRuleSets + the current ruleset. 404 means nothing was found, and not error.
    const load = useCallback(async () => {
        const [list, current] = await Promise.allSettled([
            api.holidayRuleSet.holidayRuleSetGetHolidayRuleSets(),
            api.holidayRuleSet.holidayRuleSetGetCurrent(),
        ]);

        if (list.status === "fulfilled") {
            setRuleSets(list.value.data);
            setError(null);
        } else if (httpStatus(list.reason) === 404) {
            setRuleSets([]);
            setError(null);
        } else {
            setError(errorMessage(list.reason));
        }

        setCurrentId(current.status === "fulfilled" ? (current.value.data.id ?? null) : null);
    }, []);

    useEffect(() => {
        load();
    }, [load]);

    async function toggleActive(rs: RuleSet) {
        if (!rs.id) return;
        setConfirmId(null);
        setTogglingId(rs.id);
        setError(null);
        try {
            await api.holidayRuleSet.holidayRuleSetToggleActive({ id: rs.id });
            await load();
        } catch (err) {
            setError(ruleSetErrorMessage(err));
        } finally {
            setTogglingId(null);
        }
    }

    // Only deactivation requires the user to confirm its action
    function onToggleClick(rs: RuleSet) {
        if (rs.isActive)
            setConfirmId(rs.id ?? null);
        else toggleActive(rs);
    }

    async function toggleExpand(id: string) {
        if (expandedId === id) {
            setExpandedId(null);
            return;
        }
        setExpandedId(id);
        setDetailError(null);
        if (details[id]) return;
        try {
            const { data } = await api.holidayRuleSet.holidayRuleSetGetRulesById({ id });
            setDetails(d => ({ ...d, [id]: data }));
        } catch (err) {
            setDetailError(ruleSetErrorMessage(err));
        }
    }

    async function onLookup(e: FormEvent<HTMLFormElement>) {
        e.preventDefault();
        setLookupError(null);
        setLookupResult(null);
        if (!lookupDate) {
            setLookupError("Vælg en dato");
            return;
        }
        try {
            const { data } = await api.holidayRuleSet.holidayRuleSetGetHolidayRuleSetByDate({ date: lookupDate });
            setLookupResult({ date: lookupDate, ruleSet: data });
        } catch (err) {
            if (httpStatus(err) === 404) setLookupResult({ date: lookupDate, ruleSet: null });
            else setLookupError(ruleSetErrorMessage(err));
        }
    }

    function showInTable(id?: string) {
        if (id && expandedId !== id) toggleExpand(id);
    }

    return (
        <div className="w-full max-w-5xl space-y-6">
            <div className="flex flex-wrap items-start justify-between gap-3">
                <div>
                    <h1 className="text-2xl font-semibold">Ferieregler</h1>
                    <p className="text-muted-foreground">
                        Hver version gælder fra en bestemt dato. Ældre versioner bevares for tidligere perioder.
                    </p>
                </div>
                <Button onClick={() => navigate("/admin/regler/ny")}>
                    <Plus />
                    Opret regelsæt
                </Button>
            </div>

            {created && (
                <Alert>
                    <AlertDescription>Regelsættet med gyldig fra {formatDate(created)} er oprettet</AlertDescription>
                </Alert>
            )}

            {error && (
                <Alert variant="destructive">
                    <AlertDescription>{error}</AlertDescription>
                </Alert>
            )}

            {/* Opslag på dato */}
            <Card>
                <CardHeader>
                    <CardTitle>Hvilke regler gælder en bestemt dato?</CardTitle>
                    <CardDescription>Slå op, hvilket aktivt regelsæt der er gældende på en given dato.</CardDescription>
                </CardHeader>
                <CardContent className="space-y-3">
                    <form onSubmit={onLookup} className="flex flex-wrap items-end gap-3">
                        <div className="grid gap-2">
                            <Label htmlFor="lookup-date">Dato</Label>
                            <Input id="lookup-date" type="date" value={lookupDate} onChange={e => setLookupDate(e.target.value)} />
                        </div>
                        <Button type="submit" variant="outline">
                            Slå op
                        </Button>
                    </form>

                    {lookupError && (
                        <Alert variant="destructive">
                            <AlertDescription>{lookupError}</AlertDescription>
                        </Alert>
                    )}

                    {lookupResult && (
                        <p className="text-sm">
                            {lookupResult.ruleSet ? (
                                <>
                                    Den {formatDate(lookupResult.date)} gælder regelsættet med gyldig fra{" "}
                                    <strong>{formatDate(lookupResult.ruleSet.validFrom)}</strong>.{" "}
                                    <button
                                        type="button"
                                        className="text-primary underline-offset-4 hover:underline"
                                        onClick={() => showInTable(lookupResult.ruleSet?.id)}
                                    >
                                        Vis i tabellen
                                    </button>
                                </>
                            ) : (
                                <>Der findes intet aktivt regelsæt, der gælder den {formatDate(lookupResult.date)}.</>
                            )}
                        </p>
                    )}
                </CardContent>
            </Card>

            {/* Tabel */}
            {!ruleSets && !error && <p className="text-muted-foreground">Henter regelsæt…</p>}

            {ruleSets && ruleSets.length === 0 && (
                <Card>
                    <CardContent className="text-muted-foreground">
                        Der findes ingen regelsæt endnu. Opret det første med knappen ovenfor.
                    </CardContent>
                </Card>
            )}

            {ruleSets && ruleSets.length > 0 && (
                <Card>
                    <CardContent>
                        <Table>
                            <TableHeader>
                                <TableRow>
                                    <TableHead>Gyldig fra</TableHead>
                                    <TableHead>Status</TableHead>
                                    <TableHead className="text-right">Sats uge 1–5</TableHead>
                                    <TableHead className="text-right">Sats uge 6</TableHead>
                                    <TableHead>Oprettet</TableHead>
                                    <TableHead className="text-right">Aktiv</TableHead>
                                </TableRow>
                            </TableHeader>
                            <TableBody>
                                {ruleSets.map(rs => {
                                    const id = rs.id ?? "";
                                    const open = expandedId === id;
                                    const status = ruleSetStatus(rs, currentId, today);

                                    return (
                                        <Fragment key={id}>
                                            <TableRow>
                                                <TableCell>
                                                    <button
                                                        type="button"
                                                        className="flex items-center gap-1 font-medium"
                                                        aria-expanded={open}
                                                        onClick={() => toggleExpand(id)}
                                                    >
                                                        {open ? <ChevronDown className="size-4" /> : <ChevronRight className="size-4" />}
                                                        {formatDate(rs.validFrom)}
                                                    </button>
                                                </TableCell>
                                                <TableCell>
                                                    <StatusBadge status={status} />
                                                </TableCell>
                                                <TableCell className="text-right">{formatNumber(rs.weeks1To5EarningRate)} dage/md.</TableCell>
                                                <TableCell className="text-right">{formatNumber(rs.week6EarningRate)} timer/md.</TableCell>
                                                <TableCell className="text-muted-foreground">
                                                    {formatDate(rs.createdAt?.slice(0, 10))}
                                                    {rs.createdBy ? ` · ${rs.createdBy}` : ""}
                                                </TableCell>
                                                <TableCell className="text-right">
                                                    {confirmId === id ? (
                                                        <div className="flex flex-col items-end gap-2">
                              <span className="max-w-56 text-xs text-muted-foreground">
                                {status === "current"
                                    ? "Dette er det gældende regelsæt. Beregninger bruger i stedet det forrige aktive (hvis der er et)."
                                    : "Deaktivere dette regelsæt?"}
                              </span>
                                                            <div className="flex gap-2">
                                                                <Button size="sm" variant="destructive" onClick={() => toggleActive(rs)}>
                                                                    Ja, deaktivér
                                                                </Button>
                                                                <Button size="sm" variant="outline" onClick={() => setConfirmId(null)}>
                                                                    Annullér
                                                                </Button>
                                                            </div>
                                                        </div>
                                                    ) : (
                                                        <Button
                                                            size="sm"
                                                            variant="outline"
                                                            disabled={togglingId === id}
                                                            onClick={() => onToggleClick(rs)}
                                                        >
                                                            {rs.isActive ? "Deaktivér" : "Aktivér"}
                                                        </Button>
                                                    )}
                                                </TableCell>
                                            </TableRow>

                                            {open && (
                                                <TableRow>
                                                    <TableCell colSpan={6} className="bg-muted/30 p-4 whitespace-normal">
                                                        {detailError && (
                                                            <Alert variant="destructive">
                                                                <AlertDescription>{detailError}</AlertDescription>
                                                            </Alert>
                                                        )}
                                                        {!detailError && !details[id] && (
                                                            <p className="text-muted-foreground">Henter detaljer…</p>
                                                        )}
                                                        {details[id] && <RuleSetDetails rs={details[id]} />}
                                                    </TableCell>
                                                </TableRow>
                                            )}
                                        </Fragment>
                                    );
                                })}
                            </TableBody>
                        </Table>
                    </CardContent>
                </Card>
            )}
        </div>
    );
}

function StatusBadge({ status }: { status: RuleSetStatus }) {
    switch (status) {
        case "current":
            return <Badge>Gældende nu</Badge>;
        case "future":
            return <Badge variant="secondary">Fremtidig</Badge>;
        case "historical":
            return <Badge variant="outline">Historisk</Badge>;
        case "inactive":
            return (
                <Badge variant="outline" className="border-dashed text-muted-foreground">
                    Inaktiv
                </Badge>
            );
    }
}

function RuleSetDetails({ rs }: { rs: RuleSet }) {
    return (
        <div className="grid gap-6 text-sm sm:grid-cols-2">
            <DetailGroup title="Optjeningssatser">
                <DetailRow label="Uge 1–5" value={`${formatNumber(rs.weeks1To5EarningRate)} dage pr. måned`} />
                <DetailRow label="Uge 6" value={`${formatNumber(rs.week6EarningRate)} timer pr. måned`} />
            </DetailGroup>

            <DetailGroup title="Optjeningsperioder">
                {EARNING_PERIODS.map(p => (
                    <DetailRow key={p.title} label={p.name} value={periodText(rs, p)} />
                ))}
            </DetailGroup>

            <DetailGroup title="Brugsperioder">
                {USAGE_PERIODS.map(p => (
                    <DetailRow key={p.title} label={p.name} value={periodText(rs, p)} />
                ))}
            </DetailGroup>

            <DetailGroup title="Hovedferie">
                <DetailRow label="Periode" value={periodText(rs, MAIN_HOLIDAY)} />
            </DetailGroup>

            <DetailGroup title="Overførsel">
                <DetailRow label="Slået til" value={rs.holidayTransferEnabled ? "Ja" : "Nej"} />
                <DetailRow label="Maks. dage" value={String(rs.maximumTransferDays ?? 0)} />
                {DEADLINES.map(d => (
                    <DetailRow key={d.title} label={`Frist ${d.name.toLowerCase()}`} value={deadlineText(rs, d)} />
                ))}
            </DetailGroup>

            <DetailGroup title="Øvrige regler">
                <DetailRow label="Sygedage uden erstatningsferie" value={`${rs.sickWaitingDays ?? 0} dage`} />
                <DetailRow label="Varsel, hovedferie" value={`${rs.mainHolidayNoticePeriodMonths ?? 0} mdr.`} />
                <DetailRow label="Varsel, anden ferie" value={`${rs.otherHolidayNoticePeriodMonths ?? 0} mdr.`} />
            </DetailGroup>
        </div>
    );
}

function DetailGroup({ title, children }: { title: string; children: React.ReactNode }) {
    return (
        <div>
            <h3 className="mb-2 font-semibold">{title}</h3>
            <dl className="grid grid-cols-[auto_1fr] gap-x-6 gap-y-1">{children}</dl>
        </div>
    );
}

function DetailRow({ label, value }: { label: string; value: string }) {
    return (
        <>
            <dt className="text-muted-foreground">{label}</dt>
            <dd>{value}</dd>
        </>
    );
}