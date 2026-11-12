
using LinqToDB;
using LinqToDB.Data;

namespace Infrastructure;

public class HolidayDb(DataOptions<HolidayDb> options) : DataConnection(options.Options)
{
}