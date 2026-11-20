using Infrastructure.Entities;
using LinqToDB;
using LinqToDB.Data;

namespace Infrastructure;

public class HolidayDb(DataOptions<HolidayDb> options) : DataConnection(options.Options)
{
    public ITable<User> Users => this.GetTable<User>();
    public ITable<WorkHour> WorkHours => this.GetTable<WorkHour>();
    public ITable<Salary> Salaries => this.GetTable<Salary>();
    public ITable<HolidayAccount> HolidayAccounts => this.GetTable<HolidayAccount>();
    public ITable<HolidayTransaction> HolidayTransactions => this.GetTable<HolidayTransaction>();
    
    public ITable<HolidayRuleSet> HolidayRuleSets => this.GetTable<HolidayRuleSet>();
}