import { useEffect, useState } from "react";
import type { HolidayAccountDTO, HolidayBalanceDTO } from "@/api/Api";
import { api } from "@/api/client";
import { errorMessage } from "@/api/errors";
import {
  WEEK_6,
  WEEKS_1_5,
  formatDate,
  formatDays,
  formatHours,
  formatMonth,
  hoursPerDay,
  periodLabel,
  sortAccounts,
  summarize,
  todayIso,
  typeLabel,
  usageState,
  type TypeSummary,
} from "@/holiday/balance";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { Badge } from "@/components/ui/badge";
import { Card, CardAction, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";

export function MyBalancePage() {
  const [balance, setBalance] = useState<HolidayBalanceDTO | null>(null);
  const [error, setError] = useState<string | null>(null);
  const today = todayIso();

  useEffect(() => {
    let cancelled = false; // ignore the answer if the page was left before it arrived
    api.holiday
      .holidayGetMyBalance()
      .then(res => {
        if (!cancelled) setBalance(res.data);
      })
      .catch(err => {
        if (!cancelled) setError(errorMessage(err));
      });
    return () => {
      cancelled = true;
    };
  }, []);

  return (
    <div className="w-full max-w-3xl space-y-6">
      <div>
        <h1 className="text-2xl font-semibold">Min ferie</h1>
        <p className="text-muted-foreground">Saldo pr. {formatDate(today)}</p>
      </div>

      {error && (
        <Alert variant="destructive">
          <AlertDescription>{error}</AlertDescription>
        </Alert>
      )}

      {!balance && !error && <p className="text-muted-foreground">Henter saldo…</p>}

      {balance && <BalanceView accounts={balance.accounts ?? []} today={today} />}
    </div>
  );
}

function BalanceView({ accounts, today }: { accounts: HolidayAccountDTO[]; today: string }) {
  if (accounts.length === 0) {
    return (
      <Card>
        <CardContent className="text-muted-foreground">
          Du har ikke optjent ferie endnu. Kontakt den ferieansvarlige, hvis det ser forkert ud.
        </CardContent>
      </Card>
    );
  }

  const perDay = hoursPerDay(accounts);

  return (
    <>
      <div className="grid gap-4 sm:grid-cols-2">
        <SummaryCard title={typeLabel(WEEKS_1_5)} summary={summarize(accounts, WEEKS_1_5, today)} perDay={perDay} />
        <SummaryCard title={typeLabel(WEEK_6)} summary={summarize(accounts, WEEK_6, today)} perDay={perDay} />
      </div>

      <section className="space-y-4">
        <h2 className="text-lg font-semibold">Feriekonti</h2>
        {sortAccounts(accounts).map(account => (
          <AccountCard key={account.id} account={account} today={today} />
        ))}
      </section>
    </>
  );
}

function SummaryCard({ title, summary, perDay }: { title: string; summary: TypeSummary; perDay: number }) {
  return (
    <Card>
      <CardHeader>
        <CardDescription>{title} – kan bruges nu</CardDescription>
        <CardTitle className="text-3xl">{formatHours(summary.availableHours)}</CardTitle>
      </CardHeader>
      <CardContent className="space-y-1 text-sm text-muted-foreground">
        <p>≈ {formatDays(summary.availableHours / perDay)}</p>
        {summary.laterHours > 0 && (
          <p>
            + {formatHours(summary.laterHours)} kan bruges fra {formatDate(summary.laterFrom)}
          </p>
        )}
      </CardContent>
    </Card>
  );
}

function UsageBadge({ account, today }: { account: HolidayAccountDTO; today: string }) {
  switch (usageState(account, today)) {
    case "now":
      return <Badge>Kan bruges nu</Badge>;
    case "later":
      return <Badge variant="secondary">Fra {formatDate(account.usageStart)}</Badge>;
    case "expired":
      return <Badge variant="outline">Periode udløbet</Badge>;
    case "closed":
      return <Badge variant="outline">Lukket</Badge>;
  }
}

function AccountCard({ account, today }: { account: HolidayAccountDTO; today: string }) {
  const hours = account.balanceHours ?? 0;
  const perDay = hoursPerDay([account]);
  const months = account.months ?? [];

  return (
    <Card>
      <CardHeader>
        <CardTitle>
          {typeLabel(account.holidayType)} · {periodLabel(account)}
        </CardTitle>
        <CardDescription>
          Optjenes {formatDate(account.earningStart)} – {formatDate(account.earningEnd)}
        </CardDescription>
        <CardAction>
          <UsageBadge account={account} today={today} />
        </CardAction>
      </CardHeader>
      <CardContent className="space-y-4 text-sm">
        <dl className="grid grid-cols-[auto_1fr] gap-x-6 gap-y-1">
          <dt className="text-muted-foreground">Saldo</dt>
          <dd>
            {formatHours(hours)} (≈ {formatDays(hours / perDay)})
          </dd>
          <dt className="text-muted-foreground">Kan holdes</dt>
          <dd>
            {formatDate(account.usageStart)} – {formatDate(account.usageEnd)}
          </dd>
        </dl>

        {months.length > 0 && (
          <details>
            <summary className="cursor-pointer text-muted-foreground">Optjening pr. måned</summary>
            <Table className="mt-2">
              <TableHeader>
                <TableRow>
                  <TableHead>Måned</TableHead>
                  <TableHead className="text-right">Timer</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {months.map(m => (
                  <TableRow key={m.month}>
                    <TableCell>{formatMonth(m.month)}</TableCell>
                    <TableCell className="text-right">{formatHours(m.hours ?? 0)}</TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </details>
        )}
      </CardContent>
    </Card>
  );
}
