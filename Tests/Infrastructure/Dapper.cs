using System.Data;
using Dapper;

namespace Tests.Infrastructure;

public static class Dapper
{
    public static void Init()
    {
        SqlMapper.AddTypeHandler(new DateOnlyTypeHandler());
    }
}

public sealed class DateOnlyTypeHandler : SqlMapper.TypeHandler<DateOnly>
{
    public override void SetValue(IDbDataParameter parameter, DateOnly value)
    {
        parameter.Value = value.ToDateTime(TimeOnly.MinValue); // midnight
        parameter.DbType = DbType.Date;
    }

    public override DateOnly Parse(object value)
    {
        return value switch
        {
            DateTime dt => DateOnly.FromDateTime(dt),
            string s => DateOnly.Parse(s),
            _ => throw new DataException($"Cannot convert {value.GetType()} to DateOnly.")
        };
    }
}