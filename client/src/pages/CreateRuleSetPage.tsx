import { useEffect, useState, type FormEvent } from "react";
import { useNavigate } from "react-router";
import { api } from "@/api/client";
import { ruleSetErrorMessage } from "@/api/errors";
import { formatDate } from "@/holiday/balance";
import {
    DEADLINES,
    EARNING_PERIODS,
    MAIN_HOLIDAY,
    USAGE_PERIODS,
    emptyForm,
    toRequestBody,
    validateForm,
    valuesFromRuleSet,
    type DateKeys,
    type NumKey,
    type PeriodDef,
    type RuleSet,
    type RuleSetForm,
} from "@/holiday/ruleset";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";

type CreateArgs = NonNullable<Parameters<typeof api.holidayRuleSet.holidayRuleSetCreateHolidayRuleSet>[0]>;

const SICK_DAYS_HINT =
    "Hvis en medarbejder bliver syg under ferie i uge 1–5, kan vedkommende få erstatningsferie for sygedagene, " +
    "men først efter de første X sygedage. (0 = erstatning fra første sygedag)"

export function CreateRuleSetPage() {
    const navigate = useNavigate();
    const [form, setForm] = useState<RuleSetForm>(emptyForm);
    const [latest, setLatest] = useState<RuleSet | null>(null);
    const [latestActiveFrom, setLatestActiveFrom] = useState<string | null>(null);
    const [copiedFrom, setCopiedFrom] = useState<string | null>(null);
    const [error, setError] = useState<string | null>(null);
    const [busy, setBusy] = useState(false);

    // Fetch existing rulesets for 'copy from latest'
    useEffect(() => {
        api.holidayRuleSet
            .holidayRuleSetGetHolidayRuleSets()
            .then(res => {
                const list = res.data;
                const byDate = (a: RuleSet, b: RuleSet) => (a.validFrom ?? "").localeCompare(b.validFrom ?? "");
                const sorted = [...list].sort(byDate);
                setLatest(sorted.at(-1) ?? null);
                setLatestActiveFrom(sorted.filter(r => r.isActive).at(-1)?.validFrom ?? null);
            })
            .catch(() => {
                // No rulesets or error
            });
    }, []);

    function setValue(key: NumKey, value: string) {
        setForm(f => ({ ...f, values: { ...f.values, [key]: value } }));
    }

    function copyLatest() {
        if (!latest) return;
        setForm(f => ({
            ...f,
            holidayTransferEnabled: latest.holidayTransferEnabled ?? false,
            values: valuesFromRuleSet(latest),
        }));
        setCopiedFrom(latest.validFrom ?? null);
    }

    async function onSubmit(e: FormEvent<HTMLFormElement>) {
        e.preventDefault();
        setError(null);

        const invalid = validateForm(form);
        if (invalid) {
            setError(invalid);
            return;
        }

        setBusy(true);
        try {
            await api.holidayRuleSet.holidayRuleSetCreateHolidayRuleSet(toRequestBody(form) as unknown as CreateArgs);
            navigate("/admin/regler", { state: { created: form.validFrom } });
        } catch (err) {
            setError(ruleSetErrorMessage(err));
        } finally {
            setBusy(false);
        }
    }

    return (
        <form onSubmit={onSubmit} className="w-full max-w-3xl space-y-6">
            <div className="flex flex-wrap items-start justify-between gap-3">
                <div>
                    <h1 className="text-2xl font-semibold">Opret regelsæt</h1>
                    <p className="text-muted-foreground">
                        Det nye regelsæt gælder fra en bestemt dato. Ældre regelsæt bevares for tidligere perioder.
                    </p>
                </div>
                <Button type="button" variant="outline" onClick={copyLatest} disabled={!latest}>
                    Kopiér fra seneste version
                </Button>
            </div>

            {copiedFrom && (
                <Alert>
                    <AlertDescription>
                        Værdierne er kopieret fra regelsættet, der gælder fra {formatDate(copiedFrom)}. Husk at vælge en ny
                        «gyldig fra»-dato.
                    </AlertDescription>
                </Alert>
            )}

            <Card>
                <CardHeader>
                    <CardTitle>Generelt</CardTitle>
                </CardHeader>
                <CardContent className="grid gap-4">
                    <div className="grid gap-2">
                        <Label htmlFor="valid-from">Gyldig fra</Label>
                        <Input
                            id="valid-from"
                            type="date"
                            className="max-w-xs"
                            value={form.validFrom}
                            onChange={e => setForm(f => ({ ...f, validFrom: e.target.value }))}
                            required
                        />
                        {latestActiveFrom && (
                            <p className="text-sm text-muted-foreground">
                                Seneste aktive regelsæt gælder fra {formatDate(latestActiveFrom)}. Datoen bør ikke ligge før dette.
                            </p>
                        )}
                    </div>
                    <CheckboxField
                        id="is-active"
                        label="Aktivt (regelsættet bruges af beregningerne)"
                        checked={form.isActive}
                        onChange={checked => setForm(f => ({ ...f, isActive: checked }))}
                    />
                </CardContent>
            </Card>

            <Card>
                <CardHeader>
                    <CardTitle>Optjeningssatser</CardTitle>
                    <CardDescription>Hvor meget ferie der optjenes pr. måned.</CardDescription>
                </CardHeader>
                <CardContent className="grid gap-4 sm:grid-cols-2">
                    <NumInput
                        id="weeks1To5EarningRate"
                        label="Uge 1–5 (dage pr. måned)"
                        step="0.001"
                        value={form.values.weeks1To5EarningRate}
                        onChange={v => setValue("weeks1To5EarningRate", v)}
                    />
                    <NumInput
                        id="week6EarningRate"
                        label="Uge 6 (timer pr. måned)"
                        step="0.001"
                        value={form.values.week6EarningRate}
                        onChange={v => setValue("week6EarningRate", v)}
                    />
                </CardContent>
            </Card>

            <Card>
                <CardHeader>
                    <CardTitle>Optjeningsperioder</CardTitle>
                    <CardDescription>
                        År-offset er antal år efter optjeningsperiodens startår: 0 = samme år, 1 = året efter, 2 = to år efter.
                    </CardDescription>
                </CardHeader>
                <CardContent className="grid gap-4">
                    {EARNING_PERIODS.map(p => (
                        <PeriodFields key={p.title} def={p} values={form.values} onChange={setValue} />
                    ))}
                </CardContent>
            </Card>

            <Card>
                <CardHeader>
                    <CardTitle>Brugsperioder</CardTitle>
                    <CardDescription>Hvornår den optjente ferie kan holdes. År-offset regnes som ovenfor.</CardDescription>
                </CardHeader>
                <CardContent className="grid gap-4">
                    {USAGE_PERIODS.map(p => (
                        <PeriodFields key={p.title} def={p} values={form.values} onChange={setValue} />
                    ))}
                </CardContent>
            </Card>

            <Card>
                <CardHeader>
                    <CardTitle>Hovedferie</CardTitle>
                    <CardDescription>Perioden, hvor hovedferien kan holdes (hvert år).</CardDescription>
                </CardHeader>
                <CardContent>
                    <PeriodFields def={MAIN_HOLIDAY} values={form.values} onChange={setValue} />
                </CardContent>
            </Card>

            <Card>
                <CardHeader>
                    <CardTitle>Overførsel af ferie</CardTitle>
                </CardHeader>
                <CardContent className="grid gap-4">
                    <CheckboxField
                        id="transfer-enabled"
                        label="Overførsel af ferie er slået til"
                        checked={form.holidayTransferEnabled}
                        onChange={checked => setForm(f => ({ ...f, holidayTransferEnabled: checked }))}
                    />
                    <NumInput
                        id="maximumTransferDays"
                        label="Maks. overførselsdage (0–5)"
                        value={form.values.maximumTransferDays}
                        onChange={v => setValue("maximumTransferDays", v)}
                    />
                    {DEADLINES.map(d => (
                        <div key={d.title} className="space-y-3 rounded-lg border p-4">
                            <h3 className="text-sm font-semibold">{d.title}</h3>
                            <DateFields label="Frist" keys={d.date} values={form.values} onChange={setValue} />
                        </div>
                    ))}
                </CardContent>
            </Card>

            <Card>
                <CardHeader>
                    <CardTitle>Øvrige regler</CardTitle>
                </CardHeader>
                <CardContent className="grid gap-6">
                    <div className="max-w-md">
                        <NumInput
                            id="sickWaitingDays"
                            label="Sygedage uden erstatningsferie"
                            hint={SICK_DAYS_HINT}
                            value={form.values.sickWaitingDays}
                            onChange={v => setValue("sickWaitingDays", v)}
                        />
                    </div>
                    <div className="grid gap-4 sm:grid-cols-2">
                        <NumInput
                            id="mainHolidayNoticePeriodMonths"
                            label="Varsel, hovedferie (mdr.)"
                            value={form.values.mainHolidayNoticePeriodMonths}
                            onChange={v => setValue("mainHolidayNoticePeriodMonths", v)}
                        />
                        <NumInput
                            id="otherHolidayNoticePeriodMonths"
                            label="Varsel, anden ferie (mdr.)"
                            value={form.values.otherHolidayNoticePeriodMonths}
                            onChange={v => setValue("otherHolidayNoticePeriodMonths", v)}
                        />
                    </div>
                </CardContent>
            </Card>

            {error && (
                <Alert variant="destructive">
                    <AlertDescription>{error}</AlertDescription>
                </Alert>
            )}

            <div className="flex gap-3">
                <Button type="submit" disabled={busy}>
                    {busy ? "Opretter…" : "Opret regelsæt"}
                </Button>
                <Button type="button" variant="outline" onClick={() => navigate("/admin/regler")}>
                    Annullér
                </Button>
            </div>
        </form>
    );
}

// Small components reused across page

function NumInput({
                      id,
                      label,
                      value,
                      onChange,
                      step = "1",
                      hint,
                  }: {
    id: string;
    label: string;
    value: string;
    onChange: (value: string) => void;
    step?: string;
    hint?: string;
}) {
    return (
        <div className="grid gap-1">
            <Label htmlFor={id} className="text-xs text-muted-foreground sm:text-sm">
                {label}
            </Label>
            <Input
                id={id}
                type="number"
                step={step}
                value={value}
                aria-describedby={hint ? `${id}-hint` : undefined}
                onChange={e => onChange(e.target.value)}
            />
            {hint && (
                <p id={`${id}-hint`} className="text-sm text-muted-foreground">
                    {hint}
                </p>
            )}
        </div>
    );
}

function CheckboxField({
   id,
   label,
   checked,
   onChange,
}: {
    id: string;
    label: string;
    checked: boolean;
    onChange: (checked: boolean) => void;
}) {
    return (
        <div className="flex items-center gap-2">
            <input
                id={id}
                type="checkbox"
                className="size-4 accent-primary"
                checked={checked}
                onChange={e => onChange(e.target.checked)}
            />
            <Label htmlFor={id}>{label}</Label>
        </div>
    );
}

type FieldProps = {
    values: Record<NumKey, string>;
    onChange: (key: NumKey, value: string) => void;
};

function DateFields({ label, keys, values, onChange }: FieldProps & { label: string; keys: DateKeys }) {
    return (
        <div className="grid gap-2">
            <p className="text-sm font-medium">{label}</p>
            <div className="grid grid-cols-3 gap-3">
                <NumInput id={keys.day} label="Dag" value={values[keys.day]} onChange={v => onChange(keys.day, v)} />
                <NumInput
                    id={keys.month}
                    label="Måned (1–12)"
                    value={values[keys.month]}
                    onChange={v => onChange(keys.month, v)}
                />
                {keys.offset ? (
                    <NumInput
                        id={keys.offset}
                        label="År-offset"
                        value={values[keys.offset]}
                        onChange={v => onChange(keys.offset as NumKey, v)}
                    />
                ) : (
                    <div />
                )}
            </div>
        </div>
    );
}

function PeriodFields({ def, values, onChange }: FieldProps & { def: PeriodDef }) {
    return (
        <div className="space-y-3 rounded-lg border p-4">
            <h3 className="text-sm font-semibold">{def.name}</h3>
            <DateFields label="Start" keys={def.start} values={values} onChange={onChange} />
            <DateFields label="Slut" keys={def.end} values={values} onChange={onChange} />
        </div>
    );
}