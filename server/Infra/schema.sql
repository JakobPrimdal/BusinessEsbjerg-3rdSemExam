create table holiday_account
(
    id                Guid          not null
        constraint PK_holiday_account
            primary key,
    user_id           NVarChar(255) not null,
    holiday_type      NVarChar(255) not null,
    earning_start     Date          not null,
    earning_end       Date          not null,
    usage_start       Date          not null,
    usage_end         Date          not null,
    transfer_deadline Date          not null,
    status            NVarChar(255) not null,
    created_at        DateTime2     not null
);

create table holiday_transaction
(
    id                      Guid          not null
        constraint PK_holiday_transaction
            primary key,
    holiday_account_id      Guid          not null,
    user_id                 NVarChar(255) not null,
    type                    NVarChar(255) not null,
    hours                   Decimal       not null,
    transaction_date        Date          not null,
    period_month            Date,
    qualifying_salary       Decimal,
    weekly_hours_used       Decimal,
    rule_valid_from         Date,
    reverses_transaction_id Guid,
    reason                  NVarChar(255),
    created_by              NVarChar(255),
    created_at              DateTime2     not null
);

create table rule_value
(
    id         Guid          not null
        constraint PK_rule_value
            primary key,
    key        NVarChar(255) not null,
    value      NVarChar(255) not null,
    valid_from Date          not null,
    created_by NVarChar(255),
    created_at DateTime2     not null
);

create table salary
(
    id             Guid          not null
        constraint PK_salary
            primary key,
    user_id        NVarChar(255) not null,
    valid_from     Date          not null,
    pay_type       NVarChar(255) not null,
    monthly_salary Decimal       not null,
    created_at     DateTime2     not null
);

create table work_hour
(
    id         Guid      not null
        constraint PK_work_hour
            primary key,
    user_id    NVarChar(255) not null,
    valid_from Date      not null,
    mon_hours  Decimal   not null,
    tue_hours  Decimal   not null,
    wed_hours  Decimal   not null,
    thu_hours  Decimal   not null,
    fri_hours  Decimal   not null,
    sat_hours  Decimal   not null,
    sun_hours  Decimal   not null,
    created_at DateTime2 not null
);