#:package Microsoft.Data.SqlClient@7.1.1
#:package Microsoft.Data.SqlClient.Extensions.Azure@7.1.1
// SqlClient가 간접 참조하는 9.0.18의 알려진 취약점(CVE-2026-70162, CVE-2026-84008)을 피하기 위해 패치 버전을 직접 참조
#:package System.Security.Cryptography.Pkcs@10.0.14
#:property TargetFramework=net10.0

// Dataverse TDS 엔드포인트에 SQL로 조회하는 샘플
// 실행: dotnet run tds-sql.cs -- [환경 호스트 이름] [사용자 UPN]

using Microsoft.Data.SqlClient;

// TODO: org00000000.crm5.dynamics.com은 예시 값이므로 실제 환경의 호스트 이름으로 바꾸거나 첫 번째 인수로 지정
// 호스트 이름: Power Platform 관리 센터의 환경 URL 또는 pac org list의 Environment URL에서 https://와 끝의 /를 제외한 부분
var host = args.ElementAtOrDefault(0) ?? "org00000000.crm5.dynamics.com";
var user = args.ElementAtOrDefault(1);

if (host.StartsWith("org00000000.", StringComparison.OrdinalIgnoreCase))
{
    Console.Error.WriteLine("호스트 이름이 예시 값(org00000000)입니다. 실제 환경의 호스트 이름을 지정하십시오.");
    Console.Error.WriteLine("예: dotnet run tds-sql.cs -- <환경>.crm5.dynamics.com user@contoso.com");
    return 1;
}

var builder = new SqlConnectionStringBuilder
{
    // https://와 포트 없이 호스트 이름만 지정
    DataSource = host,
    // SqlClient 7.x는 Microsoft.Data.SqlClient.Extensions.Azure 패키지가 있어야 Entra 인증을 사용할 수 있음
    Authentication = SqlAuthenticationMethod.ActiveDirectoryInteractive,
    Encrypt = SqlConnectionEncryptOption.Mandatory,
    ConnectTimeout = 60,
};
if (user is not null)
    builder.UserID = user;

await using var connection = new SqlConnection(builder.ConnectionString);
await connection.OpenAsync();

// Dataverse에서는 connection.Database가 비어 있으므로 db_name()으로 조회
await using (var dbCommand = new SqlCommand("select db_name()", connection))
    Console.WriteLine($"데이터베이스: {await dbCommand.ExecuteScalarAsync()}");

const string sql = """
    select top 5 name, createdon
    from account
    order by createdon desc
    """;

await using var command = new SqlCommand(sql, connection);
await using var reader = await command.ExecuteReaderAsync();
while (await reader.ReadAsync())
{
    var name = reader.IsDBNull(0) ? "(이름 없음)" : reader.GetString(0);
    Console.WriteLine($"{reader.GetDateTime(1):yyyy-MM-dd HH:mm}  {name}");
}
return 0;
