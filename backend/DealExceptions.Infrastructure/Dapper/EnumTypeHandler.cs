using Dapper;
using System.Data;

namespace DealExceptions.Infrastructure.Dapper;

public class EnumTypeHandler<T> : SqlMapper.TypeHandler<T> where T : struct, Enum
{
    public override T Parse(object value) => Enum.Parse<T>(value.ToString()!, ignoreCase: true);
    public override void SetValue(IDbDataParameter parameter, T value) => parameter.Value = value.ToString();
}
